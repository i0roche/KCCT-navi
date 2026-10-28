# 校内マップ アプリケーション仕様書 (Map Data Specification)

本ドキュメントは、地図アプリケーションにおけるSVGファイル（Inkscape）のID命名規則、および連動するJSONデータの構造を定義するものです。

## 1. データ構造の基本方針

- **ID (SVG & JSON共通):** 部屋・施設を一意に特定するキー。**スネークケース**（小文字と `_` ）で統一。
- **属性データ (JSON):** 検索・表示用のデータはSVGに持たせず、すべて `*.json` で管理する。
- **座標データ (SVG):** 経路計算（A*）用の座標はSVGの円・線の座標をそのまま利用する。

---

## 2. 学科・エリア略称コード (Department Codes)

IDやJSONの `department` キーで使用する略称一覧です。

| 学科・エリア名 | 略称コード | 備考 |
| --- | --- | --- |
| 機械工学科 | `mech` |  |
| 電気工学科 | `elec` |  |
| 電子工学科 | `denshi` |  |
| 都市工学科 | `toshi` |  |
| 応用化学科 | `chem` |  |
| 一般科 | `general` |  |
| 専攻科 | `advanced` |  |
| 体育館・共用施設 | `gym` / `common` |  |

---

## 3. ID命名規則 (ID Naming Conventions)

### 3-1. 部屋・施設 (Rooms & Facilities)

検索の目的地となる場所。JSONとデータを紐付けるため、厳格に命名すること。
**基本フォーマット: `room_[階]f_[学科]_[タイプ]_[識別名]`**

| タイプ (`type`) | 命名ルール・例 | 備考 |
| --- | --- | --- |
| **ホームルーム** | `room_[階]f_[学科]_hr_[クラス名]`例: `room_2f_mech_hr_m1a` | クラス名は小文字。 |
| **研究室** | `room_[階]f_[学科]_lab_[教員名]`例: `room_2f_mech_lab_tanabe` | 教員名はローマ字。同姓はイニシャル追加。 |
| **実験室** | `room_[階]f_[学科]_lab_[英単語]`例: `room_1f_elec_lab_energy` | 特徴を表す英単語、または連番。 |
| **講義室・演習室** | `room_[階]f_[学科]_classroom_[名前]`例: `room_4f_toshi_classroom_seminar` |  |
| **職員室・会議室等** | `room_[階]f_[学科]_facility_[名前]`例: `room_1f_mech_office_tech`例: `room_1f_general_facility_rokushin` | 技術職員室は `office_tech`。 |

### 3-2. ナビゲーション・縦移動 (Navigation & Verticals)

A*アルゴリズムで上下階を繋いだり、特殊な目的地（トイレ等）として扱うノード。

| 種類 | ルール | 具体例 | 役割 |
| --- | --- | --- | --- |
| **階段** | `stair_[階]f_[名前]` | `stair_1f_main / stair_2f_west` | **最重要。** ID に `stair` を含む隣の階のノードどうしで、地図上の位置が 60px 以内のものを、プログラムで自動接続する (名前が同じである必要は無い。位置で決まる)。 |
| **エレベーター** | `elv_[階]f_[名前]` | `elv_1f_main` | 階段と同様に縦移動の接続用 (ふだんは使えない重みにしてある。「階段を避ける」で使う)。 |
| **トイレ** | `wc_[階]f_[性別]_[場所]` | `wc_1f_men_west / wc_1f_multi_main` | 性別: `men`, `women`, `multi` |

---

## 4. JSON データスキーマ (JSON Schema)

`*.json` の各要素の定義。

```json
{
  "Id": "room_4f_mech_hr_m1a",  // SVGのIDと完全一致させる
  "Name": "M1A",                // アプリ表示名
  "Reading": "えむいちえー",     // 名前の読み (ひらがな。省略可。ASCII だけの名前は不要)
  "Type": "classroom",          // 種類 (classroom, lab, facility, club, office, office_tech, restroom, stairs, elevator)
  "Building": "一般科A棟",       // 建物名
  "Floor": 2,                   // 階数 (int)
  "Department": "mech",         // 学科コード
  "Description": "",            // 説明 (省略可。吹き出しに出す)
  "Tags": [                     // 検索ヒット用キーワード
    "HR",
    "M1A",
    "機械工学科",
    "1年"
  ],
  "Hidden": true                // 省略可。true なら検索・一覧に出さず、データ検証の対象外にする
                                // (地図にまだ載せられていない場所。位置が分かればノードを足して外す)
}
```

---

## 5. スクリプトが作る ID・ファイル

地図データを作るスクリプト (`scripts/`) が足すものは、手で書いた ID と区別できる接頭辞を持つ。

| ID・ファイル | 作るもの | 内容 |
| --- | --- | --- |
| `kcct_N####` | `add_missing_nodes.py` | KCCT-SiteMap の通路を変換して足した、通路のノード |
| `walk_gym_#` | `connect_gym.py` | 体育館への屋根付き通路 (地図の通路の図形の中心線) のノード |
| `link_*` (path の id) | 上の 2 つ | 足したノードをつなぐ、2 点だけの通路 |
| `~mid#` | 実行時 (`GraphDensifier`) | 通路を細かく分けるために足すノード。SVG には書かれず、経路の線にだけ現れる |
| `outdoor_edges.txt` | `mark_outdoor_edges.py` | 屋外の通路の一覧。1 行に 2 つのノード ID (`#` 以降はコメント)。雨の日に避ける |
