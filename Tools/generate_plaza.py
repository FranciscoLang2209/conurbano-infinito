"""Compose the repeating plaza floor and its two original, collider-free landmarks."""

from pathlib import Path
import random

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


def sign(c, x, y):
    c.rect(x - 2, y - 4, x + 2, y + 22, "ink")
    c.rect(x - 1, y - 3, x + 1, y + 21, "pavement")
    c.poly([(x - 14, y - 11), (x + 12, y - 14),
            (x + 14, y + 1), (x - 12, y + 3)], "ink")
    c.poly([(x - 11, y - 9), (x + 10, y - 11),
            (x + 11, y - 1), (x - 10, y + 1)], "cream")
    c.line(x - 8, y - 5, x + 7, y - 7, "red_dark")


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
    for y in range(0, 512, 32):
        for x in range(0, 512, 32):
            grass(c, x, y, RNG.choices((0, 1, 2), (2, 8, 1))[0])

    # Paving crosses both tile boundaries: the existing plane can still repeat forever.
    for x in range(0, 512, 32):
        for y in (224, 256):
            paving(c, x, y)
    for y in range(0, 512, 32):
        for x in (224, 256):
            paving(c, x, y)

    for x, y in ((42, 46), (170, 48), (388, 50), (468, 150),
                 (64, 365), (178, 441), (412, 428), (474, 356)):
        tree(c, x, y)
    for x, y in ((135, 153), (374, 164), (137, 354), (373, 367)):
        bench(c, x, y)
    fountain(c, 124, 94)
    for x, y in ((72, 214), (362, 296), (180, 280), (411, 398), (329, 102)):
        dirt(c, x, y)
    for x, y in ((306, 193), (200, 329), (461, 303)):
        bin_(c, x, y)
    for x, y in ((164, 196), (333, 378), (94, 300)):
        sign(c, x, y)

    # The risk areas sit at x=-3 and x=+3 world units (tile x=112 and 400).
    # Clutter, rather than a colored patch, identifies the busier parrilla sector.
    for x, y in ((353, 211), (444, 206), (348, 314), (455, 308), (479, 250)):
        bag(c, x, y)
    for x, y in ((365, 227), (462, 230), (341, 279), (438, 319)):
        crate(c, x, y)
    for x, y in ((334, 241), (469, 274), (388, 333), (459, 182)):
        cone(c, x, y)
    for x, y in ((353, 262), (426, 198), (474, 321), (395, 307),
                 (73, 274), (148, 300), (178, 236)):
        bottle(c, x, y)
    for x, y in ((371, 194), (473, 215), (331, 287), (450, 337),
                 (74, 308), (168, 213), (126, 326), (191, 261)):
        paper(c, x, y)
    for x, y in ((307, 277), (475, 276), (365, 344), (78, 326)):
        dirt(c, x, y)
    for x, y in ((58, 180), (204, 100), (307, 454), (487, 418),
                 (102, 411), (443, 94), (197, 381)):
        paper(c, x, y)
    c.save(OUT / "PlazaPiso.png")


if __name__ == "__main__":
    floor()
    parrilla()
    kiosco()
    first = (OUT / "PlazaPiso.png").read_bytes()
    floor()
    assert (OUT / "PlazaPiso.png").read_bytes() == first, "Plaza floor must regenerate identically"
    print("PlazaPiso + ParrillaPlaza + KioscoPlaza generated")
