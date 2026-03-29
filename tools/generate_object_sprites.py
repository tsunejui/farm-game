"""
Generate detailed pixel art object sprites matching reference quality.
Proportional to tree reference: ~96x128 @ 30 colors for 3x3 tiles.
"""
import os
import math
import random
import yaml
from PIL import Image


def hex_to_rgb(h):
    h = h.lstrip('#')
    return tuple(int(h[i:i+2], 16) for i in (0, 2, 4))


def rgb_to_hex(r, g, b):
    return f'#{max(0,min(255,r)):02x}{max(0,min(255,g)):02x}{max(0,min(255,b)):02x}'


def lerp(a, b, t):
    return a + (b - a) * t


def noise2d(x, y, seed=0):
    n = int(x * 374761 + y * 668265 + seed * 1013) & 0xFFFFFFFF
    n = (n ^ (n >> 13)) * 1274126177
    n = n & 0xFFFFFFFF
    return (n / 0xFFFFFFFF) * 2.0 - 1.0


def smooth_noise(x, y, seed=0, scale=8.0):
    sx, sy = x / scale, y / scale
    ix, iy = int(math.floor(sx)), int(math.floor(sy))
    fx, fy = sx - ix, sy - iy
    fx = fx * fx * (3 - 2 * fx)
    fy = fy * fy * (3 - 2 * fy)
    v00 = noise2d(ix, iy, seed)
    v10 = noise2d(ix+1, iy, seed)
    v01 = noise2d(ix, iy+1, seed)
    v11 = noise2d(ix+1, iy+1, seed)
    return lerp(lerp(v00, v10, fx), lerp(v01, v11, fx), fy)


def fbm(x, y, seed=0, octaves=4, scale=10.0):
    val = 0.0
    amp = 1.0
    s = scale
    for _ in range(octaves):
        val += amp * smooth_noise(x, y, seed, s)
        amp *= 0.5
        s *= 0.5
        seed += 1000
    return val


def dist(x1, y1, x2, y2):
    return math.sqrt((x1-x2)**2 + (y1-y2)**2)


# ────────────────────────────────────────────────────────────
# ROCK
# ────────────────────────────────────────────────────────────
def generate_rock(W, H, variant=0, state='normal'):
    pixels = [[None]*W for _ in range(H)]
    rng = random.Random(42 + variant * 71)
    seed = 100 + variant * 37

    # Rock shape: irregular ellipse with bumps
    cx, cy = W / 2.0, H * 0.55
    rx, ry = W * 0.40, H * 0.38

    # Generate rock outline with noise
    for y in range(H):
        for x in range(W):
            dx = (x - cx) / rx
            dy = (y - cy) / ry
            d = math.sqrt(dx*dx + dy*dy)
            # Noise on edge
            angle = math.atan2(y - cy, x - cx)
            edge_noise = fbm(angle * 3, 0, seed, 3, 2.0) * 0.15
            d += edge_noise

            if d < 1.0:
                # Stone color based on lighting
                lnx = (x - cx) / (W / 2.0)
                lny = (y - cy) / (H / 2.0)
                light = 0.4 + 0.25 * lnx - 0.15 * lny + (1.0 - d) * 0.15
                # Stone texture
                tex = fbm(x * 2, y * 2, seed + 50, 3, 4.0) * 0.1
                light += tex
                # Cracks
                crack = fbm(x * 4, y * 3, seed + 100, 2, 2.0)
                if abs(crack) < 0.05:
                    light -= 0.2

                light = max(0.0, min(1.0, light))

                if state == 'dead':
                    # Crumbled — more cracks, darker
                    if rng.random() < 0.3:
                        continue
                    light *= 0.7
                elif state == 'damaged':
                    if rng.random() < 0.1:
                        continue
                    light *= 0.85

                # Grey stone palette
                base_r = int(lerp(60, 180, light))
                base_g = int(lerp(58, 172, light))
                base_b = int(lerp(54, 160, light))
                # Warm/cool shift
                shift = fbm(x, y, seed + 200, 2, 6.0) * 8
                base_r = max(0, min(255, base_r + int(shift)))
                base_b = max(0, min(255, base_b - int(shift)))

                pixels[y][x] = rgb_to_hex(base_r, base_g, base_b)

    # Shadow underneath
    for x in range(W):
        for dy in range(1, 3):
            sy = int(cy + ry + dy)
            if 0 <= sy < H and pixels[sy][x] is None:
                sdx = abs(x - cx) / rx
                if sdx < 0.9:
                    alpha = (1.0 - sdx) * 0.3
                    v = int(30 + alpha * 20)
                    pixels[sy][x] = rgb_to_hex(v, v+2, v)

    return pixels


# ────────────────────────────────────────────────────────────
# BOX / CRATE
# ────────────────────────────────────────────────────────────
def generate_box(W, H, variant=0, state='normal'):
    pixels = [[None]*W for _ in range(H)]
    rng = random.Random(42 + variant * 83)
    seed = 200 + variant * 41

    # Box shape with slight 3D perspective
    margin = int(W * 0.1)
    top = int(H * 0.15)
    bot = int(H * 0.88)
    left, right = margin, W - margin

    for y in range(top, bot):
        for x in range(left, right):
            rel_x = (x - left) / max(1, right - left - 1)
            rel_y = (y - top) / max(1, bot - top - 1)

            # Wood grain lighting
            light = 0.35 + 0.3 * rel_x - 0.1 * rel_y
            grain = fbm(x * 0.5, y * 3, seed, 3, 4.0) * 0.12
            light += grain
            tex = fbm(x * 2, y * 2, seed + 50, 2, 3.0) * 0.06
            light += tex

            # Board lines (horizontal slats)
            board_y = (y - top) % int((bot - top) / 4)
            if board_y < 1:
                light -= 0.15

            # Vertical edge boards
            if x - left < 2 or right - x < 3:
                light -= 0.08

            # Cross boards
            cx_board = (left + right) // 2
            if abs(x - cx_board) < 2:
                light -= 0.05

            if state == 'dead':
                if rng.random() < 0.2:
                    continue
                light *= 0.6
            elif state == 'damaged':
                if rng.random() < 0.05:
                    continue
                light *= 0.8

            light = max(0.0, min(1.0, light))
            r = int(lerp(50, 180, light))
            g = int(lerp(30, 120, light))
            b = int(lerp(15, 65, light))
            pixels[y][x] = rgb_to_hex(r, g, b)

    # Metal corner/nail accents
    for cy_n, cx_n in [(top+2, left+2), (top+2, right-3), (bot-3, left+2), (bot-3, right-3)]:
        for ddy in range(-1, 2):
            for ddx in range(-1, 2):
                ny, nx = cy_n+ddy, cx_n+ddx
                if 0 <= ny < H and 0 <= nx < W:
                    pixels[ny][nx] = rgb_to_hex(100, 100, 110)

    return pixels


# ────────────────────────────────────────────────────────────
# FLOWERS (chrysanthemum, morning_glory, red_flower)
# ────────────────────────────────────────────────────────────
def generate_flower(W, H, flower_type, variant=0, state='normal'):
    pixels = [[None]*W for _ in range(H)]
    rng = random.Random(42 + variant * 67 + hash(flower_type) % 1000)
    seed = 300 + variant * 29

    # Stem
    stem_x = W // 2
    stem_top = int(H * 0.35)
    stem_bot = int(H * 0.92)

    for y in range(stem_top, stem_bot):
        t = (y - stem_top) / max(1, stem_bot - stem_top - 1)
        lean = int(math.sin(t * 2.5 + seed * 0.1) * 2)
        sw = 1 + int(t * 0.8)
        for dx in range(-sw, sw + 1):
            sx = stem_x + lean + dx
            if 0 <= sx < W:
                light = 0.4 + 0.3 * ((dx + sw) / max(1, 2 * sw))
                r = int(lerp(20, 60, light))
                g = int(lerp(50, 110, light))
                b = int(lerp(15, 45, light))
                pixels[y][sx] = rgb_to_hex(r, g, b)

    # Leaves on stem
    for li in range(2):
        ly = stem_top + int((stem_bot - stem_top) * (0.4 + li * 0.3))
        direction = 1 if li % 2 == 0 else -1
        for step in range(int(W * 0.2)):
            lx = stem_x + direction * (step + 1)
            lly = ly - step // 2
            if 0 <= lx < W and 0 <= lly < H:
                light = 0.5 - step * 0.04
                r = int(lerp(25, 70, light))
                g = int(lerp(55, 130, light))
                b = int(lerp(18, 50, light))
                pixels[lly][lx] = rgb_to_hex(r, g, b)
                if lly + 1 < H:
                    pixels[lly+1][lx] = rgb_to_hex(r-10, g-10, b-5)

    if state == 'dead':
        # Just stem remains, brownish
        for y in range(H):
            for x in range(W):
                if pixels[y][x] is not None:
                    r, g, b = hex_to_rgb(pixels[y][x])
                    pixels[y][x] = rgb_to_hex(r+20, g-20, b-5)
        return pixels

    # Flower head — center positioned so petals fit within canvas
    fr = W * 0.28
    fcx, fcy = W / 2.0, fr + 3  # ensure top petals don't clip

    if flower_type == 'chrysanthemum':
        petal_base = (200, 180, 40)  # Yellow
        petal_tip = (255, 240, 80)
        center_col = (140, 100, 20)
        num_petals = 24
        fr = W * 0.26
    elif flower_type == 'morning_glory':
        petal_base = (80, 50, 160)  # Purple/blue
        petal_tip = (140, 100, 220)
        center_col = (240, 230, 180)
        num_petals = 5
        fr = W * 0.25
    else:  # red_flower
        petal_base = (180, 30, 25)
        petal_tip = (240, 60, 50)
        center_col = (240, 210, 60)
        num_petals = 8

    # Draw petals
    for pi in range(num_petals):
        angle = (pi / num_petals) * 2 * math.pi + seed * 0.1
        for step in range(int(fr * 1.2)):
            t = step / (fr * 1.2)
            px = fcx + math.cos(angle) * step
            py = fcy + math.sin(angle) * step
            # Petal width narrows towards tip
            pw = max(1, int((1.0 - t) * 3.5))
            for dw in range(-pw, pw + 1):
                ppx = int(px + math.cos(angle + math.pi/2) * dw)
                ppy = int(py + math.sin(angle + math.pi/2) * dw)
                if 0 <= ppx < W and 0 <= ppy < H:
                    r = int(lerp(petal_base[0], petal_tip[0], t))
                    g = int(lerp(petal_base[1], petal_tip[1], t))
                    b = int(lerp(petal_base[2], petal_tip[2], t))
                    # Edge darkening
                    edge = abs(dw) / max(1, pw)
                    r = max(0, r - int(edge * 30))
                    g = max(0, g - int(edge * 25))
                    b = max(0, b - int(edge * 20))
                    if state == 'damaged' and rng.random() < 0.15:
                        continue
                    pixels[ppy][ppx] = rgb_to_hex(r, g, b)

    # Flower center
    cr = max(2, int(fr * 0.25))
    for dy in range(-cr, cr + 1):
        for dx in range(-cr, cr + 1):
            if dx*dx + dy*dy <= cr*cr:
                cx_p, cy_p = int(fcx + dx), int(fcy + dy)
                if 0 <= cx_p < W and 0 <= cy_p < H:
                    d = math.sqrt(dx*dx + dy*dy) / cr
                    r = int(center_col[0] * (1.0 - d * 0.3))
                    g = int(center_col[1] * (1.0 - d * 0.3))
                    b = int(center_col[2] * (1.0 - d * 0.3))
                    pixels[cy_p][cx_p] = rgb_to_hex(r, g, b)

    return pixels


# ────────────────────────────────────────────────────────────
# SIGNPOST
# ────────────────────────────────────────────────────────────
def generate_signpost(W, H, variant=0, state='normal'):
    pixels = [[None]*W for _ in range(H)]
    rng = random.Random(42 + variant * 53)
    seed = 400 + variant * 31

    # Post (vertical pole)
    post_x = W // 2
    post_top = int(H * 0.15)
    post_bot = int(H * 0.92)
    post_w = max(2, int(W * 0.08))

    for y in range(post_top, post_bot):
        for dx in range(-post_w, post_w + 1):
            x = post_x + dx
            if 0 <= x < W:
                rel = (dx + post_w) / (2 * post_w)
                light = 0.3 + 0.4 * rel
                grain = fbm(x, y * 3, seed, 2, 3.0) * 0.08
                light += grain
                light = max(0.0, min(1.0, light))
                r = int(lerp(45, 140, light))
                g = int(lerp(28, 90, light))
                b = int(lerp(12, 50, light))
                pixels[y][x] = rgb_to_hex(r, g, b)

    # Sign board (rectangle)
    sign_top = int(H * 0.12)
    sign_bot = int(H * 0.45)
    sign_left = int(W * 0.12)
    sign_right = int(W * 0.88)

    for y in range(sign_top, sign_bot):
        for x in range(sign_left, sign_right):
            rel_x = (x - sign_left) / max(1, sign_right - sign_left - 1)
            rel_y = (y - sign_top) / max(1, sign_bot - sign_top - 1)
            light = 0.35 + 0.3 * rel_x - 0.05 * rel_y
            grain = fbm(x * 0.3, y * 4, seed + 50, 3, 3.0) * 0.1
            light += grain
            # Board edge
            if y - sign_top < 1 or sign_bot - y < 2 or x - sign_left < 1 or sign_right - x < 2:
                light -= 0.12
            light = max(0.0, min(1.0, light))
            r = int(lerp(55, 170, light))
            g = int(lerp(35, 115, light))
            b = int(lerp(18, 65, light))
            pixels[y][x] = rgb_to_hex(r, g, b)

    return pixels


# ────────────────────────────────────────────────────────────
# CAMPFIRE
# ────────────────────────────────────────────────────────────
def generate_campfire(W, H, variant=0, state='normal', frame=0):
    pixels = [[None]*W for _ in range(H)]
    rng = random.Random(42 + variant * 47 + frame * 13)
    seed = 500 + variant * 23 + frame * 7

    # Logs at bottom
    log_cy = int(H * 0.72)
    for li, angle in enumerate([math.pi * 0.15, math.pi * 0.85]):
        lcx = W // 2 + int(math.cos(angle) * W * 0.1)
        for step in range(-int(W*0.3), int(W*0.3)):
            lx = lcx + int(math.cos(angle) * step)
            ly = log_cy + int(math.sin(angle) * step * 0.3)
            log_r = 2 + int(abs(step) < W * 0.15)
            for ddy in range(-log_r, log_r + 1):
                for ddx in range(-1, 2):
                    px, py = lx + ddx, ly + ddy
                    if 0 <= px < W and 0 <= py < H:
                        d = abs(ddy) / log_r
                        light = 0.4 + 0.3 * (1.0 - d)
                        r = int(lerp(40, 110, light))
                        g = int(lerp(22, 60, light))
                        b = int(lerp(10, 30, light))
                        pixels[py][px] = rgb_to_hex(r, g, b)

    if state == 'dead':
        # Just charred logs with ash
        for y in range(H):
            for x in range(W):
                if pixels[y][x]:
                    r, g, b = hex_to_rgb(pixels[y][x])
                    pixels[y][x] = rgb_to_hex(r//2, g//2, b//2)
        return pixels

    # Fire flames
    fire_cx, fire_cy = W / 2.0, H * 0.50
    fire_h = H * 0.55
    fire_w = W * 0.30

    for y in range(int(fire_cy - fire_h * 0.7), int(fire_cy + fire_h * 0.3)):
        for x in range(W):
            rel_y = (y - (fire_cy - fire_h * 0.7)) / fire_h
            # Flame width narrows at top
            half_w = fire_w * (0.3 + 0.7 * rel_y)
            dx = abs(x - fire_cx)
            if dx < half_w:
                rel_dx = dx / half_w
                intensity = (1.0 - rel_dx) * (1.0 - rel_y * 0.8)
                # Flame noise
                fn = fbm(x * 3, y * 3 + frame * 8, seed, 3, 4.0) * 0.3
                intensity += fn
                if intensity > 0.35:
                    # Fire gradient: white center → yellow → orange → red
                    if intensity > 0.8:
                        r, g, b = 255, 240, 200
                    elif intensity > 0.6:
                        t = (intensity - 0.6) / 0.2
                        r = 255
                        g = int(lerp(180, 240, t))
                        b = int(lerp(40, 200, t))
                    elif intensity > 0.4:
                        t = (intensity - 0.4) / 0.2
                        r = int(lerp(220, 255, t))
                        g = int(lerp(100, 180, t))
                        b = int(lerp(20, 40, t))
                    else:
                        t = (intensity - 0.2) / 0.2
                        r = int(lerp(150, 220, t))
                        g = int(lerp(40, 100, t))
                        b = int(lerp(10, 20, t))

                    if state == 'damaged':
                        intensity *= 0.6
                        r = int(r * 0.7)
                        g = int(g * 0.6)

                    pixels[y][x] = rgb_to_hex(r, g, b)

    return pixels


# ────────────────────────────────────────────────────────────
# FENCE
# ────────────────────────────────────────────────────────────
def generate_fence(W, H, variant=0, state='normal'):
    pixels = [[None]*W for _ in range(H)]
    seed = 600 + variant * 19

    post_w = max(2, W // 6)
    rail_h = max(1, H // 8)

    # Two vertical posts
    for post_x in [W // 4, 3 * W // 4]:
        for y in range(int(H * 0.08), int(H * 0.92)):
            for dx in range(-post_w//2, post_w//2 + 1):
                x = post_x + dx
                if 0 <= x < W:
                    rel = (dx + post_w//2) / post_w
                    light = 0.3 + 0.4 * rel
                    grain = fbm(x, y * 3, seed, 2, 3.0) * 0.08
                    light = max(0.0, min(1.0, light + grain))
                    r = int(lerp(50, 150, light))
                    g = int(lerp(32, 100, light))
                    b = int(lerp(15, 55, light))
                    if state == 'damaged' and random.Random(x*H+y).random() < 0.08:
                        continue
                    pixels[y][x] = rgb_to_hex(r, g, b)

    # Two horizontal rails
    for rail_y in [int(H * 0.3), int(H * 0.6)]:
        for y in range(rail_y, min(H, rail_y + rail_h)):
            for x in range(W // 4 - post_w//2, 3 * W // 4 + post_w//2 + 1):
                if 0 <= x < W:
                    rel = (y - rail_y) / max(1, rail_h)
                    light = 0.35 + 0.25 * (1.0 - rel)
                    grain = fbm(x * 3, y, seed + 30, 2, 3.0) * 0.06
                    light = max(0.0, min(1.0, light + grain))
                    r = int(lerp(55, 155, light))
                    g = int(lerp(35, 105, light))
                    b = int(lerp(18, 58, light))
                    pixels[y][x] = rgb_to_hex(r, g, b)

    return pixels


# ────────────────────────────────────────────────────────────
# FENCE RAIL (horizontal piece)
# ────────────────────────────────────────────────────────────
def generate_fence_rail(W, H, variant=0, state='normal'):
    pixels = [[None]*W for _ in range(H)]
    seed = 650 + variant * 17

    rail_top = int(H * 0.25)
    rail_bot = int(H * 0.75)

    for y in range(rail_top, rail_bot):
        for x in range(2, W - 2):
            rel_y = (y - rail_top) / max(1, rail_bot - rail_top - 1)
            light = 0.3 + 0.35 * (1.0 - rel_y)
            grain = fbm(x * 2, y * 0.5, seed, 3, 5.0) * 0.1
            light = max(0.0, min(1.0, light + grain))
            r = int(lerp(50, 155, light))
            g = int(lerp(32, 102, light))
            b = int(lerp(15, 56, light))
            if y == rail_top or y == rail_bot - 1:
                r, g, b = max(0,r-20), max(0,g-15), max(0,b-8)
            pixels[y][x] = rgb_to_hex(r, g, b)

    return pixels


# ────────────────────────────────────────────────────────────
# PORTAL
# ────────────────────────────────────────────────────────────
def generate_portal(W, H, variant=0, state='normal', frame=0):
    pixels = [[None]*W for _ in range(H)]
    rng = random.Random(42 + variant * 59 + frame * 11)
    seed = 700 + variant * 31 + frame * 5

    cx, cy = W / 2.0, H * 0.48
    rx, ry = W * 0.40, H * 0.42

    # Stone frame (arch)
    frame_w = max(3, int(W * 0.08))
    for y in range(H):
        for x in range(W):
            dx = (x - cx) / rx
            dy = (y - cy) / ry
            d = math.sqrt(dx*dx + dy*dy)
            if 0.85 < d < 1.15:
                light = 0.3 + 0.3 * ((x - cx) / W + 0.5)
                tex = fbm(x * 2, y * 2, seed + 100, 3, 3.0) * 0.1
                light = max(0.0, min(1.0, light + tex))
                r = int(lerp(50, 140, light))
                g = int(lerp(48, 135, light))
                b = int(lerp(55, 150, light))
                pixels[y][x] = rgb_to_hex(r, g, b)

    # Portal energy inside
    for y in range(H):
        for x in range(W):
            dx = (x - cx) / (rx * 0.82)
            dy = (y - cy) / (ry * 0.82)
            d = math.sqrt(dx*dx + dy*dy)
            if d < 1.0:
                swirl = math.sin(d * 6 + math.atan2(dy, dx) * 3 + frame * 1.5)
                intensity = (1.0 - d) * 0.8 + swirl * 0.15
                en = fbm(x * 2 + frame * 3, y * 2, seed, 3, 5.0) * 0.2
                intensity += en
                intensity = max(0.0, min(1.0, intensity))

                # Purple/blue energy gradient
                r = int(lerp(30, 180, intensity * 0.6))
                g = int(lerp(10, 120, intensity * 0.4))
                b = int(lerp(80, 255, intensity))
                # Bright center
                if d < 0.3:
                    r = min(255, r + int((0.3 - d) * 200))
                    g = min(255, g + int((0.3 - d) * 180))
                    b = min(255, b + int((0.3 - d) * 100))

                pixels[y][x] = rgb_to_hex(r, g, b)

    # Base columns
    col_w = max(3, int(W * 0.09))
    col_top = int(cy)
    col_bot = int(H * 0.95)
    for col_x in [int(cx - rx * 0.95), int(cx + rx * 0.95)]:
        for y in range(col_top, col_bot):
            for dx in range(-col_w, col_w + 1):
                px = col_x + dx
                if 0 <= px < W and 0 <= y < H:
                    rel = (dx + col_w) / (2 * col_w)
                    light = 0.3 + 0.35 * rel
                    light = max(0.0, min(1.0, light))
                    r = int(lerp(45, 130, light))
                    g = int(lerp(43, 125, light))
                    b = int(lerp(50, 140, light))
                    pixels[y][px] = rgb_to_hex(r, g, b)

    return pixels


# ────────────────────────────────────────────────────────────
# FIREWOOD (burning logs)
# ────────────────────────────────────────────────────────────
def generate_firewood(W, H, variant=0, state='normal', frame=0):
    pixels = [[None]*W for _ in range(H)]
    rng = random.Random(42 + variant * 43 + frame * 9)
    seed = 800 + variant * 27 + frame * 5

    # Stacked logs
    num_logs = 3
    log_h = int(H * 0.28)

    for li in range(num_logs):
        ly_center = int(H * 0.35 + li * log_h * 0.9)
        lx_offset = int((li - 1) * W * 0.05)

        for y in range(max(0, ly_center - log_h//2), min(H, ly_center + log_h//2)):
            for x in range(int(W * 0.08) + lx_offset, int(W * 0.92) + lx_offset):
                if 0 <= x < W:
                    rel_y = (y - (ly_center - log_h//2)) / max(1, log_h - 1)
                    # Cylindrical shading
                    cylinder = math.sin(rel_y * math.pi)
                    light = 0.25 + 0.45 * cylinder
                    grain = fbm(x * 2, y * 0.5, seed + li * 50, 3, 4.0) * 0.1
                    light = max(0.0, min(1.0, light + grain))

                    if state == 'dead':
                        # Charred
                        r = int(lerp(20, 55, light))
                        g = int(lerp(18, 45, light))
                        b = int(lerp(15, 35, light))
                    else:
                        r = int(lerp(40, 130, light))
                        g = int(lerp(24, 75, light))
                        b = int(lerp(10, 38, light))

                    pixels[y][x] = rgb_to_hex(r, g, b)

    # Larger flames if normal
    if state == 'normal':
        for fi in range(5):
            fx = int(W * (0.15 + fi * 0.175))
            fy_base = int(H * 0.32)
            flame_h = int(H * 0.45) + int(fbm(fi, frame, seed, 1, 2.0) * H * 0.15)
            for y in range(max(0, fy_base - flame_h), fy_base):
                rel = (fy_base - y) / flame_h
                fw = max(1, int((1.0 - rel) * 5))
                for dx in range(-fw, fw + 1):
                    px = fx + dx
                    if 0 <= px < W and 0 <= y < H:
                        fn = fbm(px * 2, y * 2 + frame * 6, seed + fi * 20, 3, 4.0)
                        if fn > -0.2:
                            edge_d = abs(dx) / max(1, fw)
                            if rel < 0.2:
                                r, g, b = 255, 240, 180
                            elif rel < 0.4:
                                r, g, b = 255, 200, 60
                            elif rel < 0.65:
                                r, g, b = 255, 140, 25
                            else:
                                r, g, b = 210, 70, 12
                            # Darken edges
                            r = max(0, int(r * (1.0 - edge_d * 0.3)))
                            g = max(0, int(g * (1.0 - edge_d * 0.4)))
                            b = max(0, int(b * (1.0 - edge_d * 0.3)))
                            pixels[y][px] = rgb_to_hex(r, g, b)

    return pixels


# ────────────────────────────────────────────────────────────
# DUMMY (training dummy)
# ────────────────────────────────────────────────────────────
def generate_dummy(W, H, variant=0, state='normal'):
    pixels = [[None]*W for _ in range(H)]
    rng = random.Random(42 + variant * 61)
    seed = 900 + variant * 33

    cx = W // 2

    # Pole
    pole_top = int(H * 0.05)
    pole_bot = int(H * 0.92)
    pw = max(1, int(W * 0.05))
    for y in range(pole_top, pole_bot):
        for dx in range(-pw, pw + 1):
            x = cx + dx
            if 0 <= x < W:
                rel = (dx + pw) / (2 * pw)
                light = 0.3 + 0.35 * rel
                light = max(0.0, min(1.0, light))
                r = int(lerp(45, 130, light))
                g = int(lerp(30, 85, light))
                b = int(lerp(15, 45, light))
                pixels[y][x] = rgb_to_hex(r, g, b)

    # Head (straw circle)
    head_cy = int(H * 0.18)
    head_r = int(W * 0.14)
    for dy in range(-head_r, head_r + 1):
        for dx in range(-head_r, head_r + 1):
            if dx*dx + dy*dy <= head_r*head_r:
                x, y = cx + dx, head_cy + dy
                if 0 <= x < W and 0 <= y < H:
                    d = math.sqrt(dx*dx + dy*dy) / head_r
                    light = 0.5 + 0.3 * (dx / head_r) - 0.1 * (dy / head_r)
                    tex = fbm(x*3, y*3, seed, 2, 3.0) * 0.1
                    light = max(0.0, min(1.0, light + tex))
                    r = int(lerp(140, 220, light))
                    g = int(lerp(110, 190, light))
                    b = int(lerp(50, 100, light))
                    pixels[y][x] = rgb_to_hex(r, g, b)

    # Body (cross-shaped straw target)
    body_top = int(H * 0.28)
    body_bot = int(H * 0.65)
    body_w = int(W * 0.22)
    arm_y = int(H * 0.38)
    arm_w = int(W * 0.38)
    arm_h = int(H * 0.08)

    # Torso
    for y in range(body_top, body_bot):
        for dx in range(-body_w, body_w + 1):
            x = cx + dx
            if 0 <= x < W:
                rel = (dx + body_w) / (2 * body_w)
                light = 0.35 + 0.3 * rel
                tex = fbm(x*2, y*3, seed+50, 2, 3.0) * 0.08
                light = max(0.0, min(1.0, light + tex))
                r = int(lerp(130, 210, light))
                g = int(lerp(100, 180, light))
                b = int(lerp(40, 90, light))
                if state == 'damaged' and rng.random() < 0.08:
                    continue
                pixels[y][x] = rgb_to_hex(r, g, b)

    # Arms
    for y in range(arm_y, min(H, arm_y + arm_h)):
        for dx in range(-arm_w, arm_w + 1):
            x = cx + dx
            if 0 <= x < W and pixels[y][x] is None:
                light = 0.4 + 0.2 * (dx / arm_w + 0.5)
                light = max(0.0, min(1.0, light))
                r = int(lerp(125, 200, light))
                g = int(lerp(95, 170, light))
                b = int(lerp(38, 85, light))
                pixels[y][x] = rgb_to_hex(r, g, b)

    # Target circles
    target_cy = int(H * 0.46)
    for ring_r, ring_col in [(int(W*0.12), (200,40,30)), (int(W*0.07), (240,240,220)), (int(W*0.03), (200,40,30))]:
        for dy in range(-ring_r, ring_r + 1):
            for dx in range(-ring_r, ring_r + 1):
                d = math.sqrt(dx*dx + dy*dy)
                if ring_r - 2 < d < ring_r + 1:
                    x, y = cx + dx, target_cy + dy
                    if 0 <= x < W and 0 <= y < H:
                        pixels[y][x] = rgb_to_hex(*ring_col)

    return pixels


# ────────────────────────────────────────────────────────────
# ────────────────────────────────────────────────────────────
# EFFECTS (high_temperature, indestructible)
# ────────────────────────────────────────────────────────────
def generate_effect(W, H, effect_type, variant=0):
    pixels = [[None]*W for _ in range(H)]
    seed = 1000 + variant * 19

    cx, cy = W / 2.0, H / 2.0
    max_r = min(W, H) * 0.44

    if effect_type == 'high_temperature':
        # Fire/heat icon — orange-red gradient with inner glow
        for y in range(H):
            for x in range(W):
                d = dist(x, y, cx, cy) / max_r
                if d > 1.0:
                    continue
                # Flame shape: taller on top
                shape_mod = 1.0 + (cy - y) / H * 0.4
                d_adj = d / shape_mod
                if d_adj > 1.0:
                    continue
                intensity = 1.0 - d_adj
                n = fbm(x * 3, y * 3, seed, 3, 4.0) * 0.15
                intensity = max(0.0, min(1.0, intensity + n))
                if intensity > 0.7:
                    r, g, b = 255, int(lerp(220, 255, (intensity-0.7)/0.3)), int(lerp(120, 200, (intensity-0.7)/0.3))
                elif intensity > 0.4:
                    t = (intensity - 0.4) / 0.3
                    r, g, b = 255, int(lerp(120, 220, t)), int(lerp(20, 120, t))
                elif intensity > 0.15:
                    t = (intensity - 0.15) / 0.25
                    r, g, b = int(lerp(160, 255, t)), int(lerp(50, 120, t)), int(lerp(5, 20, t))
                else:
                    continue
                pixels[y][x] = rgb_to_hex(r, g, b)

    elif effect_type == 'indestructible':
        # Shield icon — golden diamond with blue glow
        for y in range(H):
            for x in range(W):
                # Diamond shape
                dx, dy = abs(x - cx), abs(y - cy)
                dd = (dx + dy) / max_r
                if dd > 1.05:
                    continue
                if dd > 1.0:
                    # Glow edge
                    pixels[y][x] = rgb_to_hex(60, 80, 180)
                    continue
                intensity = 1.0 - dd
                n = fbm(x * 2, y * 2, seed, 2, 5.0) * 0.1
                intensity = max(0.0, min(1.0, intensity + n))
                if intensity > 0.6:
                    t = (intensity - 0.6) / 0.4
                    r = int(lerp(220, 255, t))
                    g = int(lerp(190, 240, t))
                    b = int(lerp(40, 120, t))
                elif intensity > 0.25:
                    t = (intensity - 0.25) / 0.35
                    r = int(lerp(140, 220, t))
                    g = int(lerp(120, 190, t))
                    b = int(lerp(20, 40, t))
                else:
                    t = intensity / 0.25
                    r = int(lerp(60, 140, t))
                    g = int(lerp(50, 120, t))
                    b = int(lerp(80, 20, t))
                # Inner shimmer
                shimmer = fbm(x * 5, y * 5, seed + 100, 2, 3.0) * 0.08
                r = max(0, min(255, r + int(shimmer * 80)))
                g = max(0, min(255, g + int(shimmer * 60)))
                pixels[y][x] = rgb_to_hex(r, g, b)

    return pixels


# ────────────────────────────────────────────────────────────
# DECORATIONS (bush, mushroom, flowers, cobblestone, etc.)
# ────────────────────────────────────────────────────────────
def generate_decoration(W, H, deco_type, variant=0):
    pixels = [[None]*W for _ in range(H)]
    rng = random.Random(42 + variant * 31 + hash(deco_type) % 1000)
    seed = 1100 + variant * 17

    cx, cy = W / 2.0, H / 2.0

    if deco_type == 'deco_bush':
        # Small round bush
        rx, ry = W * 0.42, H * 0.38
        for y in range(H):
            for x in range(W):
                dx = (x - cx) / rx
                dy = (y - (cy + 1)) / ry
                d = math.sqrt(dx*dx + dy*dy)
                if d < 1.0:
                    light = 0.35 + 0.25 * (dx) - 0.1 * dy + (1.0 - d) * 0.2
                    n = fbm(x*3, y*3, seed, 3, 3.0) * 0.12
                    light = max(0.0, min(1.0, light + n))
                    r = int(lerp(15, 70, light))
                    g = int(lerp(40, 140, light))
                    b = int(lerp(10, 45, light))
                    if d > 0.85:
                        fade = (d - 0.85) / 0.15
                        r = max(0, int(r * (1.0 - fade * 0.5)))
                        g = max(0, int(g * (1.0 - fade * 0.5)))
                    pixels[y][x] = rgb_to_hex(r, g, b)

    elif deco_type == 'deco_mushroom':
        # Small mushroom: stem + cap
        cap_cy = int(H * 0.35)
        cap_rx, cap_ry = W * 0.38, H * 0.25
        # Stem
        for y in range(int(H * 0.4), int(H * 0.85)):
            for dx in range(-2, 3):
                x = W // 2 + dx
                if 0 <= x < W:
                    rel = (dx + 2) / 4.0
                    light = 0.4 + 0.35 * rel
                    r = int(lerp(180, 230, light))
                    g = int(lerp(170, 220, light))
                    b = int(lerp(150, 200, light))
                    pixels[y][x] = rgb_to_hex(r, g, b)
        # Cap
        for y in range(H):
            for x in range(W):
                dx = (x - cx) / cap_rx
                dy = (y - cap_cy) / cap_ry
                d = math.sqrt(dx*dx + dy*dy)
                if d < 1.0 and y < int(H * 0.5):
                    light = 0.3 + 0.3 * dx + (1.0 - d) * 0.3
                    n = fbm(x*3, y*3, seed, 2, 3.0) * 0.1
                    light = max(0.0, min(1.0, light + n))
                    r = int(lerp(140, 220, light))
                    g = int(lerp(30, 60, light))
                    b = int(lerp(20, 40, light))
                    # White spots
                    spot = fbm(x * 5, y * 5, seed + 50, 2, 2.0)
                    if spot > 0.35:
                        r, g, b = min(255, r+80), min(255, g+100), min(255, b+90)
                    pixels[y][x] = rgb_to_hex(r, g, b)

    elif deco_type in ('deco_red_flower', 'deco_yellow_flower'):
        is_red = 'red' in deco_type
        # Tiny flower: 3-5 petals + center
        petal_r = W * 0.28
        for pi in range(5):
            angle = (pi / 5) * 2 * math.pi - math.pi / 2
            for step in range(int(petal_r)):
                t = step / petal_r
                px = cx + math.cos(angle) * step
                py = cy - 1 + math.sin(angle) * step
                pw = max(1, int((1.0 - t) * 2.5))
                for dw in range(-pw, pw + 1):
                    ppx = int(px + math.cos(angle + math.pi/2) * dw)
                    ppy = int(py + math.sin(angle + math.pi/2) * dw)
                    if 0 <= ppx < W and 0 <= ppy < H:
                        if is_red:
                            r = int(lerp(160, 240, t))
                            g = int(lerp(20, 50, t))
                            b = int(lerp(15, 35, t))
                        else:
                            r = int(lerp(200, 255, t))
                            g = int(lerp(180, 240, t))
                            b = int(lerp(20, 50, t))
                        pixels[ppy][ppx] = rgb_to_hex(r, g, b)
        # Center dot
        for dy in range(-1, 2):
            for dx in range(-1, 2):
                x, y = int(cx)+dx, int(cy)-1+dy
                if 0 <= x < W and 0 <= y < H:
                    pixels[y][x] = rgb_to_hex(240, 210, 40) if is_red else rgb_to_hex(180, 100, 20)

    elif deco_type == 'deco_cobblestone':
        # Stone pattern tile
        for y in range(H):
            for x in range(W):
                light = 0.4 + fbm(x*2, y*2, seed, 3, 3.0) * 0.2
                # Grid cracks
                gx = (x * 3) % W
                gy = (y * 3) % H
                if gx < 1 or gy < 1:
                    light -= 0.2
                light = max(0.0, min(1.0, light))
                v = int(lerp(90, 180, light))
                r, g, b = v + 5, v, v - 5
                pixels[y][x] = rgb_to_hex(r, g, b)

    elif deco_type == 'deco_pebbles':
        # Scattered small stones
        for pi in range(rng.randint(4, 7)):
            px = rng.randint(2, W - 3)
            py = rng.randint(2, H - 3)
            pr = rng.randint(1, 3)
            base_v = rng.randint(100, 160)
            for dy in range(-pr, pr + 1):
                for dx in range(-pr, pr + 1):
                    if dx*dx + dy*dy <= pr*pr:
                        x, y = px + dx, py + dy
                        if 0 <= x < W and 0 <= y < H:
                            d = math.sqrt(dx*dx + dy*dy) / pr
                            light = 0.5 + 0.3 * (dx / max(1, pr)) + (1.0 - d) * 0.15
                            v = int(base_v * light)
                            pixels[y][x] = rgb_to_hex(v+3, v, v-3)

    elif deco_type == 'deco_green_grass':
        # Grass blades
        for gi in range(rng.randint(5, 8)):
            gx = rng.randint(1, W - 2)
            g_height = rng.randint(int(H * 0.3), int(H * 0.7))
            lean = rng.uniform(-0.3, 0.3)
            for step in range(g_height):
                t = step / g_height
                x = int(gx + lean * step)
                y = H - 1 - step
                if 0 <= x < W and 0 <= y < H:
                    light = 0.4 + t * 0.4
                    r = int(lerp(20, 50, light))
                    g_c = int(lerp(60, 150, light))
                    b = int(lerp(15, 40, light))
                    pixels[y][x] = rgb_to_hex(r, g_c, b)
                    if x + 1 < W:
                        pixels[y][x+1] = rgb_to_hex(max(0,r-10), max(0,g_c-15), max(0,b-5))

    elif deco_type == 'deco_dead_leaves':
        # Scattered brown leaves
        for li in range(rng.randint(4, 7)):
            lx = rng.randint(1, W - 3)
            ly = rng.randint(1, H - 3)
            lr = rng.randint(1, 3)
            base_r = rng.randint(100, 160)
            base_g = rng.randint(60, 90)
            for dy in range(-lr, lr + 1):
                for dx in range(-lr, lr + 1):
                    if abs(dx) + abs(dy) <= lr + 1:
                        x, y = lx + dx, ly + dy
                        if 0 <= x < W and 0 <= y < H:
                            n = fbm(x*4, y*4, seed + li, 2, 2.0) * 0.15
                            r = max(0, min(255, base_r + int(n * 100)))
                            g = max(0, min(255, base_g + int(n * 60)))
                            b = max(0, min(255, 20 + int(n * 30)))
                            pixels[y][x] = rgb_to_hex(r, g, b)

    elif deco_type == 'deco_stone_crack':
        # Crack lines on stone
        for y in range(H):
            for x in range(W):
                # Background stone
                sv = 140 + int(fbm(x*2, y*2, seed, 2, 3.0) * 20)
                pixels[y][x] = rgb_to_hex(sv, sv-2, sv-5)
        # Draw crack
        cx_c, cy_c = W // 2, 1
        for _ in range(H * 2):
            if 0 <= cx_c < W and 0 <= cy_c < H:
                pixels[cy_c][cx_c] = rgb_to_hex(50, 48, 45)
                if cx_c + 1 < W:
                    pixels[cy_c][cx_c + 1] = rgb_to_hex(70, 68, 62)
            cy_c += 1
            cx_c += rng.randint(-1, 1)
            if cy_c >= H:
                break

    else:
        # Generic decoration — small textured square
        for y in range(H):
            for x in range(W):
                n = fbm(x * 3, y * 3, seed, 3, 3.0)
                light = 0.5 + n * 0.3
                light = max(0.1, min(0.9, light))
                v = int(lerp(80, 200, light))
                pixels[y][x] = rgb_to_hex(v, v, v)

    return pixels


# Output helpers (same as tree generator)
# ────────────────────────────────────────────────────────────
def remove_orphans(pixels, min_neighbors=1):
    """Remove isolated pixels with fewer than min_neighbors adjacent filled pixels."""
    H = len(pixels)
    W = len(pixels[0]) if H > 0 else 0
    to_clear = []
    for y in range(H):
        for x in range(W):
            if pixels[y][x] is None:
                continue
            count = 0
            for dx, dy in [(-1,0),(1,0),(0,-1),(0,1)]:
                nx, ny = x+dx, y+dy
                if 0 <= nx < W and 0 <= ny < H and pixels[ny][nx] is not None:
                    count += 1
            if count < min_neighbors:
                to_clear.append((x, y))
    for x, y in to_clear:
        pixels[y][x] = None
    return pixels


def quantize_pixels(pixels, max_colors=30):
    """Reduce pixel colors to max_colors by snapping to nearest palette color."""
    # Collect all unique colors
    all_colors = {}
    for row in pixels:
        for c in row:
            if c is not None:
                all_colors[c] = all_colors.get(c, 0) + 1

    if len(all_colors) <= max_colors:
        return pixels

    # Sort by frequency, keep top max_colors
    sorted_colors = sorted(all_colors.items(), key=lambda x: -x[1])
    palette = [c for c, _ in sorted_colors[:max_colors]]
    palette_rgb = [hex_to_rgb(c) for c in palette]

    # Map remaining colors to nearest palette color
    color_map = {}
    for c in all_colors:
        if c in palette:
            color_map[c] = c
        else:
            r, g, b = hex_to_rgb(c)
            best_dist = float('inf')
            best = palette[0]
            for pc, (pr, pg, pb) in zip(palette, palette_rgb):
                d = (r-pr)**2 + (g-pg)**2 + (b-pb)**2
                if d < best_dist:
                    best_dist = d
                    best = pc
            color_map[c] = best

    # Apply mapping
    H, W = len(pixels), len(pixels[0])
    result = [[None]*W for _ in range(H)]
    for y in range(H):
        for x in range(W):
            c = pixels[y][x]
            result[y][x] = color_map[c] if c is not None else None
    return result


def pixels_to_yaml_data(pixels):
    """Convert pixel array to (palette_dict, data_string)."""
    chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
    color_to_char = {}
    char_to_hex = {}
    char_idx = 0
    rows = []
    for row in pixels:
        line = ""
        for color in row:
            if color is None:
                line += "."
            else:
                if color not in color_to_char:
                    if char_idx < len(chars):
                        c = chars[char_idx]
                        color_to_char[color] = c
                        char_to_hex[c] = color
                        char_idx += 1
                    else:
                        c = "?"
                line += color_to_char.get(color, "?")
        rows.append(line)
    palette = {'.': '#00000000'}
    palette.update(char_to_hex)
    return palette, "\n".join(rows)


def write_sprite_yaml(path, name, img_type, output_dir, palette, data_entries, frame_delay=None):
    """Write sprite YAML compatible with pixel_artify.py / safe_load."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w') as f:
        f.write(f'metadata:\n  name: "{name}"\n')
        f.write(f'type: "{img_type}"\n')
        if frame_delay is not None:
            f.write(f'frame_delay: {frame_delay}\n')
        f.write(f'output_dir: "{output_dir}"\n')
        f.write('palette:\n')
        for k, v in palette.items():
            f.write(f'  "{k}": "{v}"\n')
        f.write('data:\n')
        for data_str in data_entries:
            f.write('  - |\n')
            for line in data_str.split('\n'):
                f.write(f'    {line}\n')


def pixels_to_png(pixels, png_path):
    height = len(pixels)
    width = len(pixels[0]) if pixels else 0
    img = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    for y, row in enumerate(pixels):
        for x, color in enumerate(row):
            if color is not None:
                r, g, b = hex_to_rgb(color)
                img.putpixel((x, y), (r, g, b, 255))
    os.makedirs(os.path.dirname(png_path), exist_ok=True)
    img.save(png_path)


def emit(pixels, name, img_dir, yaml_base, png_base):
    pixels = remove_orphans(pixels)
    pixels = quantize_pixels(pixels, max_colors=30)
    pal, data_str = pixels_to_yaml_data(pixels)

    yaml_path = os.path.join(yaml_base, img_dir, f'{name}.yaml')
    write_sprite_yaml(yaml_path, name, 'png', f'Images/{img_dir}', pal, [data_str])

    pixels_to_png(pixels, os.path.join(png_base, img_dir, f'{name}.png'))
    nc = len(pal) - 1  # exclude '.'
    w = len(pixels[0])
    h = len(pixels)
    print(f'  {name}: {w}x{h}, {nc} colors')


# ────────────────────────────────────────────────────────────
# Main
# ────────────────────────────────────────────────────────────
def generate_all():
    yaml_base = 'assets/images'
    png_base = 'game/FarmGame/Content/Images'

    OBJECTS = [
        # (name, img_dir, size(W,H), generator, has_frames, states)
        ('rock',            'rocks',          (48,48),  generate_rock,     False, ['normal','damaged','dead']),
        ('box',             'boxes',          (48,48),  generate_box,      False, ['normal','damaged','dead']),
        ('chrysanthemum',   'chrysanthemums', (48,48),  None,              False, ['normal','damaged','dead']),
        ('morning_glory',   'morning_glories',(48,48),  None,              False, ['normal','damaged','dead']),
        ('red_flower',      'red_flowers',    (48,48),  None,              False, ['normal','damaged','dead']),
        ('signpost',        'signposts',      (48,48),  generate_signpost, False, ['normal']),
        ('dummy',           'dummies',        (48,48),  generate_dummy,    False, ['normal','damaged','dead']),
        ('campfire',        'campfires',      (48,48),  None,              True,  ['normal','damaged','dead']),
        ('fence',           'fences',         (32,32),  generate_fence,    False, ['normal','damaged','dead']),
        ('fence_rail',      'fences',         (48,32),  generate_fence_rail,False,['normal']),
        ('portal',          'portals',        (64,96),  None,              True,  ['normal']),
        ('firewood',        'firewoods',      (64,32),  None,              True,  ['normal','damaged','dead']),
    ]

    for obj_name, img_dir, (W, H), gen_fn, has_frames, states in OBJECTS:
        print(f'\n── {obj_name} ──')

        for state in states:
            if gen_fn is not None:
                pixels = gen_fn(W, H, variant=0, state=state)
            elif obj_name in ('chrysanthemum', 'morning_glory', 'red_flower'):
                pixels = generate_flower(W, H, obj_name, variant=0, state=state)
            elif obj_name == 'campfire':
                pixels = generate_campfire(W, H, variant=0, state=state, frame=0)
            elif obj_name == 'portal':
                pixels = generate_portal(W, H, variant=0, state=state, frame=0)
            elif obj_name == 'firewood':
                pixels = generate_firewood(W, H, variant=0, state=state, frame=0)
            else:
                continue

            name = f'{obj_name}_{state}'
            emit(pixels, name, img_dir, yaml_base, png_base)

        # Animated frames — write combined GIF YAML + individual frame PNGs
        if has_frames and 'normal' in states:
            FRAME_DELAYS = {'campfire': 200, 'firewood': 200, 'portal': 300}
            num_frames = 4 if obj_name == 'firewood' else 3
            frame_delay = FRAME_DELAYS.get(obj_name, 200)
            base_name = f'{obj_name}_normal'

            all_frame_pixels = []
            for frame in range(num_frames):
                if obj_name == 'campfire':
                    pixels = generate_campfire(W, H, variant=0, state='normal', frame=frame)
                elif obj_name == 'portal':
                    pixels = generate_portal(W, H, variant=0, state='normal', frame=frame)
                elif obj_name == 'firewood':
                    pixels = generate_firewood(W, H, variant=0, state='normal', frame=frame)
                else:
                    continue
                pixels = remove_orphans(pixels)
                pixels = quantize_pixels(pixels, max_colors=30)
                all_frame_pixels.append(pixels)

            # Build merged palette
            chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
            merged_pal = {'.': '#00000000'}
            color_to_char = {}
            char_idx = 0
            for pixels in all_frame_pixels:
                for row in pixels:
                    for color in row:
                        if color is not None and color not in color_to_char:
                            if char_idx < len(chars):
                                c = chars[char_idx]
                                color_to_char[color] = c
                                merged_pal[c] = color
                                char_idx += 1

            # Encode frames and save PNGs
            data_entries = []
            for fi, pixels in enumerate(all_frame_pixels):
                rows = []
                for row in pixels:
                    line = ""
                    for color in row:
                        line += "." if color is None else color_to_char.get(color, "?")
                    rows.append(line)
                data_entries.append("\n".join(rows))
                png_path = os.path.join(png_base, img_dir, f'{base_name}_frame{fi}.png')
                pixels_to_png(pixels, png_path)

            yaml_path = os.path.join(yaml_base, img_dir, f'{base_name}.yaml')
            write_sprite_yaml(yaml_path, base_name, 'gif', f'Images/{img_dir}',
                            merged_pal, data_entries, frame_delay=frame_delay)
            nc = len(merged_pal) - 1
            print(f'  {base_name}: {W}x{H} gif, {nc} colors, {num_frames} frames')


    # ── Effects ──
    EFFECTS = [
        ('high_temperature', 'effects', (32, 32)),
        ('indestructible',   'effects', (32, 32)),
    ]
    for eff_name, img_dir, (W, H) in EFFECTS:
        print(f'\n── {eff_name} ──')
        pixels = generate_effect(W, H, eff_name)
        emit(pixels, eff_name, img_dir, yaml_base, png_base)

    # ── Decorations ──
    DECORATIONS = [
        ('deco_bush',         'decorations', (24, 24)),
        ('deco_mushroom',     'decorations', (24, 24)),
        ('deco_red_flower',   'decorations', (16, 16)),
        ('deco_yellow_flower','decorations', (16, 16)),
        ('deco_cobblestone',  'decorations', (16, 16)),
        ('deco_pebbles',      'decorations', (16, 16)),
        ('deco_green_grass',  'decorations', (16, 16)),
        ('deco_dead_leaves',  'decorations', (16, 16)),
        ('deco_stone_crack',  'decorations', (16, 16)),
    ]
    for deco_name, img_dir, (W, H) in DECORATIONS:
        print(f'\n── {deco_name} ──')
        pixels = generate_decoration(W, H, deco_name)
        emit(pixels, deco_name, img_dir, yaml_base, png_base)


if __name__ == '__main__':
    generate_all()
    print('\nDone!')
