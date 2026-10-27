"""KCCT-SiteMap の座標を team1 の map.svg の座標系 (2560x1600) に合わせる変換を求める。

KCCT の FacilityData.cs (部屋名 -> ノード ID) と GraphData.json (ノード ID -> 階・座標) から、
team1 の場所データ (部屋名 -> map.svg のノード座標) と同じ部屋を探して対応点にし、
階ごとに最小二乗で相似変換 (拡大・回転・平行移動) を当てはめて、残差を表示する。

使い方: python scripts/register_maps.py <KCCT-SiteMap のクローン>
"""
import json
import math
import re
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path

SVG_NS = "{http://www.w3.org/2000/svg}"
DATA = Path(__file__).resolve().parent.parent / "src" / "team1" / "Data"


def kcct_points(root):
    graph = json.loads((root / "SiteMapApp/Data/GraphData.json").read_text(encoding="utf-8-sig"))
    nodes = {n["NId"]: n for n in graph["Nodes"]}
    src = (root / "SiteMapApp/Data/FacilityData.cs").read_text(encoding="utf-8-sig")
    out = []  # (名前, 階, x, y)
    for name, _rid, nid in re.findall(r'new\(\s*"([^"]+)"\s*,\s*"([^"]*)"\s*,\s*"(N\d+)"', src):
        n = nodes.get(nid)
        if n:
            out.append((name.strip(), n["Floor"], n["X"], n["Y"]))
    return out


def team1_points():
    circles = {}
    root = ET.parse(DATA / "map.svg").getroot()
    for g in root.iter(SVG_NS + "g"):
        gid = g.get("id", "")
        if gid.startswith("nodes_"):
            for c in g.iter(SVG_NS + "circle"):
                circles[c.get("id")] = (float(c.get("cx")), float(c.get("cy")))
    out = []
    for p in sorted(DATA.glob("*.json")):
        for place in json.loads(p.read_text(encoding="utf-8")):
            if place["Id"] in circles and not place.get("Hidden"):
                out.append((place["Name"].strip(), place["Floor"], *circles[place["Id"]]))
    return out


def fit_similarity(pairs):
    """pairs: [((x, y) 元, (X, Y) 先)]。X = a*x - b*y + tx, Y = b*x + a*y + ty の最小二乗"""
    n = len(pairs)
    mx = sum(p[0][0] for p in pairs) / n
    my = sum(p[0][1] for p in pairs) / n
    mX = sum(p[1][0] for p in pairs) / n
    mY = sum(p[1][1] for p in pairs) / n
    num_a = num_b = den = 0.0
    for (x, y), (X, Y) in pairs:
        x -= mx; y -= my; X -= mX; Y -= mY
        num_a += x * X + y * Y
        num_b += x * Y - y * X
        den += x * x + y * y
    a, b = num_a / den, num_b / den
    tx = mX - (a * mx - b * my)
    ty = mY - (b * mx + a * my)
    return a, b, tx, ty


def apply(t, x, y):
    a, b, tx, ty = t
    return a * x - b * y + tx, b * x + a * y + ty


def robust_fit(pairs):
    """外れ値を除きながら相似変換を当てはめる。(残った対応点, 変換) を返す。

    名前が同じでも別の部屋 (同名の研究室など) が混じるので、残差の大きい点を外して当てはめ直す。
    """
    ps = pairs
    tr = fit_similarity(ps)
    for _ in range(3):
        res = [math.dist(apply(tr, *s), d) for s, d in ps]
        limit = max(2 * sorted(res)[len(res) // 2], 15)
        kept = [p for p, r in zip(ps, res) if r <= limit]
        if len(kept) < 3 or len(kept) == len(ps):
            break
        ps, tr = kept, fit_similarity(kept)
    return ps, tr


def matched_pairs(kcct_root):
    """同名・同階の部屋が KCCT と team1 に 1 件ずつあるものを、階ごとの対応点にして返す"""
    k = kcct_points(kcct_root)
    t = team1_points()
    by_key = defaultdict(list)
    for name, floor, x, y in t:
        by_key[(name, floor)].append((x, y))
    k_by_key = defaultdict(list)
    for name, floor, x, y in k:
        k_by_key[(name, floor)].append((x, y))
    pairs = defaultdict(list)
    for key, kv in k_by_key.items():
        tv = by_key.get(key)
        if len(kv) == 1 and tv and len(tv) == 1:
            pairs[key[1]].append((kv[0], tv[0]))
    return pairs


def floor_transforms(kcct_root):
    """階ごとの KCCT -> team1 の変換 {階: (a, b, tx, ty)}。対応点が 3 組未満の階は含めない"""
    return {f: robust_fit(ps)[1] for f, ps in matched_pairs(kcct_root).items() if len(ps) >= 3}


def main():
    root = Path(sys.argv[1])
    k = kcct_points(root)
    t = team1_points()

    by_key = defaultdict(list)
    for name, floor, x, y in t:
        by_key[(name, floor)].append((x, y))
    k_by_key = defaultdict(list)
    for name, floor, x, y in k:
        k_by_key[(name, floor)].append((x, y))

    # 両方で 1 件だけの同名・同階の部屋を対応点にする
    pairs = defaultdict(list)
    for key, kv in k_by_key.items():
        tv = by_key.get(key)
        if len(kv) == 1 and tv and len(tv) == 1:
            pairs[key[1]].append((kv[0], tv[0]))

    print(f"KCCT の部屋 {len(k)} / team1 の部屋 {len(t)}")
    allpairs = [p for v in pairs.values() for p in v]
    print(f"対応点 {len(allpairs)} 組 (階別: " + ", ".join(f"{f}F={len(v)}" for f, v in sorted(pairs.items())) + ")")
    if len(allpairs) < 3:
        return

    def report(label, ps):
        ps, tr = robust_fit(ps)
        if "--emit" in sys.argv:
            print(f"EMIT {label} {tr[0]:.6f} {tr[1]:.6f} {tr[2]:.3f} {tr[3]:.3f}")
        res = sorted(math.dist(apply(tr, *s), d) for s, d in ps)
        a, b = tr[0], tr[1]
        print(f"{label}: 拡大 {math.hypot(a, b):.4f} 回転 {math.degrees(math.atan2(b, a)):.2f}° 移動 ({tr[2]:.1f}, {tr[3]:.1f})"
              f"  残差 中央値 {res[len(res)//2]:.1f}px 最大 {res[-1]:.1f}px (n={len(ps)})")
        return tr

    print("--- 全階まとめて ---")
    report("全体", allpairs)
    print("--- 階ごと ---")
    for f, ps in sorted(pairs.items()):
        if len(ps) >= 3:
            report(f"{f}F", ps)


if __name__ == "__main__":
    main()
