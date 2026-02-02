#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
CSVからJSONへの変換スクリプト
施設一覧のCSVファイルを読み込み、JSON形式に変換します。
"""

import csv
import json
import re
import unicodedata
from pathlib import Path
from typing import Optional


# CSVファイルとDepartmentのマッピング
CSV_MAPPING = {
    "施設一覧 - 機械工学科.csv": ("mech", "機械工学科"),
    "施設一覧 - 電気工学科.csv": ("elec", "電気工学科"),
    "施設一覧 - 電子工学科.csv": ("denshi", "電子工学科"),
    "施設一覧 - 応用化学科.csv": ("chem", "応用化学科"),
    "施設一覧 - 都市工学科.csv": ("toshi", "都市工学科"),
    "施設一覧 - 一般科.csv": ("general", "一般科"),
    "施設一覧 - 専攻科.csv": ("advanced", "専攻科"),
    "施設一覧 - ホームルーム.csv": ("common", "共通"),
    # 教員研究室は別処理
}

# カテゴリからTypeへのマッピング
CATEGORY_TO_TYPE = {
    "技術職員室": "office_tech",
    "実験室・教室": "lab",  # デフォルト、室名で判断
    "卒研室": "lab_grad",
    "教室": "classroom",
    "ホームルーム": "homeroom",
}


def normalize_text(text: str) -> str:
    """テキストを正規化する（全角→半角など）"""
    return unicodedata.normalize("NFKC", text.strip())


def parse_floor(floor_str: str) -> int:
    """階数の文字列から数値を抽出する (例: "2F" -> 2, "B1F" -> -1)"""
    floor_str = normalize_text(floor_str).upper()
    match = re.match(r"B?(\d+)F?", floor_str)
    if match:
        floor_num = int(match.group(1))
        if floor_str.startswith("B"):
            return -floor_num
        return floor_num
    return 1  # デフォルト


def generate_type_from_name(room_name: str, category: str) -> str:
    """室名からTypeを推測する"""
    room_name = normalize_text(room_name)
    category = normalize_text(category)
    
    # カテゴリベースの判定
    if category == "技術職員室":
        return "office_tech"
    if category == "卒研室":
        return "lab_grad"
    if category == "ホームルーム":
        return "homeroom"
    
    # 室名ベースの判定
    if "実験室" in room_name or "実験" in room_name:
        return "lab"
    if "教室" in room_name or "演習室" in room_name:
        return "classroom"
    if "研究室" in room_name:
        return "lab_faculty"
    if "工場" in room_name:
        return "factory"
    if "ホール" in room_name or "会議室" in room_name:
        return "facility"
    if "ホームルーム" in room_name or re.match(r"\d+[A-Z]", room_name):
        return "homeroom"
    
    return "facility"  # デフォルト


def generate_id(floor: int, dept: str, room_type: str, room_name: str, index: int) -> str:
    """一意のIDを生成する"""
    # 簡易的なID生成（必要に応じてカスタマイズ）
    floor_str = f"{floor}f" if floor > 0 else f"b{abs(floor)}f"
    
    # 室名から英語のスラッグを生成（簡易版）
    slug = room_name.lower()
    # 数字を保持
    nums = re.findall(r'\d+', slug)
    num_suffix = "_".join(nums) if nums else ""
    
    # タイプ名を使用
    type_slug = room_type.replace("_", "")
    
    return f"room_{floor_str}_{dept}_{type_slug}_{index}"


def generate_tags(room_name: str, building: str, dept_name: str, dept_code: str, room_type: str) -> list:
    """タグを生成する"""
    tags = [room_name, building, dept_name]
    
    if dept_code:
        tags.append(dept_code)
    
    # タイプに応じた追加タグ
    if room_type == "lab":
        tags.append("実験室")
    elif room_type == "classroom":
        tags.append("教室")
    elif room_type == "lab_grad":
        tags.append("卒研室")
    elif room_type == "lab_faculty":
        tags.append("研究室")
    
    return tags


def convert_facility_csv(csv_path: Path, dept_code: str, dept_name: str) -> list:
    """施設CSVを変換する"""
    rooms = []
    
    with open(csv_path, 'r', encoding='utf-8-sig') as f:
        reader = csv.reader(f)
        rows = list(reader)
    
    # ヘッダー行を探す
    header_idx = None
    for i, row in enumerate(rows):
        if len(row) >= 5 and "カテゴリ" in row:
            header_idx = i
            break
    
    if header_idx is None:
        print(f"Warning: ヘッダーが見つかりません: {csv_path}")
        return rooms
    
    # データ行を処理
    index = 1
    for row in rows[header_idx + 1:]:
        # 空行またはヘッダー行をスキップ
        if len(row) < 5:
            continue
        
        # 実際のデータ列を取得（最初の列が空の場合がある）
        data = [cell.strip() for cell in row if cell.strip()]
        if len(data) < 4:
            continue
        
        # カテゴリが空または別のヘッダー行ならスキップ
        if not data[0] or data[0] == "カテゴリ" or data[0] == "学科":
            continue
        
        category = data[0]
        building = data[1] if len(data) > 1 else ""
        floor_str = data[2] if len(data) > 2 else "1F"
        room_name = data[3] if len(data) > 3 else ""
        note = data[4] if len(data) > 4 else ""
        
        if not room_name:
            continue
        
        floor = parse_floor(floor_str)
        room_type = generate_type_from_name(room_name, category)
        
        room = {
            "Id": generate_id(floor, dept_code, room_type, room_name, index),
            "Name": room_name,
            "Type": room_type,
            "Building": building,
            "Floor": floor,
            "Department": dept_code,
            "Tags": generate_tags(room_name, building, dept_name, dept_code, room_type)
        }
        
        if note:
            room["Note"] = note
        
        rooms.append(room)
        index += 1
    
    return rooms


def convert_faculty_csv(csv_path: Path) -> dict:
    """教員研究室CSVを変換する（学科ごとに分類）"""
    faculty_by_dept = {}
    
    # 学科名からdept_codeへのマッピング
    dept_mapping = {
        "機械工学科": "mech",
        "電気工学科": "elec",
        "電子工学科": "denshi",
        "応用化学科": "chem",
        "都市工学科": "toshi",
        "一般科目": "general",
        "一般科": "general",
    }
    
    with open(csv_path, 'r', encoding='utf-8-sig') as f:
        reader = csv.reader(f)
        rows = list(reader)
    
    current_dept = None
    index_by_dept = {}
    
    for row in rows:
        # 空行をスキップ
        data = [cell.strip() for cell in row if cell.strip()]
        if len(data) < 4:
            continue
        
        # ヘッダー行をスキップ
        if "学科" in data[0] and "棟" in data:
            continue
        
        # 学科が変わったか確認
        dept_name = data[0]
        if dept_name in dept_mapping:
            current_dept = dept_mapping[dept_name]
            if current_dept not in faculty_by_dept:
                faculty_by_dept[current_dept] = []
                index_by_dept[current_dept] = 1
        
        if current_dept is None:
            continue
        
        building = data[1] if len(data) > 1 else ""
        floor_str = data[2] if len(data) > 2 else "1F"
        room_name = data[3] if len(data) > 3 else ""
        teacher = data[4] if len(data) > 4 else ""
        
        if not room_name or room_name == "研究室名":
            continue
        
        floor = parse_floor(floor_str)
        
        room = {
            "Id": generate_id(floor, current_dept, "lab_faculty", room_name, index_by_dept[current_dept]),
            "Name": room_name,
            "Type": "lab_faculty",
            "Building": building,
            "Floor": floor,
            "Department": current_dept,
            "Teacher": teacher,
            "Tags": [room_name, building, dept_name, current_dept, "研究室", teacher]
        }
        
        faculty_by_dept[current_dept].append(room)
        index_by_dept[current_dept] += 1
    
    return faculty_by_dept


def save_json(data: list, output_path: Path):
    """JSONファイルを保存する"""
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    print(f"Saved: {output_path} ({len(data)} items)")


def main():
    """メイン処理"""
    script_dir = Path(__file__).parent
    
    # 施設CSVを処理
    for csv_name, (dept_code, dept_name) in CSV_MAPPING.items():
        csv_path = script_dir / csv_name
        if not csv_path.exists():
            print(f"Skip: {csv_name} (not found)")
            continue
        
        rooms = convert_facility_csv(csv_path, dept_code, dept_name)
        output_path = script_dir / f"{dept_code}.json"
        save_json(rooms, output_path)
    
    # 教員研究室CSVを処理
    faculty_csv = script_dir / "施設一覧 - 教員研究室.csv"
    if faculty_csv.exists():
        faculty_by_dept = convert_faculty_csv(faculty_csv)
        
        # 各学科のJSONに研究室を追加するか、別ファイルとして保存
        faculty_output = script_dir / "faculty.json"
        all_faculty = []
        for dept_rooms in faculty_by_dept.values():
            all_faculty.extend(dept_rooms)
        save_json(all_faculty, faculty_output)
        
        print(f"\n教員研究室を学科別に追加する場合は、手動で各JSONにマージしてください。")
        print(f"または faculty.json を参照してください。")


if __name__ == "__main__":
    main()
