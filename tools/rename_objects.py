#!/usr/bin/env python3
"""
rename_objects.py — 批次重新編號工具

用途:
    將指定目錄下的所有 PNG 檔案按照字母順序重新命名為統一格式（如 prefix_001.png）。
    這通常用於填補清理碎屑後產生的編號空缺，讓資產編號保持連續。

用法:
    python3 tools/rename_objects.py <目標目錄> [字首]

範例:
    python3 tools/rename_objects.py example/images/tree/extracted tree
"""

import os

def rename_sequentially(directory, prefix="tree"):
    if not os.path.exists(directory):
        print(f"Directory {directory} not found.")
        return

    files = [f for f in os.listdir(directory) if f.endswith('.png')]
    files.sort()
    
    print(f"Renaming {len(files)} files in {directory} with prefix '{prefix}'...")
    
    # 1. 使用臨時名稱以避免更名過程中產生衝突
    temp_prefix = "temp_rename_v2_"
    for i, filename in enumerate(files):
        old_path = os.path.join(directory, filename)
        temp_name = f"{temp_prefix}{i:03d}.png"
        temp_path = os.path.join(directory, temp_name)
        os.rename(old_path, temp_path)
    
    # 2. 正式命名為目標格式 (從 001 開始)
    temp_files = [f for f in os.listdir(directory) if f.startswith(temp_prefix)]
    temp_files.sort()
    
    for i, filename in enumerate(temp_files):
        old_path = os.path.join(directory, filename)
        new_name = f"{prefix}_{i+1:03d}.png"
        new_path = os.path.join(directory, new_name)
        os.rename(old_path, new_path)
        
    print(f"Renaming complete for {directory}.")

if __name__ == "__main__":
    import sys
    target_dir = sys.argv[1] if len(sys.argv) > 1 else "example/images/tree/extracted"
    pfx = sys.argv[2] if len(sys.argv) > 2 else "tree"
    rename_sequentially(target_dir, pfx)
