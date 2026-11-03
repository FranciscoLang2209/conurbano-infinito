"""Compose the repeating plaza floor and its two original, collider-free landmarks."""

from pathlib import Path
import random
import math

from generate_visual_slice import Canvas


OUT = Path(__file__).resolve().parents[1] / "Conurbano Infinito/Assets/_Game/Art/Sprites/Environment"
RNG = random.Random(1909)


def grass(c, x, y, kind):
    colors = ("grass_dark", "grass", "grass_light")
    c.rect(x, y, x + 31, y + 31, colors[kind])
    for ox, oy in ((4, 7), (21, 4), (12, 19), (27, 25)):
        color = "grass_light" if kind < 2 else "grass_sun"
        c.rect(x + ox, y + oy, x + ox + 3, y + oy + 1, color)
        c.dot(x + ox + 1, y + oy - 2, color)
    if kind == 0:
        c.rect(x + 24, y + 17, x + 28, y + 19, "grass")


def paving(c, x, y, size=32):
    c.rect(x, y, x + size - 1, y + size - 1, "pavement_dark")
    c.rect(x + 2, y + 2, x + size - 3, y + size - 3, "concrete")
    c.rect(x + 4, y + 4, x + size - 4, y + 5, "cream")
    c.rect(x + 6, y + size - 6, x + size - 5, y + size - 5, "pavement")
    if (x // size + y // size) % 3 == 0:
        c.line(x + 15, y + 25, x + 20, y + 19, "pavement")


def dirt(c, x, y):
    c.ellipse(x, y, 11, 5, "grass_dark")
    c.rect(x - 7, y - 1, x + 6, y + 2, "pavement")
    c.rect(x - 5, y - 1, x + 1, y, "ochre")


def tree(c, x, y):
    c.ellipse(x + 3, y + 10, 19, 7, "grass_dark")
    c.rect(x - 3, y - 3, x + 4, y + 11, "ink")
    c.rect(x - 1, y - 4, x + 3, y + 9, "ochre")
    c.ellipse(x, y - 8, 17, 13, "ink")
    c.ellipse(x - 2, y - 10, 15, 11, "grass_dark")
    c.ellipse(x + 4, y - 10, 10, 9, "grass")
    c.rect(x - 8, y - 16, x + 2, y - 13, "grass_light")
    c.rect(x + 5, y - 6, x + 10, y - 4, "grass_light")


def bench(c, x, y):
    c.rect(x - 18, y - 6, x + 18, y + 8, "ink")
    c.rect(x - 16, y - 4, x + 16, y - 1, "ochre")
    c.rect(x - 16, y + 1, x + 16, y + 4, "cream")
    c.rect(x - 16, y + 5, x + 16, y + 6, "ochre")
    c.rect(x - 13, y + 8, x - 9, y + 12, "pavement_dark")
    c.rect(x + 9, y + 8, x + 13, y + 12, "pavement_dark")


def bin_(c, x, y):
    c.rect(x - 6, y - 7, x + 6, y + 8, "ink")
    c.rect(x - 4, y - 5, x + 4, y + 6, "blue_dark")
    c.rect(x - 4, y - 5, x - 2, y + 6, "blue")
    c.rect(x - 8, y - 8, x + 8, y - 5, "pavement_dark")
    c.rect(x - 6, y - 8, x + 5, y - 7, "concrete")


def cone(c, x, y):
    c.rect(x - 7, y + 6, x + 7, y + 8, "ink")
    c.poly([(x, y - 11), (x - 6, y + 5), (x + 6, y + 5)], "red_dark")
    c.rect(x - 3, y - 3, x + 3, y, "cream")
    c.rect(x - 5, y + 2, x + 5, y + 4, "red")


def crate(c, x, y):
    c.rect(x - 9, y - 7, x + 9, y + 7, "ink")
    c.rect(x - 7, y - 5, x + 7, y + 5, "ochre")
    c.rect(x - 5, y - 3, x + 5, y + 3, "pavement")
    c.line(x - 7, y - 5, x + 7, y + 5, "pavement_dark")
    c.line(x + 7, y - 5, x - 7, y + 5, "pavement_dark")


def bag(c, x, y):
    c.poly([(x - 7, y - 2), (x - 4, y - 8), (x + 4, y - 8),
            (x + 8, y - 2), (x + 6, y + 7), (x - 6, y + 7)], "ink")
    c.poly([(x - 4, y - 4), (x + 3, y - 5), (x + 6, y + 5),
            (x - 4, y + 5)], "pavement_dark")
    c.rect(x - 3, y - 9, x + 2, y - 7, "pavement")


def bottle(c, x, y):
    c.rect(x - 2, y - 8, x + 2, y + 7, "ink")
    c.rect(x - 1, y - 5, x + 1, y + 5, "grass_light")
    c.rect(x - 2, y - 9, x + 2, y - 7, "cream")
    c.rect(x - 2, y + 1, x + 2, y + 3, "cream")


def paper(c, x, y):
    c.poly([(x - 5, y - 4), (x + 4, y - 6), (x + 6, y + 3),
            (x - 4, y + 5)], "cream")
    c.line(x - 2, y - 1, x + 3, y - 2, "pavement")


def litter(c, draw, x, y, variant):
    # Ground litter is smaller, muted and often lying down: no enemy eyes or pickup glow.
    stamp = Canvas(64)
    draw(stamp, 32, 32)
    shades = {"ink": "pavement_dark", "cream": "concrete",
              "grass_light": "turf_detail", "pavement": "concrete"}
    from generate_visual_slice import rgba
    shades = {rgba(source): rgba(target) for source, target in shades.items()}
    for sy, row in enumerate(stamp.pixels):
        for sx in range(64):
            pixel = bytes(row[sx * 4:sx * 4 + 4])
            if not pixel[3]: continue
            dx, dy = sx - 32, sy - 32
            if variant % 2: dx = -dx
            if draw in (bottle, paper) and variant % 3: dx, dy = dy, -dx
            factor = (.7, .85, 1)[variant % 3]
            px, py = x + round(dx * factor), y + round(dy * factor)
            assert 8 <= px < c.size - 8 and 8 <= py < c.size - 8, "Decoration must not cross repeat seam"
            c.pixels[py][px * 4:px * 4 + 4] = shades.get(pixel, pixel)


def sign(c, x, y):
    c.rect(x - 2, y - 4, x + 2, y + 22, "ink")
    c.rect(x - 1, y - 3, x + 1, y + 21, "pavement")
    c.poly([(x - 14, y - 11), (x + 12, y - 14),
            (x + 14, y + 1), (x - 12, y + 3)], "ink")
    c.rect(x - 11, y - 10, x + 11, y, "blue_dark")
    letters = ("111101111100100", "100100100100111", "010101111101101", "111001010100111", "010101111101101")
    for i, letter in enumerate(letters):
        for row in range(5):
            for col in range(3):
                if letter[row*3+col] == "1": c.dot(x-9+i*4+col, y-7+row, "cream")



def fountain(c, x, y):
    c.ellipse(x, y + 9, 35, 20, "ink")
    c.ellipse(x, y + 7, 31, 16, "concrete")
    c.ellipse(x, y + 7, 26, 12, "blue_dark")
    c.ellipse(x, y + 7, 22, 9, "blue")
    c.rect(x - 8, y - 8, x + 8, y + 8, "pavement_dark")
    c.rect(x - 5, y - 13, x + 5, y + 4, "concrete")
    c.rect(x - 3, y - 20, x + 3, y - 12, "cream")
    c.rect(x - 1, y - 25, x + 1, y - 20, "cyan_light")


def parrilla():
    c = Canvas(96)
    c.ellipse(48, 70, 38, 14, "grass_dark", 210)
    c.rect(13, 19, 82, 72, "ink")
    c.rect(17, 23, 78, 67, "red_dark")
    c.rect(21, 28, 74, 62, "pavement_dark")
    for y in (34, 42, 50, 58):
        c.rect(24, y, 72, y + 2, "concrete")
    c.rect(9, 10, 86, 23, "ink")
    c.rect(13, 12, 82, 21, "ochre")
    c.rect(20, 14, 75, 18, "cream")
    c.rect(26, 15, 34, 17, "red_dark")
    c.rect(42, 15, 51, 17, "red_dark")
    c.rect(59, 15, 68, 17, "red_dark")
    c.rect(20, 69, 30, 77, "ink")
    c.rect(66, 69, 76, 77, "ink")
    crate(c, 4, 74)
    bag(c, 83, 76)
    c.save(OUT / "ParrillaPlaza.png")


def kiosco():
    c = Canvas(96)
    c.ellipse(48, 74, 37, 12, "grass_dark", 210)
    c.rect(12, 21, 83, 75, "ink")
    c.rect(16, 24, 79, 72, "cream")
    c.rect(20, 28, 75, 54, "blue_dark")
    c.rect(25, 30, 70, 50, "blue")
    c.rect(21, 55, 74, 68, "ochre")
    c.rect(28, 57, 68, 60, "cream")
    c.rect(8, 13, 87, 27, "ink")
    c.rect(12, 15, 83, 24, "red_dark")
    for x in (15, 32, 49, 66):
        c.rect(x, 16, x + 7, 23, "cream")
    c.rect(25, 32, 36, 42, "yellow")
    c.rect(44, 33, 55, 44, "red")
    c.rect(62, 34, 68, 44, "grass_light")
    bin_(c, 84, 76)
    c.save(OUT / "KioscoPlaza.png")


def floor():
    RNG.seed(1909)
    c = Canvas(512)
    # Periodic contours keep the existing repeating material seamless at 48 world PPU.
    for y in range(512):
        for x in range(512):
            wave = math.sin(x * math.tau / 512) + math.cos(y * math.tau / 512) + math.sin((x+y) * math.tau / 256) * .4
            color = "turf_shade" if wave < -.8 else "turf_light" if wave > 1 else "turf"
            distance = min(abs(x-256) - (43 + round(3*math.sin(y*math.tau/128))),
                           abs(y-256) - (39 + round(3*math.sin(x*math.tau/256))))
            if distance < 0:
                color = "path_joint" if (x % 32 == 0 or y % 32 == 0) else "path"
            elif distance < 4 and (x+y) % 5 != 0:
                color = "path_joint" if distance < 2 else "turf_shade"
            c.dot(x, y, color)
    for _ in range(180):
        x, y = RNG.randrange(8, 504), RNG.randrange(8, 504)
        if abs(x-256) > 58 and abs(y-256) > 54:
            c.line(x, y, x+2, y, "turf_detail")
            if RNG.randrange(3) == 0: c.dot(x+1, y-1, "turf_detail")

    terrain = [bytes(row) for row in c.pixels]
    # Unequal distances and unpaired furniture: a busy fountain, a quiet lawn and service corners.
    for x, y in ((45, 54), (176, 48), (58, 178), (401, 61), (468, 169),
                 (52, 367), (183, 466), (414, 420), (471, 467)):
        tree(c, x, y)
    fountain(c, 122, 111)
    for x, y in ((91, 166), (376, 154), (158, 355), (391, 387), (65, 455)):
        bench(c, x, y)
    for x, y in ((159, 184), (452, 122), (79, 330), (448, 395)):
        bin_(c, x, y)
    for x, y in ((347, 78), (115, 420)):
        sign(c, x, y)
    # Dense pockets gather near bins/benches, with isolated scraps on the quieter lawns.
    objects = (
        (bag, 172, 195), (bag, 149, 200), (paper, 183, 182), (bottle, 139, 185),
        (paper, 166, 167), (paper, 68, 183), (bottle, 117, 174),
        (paper, 94, 133), (bottle, 153, 132), (paper, 83, 102),
        (crate, 472, 104), (crate, 489, 123), (bag, 458, 145),
        (paper, 434, 140), (paper, 480, 160), (bottle, 418, 160),
        (cone, 325, 184), (cone, 345, 196), (paper, 389, 185),
        (bottle, 373, 170), (paper, 338, 120), (paper, 485, 63),
        (bag, 65, 345), (bag, 85, 351), (paper, 98, 333),
        (bottle, 66, 320), (paper, 46, 342), (crate, 32, 454),
        (bottle, 81, 470), (paper, 110, 454), (paper, 168, 373),
        (paper, 193, 407), (bottle, 142, 393),
        (bag, 461, 412), (crate, 479, 390), (cone, 484, 354),
        (paper, 433, 411), (bottle, 414, 394), (paper, 456, 374),
        (paper, 358, 431), (bottle, 333, 341), (paper, 373, 366),
    )
    for variant, (draw, x, y) in enumerate(objects):
        litter(c, draw, x, y, variant)
    # The full-width walking/combat cross retains the exact current terrain pixels.
    for y, row in enumerate(c.pixels):
        for x in range(512):
            if abs(x - 256) < 44 or abs(y - 256) < 40:
                assert row[x*4:x*4+4] == terrain[y][x*4:x*4+4], "Keep circulation clear"
    c.pixels[-1][:] = c.pixels[0]
    for row in c.pixels: row[-4:] = row[:4]
    assert all(row[3::4].count(255) == 512 for row in c.pixels)
    assert c.pixels[0] == c.pixels[-1], "Vertical repeat seam"
    assert all(row[:4] == row[-4:] for row in c.pixels), "Horizontal repeat seam"
    c.save(OUT / "PlazaPiso.png")


if __name__ == "__main__":
    floor()
    parrilla()
    kiosco()
    first = (OUT / "PlazaPiso.png").read_bytes()
    floor()
    assert (OUT / "PlazaPiso.png").read_bytes() == first, "Plaza floor must regenerate identically"
    print("PlazaPiso + ParrillaPlaza + KioscoPlaza generated")
