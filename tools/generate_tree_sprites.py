"""
Generate detailed pixel art tree sprites matching reference standard.
Reference: ~96x128 pixels, ~30 colors per sprite.
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
    return f'#{r:02x}{g:02x}{b:02x}'


def lerp(a, b, t):
    return a + (b - a) * t


def lerp_rgb(c1, c2, t):
    r1, g1, b1 = hex_to_rgb(c1)
    r2, g2, b2 = hex_to_rgb(c2)
    return rgb_to_hex(int(lerp(r1, r2, t)), int(lerp(g1, g2, t)), int(lerp(b1, b2, t)))


def dist(x1, y1, x2, y2):
    return math.sqrt((x1-x2)**2 + (y1-y2)**2)


def noise2d(x, y, seed=0):
    """Simple hash-based noise."""
    n = int(x * 374761 + y * 668265 + seed * 1013) & 0xFFFFFFFF
    n = (n ^ (n >> 13)) * 1274126177
    n = n & 0xFFFFFFFF
    return (n / 0xFFFFFFFF) * 2.0 - 1.0


def smooth_noise(x, y, seed=0, scale=8.0):
    """Smoothed noise for organic shapes."""
    sx, sy = x / scale, y / scale
    ix, iy = int(math.floor(sx)), int(math.floor(sy))
    fx, fy = sx - ix, sy - iy
    fx = fx * fx * (3 - 2 * fx)
    fy = fy * fy * (3 - 2 * fy)
    v00 = noise2d(ix, iy, seed)
    v10 = noise2d(ix+1, iy, seed)
    v01 = noise2d(ix, iy+1, seed)
    v11 = noise2d(ix+1, iy+1, seed)
    v0 = lerp(v00, v10, fx)
    v1 = lerp(v01, v11, fx)
    return lerp(v0, v1, fy)


def fbm(x, y, seed=0, octaves=4, scale=10.0):
    """Fractal Brownian Motion noise."""
    val = 0.0
    amp = 1.0
    s = scale
    for _ in range(octaves):
        val += amp * smooth_noise(x, y, seed, s)
        amp *= 0.5
        s *= 0.5
        seed += 1000
    return val


# ── Palette builder ──────────────────────────────────────────

def build_palette_30(base_hue, tree_type):
    """
    Build a 30-color palette keyed by role.
    base_hue: (r_center, g_center, b_center) for the crown midtone.
    """
    cr, cg, cb = base_hue

    colors = {}

    if tree_type == 'oak':
        # 18 crown greens: dark shadow → bright highlight
        crown_dark = (max(0,cr-60), max(0,cg-50), max(0,cb-20))
        crown_light = (min(255,cr+60), min(255,cg+70), min(255,cb+30))
        for i in range(18):
            t = i / 17.0
            r = int(lerp(crown_dark[0], crown_light[0], t))
            g = int(lerp(crown_dark[1], crown_light[1], t))
            b = int(lerp(crown_dark[2], crown_light[2], t))
            # Add slight hue shift: darker = cooler, lighter = warmer
            r = max(0, min(255, r + int((t - 0.5) * 15)))
            colors[f'crown_{i}'] = rgb_to_hex(r, g, b)
        # 8 trunk/bark browns
        for i in range(8):
            t = i / 7.0
            r = int(lerp(30, 140, t))
            g = int(lerp(18, 95, t))
            b = int(lerp(8, 55, t))
            colors[f'trunk_{i}'] = rgb_to_hex(r, g, b)
        # 4 deep shadow / background
        for i in range(4):
            t = i / 3.0
            r = int(lerp(10, 35, t))
            g = int(lerp(20, 50, t))
            b = int(lerp(10, 30, t))
            colors[f'shadow_{i}'] = rgb_to_hex(r, g, b)

    elif tree_type == 'pine':
        # Darker, more blue-green crown
        crown_dark = (max(0,cr-50), max(0,cg-40), max(0,cb-10))
        crown_light = (min(255,cr+40), min(255,cg+55), min(255,cb+35))
        for i in range(18):
            t = i / 17.0
            r = int(lerp(crown_dark[0], crown_light[0], t))
            g = int(lerp(crown_dark[1], crown_light[1], t))
            b = int(lerp(crown_dark[2], crown_light[2], t))
            colors[f'crown_{i}'] = rgb_to_hex(r, g, b)
        for i in range(8):
            t = i / 7.0
            r = int(lerp(35, 130, t))
            g = int(lerp(16, 80, t))
            b = int(lerp(6, 40, t))
            colors[f'trunk_{i}'] = rgb_to_hex(r, g, b)
        for i in range(4):
            t = i / 3.0
            r = int(lerp(6, 28, t))
            g = int(lerp(16, 42, t))
            b = int(lerp(12, 34, t))
            colors[f'shadow_{i}'] = rgb_to_hex(r, g, b)

    elif tree_type == 'birch':
        # Lighter, yellow-green crown
        crown_dark = (max(0,cr-45), max(0,cg-35), max(0,cb-15))
        crown_light = (min(255,cr+70), min(255,cg+80), min(255,cb+40))
        for i in range(18):
            t = i / 17.0
            r = int(lerp(crown_dark[0], crown_light[0], t))
            g = int(lerp(crown_dark[1], crown_light[1], t))
            b = int(lerp(crown_dark[2], crown_light[2], t))
            r = max(0, min(255, r + int((t - 0.3) * 20)))
            colors[f'crown_{i}'] = rgb_to_hex(r, g, b)
        # White/grey trunk
        for i in range(8):
            t = i / 7.0
            v = int(lerp(130, 235, t))
            r = v
            g = int(v * 0.97)
            b = int(v * 0.90)
            colors[f'trunk_{i}'] = rgb_to_hex(r, g, b)
        for i in range(4):
            t = i / 3.0
            r = int(lerp(15, 40, t))
            g = int(lerp(25, 55, t))
            b = int(lerp(12, 35, t))
            colors[f'shadow_{i}'] = rgb_to_hex(r, g, b)

    elif tree_type == 'apple':
        # Warm green crown with apple reds
        crown_dark = (max(0,cr-55), max(0,cg-45), max(0,cb-18))
        crown_light = (min(255,cr+55), min(255,cg+65), min(255,cb+30))
        for i in range(15):
            t = i / 14.0
            r = int(lerp(crown_dark[0], crown_light[0], t))
            g = int(lerp(crown_dark[1], crown_light[1], t))
            b = int(lerp(crown_dark[2], crown_light[2], t))
            r = max(0, min(255, r + int((t - 0.5) * 12)))
            colors[f'crown_{i}'] = rgb_to_hex(r, g, b)
        # 3 apple reds
        colors['apple_0'] = '#8c1010'
        colors['apple_1'] = '#c01818'
        colors['apple_2'] = '#e03030'
        for i in range(8):
            t = i / 7.0
            r = int(lerp(32, 138, t))
            g = int(lerp(20, 92, t))
            b = int(lerp(10, 52, t))
            colors[f'trunk_{i}'] = rgb_to_hex(r, g, b)
        for i in range(4):
            t = i / 3.0
            r = int(lerp(12, 38, t))
            g = int(lerp(22, 52, t))
            b = int(lerp(10, 30, t))
            colors[f'shadow_{i}'] = rgb_to_hex(r, g, b)

    else:  # basic
        crown_dark = (max(0,cr-55), max(0,cg-45), max(0,cb-18))
        crown_light = (min(255,cr+60), min(255,cg+68), min(255,cb+32))
        for i in range(18):
            t = i / 17.0
            r = int(lerp(crown_dark[0], crown_light[0], t))
            g = int(lerp(crown_dark[1], crown_light[1], t))
            b = int(lerp(crown_dark[2], crown_light[2], t))
            colors[f'crown_{i}'] = rgb_to_hex(r, g, b)
        for i in range(8):
            t = i / 7.0
            r = int(lerp(28, 135, t))
            g = int(lerp(16, 88, t))
            b = int(lerp(8, 50, t))
            colors[f'trunk_{i}'] = rgb_to_hex(r, g, b)
        for i in range(4):
            t = i / 3.0
            r = int(lerp(10, 32, t))
            g = int(lerp(18, 48, t))
            b = int(lerp(10, 28, t))
            colors[f'shadow_{i}'] = rgb_to_hex(r, g, b)

    return colors


# ── Crown shape generators ──────────────────────────────────

def gen_crown_oak(W, H, crown_h, rng, seed):
    """Wide, full, round oak crown built from overlapping leaf clusters."""
    cx, cy = W / 2.0, crown_h * 0.50
    mask = [[0.0]*W for _ in range(crown_h)]

    # Main ellipse
    rx_main, ry_main = W * 0.46, crown_h * 0.48

    # Generate 15-25 overlapping sub-clusters
    clusters = []
    for _ in range(rng.randint(18, 28)):
        angle = rng.uniform(0, 2 * math.pi)
        dist_from_center = rng.uniform(0, 0.7)
        ccx = cx + math.cos(angle) * rx_main * dist_from_center
        ccy = cy + math.sin(angle) * ry_main * dist_from_center
        cr = rng.uniform(W * 0.10, W * 0.22)
        clusters.append((ccx, ccy, cr))

    for y in range(crown_h):
        for x in range(W):
            # Base ellipse
            dx = (x - cx) / rx_main
            dy = (y - cy) / ry_main
            d = math.sqrt(dx*dx + dy*dy)
            v = 0.0
            if d < 1.05:
                v = max(0, 1.0 - d)

            # Add sub-clusters for organic feel
            for ccx, ccy, cr in clusters:
                cd = dist(x, y, ccx, ccy) / cr
                if cd < 1.0:
                    cv = (1.0 - cd * cd) * 0.6
                    v = max(v, v + cv * 0.4)

            # Gentle noise for texture (not edge breakup)
            n = fbm(x, y, seed, 3, 12.0) * 0.12
            v += n

            # Soft edge falloff (no random scatter)
            if d > 0.85:
                edge_fade = (d - 0.85) / 0.15
                v *= max(0.0, 1.0 - edge_fade * 1.2)

            mask[y][x] = max(0.0, min(1.0, v))

    return mask


def gen_crown_pine(W, H, crown_h, rng, seed):
    """Conical pine crown with distinct branch layers."""
    cx = W / 2.0
    mask = [[0.0]*W for _ in range(crown_h)]

    num_layers = rng.randint(5, 7)
    layer_h = crown_h / num_layers

    for y in range(crown_h):
        t = y / crown_h  # 0=top, 1=bottom
        # Triangular envelope
        base_half = 2.0 + t * (W * 0.46)

        # Branch layer scalloping
        layer_idx = y / layer_h
        layer_phase = layer_idx - int(layer_idx)
        # Each layer widens then narrows
        scallop = math.sin(layer_phase * math.pi) * 0.3 + 0.7
        if layer_phase < 0.15:
            scallop *= 0.6  # Indent between layers

        half_w = base_half * scallop

        for x in range(W):
            dx = abs(x - cx)
            if dx < half_w:
                rel = dx / half_w
                v = 1.0 - rel * 0.4
                # Scallop depth
                v *= scallop
                # Noise
                n = fbm(x, y, seed, 2, 8.0) * 0.15
                v += n
                # Soft edge fade for pine
                if rel > 0.75:
                    edge_fade = (rel - 0.75) / 0.25
                    v *= max(0.0, 1.0 - edge_fade)
                mask[y][x] = max(0.0, min(1.0, v))

    return mask


def gen_crown_birch(W, H, crown_h, rng, seed):
    """Oval birch crown — lighter and slightly airy but still solid."""
    cx, cy = W / 2.0, crown_h * 0.46
    rx, ry = W * 0.40, crown_h * 0.46
    mask = [[0.0]*W for _ in range(crown_h)]

    # Moderate clusters for organic shape
    clusters = []
    for _ in range(rng.randint(15, 22)):
        angle = rng.uniform(0, 2 * math.pi)
        dist_f = rng.uniform(0.05, 0.65)
        ccx = cx + math.cos(angle) * rx * dist_f
        ccy = cy + math.sin(angle) * ry * dist_f
        cr = rng.uniform(W * 0.10, W * 0.20)
        clusters.append((ccx, ccy, cr))

    for y in range(crown_h):
        for x in range(W):
            dx = (x - cx) / rx
            dy = (y - cy) / ry
            d = math.sqrt(dx*dx + dy*dy)
            v = 0.0
            if d < 1.0:
                v = (1.0 - d) * 0.85

            for ccx, ccy, cr in clusters:
                cd = dist(x, y, ccx, ccy) / cr
                if cd < 1.0:
                    v += (1.0 - cd * cd) * 0.35

            # Gentle texture noise (no aggressive gaps)
            n = fbm(x, y, seed, 3, 10.0) * 0.1
            v += n

            # Soft edge fade
            if d > 0.85:
                edge_fade = (d - 0.85) / 0.15
                v *= max(0.0, 1.0 - edge_fade * 1.1)

            mask[y][x] = max(0.0, min(1.0, v))

    return mask


def gen_crown_apple(W, H, crown_h, rng, seed):
    """Full, rounded apple crown."""
    cx, cy = W / 2.0, crown_h * 0.48
    rx, ry = W * 0.44, crown_h * 0.46
    mask = [[0.0]*W for _ in range(crown_h)]

    clusters = []
    for _ in range(rng.randint(20, 30)):
        angle = rng.uniform(0, 2 * math.pi)
        dist_f = rng.uniform(0, 0.65)
        ccx = cx + math.cos(angle) * rx * dist_f
        ccy = cy + math.sin(angle) * ry * dist_f
        cr = rng.uniform(W * 0.08, W * 0.20)
        clusters.append((ccx, ccy, cr))

    for y in range(crown_h):
        for x in range(W):
            dx = (x - cx) / rx
            dy = (y - cy) / ry
            d = math.sqrt(dx*dx + dy*dy)
            v = 0.0
            if d < 1.0:
                v = 1.0 - d * 0.6

            for ccx, ccy, cr in clusters:
                cd = dist(x, y, ccx, ccy) / cr
                if cd < 1.0:
                    v = max(v, v + (1.0 - cd) * 0.3)

            n = fbm(x, y, seed, 3, 10.0) * 0.15
            v += n

            # Soft edge fade
            if d > 0.85:
                edge_fade = (d - 0.85) / 0.15
                v *= max(0.0, 1.0 - edge_fade * 1.2)

            mask[y][x] = max(0.0, min(1.0, v))

    return mask


def gen_crown_basic(W, H, crown_h, rng, seed):
    """Generic tree crown."""
    cx, cy = W / 2.0, crown_h * 0.48
    rx, ry = W * 0.43, crown_h * 0.46
    mask = [[0.0]*W for _ in range(crown_h)]

    clusters = []
    for _ in range(rng.randint(15, 25)):
        angle = rng.uniform(0, 2 * math.pi)
        dist_f = rng.uniform(0, 0.7)
        ccx = cx + math.cos(angle) * rx * dist_f
        ccy = cy + math.sin(angle) * ry * dist_f
        cr = rng.uniform(W * 0.08, W * 0.18)
        clusters.append((ccx, ccy, cr))

    for y in range(crown_h):
        for x in range(W):
            dx = (x - cx) / rx
            dy = (y - cy) / ry
            d = math.sqrt(dx*dx + dy*dy)
            v = 0.0
            if d < 1.0:
                v = 1.0 - d * 0.7

            for ccx, ccy, cr in clusters:
                cd = dist(x, y, ccx, ccy) / cr
                if cd < 1.0:
                    v = max(v, v + (1.0 - cd) * 0.35)

            n = fbm(x, y, seed, 3, 10.0) * 0.18
            v += n

            # Soft edge fade
            if d > 0.85:
                edge_fade = (d - 0.85) / 0.15
                v *= max(0.0, 1.0 - edge_fade * 1.2)

            mask[y][x] = max(0.0, min(1.0, v))

    return mask


# ── Main tree generator ─────────────────────────────────────

CROWN_FN = {
    'oak': gen_crown_oak,
    'pine': gen_crown_pine,
    'birch': gen_crown_birch,
    'apple': gen_crown_apple,
    'basic': gen_crown_basic,
}


def generate_tree(W, H, tree_type, palette, variant=0, state='normal'):
    """Generate a full tree sprite at WxH with ~30 colors."""
    pixels = [[None]*W for _ in range(H)]
    rng = random.Random(42 + variant * 137 + hash(tree_type) % 10000)
    seed = 42 + variant * 53

    crown_h = int(H * 0.65)
    trunk_top = crown_h - int(H * 0.08)  # trunk starts behind lower crown

    # ── Crown ──
    mask = CROWN_FN[tree_type](W, H, crown_h, rng, seed)

    # State modifications
    if state == 'damaged':
        drng = random.Random(99 + variant)
        for y in range(crown_h):
            for x in range(W):
                n = fbm(x, y, seed + 800, 2, 6.0)
                if n > 0.0 or drng.random() < 0.25:
                    mask[y][x] *= drng.uniform(0.0, 0.5)

    elif state == 'dead':
        for y in range(crown_h):
            for x in range(W):
                mask[y][x] = 0.0

    # Crown colors
    crown_keys = sorted([k for k in palette if k.startswith('crown_')],
                        key=lambda k: int(k.split('_')[1]))
    crown_colors = [palette[k] for k in crown_keys]
    num_cc = len(crown_colors)

    # Lighting: upper-right light source
    cx, cy = W / 2.0, crown_h * 0.45
    shadow_keys_s = sorted([k for k in palette if k.startswith('shadow_')],
                           key=lambda k: int(k.split('_')[1]))
    shadow_colors_for_crown = [palette[k] for k in shadow_keys_s]

    for y in range(crown_h):
        for x in range(W):
            if mask[y][x] > 0.12:
                # Directional light
                lnx = (x - cx) / (W / 2.0)
                lny = (y - cy) / (crown_h / 2.0)
                light = 0.42 + 0.22 * lnx - 0.12 * lny
                # Volume from mask
                light += mask[y][x] * 0.18
                # Depth texture (multi-scale for variety)
                tn = fbm(x, y, seed + 100, 3, 6.0) * 0.12
                tn2 = fbm(x * 2, y * 2, seed + 150, 2, 3.0) * 0.08
                light += tn + tn2

                # Edge darkening — use shadow palette near crown edges
                if mask[y][x] < 0.25 and shadow_colors_for_crown:
                    si = min(len(shadow_colors_for_crown)-1,
                             int((0.25 - mask[y][x]) / 0.25 * len(shadow_colors_for_crown)))
                    pixels[y][x] = shadow_colors_for_crown[si]
                    continue

                # Ambient occlusion: darken deep interior crevices
                ao = 0.0
                for ddx, ddy in [(-2,0),(2,0),(0,-2),(0,2)]:
                    nx2, ny2 = x+ddx, y+ddy
                    if 0 <= nx2 < W and 0 <= ny2 < crown_h:
                        ao += mask[ny2][nx2]
                    else:
                        ao += 0.0
                ao /= 4.0
                if ao > mask[y][x] * 1.3:
                    light -= 0.08

                light = max(0.0, min(1.0, light))
                idx = int(light * (num_cc - 1))
                # Texture jitter (wider range for more color variety)
                jitter = int(fbm(x * 3, y * 3, seed + 200, 2, 4.0) * 2.5)
                idx = max(0, min(num_cc - 1, idx + jitter))
                pixels[y][x] = crown_colors[idx]

    # ── Apples ──
    if tree_type == 'apple' and state == 'normal':
        apple_keys = sorted([k for k in palette if k.startswith('apple_')])
        if apple_keys:
            apple_colors = [palette[k] for k in apple_keys]
            arng = random.Random(42 + variant * 17)
            placed = []
            for _ in range(120):
                ax = arng.randint(int(W*0.15), int(W*0.85))
                ay = arng.randint(int(crown_h*0.2), int(crown_h*0.85))
                if mask[ay][ax] > 0.4:
                    too_close = any(abs(ax-px)+abs(ay-py) < 8 for px, py in placed)
                    if not too_close:
                        placed.append((ax, ay))
                        ac = arng.choice(apple_colors)
                        pixels[ay][ax] = ac
                        # Apple is 2-3 pixels
                        for ddx, ddy in [(1,0),(0,1),(-1,0),(0,-1)]:
                            nx2, ny2 = ax+ddx, ay+ddy
                            if 0 <= nx2 < W and 0 <= ny2 < crown_h and mask[ny2][nx2] > 0.3:
                                if arng.random() < 0.5:
                                    pixels[ny2][nx2] = arng.choice(apple_colors)
                        if len(placed) >= 12:
                            break

    # ── Trunk ──
    trunk_keys = sorted([k for k in palette if k.startswith('trunk_')],
                        key=lambda k: int(k.split('_')[1]))
    trunk_colors = [palette[k] for k in trunk_keys]
    num_tc = len(trunk_colors)

    shadow_keys = sorted([k for k in palette if k.startswith('shadow_')],
                         key=lambda k: int(k.split('_')[1]))
    shadow_colors = [palette[k] for k in shadow_keys]

    trunk_cx = W / 2.0

    # Birch trunk marks positions
    birch_marks = set()
    if tree_type == 'birch':
        mrng = random.Random(42 + variant * 11)
        for y in range(trunk_top, H - 3):
            if mrng.random() < 0.25:
                birch_marks.add(y)
                if mrng.random() < 0.5:
                    birch_marks.add(y + 1)

    for y in range(trunk_top, H):
        t = (y - trunk_top) / max(1, H - trunk_top - 1)

        # Trunk width varies by type
        if tree_type == 'pine':
            tw_half = 2.5 + t * 3.5
        elif tree_type == 'birch':
            tw_half = 2.0 + t * 3.0
        else:
            tw_half = 3.0 + t * 4.5

        # Root flare
        if t > 0.82:
            flare = (t - 0.82) / 0.18
            tw_half += flare * 3.0

        # Slight lean from noise
        lean = fbm(0, y, seed + 600, 2, 20.0) * 1.5

        x_start = int(trunk_cx + lean - tw_half)
        x_end = int(trunk_cx + lean + tw_half) + 1

        for x in range(max(0, x_start), min(W, x_end)):
            # Skip if crown pixel already there (trunk behind crown)
            if y < crown_h and pixels[y][x] is not None:
                continue

            rel_x = (x - x_start) / max(1, x_end - x_start - 1)
            # Bark lighting
            bark_light = 0.15 + 0.7 * rel_x
            # Bark texture
            bark_noise = fbm(x * 4, y * 2, seed + 300, 3, 3.0) * 0.15
            bark_light += bark_noise
            bark_light = max(0.0, min(1.0, bark_light))

            idx = int(bark_light * (num_tc - 1))
            idx = max(0, min(num_tc - 1, idx))

            # Birch dark marks
            if tree_type == 'birch' and y in birch_marks:
                # Dark horizontal bands
                idx = max(0, idx - 4)
                r, g, b = hex_to_rgb(trunk_colors[idx])
                r = max(0, r - 80)
                g = max(0, g - 80)
                b = max(0, b - 75)
                pixels[y][x] = rgb_to_hex(r, g, b)
            else:
                pixels[y][x] = trunk_colors[idx]

        # Root tendrils at the very bottom
        if t > 0.9:
            for dx_off in [-2, -1, 1, 2]:
                rx = x_start + dx_off if dx_off < 0 else x_end + dx_off - 1
                if 0 <= rx < W and pixels[y][rx] is None:
                    if rng.random() < 0.4 * (t - 0.9) / 0.1:
                        si = min(len(shadow_colors)-1, rng.randint(0, 2))
                        pixels[y][rx] = shadow_colors[si] if shadow_colors else trunk_colors[0]

    # ── Dead tree: bare branches ──
    if state == 'dead':
        branch_colors = trunk_colors[1:5] if len(trunk_colors) >= 5 else trunk_colors
        brng = random.Random(55 + variant)
        num_branches = brng.randint(4, 8)
        for _ in range(num_branches):
            by = brng.randint(int(crown_h * 0.15), trunk_top)
            direction = brng.choice([-1, 1])
            length = brng.randint(int(W*0.08), int(W*0.30))
            thickness = brng.randint(1, 2)
            x, y_pos = int(trunk_cx), by

            for step in range(length):
                for ty in range(thickness):
                    px, py = x, y_pos + ty
                    if 0 <= px < W and 0 <= py < H:
                        bi = min(len(branch_colors)-1, step * len(branch_colors) // (length + 1))
                        pixels[py][px] = branch_colors[bi]
                x += direction
                if brng.random() < 0.35:
                    y_pos -= 1
                elif brng.random() < 0.1:
                    y_pos += 1
                # Sub-branches
                if step > length // 3 and brng.random() < 0.2:
                    sx, sy = x, y_pos - 1
                    for _ in range(brng.randint(2, 5)):
                        if 0 <= sx < W and 0 <= sy < H:
                            pixels[sy][sx] = branch_colors[min(len(branch_colors)-1, 2)]
                        sx += direction
                        sy -= 1

    # ── Post-process: remove orphan pixels (no adjacent neighbors) ──
    pixels = remove_orphans(pixels, W, H)

    return pixels


def remove_orphans(pixels, W, H, min_neighbors=1):
    """Remove isolated pixels that have fewer than min_neighbors adjacent filled pixels."""
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


# ── Output functions ─────────────────────────────────────────

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

    # Add transparency key first
    palette = {'.': '#00000000'}
    palette.update(char_to_hex)
    data_str = "\n".join(rows)
    return palette, data_str


def write_sprite_yaml(path, name, img_type, output_dir, palette, data_entries, frame_delay=None):
    """Write a sprite YAML in the format compatible with pixel_artify.py / safe_load."""
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


# ── Main ─────────────────────────────────────────────────────

TREE_CONFIGS = {
    'tree': {
        'type': 'basic',
        'base_hue': (50, 120, 35),
        'dir': 'trees',
        'variants': [],
        'states': ['normal', 'damaged', 'dead'],
    },
    'oak_tree': {
        'type': 'oak',
        'base_hue': (55, 130, 38),
        'dir': 'oak_trees',
        'variants': ['a', 'b', 'c'],
        'states': ['normal', 'damaged', 'dead'],
    },
    'pine_tree': {
        'type': 'pine',
        'base_hue': (30, 90, 42),
        'dir': 'pine_trees',
        'variants': ['a', 'b', 'c'],
        'states': ['normal', 'damaged', 'dead'],
    },
    'birch_tree': {
        'type': 'birch',
        'base_hue': (60, 130, 42),
        'dir': 'birch_trees',
        'variants': ['a', 'b', 'c'],
        'states': ['normal', 'damaged', 'dead'],
    },
    'apple_tree': {
        'type': 'apple',
        'base_hue': (48, 125, 35),
        'dir': 'apple_trees',
        'variants': ['a', 'b', 'c'],
        'states': ['normal', 'damaged', 'dead'],
    },
}

# Target dimensions matching reference standard
SPRITE_W = 96
SPRITE_H = 128


def generate_all():
    yaml_base = 'assets/images'
    png_base = 'game/FarmGame/Content/Images'

    FRAME_DELAY = 800  # tree animation frame delay

    for tree_name, cfg in TREE_CONFIGS.items():
        tree_type = cfg['type']
        palette = build_palette_30(cfg['base_hue'], tree_type)
        img_dir = cfg['dir']

        print(f'\n── {tree_name} ({len(palette)} colors) ──')

        # Base states (static png)
        for state in cfg['states']:
            name = f'{tree_name}_{state}'
            pixels = generate_tree(SPRITE_W, SPRITE_H, tree_type, palette,
                                   variant=0, state=state)
            pal, data_str = pixels_to_yaml_data(pixels)

            yaml_path = os.path.join(yaml_base, img_dir, f'{name}.yaml')
            write_sprite_yaml(yaml_path, name, 'png', f'Images/{img_dir}', pal, [data_str])

            png_path = os.path.join(png_base, img_dir, f'{name}.png')
            pixels_to_png(pixels, png_path)

            nc = len(pal) - 1  # exclude '.'
            print(f'  {name}: {SPRITE_W}x{SPRITE_H}, {nc} colors')

        # Variant frames (animated gif — all frames in one YAML)
        for vi, variant in enumerate(cfg['variants']):
            base_name = f'{tree_name}_{variant}_normal'
            all_palettes = {'.': '#00000000'}
            frame_pixels = []

            for frame in range(3):
                pixels = generate_tree(SPRITE_W, SPRITE_H, tree_type, palette,
                                       variant=vi * 10 + frame + 1, state='normal')
                frame_pixels.append(pixels)

            # Build merged palette across all frames
            chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
            color_to_char = {}
            char_idx = 0
            for pixels in frame_pixels:
                for row in pixels:
                    for color in row:
                        if color is not None and color not in color_to_char:
                            if char_idx < len(chars):
                                color_to_char[color] = chars[char_idx]
                                all_palettes[chars[char_idx]] = color
                                char_idx += 1

            # Encode each frame with merged palette
            data_entries = []
            for pixels in frame_pixels:
                rows = []
                for row in pixels:
                    line = ""
                    for color in row:
                        if color is None:
                            line += "."
                        else:
                            line += color_to_char.get(color, "?")
                    rows.append(line)
                data_entries.append("\n".join(rows))

                # Also save individual frame PNGs
                fidx = len(data_entries) - 1
                png_path = os.path.join(png_base, img_dir, f'{base_name}_frame{fidx}.png')
                pixels_to_png(pixels, png_path)

            yaml_path = os.path.join(yaml_base, img_dir, f'{base_name}.yaml')
            write_sprite_yaml(yaml_path, base_name, 'gif', f'Images/{img_dir}',
                            all_palettes, data_entries, frame_delay=FRAME_DELAY)

            nc = len(all_palettes) - 1
            print(f'  {base_name}: {SPRITE_W}x{SPRITE_H} gif, {nc} colors, 3 frames')


if __name__ == '__main__':
    generate_all()
    print('\nDone!')
