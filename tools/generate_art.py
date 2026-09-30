#!/usr/bin/env python3
"""Generates the app icons and launch/splash images for The Ultra.

The artwork is drawn from the same pixel definitions the game uses (see
Ultra.Core/SpriteArt.cs and PixelFont.cs) so the icon matches the game.
Run from the repository root:  python3 tools/generate_art.py
Requires Pillow.
"""
import os
import random
import re
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

BLACK = (0, 0, 0)
RED = (255, 0, 0)
GREEN = (0, 255, 0)
YELLOW = (255, 255, 0)
BLUE = (0, 0, 255)
MAGENTA = (255, 0, 255)
CYAN = (0, 255, 255)
WHITE = (255, 255, 255)
RAINBOW = [RED, YELLOW, GREEN, CYAN, BLUE, MAGENTA, WHITE]
WAVE_COLOURS = [GREEN] * 16          # all the original aliens are green


def load_font():
    src = open(os.path.join(ROOT, "Ultra.Core", "PixelFont.cs")).read()
    glyphs = {}
    for ch, hexrows in re.findall(r"\['(\\'|.)'\] = \"([0-9A-F]{14})\"", src):
        ch = "'" if ch == "\\'" else ch
        glyphs[ch] = [int(hexrows[i * 2:i * 2 + 2], 16) for i in range(7)]
    return glyphs


def load_sprites():
    """Alien frames and the player's ship from the original game's data (OriginalData.cs)."""
    src = open(os.path.join(ROOT, "Ultra.Core", "OriginalData.cs")).read()
    frames_block = src[src.index("AlienFrames ="):src.index("public static readonly byte[][] Paths")]
    frames = [re.findall(r'"([.X]+)"', m.group(1)) for m in re.finditer(r"new\[\] \{ (.*?) \}", frames_block)]
    aliens = [frames[i:i + 3] for i in range(0, len(frames), 3)]      # [sheet][frame] -> rows
    ship_block = src[src.index("PlayerShip ="):src.index("public static readonly string[] Shot")]
    player = re.findall(r'"([.X]+)"', ship_block)
    return aliens, player


FONT = load_font()
ALIENS, PLAYER = load_sprites()


def draw_rows(img, rows, x, y, scale, colour):
    px = img.load()
    for ry, row in enumerate(rows):
        for rx, c in enumerate(row):
            if c == "X":
                for dy in range(scale):
                    for dx in range(scale):
                        xx, yy = x + rx * scale + dx, y + ry * scale + dy
                        if 0 <= xx < img.width and 0 <= yy < img.height:
                            px[xx, yy] = colour


def text_width(text, scale):
    return (len(text) * 6 - 1) * scale


def draw_text(img, text, x, y, scale, colour=None, banded=False):
    px = img.load()
    for i, ch in enumerate(text.upper()):
        rows = FONT.get(ch)
        if rows is None:
            continue
        for ry, bits in enumerate(rows):
            col = RAINBOW[ry % len(RAINBOW)] if banded else colour
            for rx in range(5):
                if bits & (0x10 >> rx):
                    for dy in range(scale):
                        for dx in range(scale):
                            xx = x + (i * 6 + rx) * scale + dx
                            yy = y + ry * scale + dy
                            if 0 <= xx < img.width and 0 <= yy < img.height:
                                px[xx, yy] = col


def draw_centred_text(img, text, y, scale, colour=None, banded=False):
    draw_text(img, text, (img.width - text_width(text, scale)) // 2, y, scale, colour, banded)


def stars(img, count, seed, max_size):
    rnd = random.Random(seed)
    px = img.load()
    for _ in range(count):
        x, y = rnd.randrange(img.width), rnd.randrange(img.height)
        c = rnd.choice([WHITE, CYAN, (90, 90, 255)])
        s = rnd.randint(1, max_size)
        for dy in range(s):
            for dx in range(s):
                if x + dx < img.width and y + dy < img.height:
                    px[x + dx, y + dy] = c


def icon_content(size, transparent):
    """Big pixel art: 'ULTRA' in Oric rainbow bands, an alien and the player's ship."""
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0) if transparent else BLACK + (255,))
    if not transparent:
        stars(img, size // 12, 7, max(2, size // 256))
    unit = size / 1024
    s_text = max(1, int(29 * unit))
    draw_centred_text(img, "ULTRA", int(110 * unit), s_text, banded=True)
    s_alien = max(1, int(30 * unit))
    alien = ALIENS[15][0]
    draw_rows(img, alien, (size - 12 * s_alien) // 2, int(330 * unit), s_alien, GREEN)
    s_ship = max(1, int(22 * unit))
    draw_rows(img, PLAYER, (size - 18 * s_ship) // 2, int(652 * unit), s_ship, CYAN)
    # a couple of shots between ship and alien
    shot_w = max(1, s_ship // 2)
    for sy in (int(612 * unit), int(640 * unit)):
        for dy in range(int(24 * unit)):
            for dx in range(shot_w):
                img.putpixel((size // 2 - shot_w // 2 + dx, sy - dy), YELLOW + (255,))
    return img


def make_icon(size):
    return icon_content(1024, False).convert("RGB").resize((size, size), Image.NEAREST if size >= 512 else Image.LANCZOS)


def make_foreground(size):
    """Adaptive-icon foreground: art inside the central safe zone on transparency."""
    canvas = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    art = icon_content(1024, True)
    inner = int(1024 * 0.62)
    art = art.resize((inner, inner), Image.NEAREST)
    canvas.paste(art, ((1024 - inner) // 2, (1024 - inner) // 2), art)
    return canvas.resize((size, size), Image.LANCZOS)


def make_splash(w, h):
    img = Image.new("RGB", (w, h), BLACK)
    stars(img, w // 10, 3, max(2, w // 640))
    u = w / 1920
    draw_centred_text(img, "THE ULTRA", int(150 * u), max(1, int(18 * u)), banded=True)
    s_alien = max(1, int(7 * u))
    for i in range(8):
        k = i * 2
        x = int((1920 / 2 - 4 * 180 + i * 180 + 90) * u) - 6 * s_alien
        draw_rows(img, ALIENS[k][0], x, int(380 * u), s_alien, WAVE_COLOURS[k])
    s_ship = max(1, int(8 * u))
    draw_rows(img, PLAYER, (w - 18 * s_ship) // 2, int(540 * u), s_ship, CYAN)
    draw_centred_text(img, "BY PFJ", int(740 * u), max(1, int(9 * u)), WHITE)
    draw_centred_text(img, "BASED ON THE PSS ORIC GAME", int(900 * u), max(1, int(4 * u)), YELLOW)
    return img


def make_feature_graphic():
    """Google Play feature graphic, 1024x500."""
    w, h = 1024, 500
    img = Image.new("RGB", (w, h), BLACK)
    stars(img, 140, 11, 2)
    draw_centred_text(img, "THE ULTRA", 40, 12, banded=True)
    for i, k in enumerate((0, 1, 2, 4, 5, 7, 11, 15)):
        x = 64 + i * 120
        draw_rows(img, ALIENS[k][0], x, 170, 5, GREEN)
    draw_rows(img, PLAYER, (w - 18 * 7) // 2, 280, 7, CYAN)
    for dy in range(0, 40, 14):
        for yy in range(8):
            img.putpixel((w // 2 - 1, 262 - dy - yy), WHITE)
            img.putpixel((w // 2, 262 - dy - yy), WHITE)
    draw_centred_text(img, "16 SHEETS OF CLASSIC ORIC ARCADE ACTION", 420, 3, YELLOW)
    draw_centred_text(img, "BY PFJ - BASED ON THE PSS ORIC GAME", 456, 2, MAGENTA)
    return img


def save(img, *parts):
    path = os.path.join(ROOT, *parts)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    print("wrote", os.path.relpath(path, ROOT))


def main():
    # iOS app icons
    ios_dir = ("Ultra.iOS", "AppIcon.xcassets", "AppIcon.appiconset")
    for s in (20, 29, 40, 58, 60, 76, 80, 87, 120, 152, 167, 180, 1024):
        save(make_icon(s), *ios_dir, f"icon_{s}x{s}.png")

    # iOS launch images (referenced by LaunchScreen.storyboard)
    save(make_splash(640, 360), "Ultra.iOS", "Resources", "LaunchImage.png")
    save(make_splash(1280, 720), "Ultra.iOS", "Resources", "LaunchImage@2x.png")
    save(make_splash(1920, 1080), "Ultra.iOS", "Resources", "LaunchImage@3x.png")

    # Android launcher icons (legacy + adaptive foreground) and splash
    densities = {"mdpi": 1, "hdpi": 1.5, "xhdpi": 2, "xxhdpi": 3, "xxxhdpi": 4}
    for name, f in densities.items():
        save(make_icon(int(48 * f)), "Ultra.Android", "Resources", f"mipmap-{name}", "icon.png")
        save(make_foreground(int(108 * f)), "Ultra.Android", "Resources", f"mipmap-{name}", "icon_foreground.png")
        save(make_splash(int(640 * f), int(360 * f)), "Ultra.Android", "Resources", f"drawable-{name}", "splash.png")

    # Store / README artwork
    save(make_icon(512), "art", "icon-512.png")
    save(make_feature_graphic(), "store", "google-play", "feature-graphic-1024x500.png")
    save(make_icon(512), "store", "google-play", "icon-512.png")
    save(make_icon(1024), "store", "app-store", "icon-1024.png")
    save(make_splash(1920, 1080), "art", "splash-1920x1080.png")


if __name__ == "__main__":
    main()
