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

```
┌──────────────────────────────────────────────────────┐
│                    Presentation Layer                 │
│  ┌────────────────────────────────────────────────┐  │
│  │              MapViewer (SKControl)              │  │
│  │  ・SVG地図のレンダリング                        │  │
│  │  ・パン/ズーム操作                              │  │
│  │  ・経路・マーカー描画                           │  │
│  │  ・階数切替ボタンUI                             │  │
│  └────────────────────────────────────────────────┘  │
├──────────────────────────────────────────────────────┤
│                     Logic Layer                       │
│  ┌──────────────────┐  ┌──────────────────────────┐  │
│  │    PathFinder    │  │    SvgGraphLoader        │  │
│  │   (A*経路探索)   │  │  (SVG→グラフ変換)       │  │
│  └──────────────────┘  └──────────────────────────┘  │
├──────────────────────────────────────────────────────┤
│                     Model Layer                       │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐              │
│  │ MapNode │  │  Edge   │  │  Place  │              │
│  └─────────┘  └─────────┘  └─────────┘              │
├──────────────────────────────────────────────────────┤
│                     Data Layer                        │
│  SVGファイル / JSONファイル (facilities, denshi等)   │
└──────────────────────────────────────────────────────┘
```

---

## 主要コンポーネント

### 1. MapViewer（地図表示コントロール）

#### プロパティ

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `SvgMap` | `SKSvg?` | 表示中のSVG地図 |
| `CurrentRoute` | `List<Edge>?` | 現在の経路 |
| `CurrentFloor` | `int` | 表示中の階（1〜6） |
| `MaxFloor` | `int` | 最大階数（デフォルト6） |
| `Places` | `List<Place>` | 施設情報リスト |
| `AllNodes` | `List<MapNode>` | 全ノード情報 |
| `FavoritePlaceIds` | `HashSet<string>` | お気に入り施設ID |
| `ShowRoomNames` | `bool` | 部屋名表示フラグ |

#### 部屋ノードの色分け

| 色 | PlaceID | 説明 |
|---|---------|------|
| 青 (`DodgerBlue`) | `room_*` | 一般の部屋 |
| 緑 (`#4CAF50`) | `wc_*` | トイレ |
| 紫 (`#9C27B0`) | `stair_*` | 階段 |
| 黄 (`#FFC107`) | （お気に入り） | お気に入り登録済み |

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
    
    // A*探索用プロパティ
    G: float         // 開始地点からの実コスト
    H: float         // ゴールまでの推定距離
    F: float         // G + H（総合評価値）
    Parent: MapNode? // 経路復元用親ノード
    ParentEdge: Edge?  // 経路復元用親エッジ
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

### SuggestionItem（サジェスト項目）
検索候補表示用データ

```csharp
SuggestionItem {
    Place: Place     // 場所情報
    ToString(): string  // リストボックス表示用（Place.Nameを返す）
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
| **ドラッグ** | 地図のパン移動（`SKMatrix.CreateTranslation`） |
| **ホイール** | ズームイン/アウト（中心点基準、0.1x〜10x） |
| **部屋クリック** | `RoomNodeClicked`イベント発火 |
| **▲ボタン** | 上階へ移動（`FloorChangeRequested`） |
| **▼ボタン** | 下階へ移動（`FloorChangeRequested`） |

---

## 座標変換システム

```
【スクリーン座標 ⇔ ワールド座標】

┌─────────────────────────────────────────┐
│  _currentMatrix (SKMatrix)              │
│  ・スケール (拡大/縮小)                 │
│  ・平行移動 (パン)                      │
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
| **START** | 青 (`#0000FF`) | 出発地点 |
| **GOAL** | 赤 (`#FF0000`) | 目的地 |
| **UP** | 緑 (`#4CAF50`) | 上階への階段 |
| **DOWN** | オレンジ (`#FF9800`) | 下階への階段 |
| **LINK** | 青 (`#2196F3`) | 階段接続点 |

---

## プロジェクト構成

```
team1/
├── Controls/           # カスタムコントロール
│   └── MapViewer.cs    # マップ表示・操作コントロール（SkiaSharp使用）
├── Data/               # データファイル
│   ├── common.json     # 共通棟の場所データ
│   ├── advanced.json   # 専攻科棟の場所データ
│   ├── chem.json       # 化学棟の場所データ
│   ├── denshi.json     # 電子棟の場所データ
│   ├── facilities.json # 施設の場所データ
│   └── general.json    # 一般教室棟の場所データ
├── Forms/              # フォーム関連（partial class分割）
│   ├── Form1.cs        # メインフォーム（初期化）
│   ├── Form1.Designer.cs # デザイナー生成コード
│   ├── Form1.Lifecycle.cs # ライフサイクル管理
│   ├── Form1.Map.cs    # 地図表示・操作
│   ├── Form1.Search.cs # 検索機能
│   ├── Form1.Input.cs  # 入力処理
│   ├── Form1.Favorites.cs # お気に入り管理
│   ├── Form1.Panels.cs # パネル切替
│   └── Form1.BuildingMenu.cs # 建物メニュー
├── Logic/              # ビジネスロジック
│   ├── PathFinder.cs   # A*経路探索アルゴリズム
│   ├── SvgGraphLoader.cs # SVGからグラフ構造読込
│   └── MapGeometry.cs  # 座標計算・変換
├── Models/             # データモデル
│   ├── Place.cs        # 場所情報（ID, 名前, 階, タグ）
│   ├── MapNode.cs      # グラフノード（座標, 階）
│   ├── Edge.cs         # グラフエッジ（重み, 経由点）
│   └── SuggestionItem.cs # サジェスト項目
├── Services/           # サービス層
│   └── PlaceDataService.cs # JSONから場所データ読込
├── Properties/         # プロジェクト設定
│   ├── Resources.resx  # リソース定義（map.svg含む）
│   └── Resources.Designer.cs # リソースアクセスコード
└── Program.cs          # エントリーポイント
```

**注記**: `map.svg`は`Properties/Resources.resx`に埋め込みリソースとして含まれています。

---

## 技術的特徴

| 特徴 | 実装方法 |
|------|----------|
| **高速描画** | SkiaSharp（GPU加速対応2Dエンジン） |
| **ベクター地図** | SVGファイル（拡大しても劣化なし） |
| **スケール不変UI** | マーカー・ボタンは画面座標で固定サイズ描画 |
| **マルチフロア** | Floor属性でノード/エッジをフィルタリング |
| **当たり判定** | 逆行列による座標変換 + 距離計算 |
| **経路探索** | A*アルゴリズム（最短経路保証） |

---

## 使用ライブラリ

| ライブラリ | バージョン | 用途 |
|-----------|-----------|------|
| SkiaSharp | 3.x | 2D描画エンジン |
| SkiaSharp.Views.WindowsForms | 3.x | WinForms統合 |
| Svg.Skia | 2.x | SVG読み込み・描画 |
| System.Text.Json | (組み込み) | JSON解析 |

---

## 開発チーム

2025年度 D3T02 チーム

---

*このドキュメントは2026年に作成されました*
