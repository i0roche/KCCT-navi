#!/usr/bin/env python3
"""map.svg に無い部屋のノードを足し、孤立している部屋を通路につなぐ。

1. 場所データ (src/team1/Data/*.json) にあるのに map.svg にノードが無い部屋
   - KCCT-SiteMap に同名・同階の部屋があれば、その座標を team1 の座標に変換して位置にする
   - トイレは KCCT のカテゴリ (男子/女子/バリアフリー) のノードから、建物内で未登録のものを選ぶ
2. ノードはあるが通路につながっていない部屋 (単独または小さな塊)
3. 位置が決まったノードは、同じ階の一番近い通路ノードへ 2 点だけの短い通路 (直線) でつなぐ

位置が決められない部屋は一覧にして残す (map.svg には何も足さない)。
既定は確認だけ (map.svg は変えない)。--apply を付けたときだけ書き込む。

使い方: python scripts/add_missing_nodes.py <KCCT-SiteMap のクローン> [--apply]
"""

import argparse
import heapq
import unicodedata
import json
import math
import re
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import register_maps as R  # noqa: E402
import validate_data as V  # noqa: E402

DATA = Path(__file__).resolve().parent.parent / "src" / "team1" / "Data"

# 通路ノードまでの距離の上限 (px)。これより遠いと別の場所につないでしまう恐れがあるので、つながない
MAX_LINK = 160.0
# 通路ノードがこの距離以内なら、直線 1 本でつなぐ。これより遠いときは KCCT の通路をたどる
NEAR_LINK = 60.0
# KCCT の通路が、team1 の通路ノードに重なっているとみなす距離
ANCHOR_RADIUS = 18.0
# 通路の接続点がこの距離以内なら、新しいノードを足さずにその接続点を部屋のノードにする
RENAME_RADIUS = 8.0
# 同じ KCCT の場所を共有する 2 つ目の部屋をずらす量 (px)
SHARED_OFFSET = 30.0
# 既存のノードにこれより近い位置には、新しいノードを置かない (ローダーが 20px 以内を同じノードとみなすため)
MIN_GAP = 24.0

# 建物名 -> 地図の表示範囲 (view_*)。Form1.Map.cs の GetBuildingViewId と同じ対応
VIEW_BY_BUILDING = [
    ("一般科", "view_ippan"), ("電気", "view_elec"), ("電子", "view_denshi"), ("機械", "view_mech"),
    ("都市", "view_toshi"), ("化学", "view_chem"), ("専攻", "view_senkou"), ("本部", "view_honbu"),
    ("講義", "view_kougi"), ("学生会館", "view_gakusei"), ("図書館", "view_info"),
]

CIRCLE_STYLE = "display:inline;fill:none;stroke:#000000;stroke-linecap:round;stroke-linejoin:round"
PATH_STYLE = "fill:none;stroke:#000000;stroke-linecap:round;stroke-linejoin:round"


def read_outdoor_ids():
    """outdoor_edges.txt にあるノード ID (屋外の通路の端点)"""
    path = DATA / "outdoor_edges.txt"
    if not path.exists():
        return set()
    ids = set()
    for line in path.read_text(encoding="utf-8").splitlines():
        ids.update(line.split("#")[0].split())
    return ids


def load_places():
    places = []
    for p in sorted(DATA.glob("*.json")):
        for place in json.loads(p.read_text(encoding="utf-8")):
            place["_file"] = p.name
            places.append(place)
    return places


def view_rects(svg_path):
    rects = {}
    for e in ET.parse(svg_path).getroot().iter():
        i = e.get("id", "")
        if i.startswith("view_") and e.get("width"):
            x, y, w, h = (float(e.get(k, "0")) for k in ("x", "y", "width", "height"))
            rects[i] = (x, y, x + w, y + h)
    return rects


def kcct_data(kcct_root):
    """名前 -> [(階, x, y)] と、トイレ種別 -> [(階, x, y)] (KCCT の座標のまま)"""
    graph = json.loads((kcct_root / "SiteMapApp/Data/GraphData.json").read_text(encoding="utf-8-sig"))
    nodes = {n["NId"]: n for n in graph["Nodes"]}
    src = (kcct_root / "SiteMapApp/Data/FacilityData.cs").read_text(encoding="utf-8-sig")

    names = {}
    for name, _rid, nid in re.findall(r'new\(\s*"([^"]+)"\s*,\s*"([^"]*)"\s*,\s*"(N\d+)"', src):
        if nid in nodes:
            n = nodes[nid]
            names.setdefault(name.strip(), []).append((n["Floor"], n["X"], n["Y"], nid))

    rest = {}
    for kind, key in (("men", "RestM"), ("women", "RestW"), ("multi", "RestB")):
        block = re.search(r"Types\.%s, new Category\(.*?nids: \[(.*?)\]\)" % key, src, re.S)
        ids = re.findall(r'"(N\d+)"', block.group(1)) if block else []
        rest[kind] = [(nodes[i]["Floor"], nodes[i]["X"], nodes[i]["Y"]) for i in dict.fromkeys(ids) if i in nodes]
    # 建物そのもの (プール・弓道/アーチェリー場など) も名前で引けるようにする
    for name, nid in re.findall(r'name:\s*"([^"]+)"[^)]*?nid:\s*"(N\d+)"', src, re.S):
        if nid in nodes:
            n = nodes[nid]
            names.setdefault(name.strip(), []).append((n["Floor"], n["X"], n["Y"], nid))

    adj = {}
    for e in graph["Edges"]:
        a, b = e["From"], e["To"]
        if a in nodes and b in nodes and nodes[a]["Floor"] == nodes[b]["Floor"]:
            adj.setdefault(a, []).append((b, e["Dist"]))
            adj.setdefault(b, []).append((a, e["Dist"]))
    return names, rest, nodes, adj


# team1 の場所名 -> KCCT の建物名 (KCCT では 1 つの建物として載っているもの)
KCCT_ALIAS = {
    "弓道場": "弓道/アーチェリー場",
    "アーチェリー場": "弓道/アーチェリー場",
}


def norm(name):
    """全角の数字・英字を半角にし、末尾の括弧書き ("技術職員室(機械工学科)" など) を除く"""
    name = unicodedata.normalize("NFKC", name).strip()
    return re.sub(r"\s*\(.*?\)\s*$", "", name)


def is_corridor(node):
    return not node.id.startswith(("room_", "wc_", "stair_", "elv_"))


def nearest(nodes, x, y):
    return min(nodes, key=lambda n: math.dist((n.x, n.y), (x, y)), default=None)


def plan(kcct_root):
    errors = []
    nodes = V.load_graph(DATA / "map.svg", errors)
    by_id = {n.id: n for n in nodes}
    comp = V.components(nodes)
    places = load_places()
    main = Counter(comp[by_id[p["Id"]]] for p in places if p["Id"] in by_id).most_common(1)[0][0]
    main_corridor = {}
    for n in nodes:
        if comp[n] == main and is_corridor(n):
            main_corridor.setdefault(n.floor, []).append(n)

    transforms = R.floor_transforms(kcct_root)
    names, rest, k_nodes, k_adj = kcct_data(kcct_root)
    names_n = {}
    for k, v in names.items():
        names_n.setdefault(norm(k), []).extend(v)
    views = view_rects(DATA / "map.svg")

    def building_rect(building):
        for key, vid in VIEW_BY_BUILDING:
            if key in building and vid in views:
                return views[vid]
        return None

    def to_team1(floor, x, y):
        tr = transforms.get(floor)
        return R.apply(tr, x, y) if tr else None

    new_nodes, new_edges, unresolved, renames = [], [], [], []
    taken = [(n.floor, n.x, n.y) for n in nodes]  # 新しく置くノードどうしも離す
    outdoor_ids = read_outdoor_ids()
    renamed, used_kcct, kmap, linked = set(), set(), {}, set()

    # KCCT の通路ノード (ID -> team1 の座標)
    ktr = {}
    for nid, kn in k_nodes.items():
        t = to_team1(kn["Floor"], kn["X"], kn["Y"])
        if t:
            ktr[nid] = t

    all_by_floor = {}
    for n in nodes:
        if comp[n] == main and not n.id.startswith(("room_", "wc_")):
            all_by_floor.setdefault(n.floor, []).append(n)

    def anchor_node(floor, nid):
        n = nearest(all_by_floor.get(floor, []), *ktr[nid])
        return (n.id, (n.x, n.y))

    def kcct_route(floor, k0):
        """KCCT の通路で k0 から、team1 の通路ノードに重なる KCCT ノードまでの一番短い道"""
        if k0 not in ktr:
            return None
        dist, prev = {k0: 0.0}, {}
        queue = [(0.0, k0)]
        while queue:
            d, u = heapq.heappop(queue)
            if d > dist[u]:
                continue
            if u != k0:
                n = nearest(all_by_floor.get(floor, []), *ktr[u])
                if n is not None and math.dist((n.x, n.y), ktr[u]) <= ANCHOR_RADIUS:
                    path = [u]
                    while path[-1] != k0:
                        path.append(prev[path[-1]])
                    return path[::-1]
            for v, w in k_adj.get(u, []):
                if v in ktr and k_nodes[v]["Floor"] == floor and d + w < dist.get(v, math.inf):
                    dist[v], prev[v] = d + w, u
                    heapq.heappush(queue, (d + w, v))
        return None

    def nearby_id(floor, x, y):
        """既存の通路ノードが MIN_GAP 以内にあれば、それを使う"""
        n = nearest(all_by_floor.get(floor, []), x, y)
        if n is not None and math.dist((n.x, n.y), (x, y)) < MIN_GAP:
            return (n.id, (n.x, n.y))
        return None

    def nearest_new(floor, x, y):
        cands = [t for t in new_nodes if t[0] == floor]
        return min(cands, key=lambda t: math.dist((t[2], t[3]), (x, y)), default=None)

    def too_close(floor, x, y):
        return any(f == floor and math.dist((px, py), (x, y)) < MIN_GAP for f, px, py in taken)

    for p in places:
        if p.get("Hidden"):
            continue
        pid, floor = p["Id"], p["Floor"]

        # 孤立している部屋 (ノードはあるが通路につながっていない)
        node = by_id.get(pid)
        if node is not None:
            if comp[node] == main:
                continue
            members = [n for n in nodes if comp[n] == comp[node]]
            best = None
            for m in members:
                c = nearest(main_corridor.get(m.floor, []), m.x, m.y)
                if c and (best is None or math.dist((m.x, m.y), (c.x, c.y)) < best[0]):
                    best = (math.dist((m.x, m.y), (c.x, c.y)), m, c)
            if best and best[0] < 1.0:
                unresolved.append((pid, p["Name"], "通路ノードと座標が完全に重なっている (別のノードのまま。SVG を直す必要がある)"))
            elif best and best[0] <= MAX_LINK:
                new_edges.append((floor, pid, best[1].id, (best[1].x, best[1].y), best[2].id, (best[2].x, best[2].y), "孤立"))
            else:
                unresolved.append((pid, p["Name"], "孤立していて、近くに通路が無い"))
            continue

        # ノードが無い部屋: KCCT の座標から位置を決める
        pos = None
        source = ""
        k_nid = None
        key = norm(KCCT_ALIAS.get(p["Name"].strip(), p["Name"].strip()))
        found = [f for f in names_n.get(key, []) if f[0] == floor]
        if len(found) > 1:
            # 同名の部屋が複数の建物にあるときは、その建物の範囲に入るものだけにする
            rect = building_rect(p["Building"])
            if rect is not None:
                found = [f for f in found
                         if (t := to_team1(f[0], f[1], f[2])) is not None
                         and rect[0] - 40 <= t[0] <= rect[2] + 40 and rect[1] - 40 <= t[1] <= rect[3] + 40]
        if len(found) == 1:
            pos, source, k_nid = to_team1(floor, found[0][1], found[0][2]), "KCCT 同名の部屋", found[0][3]
        elif pid.startswith("wc_"):
            kind = "women" if "_women_" in pid else "men" if "_men_" in pid else "multi"
            rect = building_rect(p["Building"])
            existing = [(n.x, n.y) for n in nodes if n.floor == floor and n.id.startswith("wc_")]
            cands = []
            for f, kx, ky in rest.get(kind, []):
                t = to_team1(f, kx, ky) if f == floor else None
                if t is None or rect is None:
                    continue
                inside = rect[0] - 30 <= t[0] <= rect[2] + 30 and rect[1] - 30 <= t[1] <= rect[3] + 30
                if inside and all(math.dist(t, e) > 40 for e in existing):
                    cands.append(t)
            if cands:
                cx, cy = (rect[0] + rect[2]) / 2, (rect[1] + rect[3]) / 2
                pos, source = min(cands, key=lambda t: math.dist(t, (cx, cy))), "KCCT のトイレ"
        if pos is None:
            unresolved.append((pid, p["Name"], "KCCT に対応する場所が無い"))
            continue

        x, y = pos

        # 同じ KCCT の場所を 2 つの部屋で共有しているとき (弓道場とアーチェリー場など) は、2 つ目を少しずらす
        shared = k_nid in used_kcct
        if shared:
            for dx, dy in ((SHARED_OFFSET, 0), (0, SHARED_OFFSET), (-SHARED_OFFSET, 0), (0, -SHARED_OFFSET)):
                if not too_close(floor, pos[0] + dx, pos[1] + dy):
                    x, y = pos[0] + dx, pos[1] + dy
                    break
            else:
                x += SHARED_OFFSET
        elif k_nid:
            used_kcct.add(k_nid)

        # 通路の接続点とほぼ同じ位置なら、その接続点を部屋のノードとして使う (id を部屋の id に付け替える)
        junction = next((n for n in nodes if n.floor == floor and comp[n] == main and n.id not in renamed
                         and n.id not in outdoor_ids and not n.id.startswith(("room_", "wc_", "stair_", "elv_", "gate_"))
                         and math.dist((n.x, n.y), (x, y)) <= RENAME_RADIUS), None)
        if junction is not None:
            renamed.add(junction.id)
            renames.append((floor, junction.id, pid, source))
            continue

        if too_close(floor, x, y):
            unresolved.append((pid, p["Name"], f"({x:.0f},{y:.0f}) が既存のノードに近すぎる"))
            continue

        c = nearest(main_corridor.get(floor, []), x, y)
        if c is not None and math.dist((x, y), (c.x, c.y)) <= NEAR_LINK:
            taken.append((floor, x, y))
            new_nodes.append((floor, pid, x, y, source))
            new_edges.append((floor, pid, pid, (x, y), c.id, (c.x, c.y), source))
            continue

        # 通路が遠い (体育館など): KCCT の通路を変換して、既存の通路にぶつかるまでたどる
        route = None if shared else kcct_route(floor, k_nid)
        if route is None:
            near = nearest_new(floor, x, y)  # 共有の 2 つ目などは、追加済みの一番近いノードにつなぐ
            if near is None or math.dist((x, y), (near[2], near[3])) > MAX_LINK:
                unresolved.append((pid, p["Name"], "近くに通路が無い (KCCT の通路でも既存の通路に届かない)"))
                continue
            taken.append((floor, x, y))
            new_nodes.append((floor, pid, x, y, source))
            new_edges.append((floor, pid, pid, (x, y), near[1], (near[2], near[3]), source))
            continue

        taken.append((floor, x, y))
        new_nodes.append((floor, pid, x, y, source))
        ids = [(pid, (x, y))]
        for nid in route[1:-1]:
            tx, ty = ktr[nid]
            existing_id = nearby_id(floor, tx, ty)
            if existing_id is not None:
                ids.append(existing_id)
            elif nid in kmap:
                ids.append(kmap[nid])
            else:
                new_id = f"kcct_{nid}"
                kmap[nid] = (new_id, (tx, ty))
                taken.append((floor, tx, ty))
                new_nodes.append((floor, new_id, tx, ty, "KCCT の通路"))
                ids.append(kmap[nid])
        ids.append(anchor_node(floor, route[-1]))
        for (a_id, a_xy), (b_id, b_xy) in zip(ids, ids[1:]):
            if a_id != b_id and frozenset((a_id, b_id)) not in linked:
                linked.add(frozenset((a_id, b_id)))
                new_edges.append((floor, f"{a_id}_{b_id}", a_id, a_xy, b_id, b_xy, source))

    return new_nodes, new_edges, unresolved, renames


def insert_before_group_end(text, group_id, addition):
    """<g id="group_id"> の閉じタグの直前に addition を入れる (グループの中に g は入れ子になっていない)"""
    start = text.index(f'id="{group_id}"')
    end = text.index("</g>", start)
    return text[:end] + addition + text[end:]


def fmt(v):
    return f"{v:.3f}".rstrip("0").rstrip(".")


def write_svg(svg_text, new_nodes, new_edges, renames):
    out = svg_text
    for _floor, old, new, _src in renames:
        assert out.count(f'id="{old}"') == 1, old
        out = out.replace(f'id="{old}"', f'id="{new}"', 1)
        out = out.replace(f'inkscape:label="{old}"', f'inkscape:label="{new}"', 1)
    for floor in sorted({n[0] for n in new_nodes}):
        add = "".join(
            f'<circle\n         style="{CIRCLE_STYLE}"\n         id="{pid}"\n         cx="{fmt(x)}"\n         cy="{fmt(y)}"\n'
            f'         r="2.375"\n         inkscape:label="{pid}" />'
            for f, pid, x, y, _ in new_nodes if f == floor
        )
        out = insert_before_group_end(out, f"nodes_{floor}f", add)
    for floor in sorted({e[0] for e in new_edges}):
        add = "".join(
            f'<path\n         style="{PATH_STYLE}"\n         d="M {fmt(a[0])},{fmt(a[1])} L {fmt(b[0])},{fmt(b[1])}"\n'
            f'         id="link_{pid}" />'
            for f, pid, _a_id, a, _b_id, b, _ in new_edges if f == floor
        )
        out = insert_before_group_end(out, f"edges_{floor}f", add)
    return out


def edge_pairs(nodes):
    return {(n.id, m.id) for n in nodes for m, _ in n.edges}


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("kcct", type=Path, help="KCCT-SiteMap のクローン")
    parser.add_argument("--apply", action="store_true", help="map.svg に書き込む")
    parser.add_argument("--out", type=Path, help="書き込み先 (既定は map.svg。確認用に別のパスも指定できる)")
    args = parser.parse_args()

    new_nodes, new_edges, unresolved, renames = plan(args.kcct)
    print(f"追加するノード {len(new_nodes)} 件 / 付け替え {len(renames)} 件 / つなぐ通路 {len(new_edges)} 本 / 決められない {len(unresolved)} 件\n")
    for f, pid, x, y, src in new_nodes:
        print(f"  + ノード {pid} ({f}F {x:.0f},{y:.0f}) [{src}]")
    for f, pid, a_id, a, b_id, b, src in new_edges:
        print(f"  + 通路   {a_id} -- {b_id}  {math.dist(a, b):.0f}px [{src}]")
    for f, old, new, src in renames:
        print(f"  ~ 付け替え {old} -> {new} ({f}F) [{src}]")
    for pid, name, why in unresolved:
        print(f"  ? 未解決 {pid} ({name}): {why}")

    svg_path = DATA / "map.svg"
    text = svg_path.read_text(encoding="utf-8")
    new_text = write_svg(text, new_nodes, new_edges, renames)

    # 確認: 書き込む内容でグラフを作り直し、既存の通路が壊れていないことを確かめる
    tmp = svg_path.with_name("_map_check.svg")
    tmp.write_text(new_text, encoding="utf-8")
    try:
        errs = []
        before = V.load_graph(svg_path, errs)
        after = V.load_graph(tmp, errs)
    finally:
        tmp.unlink(missing_ok=True)
    rename_map = {old: new for _f, old, new, _s in renames}
    expected = {(rename_map.get(a, a), rename_map.get(b, b)) for a, b in edge_pairs(before)}
    lost = expected - edge_pairs(after)
    print(f"\n既存の通路で失われたもの: {len(lost)} 本")
    for a, b in sorted(lost)[:20]:
        print(f"  - {a} -> {b}")

    if lost:
        print("既存の通路が変わるので書き込まない。")
        return 1
    if not args.apply:
        print("(確認のみ。書き込むには --apply)")
        return 0

    (args.out or svg_path).write_text(new_text, encoding="utf-8")
    print(f"書き込みました: {args.out or svg_path}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
