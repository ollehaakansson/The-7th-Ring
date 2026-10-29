#!/usr/bin/env python3
"""
Recolors pixel art onto the Doom palette, optionally graded for one circle of Hell.

  python3 scripts/recolor.py art/ -o assets/sprites/              palette only
  python3 scripts/recolor.py art/ -o assets/sprites/wrath --circle wrath
  python3 scripts/recolor.py --selftest

Inputs can be PNG files or folders (searched recursively; the folder layout is kept).
Needs Pillow: pip install pillow
"""
import argparse
import sys
from pathlib import Path

from PIL import Image

PALETTE = Path(__file__).resolve().parent.parent / "assets/palettes/doom.png"

CIRCLES = {
    "limbo":     {"grade": (0.85, 0.85, 0.85), "extra": []},
    "lust":      {"grade": (1.10, 0.70, 0.95), "extra": []},
    "gluttony":  {"grade": (0.85, 1.05, 0.60), "extra": []},
    "greed":     {"grade": (1.15, 1.00, 0.55), "extra": []},
    "wrath":     {"grade": (1.15, 0.75, 0.60), "extra": []},
    "heresy":    {"grade": (1.25, 0.65, 0.45), "extra": []},
    "violence":  {"grade": (1.20, 0.55, 0.50), "extra": []},
    "fraud":     {"grade": (0.75, 0.95, 0.85), "extra": []},
    "treachery": {"grade": (0.65, 0.85, 1.25), "extra": ["#1c2a4a", "#3a5a8c", "#6e9ccf", "#b8d8f0"]},
}


def doom_colors():
    raw = Image.open(PALETTE).convert("RGB").tobytes()
    return [tuple(raw[i:i + 3]) for i in range(0, len(raw), 3)]


def pixels(img):
    return [img.getpixel((x, y)) for y in range(img.height) for x in range(img.width)]


def load_palette(extra=()):
    colors = doom_colors()
    colors += [tuple(int(h.lstrip("#")[i:i + 2], 16) for i in (0, 2, 4)) for h in extra]
    # PIL palettes hold 256 colors, so extras replace Doom's unused magentas at the end.
    colors = (colors[:256 - len(extra)] + colors[256:])[:256]
    pal = Image.new("P", (1, 1))
    pal.putpalette([c for rgb in colors for c in rgb])
    return pal


def recolor(img, pal, grade=None):
    rgba = img.convert("RGBA")
    rgb = rgba.convert("RGB")
    if grade:
        rgb = Image.merge("RGB", [ch.point(lambda v, k=k: min(255, int(v * k))) for ch, k in zip(rgb.split(), grade)])
    out = rgb.quantize(palette=pal, dither=Image.Dither.NONE).convert("RGBA")
    out.putalpha(rgba.getchannel("A"))
    return out


def selftest():
    pal = load_palette()
    allowed = set(doom_colors())
    img = Image.new("RGBA", (4, 1))
    img.putdata([(123, 45, 200, 255), (0, 0, 0, 0), (250, 250, 250, 128), (90, 60, 30, 255)])
    out = recolor(img, pal, CIRCLES["wrath"]["grade"])
    assert all(px[:3] in allowed for px in pixels(out)), "pixel left the palette"
    assert [px[3] for px in pixels(out)] == [255, 0, 128, 255], "alpha changed"
    ice = CIRCLES["treachery"]
    blue = recolor(Image.new("RGB", (1, 1), (58, 90, 140)), load_palette(ice["extra"]))
    assert blue.getpixel((0, 0))[:3] == (58, 90, 140), "extra color not usable"
    print("selftest ok")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("inputs", nargs="*", type=Path)
    ap.add_argument("-o", "--out", type=Path)
    ap.add_argument("--circle", choices=CIRCLES)
    ap.add_argument("--selftest", action="store_true")
    a = ap.parse_args()
    if a.selftest:
        return selftest()
    if not a.inputs or not a.out:
        ap.error("give input files/folders and -o OUT")

    c = CIRCLES.get(a.circle, {})
    pal = load_palette(c.get("extra", ()))
    jobs = []
    for src in a.inputs:
        if src.is_dir():
            jobs += [(f, f.relative_to(src)) for f in sorted(src.rglob("*.png"))]
        elif src.suffix.lower() == ".png":
            jobs.append((src, Path(src.name)))
        else:
            sys.exit(f"not a PNG or folder: {src}")
    for src, rel in jobs:
        dst = a.out / rel
        if dst.resolve() == src.resolve():
            sys.exit(f"refusing to overwrite the source: {src}")
        dst.parent.mkdir(parents=True, exist_ok=True)
        recolor(Image.open(src), pal, c.get("grade")).save(dst)
    print(f"recolored {len(jobs)} file(s) -> {a.out}")


if __name__ == "__main__":
    main()
