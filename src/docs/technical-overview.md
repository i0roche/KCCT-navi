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
    G, H, F: float   // A*探索用コスト
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
    Name: string     // "3E教室", "電子情報科 男子トイレ(2F)"
    Type: string     // "room", "stairs", "elevator", "restroom"
    Building: string // "一般科棟", "電子情報科棟"
    Floor: int       // 階数
    Department: string  // "denshi", "common"
    Tags: List<string>  // 検索用タグ
}
```

---

## 描画パイプライン

```
OnPaintSurface() 実行
    │
    ├─① canvas.Clear(White)         // 背景クリア
    │
    ├─② canvas.SetMatrix(_currentMatrix)  // 変換行列適用
    │                                       // （パン・ズーム状態）
    │
    ├─③ DrawPicture(SvgMap)          // SVG地図描画
    │
    ├─④ DrawRoomNames()              // 部屋名ラベル描画
    │   └─ DrawRoomNode()            // 青い丸マーカー
    │
    ├─⑤ DrawRoute()                  // 経路描画
    │   ├─ DrawPath(floorPath)       // 経路線（青）
    │   ├─ DrawMarker(START)         // 開始ピン（青）
    │   ├─ DrawMarker(GOAL)          // 終点ピン（赤）
    │   └─ DrawStairMarker()         // 階段マーカー
    │
    └─⑥ DrawFloorButtons()           // 階数切替ボタン（UI固定位置）
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
│   └── MapViewer.cs    # マップ表示・操作コントロール
├── Data/               # データファイル
│   ├── map.svg         # キャンパスマップ
│   ├── facilities.json # 共通施設（階段・トイレ・EV）
│   ├── denshi.json     # 電子情報科
│   ├── chem.json       # 化学科
│   ├── advanced.json   # 専攻科
│   └── common.json     # 共通教室
├── Forms/              # フォーム関連
│   ├── Form1.cs        # メインフォーム
│   ├── Form1.Map.cs    # 地図表示・操作
│   ├── Form1.Search.cs # 検索機能
│   └── Form1.Favorites.cs # お気に入り管理
├── Logic/              # ビジネスロジック
│   ├── PathFinder.cs   # A*経路探索
│   ├── SvgGraphLoader.cs # SVGグラフ読込
│   └── MapGeometry.cs  # 座標計算
├── Models/             # データモデル
│   ├── Place.cs        # 場所情報
│   ├── MapNode.cs      # ノード
│   └── Edge.cs         # エッジ
├── Services/           # サービス層
│   └── PlaceDataService.cs # 場所データ読込
└── Program.cs          # エントリーポイント
```

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

*このドキュメントは2025年に作成されました*
