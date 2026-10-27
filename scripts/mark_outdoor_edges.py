#!/usr/bin/env python3
"""team1 の通路のうち、屋外のものを outdoor_edges.txt に書き出す。

屋外かどうかは、KCCT-SiteMap の通路 (GraphData.json の Edges の IsOutdoor) から移す。
KCCT のノードを register_maps.py の変換で team1 の座標にし、team1 の通路 (2 つのノードを結ぶ線)
と同じ場所にある KCCT の通路を探して、その IsOutdoor を使う。
屋根のある渡り廊下は KCCT でも屋外ではないので、雨の日に濡れずに通れる。

KCCT の通路と重ならない team1 の通路 (KCCT に無い道) は、どの建物の表示範囲 (view_*) にも入っていなければ屋外とみなす。

使い方: python scripts/mark_outdoor_edges.py <KCCT-SiteMap のクローン> [--apply]
"""

import argparse
import json
import xml.etree.ElementTree as ET
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import add_missing_nodes as A  # noqa: E402
import register_maps as R  # noqa: E402
import validate_data as V  # noqa: E402

DATA = A.DATA
# 通路の上の点が、KCCT の通路からこれ以内なら重なっているとみなす (位置合わせの誤差が 10px 前後あるため)
MATCH_DISTANCE = 22.0
# 通路の上の点の何割がそろえば「同じ通路」とみなすか
MATCH_RATIO = 0.7
SAMPLE_STEP = 12.0


def point_segment_distance(p, a, b):
    ax, ay = a
    bx, by = b
    dx, dy = bx - ax, by - ay
    length2 = dx * dx + dy * dy
    if length2 == 0:
        return math.dist(p, a)
    t = max(0.0, min(1.0, ((p[0] - ax) * dx + (p[1] - ay) * dy) / length2))
    return math.dist(p, (ax + t * dx, ay + t * dy))


def samples(a, b):
    length = math.dist(a, b)
    n = max(2, int(length / SAMPLE_STEP) + 1)
    return [(a[0] + (b[0] - a[0]) * i / (n - 1), a[1] + (b[1] - a[1]) * i / (n - 1)) for i in range(n)]


def covered_shapes():
    """map.svg に描かれた屋根付き通路 (渡り廊下) の図形。色 #c4ccce の path の頂点列"""
    shapes = []
    for e in ET.parse(DATA / "map.svg").getroot().iter():
        if e.tag.endswith("path") and "#c4ccce" in (e.get("style") or ""):
            pts = V.parse_path_points(e.get("d", ""))
            if len(pts) >= 3:
                shapes.append(pts)
    return shapes


def inside(point, polygon):
    x, y = point
    hit = False
    j = len(polygon) - 1
    for i in range(len(polygon)):
        xi, yi = polygon[i]
        xj, yj = polygon[j]
        if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi:
            hit = not hit
        j = i
    return hit


def near_polygon(point, polygon, tolerance):
    """図形の中、または縁から tolerance 以内か (通路の中心線を引いた位置の誤差を許す)"""
    if inside(point, polygon):
        return True
    return any(point_segment_distance(point, polygon[i - 1], polygon[i]) <= tolerance for i in range(len(polygon)))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("kcct", type=Path)
    parser.add_argument("--apply", action="store_true", help="outdoor_edges.txt を書き込む")
    args = parser.parse_args()

    graph = json.loads((args.kcct / "SiteMapApp/Data/GraphData.json").read_text(encoding="utf-8-sig"))
    knodes = {n["NId"]: n for n in graph["Nodes"]}
    transforms = R.floor_transforms(args.kcct)

    # KCCT の通路 (階ごとの線分と屋外フラグ)
    segments = {}
    for e in graph["Edges"]:
        a, b = knodes[e["From"]], knodes[e["To"]]
        if a["Floor"] != b["Floor"] or a["Floor"] not in transforms:
            continue
        tr = transforms[a["Floor"]]
        segments.setdefault(a["Floor"], []).append(
            (R.apply(tr, a["X"], a["Y"]), R.apply(tr, b["X"], b["Y"]), bool(e["IsOutdoor"]), e["State"]))

    errors = []
    nodes = V.load_graph(DATA / "map.svg", errors)
    views = list(A.view_rects(DATA / "map.svg").values())

    def in_building(x, y):
        return any(r[0] <= x <= r[2] and r[1] <= y <= r[3] for r in views)

    covered = covered_shapes()
    outdoor, stats = [], {"屋根付き通路の上 (屋内)": 0, "KCCT と一致 (屋外)": 0, "KCCT と一致 (屋内)": 0, "一致なし (屋外とみなす)": 0, "一致なし (屋内とみなす)": 0}
    seen = set()
    for n in nodes:
        for m, _cost in n.edges:
            key = frozenset((n.id, m.id))
            if n.floor != m.floor or key in seen:
                continue
            seen.add(key)
            segs = segments.get(n.floor, [])
            pts = samples((n.x, n.y), (m.x, m.y))
            # 地図に屋根付き通路として描かれた図形の上にある通路は、KCCT の判定より優先して屋内にする
            if n.floor == 1 and sum(any(near_polygon(p, shape, 8.0) for shape in covered) for p in pts) / len(pts) >= MATCH_RATIO:
                stats["屋根付き通路の上 (屋内)"] += 1
                continue
            votes, near = [], 0
            for p in pts:
                best = min(((point_segment_distance(p, s[0], s[1]), s) for s in segs), key=lambda t: t[0], default=None)
                if best and best[0] <= MATCH_DISTANCE:
                    near += 1
                    votes.append(best[1][2])
            if segs and near / len(pts) >= MATCH_RATIO:
                is_outdoor = sum(votes) * 2 > len(votes)
                stats["KCCT と一致 (屋外)" if is_outdoor else "KCCT と一致 (屋内)"] += 1
            else:
                mid = ((n.x + m.x) / 2, (n.y + m.y) / 2)
                is_outdoor = n.floor == 1 and not in_building(*mid)
                stats["一致なし (屋外とみなす)" if is_outdoor else "一致なし (屋内とみなす)"] += 1
            if is_outdoor:
                outdoor.append(tuple(sorted((n.id, m.id))))

    for k, v in stats.items():
        print(f"{k}: {v}")
    print(f"屋外の通路 {len(outdoor)} 本")

    if args.apply:
        lines = [
            "# 屋外の通路 (1 行に 2 つのノード ID)。雨の日の経路で、この通路を避ける。",
            "# scripts/mark_outdoor_edges.py が KCCT-SiteMap の IsOutdoor から作った。屋根のある渡り廊下は含まれない。",
            "# 手で直してよい (地図の通路を変えたら、もう一度スクリプトを実行する)。",
        ]
        lines += [f"{a} {b}" for a, b in sorted(outdoor)]
        (DATA / "outdoor_edges.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
        print("outdoor_edges.txt を書き込みました")


if __name__ == "__main__":
    main()
