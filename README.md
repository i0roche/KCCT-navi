# 校内マップ ナビアプリ README

本アプリは校内の教室・施設を検索し、目的地までの最短経路を表示する Windows フォームアプリです。SVG 地図と JSON メタデータを組み合わせ、階ごとの表示や経路ハイライトを行います。

## 主な機能
- 建物・階層表示: メニューから学科棟や共用棟の階を直接表示
- 施設検索: 名前・ID・タグで部分一致検索、候補サジェスト表示
- 経路案内: 開始と目的を指定して最短経路を計算し、現在階に応じて強調描画
- お気に入り: 部屋/施設をお気に入り登録し、検索欄にワンクリック入力
- マップ操作: ドラッグで移動、ホイールでズーム、部屋ノードをクリックして詳細表示
- キーボード操作: Ctrl+F で開始入力、Ctrl+G で目的入力、Ctrl+Enter で検索/経路決定、Esc でマップにフォーカス

## 動作環境
- OS: Windows 10/11
- SDK/ランタイム: .NET 8.0 (Windows デスクトップ) + Visual Studio 2022 以降推奨
- 依存ライブラリ: SkiaSharp, Svg.Skia, System.Text.Json（既にプロジェクト参照済み）

## セットアップとビルド
1) Visual Studio で team1.sln を開く（ソリューションルートに配置）。
2) ソリューション構成は Debug/Any CPU のままでビルド可能です。
3) `src/Data` フォルダの JSON 群と `map.svg` を実行ファイルと同じ階層の `Data/` に配置してください。既定では `bin/Debug/net8.0-windows/Data` を参照します。
4) F5 で実行、またはコマンドラインの場合:
   - `dotnet build src/team1.csproj`
   - `dotnet run --project src/team1.csproj`

## 使い方の流れ
1) 起動後、検索タブにある「開始」「目的」にキーワードを入力（候補リストが自動表示）。
2) Enter または「検索 / 経路案内」ボタンで決定。開始のみの場合は該当地点にズーム、開始+目的の場合は経路を計算して表示。
3) マップ上の部屋ノードをクリックすると情報タブに詳細が表示され、そこからお気に入り登録/解除が可能。
4) メニューの建物/階メニューから任意の棟・階にジャンプ可能。
5) マップ操作: ホイールで拡大縮小、ドラッグで移動。Esc でマップにフォーカスを戻せます。

## データ仕様
- 部屋・施設情報は `src/Data/*.json` にあり、実行時はリソースから読み込みます。
- ID 命名規則と JSON スキーマは [src/Data/NAMING_CONVENTIONS.md](src/Data/NAMING_CONVENTIONS.md) を参照してください。
- 地図 SVG は `Data/map.svg` を読み込み、階層グループ `floor_*` とビュー矩形 `view_*` を用いて表示範囲を切り替えます。

## フォルダ構成（抜粋）
- [src/Program.cs](src/Program.cs): エントリポイント
- [src/Form1.cs](src/Form1.cs): フォーム本体と初期化
- [src/Form1.Search.cs](src/Form1.Search.cs): 検索・経路案内ロジック
- [src/Form1.Map.cs](src/Form1.Map.cs): 地図表示更新、建物/階移動
- [src/Form1.Favorites.cs](src/Form1.Favorites.cs): お気に入り操作
- [src/Controls/MapViewer.cs](src/Controls/MapViewer.cs): SkiaSharp ベースの地図描画と操作
- [src/Data](src/Data): 施設 JSON、命名規約ドキュメント、SVG などデータ類

## 開発メモ
- SVG に含まれるノード ID と JSON の Id を一致させると、クリック判定と経路計算に利用されます。
- 新しい部屋や施設を追加する場合は、SVG のノードと JSON のエントリを同じ Id で追加し、必要に応じてタグを付与してください。
