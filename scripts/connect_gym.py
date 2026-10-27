#!/usr/bin/env python3
"""体育館へ続く屋根付き通路 (渡り廊下) を、地図に描かれた通路の形に沿ってつなぐ。

add_missing_nodes.py が KCCT の座標から作った体育館への道は、校舎から 600px 以上離れているため
位置合わせの誤差が大きく、描かれた通路から数十 px ずれていた (雨の日の経路が通路に沿わない)。
そこで体育館まわりだけ、map.svg に描かれた通路の図形 (path627, path628: 色 #c4ccce) の中心線にノードを置いて作り直す。

  一般科側のノード (1232,419) ─ 帯の東端 ─ 帯の中心線 (y≈386) ─ 西端 ─ 体育館の入口 ─ トレーニングルーム / サブ / メイン

1. KCCT 由来の体育館まわりのノードと通路 (kcct_N0415〜N0418, N0422, N0423 と、それにつながる link_*) を取り除く
2. 通路の中心線のノードと、体育館の中の通路を足す

何度実行しても同じ結果になる。既定は確認だけ。--apply で map.svg に書き込む。
"""

import argparse
import math
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import add_missing_nodes as A  # noqa: E402
import validate_data as V  # noqa: E402

DATA = A.DATA

# 取り除く KCCT 由来のノード (体育館の中と入口)
OLD_NODES = ["kcct_N0415", "kcct_N0416", "kcct_N0417", "kcct_N0418", "kcct_N0422", "kcct_N0423"]
# 取り除く通路 (id の接頭辞 link_ を除いたもの)
OLD_LINKS = [
    "room_1f_common_facility_arena_main_kcct_N0422",
    "kcct_N0422_kcct_N0417",
    "kcct_N0417_kcct_N0416",
    "kcct_N0416_kcct_N0415",
    "kcct_N0415_kcct_N0424",
    "room_1f_common_facility_arena_sub_kcct_N0418",
    "kcct_N0418_kcct_N0417",
    "room_1f_common_facility_training_kcct_N0423",
    "kcct_N0423_kcct_N0416",
]

# 通路の帯 (path627 の y 378〜393、path628 の y 374〜383) の中心線
BAND_Y = 386.0          # 東西にまっすぐな帯の中心
BAND_EAST = 1232.0      # 帯の東端 (一般科側のノード x=1232 の真上)
BAND_WEST = 972.0       # path627 の西端
ENTRANCE = (905.0, 379.0)   # path628 の西端 (体育館の南の壁ぞい)
INSIDE = (885.0, 362.0)     # 体育館に入ったところ
EAST_ANCHOR = "path7-5-684-72-89-0-15"   # 帯の東端の下にある、既存の通路ノード (1232,419)

GYM_ROOMS = [
    "room_1f_common_facility_training",
    "room_1f_common_facility_arena_sub",
    "room_1f_common_facility_arena_main",
]


def node_xy(text, node_id):
    m = re.search(r'<circle\s[^>]*?id="%s"[^>]*?cx="([^"]+)"[^>]*?cy="([^"]+)"' % re.escape(node_id), text, re.S)
    if not m:
        raise SystemExit(f"map.svg に {node_id} が無い")
    return float(m.group(1)), float(m.group(2))


def remove_element(text, tag, element_id):
    """<tag ... id="element_id" ... /> を 1 つ取り除く (なければそのまま)"""
    pattern = re.compile(r'<%s\b(?:(?!/>).)*?\bid="%s"(?:(?!/>).)*?/>' % (tag, re.escape(element_id)), re.S)
    return pattern.sub("", text, count=1)


def build(text):
    for node_id in OLD_NODES:
        text = remove_element(text, "circle", node_id)
    for link in OLD_LINKS:
        text = remove_element(text, "path", "link_" + link)

    already = f'id="walk_gym_0"' in text
    anchor = node_xy(text, EAST_ANCHOR)
    rooms = {r: node_xy(text, r) for r in GYM_ROOMS}

    nodes = [("walk_gym_0", (BAND_EAST, BAND_Y)), ("walk_gym_1", (1100.0, BAND_Y)),
             ("walk_gym_2", (BAND_WEST, BAND_Y)), ("walk_gym_3", ENTRANCE), ("walk_gym_4", INSIDE)]
    pos = dict(nodes)
    pos[EAST_ANCHOR] = anchor
    pos.update(rooms)

    chain = [EAST_ANCHOR, "walk_gym_0", "walk_gym_1", "walk_gym_2", "walk_gym_3", "walk_gym_4",
             GYM_ROOMS[0], GYM_ROOMS[1]]
    # 入口 → トレーニングルーム → サブアリーナ、入口 → メインアリーナ
    edges = list(zip(chain[:-1], chain[1:]))
    edges.append(("walk_gym_4", GYM_ROOMS[2]))

    if already:
        return text, [], []
    new_nodes = [(1, nid, xy[0], xy[1], "地図の通路の中心線") for nid, xy in nodes]
    new_edges = [(1, f"{a}_{b}", a, pos[a], b, pos[b], "地図の通路の中心線") for a, b in edges]
    return A.write_svg(text, new_nodes, new_edges, []), new_nodes, new_edges


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()

    svg_path = DATA / "map.svg"
    old_text = svg_path.read_text(encoding="utf-8")
    new_text, new_nodes, new_edges = build(old_text)

    for _f, nid, x, y, _s in new_nodes:
        print(f"  + ノード {nid} ({x:.0f},{y:.0f})")
    for _f, _l, a, ap, b, bp, _s in new_edges:
        print(f"  + 通路   {a} -- {b}  {math.dist(ap, bp):.0f}px")
    print(f"追加 {len(new_nodes)} ノード / {len(new_edges)} 通路" if new_nodes else "追加済み (何もしない)")

    tmp = svg_path.with_name("_map_check.svg")
    tmp.write_text(new_text, encoding="utf-8")
    try:
        errs = []
        nodes = V.load_graph(tmp, errs)
    finally:
        tmp.unlink(missing_ok=True)
    comp = V.components(nodes)
    by_id = {n.id: n for n in nodes}
    main_comp = comp[by_id[EAST_ANCHOR]]
    ok = all(r in by_id and comp[by_id[r]] == main_comp for r in GYM_ROOMS)
    print("体育館の 3 室が通路につながっている:", ok)
    if not ok:
        return 1

    if args.apply and new_text != old_text:
        svg_path.write_text(new_text, encoding="utf-8")
        print("map.svg を書き込みました")
    return 0


if __name__ == "__main__":
    sys.exit(main())
