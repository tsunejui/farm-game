#!/usr/bin/env python3
"""
separate_objects.py — 物件提取與自動去背工具

用途:
    從一張包含多個物件且帶有漸層背景的圖片中，自動偵測背景顏色，
    將其轉為透明，並利用連通區域演算法將每個獨立物件分離成獨立的 PNG。

功能亮點:
    1. 自動背景採樣: 從圖片角落與邊緣採樣，能有效處理漸層背景。
    2. 連通性偵測: 透過洪水填充(Flood Fill)區分「背景」與「物件內部顏色」，
       避免物件內部的顏色（如雪樹的白色）被誤刪。
    3. 軟邊緣處理: 支援漸變透明度，減少邊緣鋸齒。

用法:
    python3 tools/separate_objects.py <圖片路徑> <輸出目錄> [門檻值]

範例:
    python3 tools/separate_objects.py example/images/tree/tree.png example/images/tree/extracted 60
"""

import os
import sys
import math
from PIL import Image, ImageChops, ImageDraw

def separate_with_auto_bg(image_path, output_dir, threshold=50, soft_range=30, min_size=(16, 16)):
    if not os.path.exists(image_path):
        print(f"Error: File {image_path} not found.")
        return

    img = Image.open(image_path).convert("RGBA")
    width, height = img.size
    
    # 1. 從四個角落與邊緣中點採樣背景色
    seeds = [
        (0, 0), (width-1, 0), (0, height-1), (width-1, height-1),
        (width//2, 5), (5, height//2), (width-6, height//2), (width//2, height-6)
    ]
    bg_samples = [img.getpixel(s)[:3] for s in seeds]
    
    # 2. 建立距離遮罩: 計算每個像素到最接近背景樣本的距離
    r, g, b, _ = img.split()
    r_data = list(r.getdata())
    g_data = list(g.getdata())
    b_data = list(b.getdata())
    
    dist_data = []
    for i in range(len(r_data)):
        rv, gv, bv = r_data[i], g_data[i], b_data[i]
        min_dist = 1000.0
        for s in bg_samples:
            d = math.sqrt((rv - s[0])**2 + (gv - s[1])**2 + (bv - s[2])**2)
            if d < min_dist:
                min_dist = d
        dist_data.append(int(min(255, min_dist)))
        
    dist_img = Image.new('L', img.size)
    dist_img.putdata(dist_data)
    
    # 3. 二值化初步背景遮罩
    potential_bg_mask = dist_img.point(lambda p: 255 if p < threshold else 0)
    
    # 4. 從種子點執行洪水填充，找出真正與邊緣相連的背景
    bg_mask = potential_bg_mask.copy()
    for seed in seeds:
        if bg_mask.getpixel(seed) == 255:
            ImageDraw.floodfill(bg_mask, seed, 128)
            
    # 5. 生成最終 Alpha 並進行去色補償
    new_data = []
    bg_mask_data = list(bg_mask.getdata())
    opaque_point = threshold + soft_range
    
    for i in range(len(r_data)):
        rv, gv, bv = r_data[i], g_data[i], b_data[i]
        bg_val = bg_mask_data[i]
        d_v = dist_data[i]
        
        if bg_val == 128: # 確認為連通背景
            if d_v <= threshold:
                new_data.append((0, 0, 0, 0)) # 全透明
            elif d_v >= opaque_point:
                new_data.append((rv, gv, bv, 255))
            else:
                alpha = int(255 * (d_v - threshold) / (opaque_point - threshold))
                new_data.append((rv, gv, bv, alpha))
        else:
            new_data.append((rv, gv, bv, 255))
            
    img.putdata(new_data)
    final_alpha = img.getchannel('A')
    
    # 6. 分離物件為獨立區域
    scale = 4
    small_alpha = final_alpha.resize((width // scale, height // scale), Image.NEAREST)
    s_mask_data = list(small_alpha.getdata())
    m_w, m_h = small_alpha.size
    
    visited = set()
    objs = []
    for y in range(m_h):
        for x in range(m_w):
            if s_mask_data[y * m_w + x] > 10 and (x, y) not in visited:
                q = [(x, y)]; visited.add((x, y))
                min_x, min_y, max_x, max_y = x, y, x, y
                while q:
                    cx, cy = q.pop()
                    min_x = min(min_x, cx); min_y = min(min_y, cy)
                    max_x = max(max_x, cx); max_y = max(max_y, cy)
                    for dx, dy in [(0, 1), (0, -1), (1, 0), (-1, 0)]:
                        nx, ny = cx + dx, cy + dy
                        if 0 <= nx < m_w and 0 <= ny < m_h:
                            if s_mask_data[ny * m_w + nx] > 10 and (nx, ny) not in visited:
                                visited.add((nx, ny))
                                q.append((nx, ny))
                bbox = (min_x * scale, min_y * scale, (max_x + 1) * scale, (max_y + 1) * scale)
                refined = final_alpha.crop(bbox).getbbox()
                if refined:
                    final_bbox = (bbox[0]+refined[0], bbox[1]+refined[1], bbox[0]+refined[2], bbox[1]+refined[3])
                    if (final_bbox[2]-final_bbox[0]) >= min_size[0] and (final_bbox[3]-final_bbox[1]) >= min_size[1]:
                        objs.append(final_bbox)

    # 7. 儲存結果
    if not os.path.exists(output_dir): os.makedirs(output_dir)
    base_name = os.path.splitext(os.path.basename(image_path))[0]
    for i, bbox in enumerate(objs):
        obj_img = img.crop(bbox)
        out_path = os.path.join(output_dir, f"{base_name}_{i:03d}.png")
        obj_img.save(out_path)
    
    print(f"Success: Extracted {len(objs)} objects from {image_path} to {output_dir}")

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python separate_objects.py <image_path> <output_dir> [threshold]")
        sys.exit(1)
    
    img_p = sys.argv[1]
    out_p = sys.argv[2]
    thr = int(sys.argv[3]) if len(sys.argv) > 3 else 50
    
    separate_with_auto_bg(img_p, out_p, thr)
