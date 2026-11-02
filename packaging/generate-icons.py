#!/usr/bin/env python3
"""Generates the app icon and the MSIX/Store logos from the white tray silhouette.

Dev-time only (needs Pillow: `pip install pillow`); the outputs are committed, so builds never run this.
Source: assets/branding/moth-eidol-tray-icon.png (white silhouette, transparent). Background: obsidian #1D1511.
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "assets/branding/moth-eidol-tray-icon.png"
OBSIDIAN = (0x1D, 0x15, 0x11, 255)
MOTH = Image.open(SRC).convert("RGBA")
MOTH = MOTH.crop(MOTH.getbbox())


def tile(w, h, fill=0.62, radius=0.0):
    """Obsidian tile w x h with the moth centered, fitting `fill` of the shorter side."""
    scale = 4  # supersample for smooth edges
    W, H = w * scale, h * scale
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    bg = Image.new("RGBA", (W, H), OBSIDIAN)
    mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, W - 1, H - 1), radius=int(min(W, H) * radius), fill=255)
    img.paste(bg, (0, 0), mask)
    box = int(min(W, H) * fill)
    m = MOTH.copy()
    m.thumbnail((box, box), Image.LANCZOS)
    img.alpha_composite(m, ((W - m.width) // 2, (H - m.height) // 2))
    return img.resize((w, h), Image.LANCZOS)


assets = ROOT / "src/MothEidol.App/Assets"
tile(256, 256, radius=0.22).save(assets / "app-icon.png")
tile(256, 256, radius=0.22).save(assets / "app-icon.ico", sizes=[(s, s) for s in (16, 24, 32, 48, 64, 128, 256)])

msix = ROOT / "packaging/msix/Assets"
for name, (w, h) in {
    "StoreLogo.png": (50, 50), "Square44x44Logo.png": (44, 44), "Square71x71Logo.png": (71, 71),
    "Square150x150Logo.png": (150, 150), "Square310x310Logo.png": (310, 310),
    "Wide310x150Logo.png": (310, 150), "SplashScreen.png": (620, 300),
}.items():
    tile(w, h, fill=0.62 if w == h else 0.7).save(msix / name)

listing = ROOT / "packaging/store-listing"
listing.mkdir(exist_ok=True)
tile(300, 300, radius=0.0).save(listing / "app-tile-icon-300x300.png")

# Banner-derived images (only when the banner artwork is present).
BANNER = ROOT / "assets/branding/moth-eidol-banner.png"
if BANNER.exists():
    b = Image.open(BANNER).convert("RGB")
    bw, bh = b.size
    # GitHub social preview 1280x640 (2:1): central crop.
    h2 = bw // 2
    b.crop((0, (bh - h2) // 2, bw, (bh - h2) // 2 + h2)).resize((1280, 640), Image.LANCZOS).save(
        ROOT / "assets/branding/moth-eidol-social-preview.png")
    # MSIX splash 620x300: crop around the moth, name and slogan.
    cw = 1100
    ch = round(cw * 300 / 620)
    x0, y0 = (bw - cw) // 2, 40
    b.crop((x0, y0, x0 + cw, y0 + ch)).resize((620, 300), Image.LANCZOS).save(msix / "SplashScreen.png")
print("done")
