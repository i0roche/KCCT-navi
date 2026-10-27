# 校内ナビゲーションシステム 技術説明書

> スライド作成用の技術資料

---

## システム概要

| 項目 | 内容 |
|------|------|
| **アプリケーション名** | 高専マップナビゲーション |
| **プラットフォーム** | Windows Forms (.NET 8 / C# 12) |
| **描画エンジン** | SkiaSharp + Svg.Skia |
| **経路探索** | A*アルゴリズム |
| **データ形式** | SVG（地図） / JSON（施設情報） |

---

## 主要機能

| 機能 | 説明 |
|------|------|
| **SVGマップ表示** | SkiaSharpを使用した高速な2D描画 |
| **マルチフロア対応** | 複数階層の地図切り替え（1F〜6F） |
| **経路探索** | A*アルゴリズムによる最短経路計算 |
| **インタラクティブ操作** | ドラッグ移動・ホイールズーム・部屋クリック |
| **お気に入り機能** | よく使う部屋の登録・管理 |

---

## システムアーキテクチャ

UI に依存しない処理は `team1.Core` (net8.0) に分け、画面 (`team1`, Windows Forms) からは
DI (`Microsoft.Extensions.DependencyInjection`) で組み立てて使う。Core は Linux でもテストできる。

```
┌─────────────────────────────── team1 (Windows Forms) ───────────────────────────────┐
│ Form1 (partial)        表示とイベント配線のみ。処理は MainViewModel に任せる           │
│ AppToolbar / RouteBar  上のツールバー (6班の配置) / 経路の入力欄 (KCCT の配置)        │
│ MapViewer (partial)    地図描画・パン/ズーム・ピンチ (WM_GESTURE)・経路アニメーション   │
│ MapResourceRepository  階ごとの SVG 描画結果をキャッシュ                               │
│ IdleWatcher / CrashGuard  無操作リセット / 例外時の復帰 (無人運用)                      │
└───────────────────────────────────────┬─────────────────────────────────────────────┘
                                        │ メソッド呼び出し ↓   ↑ メッセージ (IMessenger)
┌─────────────────────────────── team1.Core (net8.0) ─────────────────────────────────┐
│ ViewModels   MainViewModel          画面操作 (出発地/目的地の設定・経路表示・リセット) │
│ Stores       RouteStateStore        出発地・目的地 (Place そのもの)・表示中の経路       │
│              MapStateStore          表示中の階                                        │
│ Usecases     PlaceSearchUsecase     キーワード検索・建物/階ごとの一覧                   │
│              RouteUsecase           経路探索・最寄りの○○ (実経路で最短のもの)          │
│              FavoriteUsecase        お気に入り                                        │
│ Repositories JsonPlaceRepository    Data/*.json                                       │
│              SvgMapGraphRepository  map.svg → グラフ (SvgGraphLoader)                 │
│              JsonFavoriteRepository AppData/KosenCampusNavi/favorite.json             │
│ Logic        PathFinder (A* / Dijkstra), MapGeometry, RouteDescriber                   │
│ Models       Place, MapNode, Edge, RouteResult, PlaceCategory                          │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

- 出発地・目的地は名前ではなく `Place` をストアに持つので、同名の場所 (会議室 ×6 など) を取り違えない
- ストアが変わると `RouteStateChangedMessage` などが送られ、Form1 (Form1.Layout.cs) が入力欄・行き方・ステータスバーを更新する

---

## 主要コンポーネント

### 1. MapViewer（地図表示コントロール）

#### プロパティ

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `SvgMap` | `SKSvg?` | 表示中のSVG地図 |
| `CurrentRoute` | `List<Edge>?` | 現在の経路 (設定すると出発地から伸びるアニメーションで描く) |
| `TouchMode` | `bool` | タッチ向け表示 (既定はオフ。大きい階数ボタン・指サイズの当たり判定・指を離したときに選択) |
| `CurrentLocationNode` | `MapNode?` | 展示場所 (「現在地」マーカー) |
| `CurrentFloor` | `int` | 表示中の階（1〜6） |
| `MaxFloor` | `int` | 最大階数（デフォルト6） |
| `Places` | `List<Place>` | 施設情報リスト |
| `AllNodes` | `List<MapNode>` | 全ノード情報 |
| `FavoritePlaceIds` | `HashSet<string>` | お気に入り施設ID |
| `ShowRoomNames` | `bool` | 部屋名表示フラグ |

#### 部屋ノードの色分け

| 色 | PlaceID | 説明 |
|---|---------|------|
| 青 (`#3B82F6`) | `room_*` | 一般の部屋 |
| 緑 (`#22C55E`) | `wc_*` | トイレ |
| 紫 (`#8B5CF6`) | `stair_*` | 階段 |
| 黄 (`#F59E0B`) | （お気に入り） | お気に入り登録済み |

#### ズームレベルによるラベル表示

| スケール | 表示内容 |
|---------|----------|
| 1.2倍以上 | すべての部屋名を表示 |
| 0.6〜1.2倍 | お気に入りの部屋のみ表示 |
| 0.6倍未満 | ラベル非表示（ノードのみ） |

#### イベント

| イベント | 説明 |
|---------|------|
| `RoomNodeClicked` | 部屋ノードクリック時 |
| `MapBackgroundClicked` | 背景クリック時 |
| `FloorChangeRequested` | 階数変更リクエスト時 |

---

### 2. PathFinder（経路探索エンジン）

```
【A*アルゴリズム】

評価関数: F(n) = G(n) + H(n)

  G(n): 開始地点からノードnまでの実コスト
  H(n): ノードnからゴールまでの推定距離（ヒューリスティック）
  F(n): 総合評価値（小さいほど優先）

探索手順:
  1. OpenSet に開始ノードを追加
  2. F値が最小のノードを選択
  3. ゴールなら経路復元して終了
  4. 隣接ノードのG値を更新
  5. 2に戻る（OpenSetが空なら経路なし）

H(n) は直線距離 + 階の差 × 階段 1 階分のコスト (30)。
実コストを上回らない (許容的) ので、最短経路が保証される。

探索中の G 値や親はノードに持たせず探索ごとに確保する (同じグラフで何度呼んでも干渉しない)。

【最寄りの○○ (FindNearest)】
  H(n) = 0 (= Dijkstra) で探索し、候補のどれかに最初に到達したらそこが実経路で最も近い。
  直線距離で選ぶと「真上の階にあるが階段が遠いトイレ」を選んでしまうため。
```

---

## データモデル

### MapNode（ノード）
経路グラフの頂点を表現

```csharp
MapNode {
    Id: string       // "room_101", "stair_1f_general_north"
    X, Y: float      // SVG座標
    Floor: int       // 階数
    Edges: List<Edge>  // 接続エッジ
    Neighbors: IEnumerable<MapNode>  // 隣接ノード（Edgesから導出）
}
```

### Edge（エッジ）
ノード間の接続を表現

```csharp
Edge {
    From: MapNode    // 始点
    To: MapNode      // 終点
    Cost: float      // 移動コスト（距離）
    Points: List<SKPoint>  // 曲線の中間点
}
```

### Place（施設情報）
ユーザー向け表示データ

```csharp
Place {
    Id: string       // ノードIDと対応
    Name: string     // "電子基礎実験室", "一般科 男子トイレ(2F)"
    Type: string     // "lab", "classroom", "facility", "stairs", "elevator", "restroom"
    Building: string // "一般科A棟", "電子工学科棟"
    Floor: int       // 階数
    Department: string  // "denshi", "common"
    Description: string // 説明文
    Tags: List<string>  // 検索用タグ
}
```

### RouteResult（経路探索の結果）

```csharp
RouteResult {
    Status: RouteStatus  // Found / SamePlace / PlaceNotOnMap / Unreachable
    Start, Goal: Place
    Edges: IReadOnlyList<Edge>
    Cost: float
    Message: string      // 見つからなかった理由 (利用者向け)
}
```

---

## 描画パイプライン

```
OnPaintSurface() 実行
│
├─① canvas.Clear(White)         // 背景クリア
│
├─② canvas.Save()               // 状態保存
│   canvas.SetMatrix(_currentMatrix)  // 変換行列適用（パン・ズーム）
│
├─③ DrawPicture(SvgMap)          // SVG地図描画
│
├─④ DrawRoomNodes()              // 部屋ノード描画（常に表示）
│   └─ 色分け: 青（部屋）/緑（トイレ）/紫（階段）/黄（お気に入り）
│
├─⑤ DrawRoomNames()              // 部屋名ラベル描画（ズームレベル依存）
│   ├─ scale >= 1.2: すべての部屋名
│   ├─ 0.6 <= scale < 1.2: お気に入りのみ
│   └─ scale < 0.6: 非表示
│
├─⑥ DrawRoute()                  // 経路描画
│   ├─ DrawPath(floorPath)       // 経路線（青）
│   ├─ DrawMarker(START)         // 開始ピン（青）
│   ├─ DrawMarker(GOAL)          // 終点ピン（赤）
│   └─ DrawStairMarker()         // 階段マーカー（緑/オレンジ/青）
│
├─⑦ canvas.Restore()             // 状態復元
│
└─⑧ DrawFloorButtons()           // 階数切替ボタン（スクリーン座標で固定位置）
```

---

## ユーザーインタラクション

| 操作 | 処理 |
|------|------|
| **ドラッグ** | 地図のパン移動（`PanBy`） |
| **ホイール** | ズームイン/アウト（`ZoomAt`、カーソル位置中心、全体表示〜10x） |
| **ピンチ (タッチ)** | `WM_GESTURE` の `GID_ZOOM` → `ZoomAt`（指の中心が基準） |
| **指でなぞる (タッチ)** | `WM_GESTURE` の `GID_PAN` → `PanBy`（離した後も慣性で少し進む） |
| **部屋クリック** | `RoomNodeClicked`イベント発火（タッチモードでは指を離したときに判定） |
| **▲ボタン** | 上階へ移動（`FloorChangeRequested`） |
| **▼ボタン** | 下階へ移動（`FloorChangeRequested`） |

---

## 座標変換システム

```
【スクリーン座標 ⇔ ワールド座標】

┌─────────────────────────────────────────┐
│  _currentMatrix (SKMatrix)              │
│  ・スケール (拡大/縮小)                   │
│  ・平行移動 (パン)                       │
└─────────────────────────────────────────┘

ワールド → スクリーン:
  screenPoint = _currentMatrix.MapPoint(worldPoint)

スクリーン → ワールド:
  _currentMatrix.TryInvert(out inverse)
  worldPoint = inverse.MapPoint(screenPoint)

※マーカー描画時はスクリーン座標に変換して固定サイズで描画
```

---

## マーカーデザイン

| マーカー | 色 | 用途 |
|---------|-----|------|
| **START** | ブルー (`#3B82F6`) | 出発地点 |
| **GOAL** | サーモンレッド (`#EF645F`) | 目的地 |
| **UP** | エメラルドグリーン (`#10B981`) | 上階への階段 |
| **DOWN** | アンバー (`#F59E0B`) | 下階への階段 |
| **LINK** | インディゴ (`#6366F1`) | 階段接続点 |

---

## プロジェクト構成

```
team1/
├── src/
│   ├── team1.sln
│   ├── team1/                  # 画面 (Windows Forms, net8.0-windows)
│   │   ├── Program.cs          # DI の組み立て・起動
│   │   ├── CrashGuard.cs       # 未処理例外の記録と復帰
│   │   ├── IdleWatcher.cs      # 無操作の検出 (展示モード)
│   │   ├── kiosk.json          # 展示モードの設定
│   │   ├── Controls/           # MapViewer (.Touch / .Route / .Inertia / .Overview / .View), AppToolbar, RouteBar, RoutePanel, PlacePopup, SearchPopup, RoomListView
│   │   ├── Forms/              # Form1 (partial: Map, Search, Favorites, Panels, Input, Lifecycle, BuildingMenu, Kiosk)
│   │   ├── Repositories/       # MapResourceRepository (地図画像)
│   │   └── Data/               # map.svg, *.json, NAMING_CONVENTIONS.md
│   └── team1.Core/             # UI 非依存 (net8.0)
│       ├── Models/ Logic/ Repositories/ Usecases/ Stores/ ViewModels/ Messages/
│       └── AppOptions.cs
├── tests/team1.Core.Tests/     # xUnit (検索・経路・最寄り・お気に入り・状態・設定)
└── scripts/validate_data.py    # 地図データの整合性チェック (CI で実行)
```

地図と JSON は埋め込みリソースではなく、ビルド時に出力先の `Data/` へコピーされ、実行時にディスクから読み込む。

---

## 技術的特徴

| 特徴 | 実装方法 |
|------|----------|
| **高速描画** | SkiaSharp（GPU加速対応2Dエンジン） |
| **ベクター地図** | SVGファイル（拡大しても劣化なし） |
| **スケール不変UI** | マーカー・ボタンは画面座標で固定サイズ描画 |
| **マルチフロア** | Floor属性でノード/エッジをフィルタリング |
| **当たり判定** | 逆行列による座標変換 + 距離計算 |
| **経路探索** | A*アルゴリズム（許容的ヒューリスティックで最短経路保証） |
| **層の分離** | DI + ViewModel/Store/Usecase/Repository（Core は UI 非依存でテスト可能） |
| **データ検証** | `scripts/validate_data.py` が SVG と JSON の対応・到達性を CI で検査 |

---

## 使用ライブラリ

| ライブラリ | バージョン | 用途 |
|-----------|-----------|------|
| SkiaSharp | 3.x | 2D描画エンジン |
| SkiaSharp.Views.WindowsForms | 3.x | WinForms統合 |
| Svg.Skia | 3.x | SVG読み込み・描画 |
| Microsoft.Extensions.DependencyInjection | 8.x | DI コンテナ |
| CommunityToolkit.Mvvm | 8.x | メッセージ (IMessenger) |
| xUnit | 2.x | テスト |
| System.Text.Json | (組み込み) | JSON解析 |

---

## 開発チーム

2025年度 D3T02 チーム

---

*このドキュメントは2026年に作成されました*

---

## 追加機能 (マウス・キーボード向け)

| 機能 | 実装 |
|------|------|
| メニューバー | `Form1.Menu.cs`。建物・階は `PlaceSearchUsecase` のデータから作る |
| キーボード操作 | `Form1.Input.cs`。文字入力中 (TextBox) は地図のキーを無視する |
| 慣性スクロール | `MapViewer.Inertia.cs`。ドラッグ中の速度を記録し、離した後タイマーで減速 |
| 部屋一覧 | `RoomListView` + `PlaceSearchUsecase.Filter` (種別・建物・キーワード) |
| 全体図から建物へ | `MapViewer.Overview.cs`。`map.svg` の `view_*` を枠と判定領域に使う |
| 雨の日モード | `Edge.IsOutdoor` (`outdoor_edges.txt`: KCCT の通路ごとの IsOutdoor を移したもの。`scripts/mark_outdoor_edges.py`)。`PathFinder` が屋外の通路を 20 倍の長さとして探す。通れないわけではないので屋外しか道が無い場所にも行ける |

### 他班の地図との位置合わせ (`scripts/register_maps.py`)

KCCT-SiteMap の部屋の座標と team1 の `map.svg` の座標を、同じ名前・同じ階の部屋 (対応点) で突き合わせ、
階ごとに最小二乗で相似変換 (拡大率 約 1.40〜1.46) を求める。外れ値を除いた残差の中央値は 1〜16px。
この変換で KCCT の F1〜F3 の壁を重ねると、主要な建物の形・区画は team1 の壁とほぼ一致した
(差は体育館側など、team1 が階ごとの内部を描いていない建物だけ)。そのため `map.svg` の形は書き換えていない。

### 地図にない部屋の追加 (`scripts/add_missing_nodes.py`)

場所データにあるのに `map.svg` にノードが無い部屋 (体育館・プール・弓道場・機械工学科実験実習棟など) と、通路につながっていない部屋について、
KCCT-SiteMap の座標を `register_maps.py` の変換で team1 の座標にして、ノードと通路を足す。
- 通路ノードが 60px 以内なら、直線 1 本でつなぐ
- 遠いとき (体育館など) は、KCCT の通路を変換して、team1 の通路にぶつかるまでたどり、途中のノードも足す (id は `kcct_N####`)
- 通路の接続点と 8px 以内に重なる部屋は、その接続点の id を部屋の id に付け替える
- 既存の通路が 1 本も変わらないことを確認してから書き込む (`--apply`)。位置が決められなかった部屋は一覧に出す

### 経路を滑らかにする (`GraphDensifier`)

SVG の通路は曲がり角ごとにノードが 1 つしかなく、経路が角で急に折れる。`SvgMapGraphRepository` がグラフを作った後、
- 通路の線を 30px ごとに分けてノードを足す (角のノードから 12px の所には必ず置く)
- 角のノードのまわりで、両側の「角から 12px の位置」のノードどうしを直線で結ぶ (角を切る近道)
- 描くときは `SKPathEffect.CreateCorner` で線の細かい折れを丸める

足したノード (id は `~mid…`) は `IMapGraphRepository.Nodes` には入らず、経路の線にだけ現れる。屋外の通路の印は、分ける前の元のノードの id で付ける。

### 雨の日の経路と屋根付き通路

- `outdoor_edges.txt` は通路ごとの屋外フラグ (`scripts/mark_outdoor_edges.py` が作る)。KCCT-SiteMap の通路の `IsOutdoor` を移し、
  地図に屋根付き通路として描かれた図形 (色 `#c4ccce`) の上にある通路は、屋内として優先する。
- 体育館へ続く屋根付き通路は、KCCT の座標から作った道が描かれた通路から数十px ずれていたので、通路の図形の中心線にノードを置いて作り直した (`scripts/connect_gym.py`)。
- 「階段を避ける」は、エレベーターの加算コストを外して階段に足す (`PathFinder.EdgeCost`)。

### タッチパッドの二本指 (移動・拡大・回転)

Windows は精密タッチパッドの二本指を「スクロール」「Ctrl+ホイール (ピンチ)」に変えてしまい、回転は伝えない。そこで:

- `TouchpadReader` (Controls): ウィンドウをタッチパッドの Raw Input (HID, 使用ページ 0x0D / 使用 0x05) の受け取り先に登録し、`WM_INPUT` から指の位置・番号・Tip (触れているか) を読む。
  ハイブリッドモード (指 1 本ずつレポート) とパラレルモード (1 レポートに全部の指) のどちらも、同じ ScanTime のレポートを 1 フレームにまとめる。位置は「タッチパッドの幅 = 1」に直す。
- `TwoFingerTracker` (Core/Logic): 前のフレームと比べて 移動 (Dx, Dy)・拡大 (Scale)・回転 (Rotation) を返す。指の数や番号が変わったら基準を取り直す。単体テストあり (`TwoFingerGestureTests`)。
- `MapViewer.Touchpad.cs`: 変化を `PanBy` / `ZoomAt` / `RotateAt` にする。拡大・回転の中心はマウスカーソル。二本指の操作中とその直後 250ms は、Windows が同時に送るホイール操作を捨てる (二重に効かないように。マウスのホイールには影響しない)。
- 回転に対応するため、倍率は `CurrentScale` (ScaleX は回転すると cos 倍になる) を使い、地図の座標で描く文字 (部屋名・建物名) は `BeginUpright` で回転を打ち消す。マーカー類は画面の座標で描いているので影響しない。

### ヘルプと、タッチパッドの操作の向き先

- ヘルプは `HelpForm` (別ウィンドウ・モーダルでない。「やりたいこと → 操作」の短い表だけ)。6班は「バージョン情報」のモーダルダイアログ (`AboutForm`: 版・著作権・NuGet のライセンス一覧) だけで、使い方の説明は README にしか無かった。
- タッチパッドの Raw Input は、登録したウィンドウにアプリが前面にある間ずっと届く。そのため `MapViewer.IsTouchpadTarget` で、地図が見えていて、アプリが前面で、カーソルが地図の上にあるときだけ地図を動かす (一覧画面・ヘルプが前面のときは動かさない)。

### フォント

画面の文字は `Theme.FontFamily` で決める。Noto Sans JP → BIZ UDPゴシック → メイリオ UI → 游ゴシック UI の順に、その PC に入っている最初のものを使う (展示用 PC に Noto Sans JP が無くても読める文字になる)。地図の描画 (SkiaSharp) も `Theme.Typeface()` で同じものを使う。

### 大きな画面 (UiScale)

展示用の大きな画面でも、離れて見る来場者に読めるよう、画面全体を拡大できる。
- `AppOptions.UiScale` (kiosk.json)。`null` なら `ResolveUiScale` が、展示モードのときだけ「論理ピクセルの画面の高さ / 900」(1〜2.5 倍、0.05 刻み) にする。通常起動は 1 倍。指定すれば 0.75〜3 に収めてそれを使う。
- `Theme.UiScale` に起動時 (Form を作る前) に設定し、コントロールの大きさは `Theme.Scale(control)` (= Windows の拡大 × UiScale)、文字は `Theme.Ui` (サイズ × UiScale) で決める。地図のラベル・マーカー・階ボタンも同じ倍率になる。
