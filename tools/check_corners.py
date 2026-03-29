#!/usr/bin/env python3
"""
check_corners.py — 顏色採樣檢查工具

用途:
    快速檢查圖片四個角落與邊緣點的 RGB 顏色值。
    這在手動去背提取前非常有用，可以幫助判斷背景是否為純色、
    漸層的程度，以及應該設定多少門檻值。

用法:
    python3 tools/check_corners.py <圖片路徑>

範例:
    python3 tools/check_corners.py example/images/tree/tree.png
"""

from PIL import Image
import sys
import os

def check_corners(path):
    if not os.path.exists(path):
        print(f"Error: File {path} not found.")
        return

    img = Image.open(path).convert("RGBA")
    w, h = img.size
    samples = [
        ("Top-Left", (0, 0)), 
        ("Top-Right", (w-1, 0)), 
        ("Bottom-Left", (0, h-1)), 
        ("Bottom-Right", (w-1, h-1)), 
        ("Top-Mid", (w//2, 5)), 
        ("Left-Mid", (5, h//2))
    ]
    
    print(f"Sampling colors for: {path} ({w}x{h})")
    for label, pos in samples:
        color = img.getpixel(pos)
        print(f"  {label:12} at {str(pos):10}: {color}")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python3 tools/check_corners.py <image_path>")
    else:
        check_corners(sys.argv[1])
