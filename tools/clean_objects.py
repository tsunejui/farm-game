#!/usr/bin/env python3
"""
clean_objects.py — 無效碎片清理工具

用途:
    掃描指定目錄下的 PNG 檔案，根據尺寸與像素密度自動刪除「太小」或「太稀疏」的圖片。
    這在自動去背提取後非常有用，可以用來過濾掉細微的雜訊或破碎的背景殘留。

篩選標準:
    1. 尺寸過濾: 寬或高小於 min_dim (預設 32 像素) 的檔案會被刪除。
    2. 密度過濾: 非透明像素佔總面積比例低於 min_density (預設 10%) 的檔案會被刪除。

用法:
    python3 tools/clean_objects.py <目標目錄>

範例:
    python3 tools/clean_objects.py example/images/tree/extracted
"""

import os
from PIL import Image

def clean_extracted_objects(directory, min_dim=32, min_density=0.1):
    if not os.path.exists(directory):
        print(f"Directory {directory} not found.")
        return

    files = [f for f in os.listdir(directory) if f.endswith('.png')]
    deleted_count = 0
    remaining_count = 0
    
    print(f"Scanning {len(files)} files in {directory}...")
    
    for filename in files:
        path = os.path.join(directory, filename)
        try:
            img = Image.open(path).convert("RGBA")
            w, h = img.size
            
            # 1. 尺寸檢查
            if w < min_dim or h < min_dim:
                os.remove(path)
                deleted_count += 1
                continue
            
            # 2. 密度檢查 (計算非透明像素比例)
            alpha = img.getchannel('A')
            non_zero = 0
            alpha_data = list(alpha.getdata())
            for a in alpha_data:
                if a > 0:
                    non_zero += 1
            
            density = non_zero / (w * h)
            if density < min_density:
                os.remove(path)
                deleted_count += 1
                continue
                
            remaining_count += 1
            
        except Exception as e:
            print(f"Error processing {filename}: {e}")
            
    print(f"Cleanup complete for {directory}.")
    print(f"Deleted: {deleted_count} fragments.")
    print(f"Remaining: {remaining_count} valid objects.")

if __name__ == "__main__":
    import sys
    target_dir = sys.argv[1] if len(sys.argv) > 1 else "example/images/tree/extracted"
    clean_extracted_objects(target_dir)
