#!/usr/bin/env python3
"""
compress_and_yaml.py — 壓縮與像素 YAML 生成工具

用途:
    將 PNG 圖片縮小至指定尺寸，進行顏色量化以符合像素藝術風格，
    並自動生成專案規範的 YAML 定義檔案（包含自動生成的調色盤與像素字元矩陣）。

功能亮點:
    1. 高品質縮放: 使用 LANCZOS 濾鏡縮小圖片，保留物件細節。
    2. 顏色量化: 自動將顏色減少至指定數量（預設 32 色），讓 YAML 調色盤保持整潔。
    3. 安全字元集: 使用小寫字母與數字作為像素代號，避免觸發 GitHub 的秘密掃描誤判。
    4. 自動去背轉化: 將透明像素自動轉化為 YAML 中的 '.' 符號。

用法:
    python3 tools/compress_and_yaml.py <來源目錄> <壓縮輸出目錄> <YAML輸出目錄>

範例:
    python3 tools/compress_and_yaml.py example/images/tree/extracted example/images/tree/compressed example/images/tree/yaml
"""

import os
import sys
from PIL import Image
import yaml

def compress_and_to_yaml(png_path, compressed_dir, yaml_dir, name, max_dim=128, color_count=32):
    if not os.path.exists(png_path): return
    
    img = Image.open(png_path).convert("RGBA")
    w, h = img.size
    
    # 1. 縮放圖片（保持長寬比）
    if max(w, h) > max_dim:
        scale = max_dim / max(w, h)
        new_w = int(w * scale)
        new_h = int(h * scale)
        img = img.resize((new_w, new_h), Image.Resampling.LANCZOS)
    
    # 2. 儲存壓縮後的 PNG
    os.makedirs(compressed_dir, exist_ok=True)
    comp_path = os.path.join(compressed_dir, f"{name}.png")
    img.save(comp_path)
    
    # 3. 顏色量化（優化調色盤）
    alpha = img.getchannel('A')
    img_rgb = img.convert("RGB").quantize(colors=color_count).convert("RGB")
    
    # 4. 生成 YAML 像素矩陣
    width, height = img_rgb.size
    # 使用安全字元集，避免 AWS Key 等誤判
    chars = "abcdefghijklmnopqrstuvwxyz0123456789"
    color_to_char = {}
    char_to_hex = {}
    char_index = 0
    pixel_matrix = []
    
    for y in range(height):
        row = ""
        for x in range(width):
            r, g, b = img_rgb.getpixel((x, y))
            a_v = alpha.getpixel((x, y))
            
            if a_v < 128:
                row += "."
            else:
                hex_color = f"#{r:02x}{g:02x}{b:02x}"
                if hex_color not in color_to_char:
                    if char_index < len(chars):
                        c = chars[char_index]
                        color_to_char[hex_color] = c
                        char_to_hex[c] = hex_color
                        char_index += 1
                    else:
                        c = "?" 
                row += color_to_char.get(hex_color, "?")
        pixel_matrix.append(row)

    # 5. 建構並輸出 YAML 結構
    yaml_data = {
        "metadata": {"name": name},
        "type": "png",
        "output_dir": f"Images/extracted_{name.split('_')[0]}s",
        "palette": char_to_hex,
        "data": ["\n" + "\n".join(pixel_matrix) + "\n"]
    }

    class LiteralDumper(yaml.SafeDumper):
        def represent_data(self, data):
            if isinstance(data, str) and "\n" in data:
                return self.represent_scalar('tag:yaml.org,002:str', data, style='|')
            return super().represent_data(data)

    os.makedirs(yaml_dir, exist_ok=True)
    yaml_path = os.path.join(yaml_dir, f"{name}.yaml")
    with open(yaml_path, "w") as f:
        yaml.dump(yaml_data, f, Dumper=LiteralDumper, sort_keys=False, allow_unicode=True)

    return name

def process_all(src_dir, comp_dir, yaml_dir):
    if not os.path.exists(src_dir): return
    files = [f for f in sorted(os.listdir(src_dir)) if f.endswith(".png")]
    for f in files:
        name = os.path.splitext(f)[0]
        compress_and_to_yaml(os.path.join(src_dir, f), comp_dir, yaml_dir, name)
        print(f"Processed YAML for: {name}")

if __name__ == "__main__":
    import sys
    src = sys.argv[1] if len(sys.argv) > 1 else "example/images/tree/extracted"
    comp = sys.argv[2] if len(sys.argv) > 2 else "example/images/tree/compressed"
    y_dir = sys.argv[3] if len(sys.argv) > 3 else "example/images/tree/yaml"
    process_all(src, comp, y_dir)
