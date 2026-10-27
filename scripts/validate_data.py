#!/usr/bin/env python3
"""地図データ (src/Data/map.svg と *.json) の整合性を検証する。

SvgGraphLoader.cs と同じ規則で SVG からグラフを組み立て、
- JSON の場所 Id が SVG のノードとして存在するか
- すべての場所が同じ連結成分 (= 互いに経路が引ける) に入っているか
- ノードの circle に transform が付いていないか (ローダーは transform を無視する)
- Id の重複がないか
を確認する。--optimality を付けると A* の結果を Dijkstra と突き合わせる。

エラーが 1 件でもあれば終了コード 1 で終わる (CI 用)。
"""

import argparse
import heapq
import json
import math
import random
import re
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path

SVG_NS = "{http://www.w3.org/2000/svg}"

# SvgGraphLoader.cs と揃える定数
SNAP_DISTANCE = 20.0
VERTICAL_RANGE = 60.0
STAIR_EXTRA_COST = 30.0
ELEVATOR_EXTRA_COST = 10000.0


class Node:
    def __init__(self, node_id, x, y, floor):
        self.id = node_id
        self.x = x
        self.y = y
        self.floor = floor
        self.edges = []  # (隣接ノード, コスト)


def parse_floor(layer_id):
    digits = "".join(c for c in layer_id if c.isdigit())
    return int(digits) if digits else 1


def parse_path_points(d):
    """SKPath.ParseSvgPathData(d).Points と同じ点列を返す (M/L/H/V/C/Z に対応)。"""
    tokens = re.findall(r"[A-Za-z]|[-+]?(?:\d*\.\d+|\d+\.?)(?:[eE][-+]?\d+)?", d)
    points = []
    i = 0
    cx = cy = sx = sy = 0.0
    cmd = None

    def num():
        nonlocal i
        value = float(tokens[i])
        i += 1
        return value

    while i < len(tokens):
        if tokens[i].isalpha():
            cmd = tokens[i]
            i += 1
        rel = cmd.islower()
        c = cmd.upper()
        if c == "Z":
            cx, cy = sx, sy
        elif c in ("M", "L"):
            x, y = num(), num()
            if rel:
                x, y = x + cx, y + cy
            cx, cy = x, y
            if c == "M":
                sx, sy = x, y
                cmd = "l" if rel else "L"  # M に続く座標は L 扱い
            points.append((x, y))
        elif c == "H":
            x = num()
            cx = cx + x if rel else x
            points.append((cx, cy))
        elif c == "V":
            y = num()
            cy = cy + y if rel else y
            points.append((cx, cy))
        elif c == "C":
            p = [num() for _ in range(6)]
            if rel:
                p = [v + (cx if k % 2 == 0 else cy) for k, v in enumerate(p)]
            points += [(p[0], p[1]), (p[2], p[3]), (p[4], p[5])]
            cx, cy = p[4], p[5]
        else:
            raise ValueError(f"未対応のパスコマンド: {cmd}")
    return points


def distance(a, b):
    """MapGeometry.Distance と同じ (エッジのコスト計算に使う)。"""
    d = math.hypot(a.x - b.x, a.y - b.y)
    if a.floor != b.floor:
        d += 1000
    return d


def heuristic(a, b):
    """MapGeometry.Heuristic と同じ (A* の推定値)。"""
    return math.hypot(a.x - b.x, a.y - b.y) + abs(a.floor - b.floor) * STAIR_EXTRA_COST


def path_length(a, b, points):
    if len(points) < 2:
        return distance(a, b)
    length = math.hypot(a.x - points[0][0], a.y - points[0][1])
    for p, q in zip(points, points[1:]):
        length += math.hypot(p[0] - q[0], p[1] - q[1])
    return length + math.hypot(points[-1][0] - b.x, points[-1][1] - b.y)


def connect(a, b, cost):
    a.edges.append((b, cost))
    b.edges.append((a, cost))


def load_graph(svg_path, errors):
    root = ET.parse(svg_path).getroot()
    nodes = []

    for group in root.iter(SVG_NS + "g"):
        gid = group.get("id", "")
        if not gid.startswith("nodes_"):
            continue
        floor = parse_floor(gid)
        for circle in group.iter(SVG_NS + "circle"):
            cid = circle.get("id", "")
            if circle.get("transform"):
                errors.append(f"[transform] {cid} ({gid}) に transform=\"{circle.get('transform')}\" があり座標がずれる")
            nodes.append(Node(cid, float(circle.get("cx", "0")), float(circle.get("cy", "0")), floor))

    for group in root.iter(SVG_NS + "g"):
        gid = group.get("id", "")
        if not gid.startswith("edges_"):
            continue
        floor = parse_floor(gid)
        on_floor = [n for n in nodes if n.floor == floor]
        for path in group.iter(SVG_NS + "path"):
            coords = parse_path_points(path.get("d", ""))
            if len(coords) < 2:
                continue
            found = []
            for idx, (x, y) in enumerate(coords):
                near = [n for n in on_floor if abs(n.x - x) < SNAP_DISTANCE and abs(n.y - y) < SNAP_DISTANCE]
                if not near:
                    continue
                node = min(near, key=lambda n: abs(n.x - x) + abs(n.y - y))
                if not found or found[-1][1] is not node:
                    found.append((idx, node))
            for (i0, a), (i1, b) in zip(found, found[1:]):
                connect(a, b, path_length(a, b, coords[i0:i1 + 1]))

    vertical = [n for n in nodes if "stair" in n.id or "elv" in n.id]
    for a in vertical:
        for b in vertical:
            if a is b:
                continue
            if abs(a.floor - b.floor) == 1 and abs(a.x - b.x) < VERTICAL_RANGE and abs(a.y - b.y) < VERTICAL_RANGE:
                is_stair = "stair" in a.id and "stair" in b.id
                extra = STAIR_EXTRA_COST if is_stair else ELEVATOR_EXTRA_COST
                # ローダーは (a,b) と (b,a) の両方で接続するので同じく 2 回つなぐ
                connect(a, b, distance(a, b) + extra)

    return nodes


def components(nodes):
    comp = {}
    for start in nodes:
        if start in comp:
            continue
        cid = len(set(comp.values()))
        comp[start] = cid
        stack = [start]
        while stack:
            u = stack.pop()
            for v, _ in u.edges:
                if v not in comp:
                    comp[v] = cid
                    stack.append(v)
    return comp


def dijkstra(source):
    dist = {source: 0.0}
    queue = [(0.0, id(source), source)]
    while queue:
        d, _, u = heapq.heappop(queue)
        if d > dist[u]:
            continue
        for v, c in u.edges:
            if d + c < dist.get(v, math.inf):
                dist[v] = d + c
                heapq.heappush(queue, (d + c, id(v), v))
    return dist


def astar(start, goal):
    """PathFinder.FindPath と同じ手順 (closed set 付き、再 Enqueue 方式)。"""
    g = {start: 0.0}
    closed = set()
    counter = 0
    queue = [(heuristic(start, goal), counter, start)]
    while queue:
        _, _, u = heapq.heappop(queue)
        if u in closed:
            continue
        closed.add(u)
        if u is goal:
            return g[u]
        for v, c in u.edges:
            if v in closed:
                continue
            ng = g[u] + c
            if ng < g.get(v, math.inf):
                g[v] = ng
                counter += 1
                heapq.heappush(queue, (ng + heuristic(v, goal), counter, v))
    return None


def main():
    repo = Path(__file__).resolve().parent.parent
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--data-dir", type=Path, default=repo / "src" / "team1" / "Data")
    parser.add_argument("--optimality", action="store_true", help="A* と Dijkstra の結果を突き合わせる")
    parser.add_argument("--samples", type=int, default=40, help="--optimality で使う出発地の数")
    args = parser.parse_args()

    errors = []
    nodes = load_graph(args.data_dir / "map.svg", errors)
    by_id = {}
    for n in nodes:
        if n.id in by_id:
            errors.append(f"[重複] SVG のノード id {n.id} が複数ある")
        by_id[n.id] = n

    places = []
    for json_path in sorted(args.data_dir.glob("*.json")):
        for place in json.loads(json_path.read_text(encoding="utf-8")):
            place["_file"] = json_path.name
            places.append(place)

    for place_id, count in Counter(p["Id"] for p in places).items():
        if count > 1:
            errors.append(f"[重複] 場所 Id {place_id} が JSON に {count} 件ある")

    comp = components(nodes)
    main_comp = Counter(comp[by_id[p["Id"]]] for p in places if p["Id"] in by_id).most_common(1)[0][0]

    for p in places:
        if p.get("Hidden"):
            continue  # 非表示の場所はノードが無くてもよい
        node = by_id.get(p["Id"])
        if node is None:
            errors.append(f"[ノードなし] {p['Id']} ({p['_file']}: {p['Name']}) が map.svg に無い")
        elif node.floor != p["Floor"]:
            errors.append(f"[階不一致] {p['Id']} は JSON で {p['Floor']}F、SVG で {node.floor}F")
        elif comp[node] != main_comp:
            errors.append(f"[非連結] {p['Id']} ({p['Name']}) から主要な通路へ経路が無い")

    # 雨の日の経路で避ける屋外の通路 (outdoor_edges.txt)。存在しない ID があると黙って効かなくなる
    outdoor_path = args.data_dir / "outdoor_edges.txt"
    if outdoor_path.exists():
        for line in outdoor_path.read_text(encoding="utf-8").splitlines():
            parts = line.split("#")[0].split()
            if not parts:
                continue
            if len(parts) != 2:
                errors.append(f"[屋外の通路の書式] outdoor_edges.txt の行 \"{line}\" は 2 つのノード ID ではない")
                continue
            for oid in parts:
                if oid not in by_id:
                    errors.append(f"[屋外ノードなし] outdoor_edges.txt の {oid} が map.svg に無い")

    if args.optimality:
        routable = [by_id[p["Id"]] for p in places if p["Id"] in by_id and comp[by_id[p["Id"]]] == main_comp]
        rng = random.Random(0)
        for source in rng.sample(routable, min(args.samples, len(routable))):
            best = dijkstra(source)
            for target in routable:
                if target is source:
                    continue
                found = astar(source, target)
                if found is None or found > best[target] + 1e-3:
                    errors.append(f"[最適性] {source.id} → {target.id}: A*={found} Dijkstra={best[target]:.1f}")

    for e in errors:
        print(e)
    print(f"ノード {len(nodes)} / 場所 {len(places)} / エラー {len(errors)} 件")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
