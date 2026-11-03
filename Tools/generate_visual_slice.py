"""Draw original Plaza del Conurbano pixel art with the stdlib."""

from pathlib import Path
import re
import struct
import zlib


ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Conurbano Infinito/Assets/_Game/Art/Sprites"
PALETTE = {
    "ink": "18242C", "shadow": "2B3A3B", "concrete": "A89C86",
    "cream": "E8D7B2", "white": "F9EED7", "blue_dark": "283F65",
    "blue": "4079BA", "cyan": "61BBD3", "cyan_light": "B5E6DF",
    "ochre": "C7932E", "yellow": "F2C447", "red_dark": "9D3C39",
    "red": "E25E4C", "skin": "C98A62", "plum": "694668",
    "violet": "A15CAC", "grass_dark": "2F5843",
    "grass": "4E8451", "grass_light": "84B96A", "grass_sun": "B9D879",
    "pavement_dark": "514E4B", "pavement": "746C62",
    "turf": "54735A", "turf_shade": "506E56", "turf_light": "58785E",
    "turf_detail": "648264", "path": "9B947F", "path_joint": "898574",
}


def rgba(name, alpha=255):
    return bytes.fromhex(PALETTE[name]) + bytes((alpha,))


class Canvas:
    def __init__(self, size):
        self.size = size
        self.pixels = [bytearray(size * 4) for _ in range(size)]

    def dot(self, x, y, color, alpha=255):
        if 0 <= x < self.size and 0 <= y < self.size:
            self.pixels[y][x * 4:x * 4 + 4] = rgba(color, alpha)

    def rect(self, x0, y0, x1, y1, color, alpha=255):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.dot(x, y, color, alpha)

    def ellipse(self, cx, cy, rx, ry, color, alpha=255):
        for y in range(cy - ry, cy + ry + 1):
            for x in range(cx - rx, cx + rx + 1):
                if (x - cx) ** 2 * ry ** 2 + (y - cy) ** 2 * rx ** 2 <= rx ** 2 * ry ** 2:
                    self.dot(x, y, color, alpha)

    def poly(self, points, color, alpha=255):
        x0, x1 = min(p[0] for p in points), max(p[0] for p in points)
        y0, y1 = min(p[1] for p in points), max(p[1] for p in points)
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                inside = False
                for i, (ax, ay) in enumerate(points):
                    bx, by = points[i - 1]
                    if (ay > y + 0.5) != (by > y + 0.5):
                        if x + 0.5 < (bx - ax) * (y + 0.5 - ay) / (by - ay) + ax:
                            inside = not inside
                if inside:
                    self.dot(x, y, color, alpha)

    def line(self, x0, y0, x1, y1, color):
        steps = max(abs(x1 - x0), abs(y1 - y0))
        for i in range(steps + 1):
            self.dot(round(x0 + (x1 - x0) * i / max(1, steps)),
                     round(y0 + (y1 - y0) * i / max(1, steps)), color)

    def save(self, path):
        path.parent.mkdir(parents=True, exist_ok=True)

        def chunk(kind, data):
            return (struct.pack(">I", len(data)) + kind + data +
                    struct.pack(">I", zlib.crc32(kind + data)))

        raw = b"".join(b"\0" + row for row in self.pixels)
        header = struct.pack(">2I5B", self.size, self.size, 8, 6, 0, 0, 0)
        path.write_bytes(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", header) +
                         chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))


def player():
    c = Canvas(48)
    c.ellipse(24, 45, 18, 2, "ink", 120)
    # Shoes, legs and the red backpack remain readable behind the coat.
    c.rect(17, 34, 23, 42, "ink")
    c.rect(18, 35, 22, 40, "blue_dark")
    c.rect(25, 34, 31, 42, "ink")
    c.rect(26, 35, 30, 40, "blue_dark")
    c.rect(15, 42, 23, 44, "ink")
    c.rect(25, 42, 33, 44, "ink")
    c.rect(16, 42, 22, 43, "concrete")
    c.rect(26, 42, 32, 43, "concrete")
    c.poly([(8, 18), (16, 16), (20, 20), (18, 36), (10, 37), (6, 31)], "ink")
    c.poly([(9, 20), (16, 18), (18, 22), (16, 34), (10, 35), (8, 30)], "red_dark")
    c.rect(10, 21, 12, 30, "red")
    c.rect(14, 19, 16, 31, "red")
    # Oversized sleeves and yellow hoodie.
    c.poly([(15, 17), (32, 17), (37, 22), (34, 37), (15, 37), (11, 23)], "ink")
    c.poly([(16, 19), (31, 19), (35, 23), (32, 35), (17, 35), (13, 23)], "ochre")
    c.rect(18, 20, 29, 34, "yellow")
    c.rect(21, 27, 27, 31, "ochre")
    c.rect(20, 25, 28, 26, "white")
    c.poly([(12, 22), (15, 21), (18, 30), (16, 35), (12, 34), (9, 29)], "ink")
    c.poly([(12, 23), (14, 23), (16, 30), (15, 32), (12, 32), (11, 28)], "yellow")
    c.rect(12, 33, 15, 35, "skin")
    c.poly([(32, 21), (35, 21), (39, 29), (36, 34), (33, 34), (31, 28)], "ink")
    c.poly([(33, 23), (35, 23), (37, 29), (35, 32), (33, 31)], "yellow")
    c.rect(35, 32, 38, 35, "skin")
    # Thermos: pale vessel, dark cap and a wide red band.
    c.rect(37, 19, 43, 37, "ink")
    c.rect(38, 23, 42, 35, "cream")
    c.rect(38, 23, 39, 35, "concrete")
    c.rect(38, 29, 42, 31, "red")
    c.rect(38, 20, 42, 23, "blue_dark")
    c.rect(39, 20, 41, 21, "blue")
    c.rect(38, 36, 42, 37, "blue_dark")
    # Cap and a small face plane, seen from slightly above.
    c.ellipse(24, 15, 10, 9, "ink")
    c.rect(18, 16, 29, 21, "skin")
    c.rect(20, 19, 27, 21, "red_dark")
    c.poly([(18, 8), (21, 5), (28, 5), (31, 8), (32, 15), (17, 15)], "ink")
    c.poly([(19, 8), (22, 6), (27, 6), (30, 9), (30, 13), (18, 13)], "blue")
    c.rect(20, 8, 25, 10, "cyan")
    c.rect(13, 14, 34, 17, "ink")
    c.rect(15, 14, 32, 16, "blue_dark")
    c.rect(19, 14, 30, 15, "blue")
    c.save(ART / "Player/ElPibeDelBarrio.png")


def mosquito():
    c = Canvas(32)
    # Four long legs and angular wings give an unmistakable insect silhouette.
    for left, right in [((13, 18), (3, 21)), ((13, 21), (5, 27)),
                        ((19, 18), (29, 21)), ((19, 21), (27, 27))]:
        c.line(*left, *right, "ink")
    c.poly([(14, 11), (9, 5), (1, 3), (2, 13), (11, 17)], "ink")
    c.poly([(13, 12), (8, 6), (3, 5), (4, 12), (10, 15)], "cyan_light")
    c.poly([(18, 11), (23, 5), (31, 3), (30, 13), (21, 17)], "ink")
    c.poly([(19, 12), (24, 6), (29, 5), (28, 12), (22, 15)], "cyan_light")
    c.line(7, 9, 12, 13, "cyan")
    c.line(25, 9, 20, 13, "cyan")
    c.ellipse(16, 12, 5, 8, "ink")
    c.ellipse(16, 11, 3, 6, "violet")
    c.rect(15, 6, 16, 11, "plum")
    c.ellipse(16, 20, 5, 5, "ink")
    c.ellipse(16, 20, 3, 3, "violet")
    c.rect(12, 20, 13, 21, "red")
    c.rect(19, 20, 20, 21, "red")
    c.rect(15, 24, 17, 30, "ink")
    c.rect(16, 25, 16, 29, "plum")
    c.save(ART / "Enemies/MosquitoGigante.png")


def paloma():
    c = Canvas(32)
    # Broad feathered wings and tail contrast with the mosquito's pointed wings.
    c.poly([(10, 13), (4, 11), (0, 17), (2, 25), (8, 23), (13, 20)], "ink")
    c.poly([(9, 14), (5, 13), (2, 18), (3, 23), (8, 21), (12, 19)], "concrete")
    c.poly([(22, 13), (28, 11), (32, 17), (30, 25), (24, 23), (19, 20)], "ink")
    c.poly([(23, 14), (27, 13), (30, 18), (29, 23), (24, 21), (20, 19)], "concrete")
    c.line(3, 20, 8, 18, "white")
    c.line(29, 20, 24, 18, "white")
    c.poly([(12, 23), (20, 23), (22, 29), (19, 31), (16, 28), (13, 31), (10, 29)], "ink")
    c.poly([(13, 23), (19, 23), (20, 28), (16, 27), (12, 28)], "concrete")
    c.ellipse(16, 18, 9, 9, "ink")
    c.ellipse(16, 17, 7, 7, "cream")
    c.rect(11, 19, 21, 21, "concrete")
    c.ellipse(16, 10, 5, 6, "ink")
    c.ellipse(16, 10, 4, 5, "concrete")
    c.rect(13, 9, 19, 10, "white")
    c.rect(12, 8, 13, 9, "ink")
    c.rect(19, 8, 20, 9, "ink")
    c.poly([(14, 4), (18, 4), (16, 1)], "ink")
    c.poly([(15, 4), (17, 4), (16, 2)], "yellow")
    c.save(ART / "Enemies/PalomaDePlaza.png")


def termo():
    c = Canvas(24)
    # Handle and lid keep the silhouette separate from a bullet.
    c.rect(16, 8, 22, 17, "ink")
    c.rect(18, 10, 20, 14, "white", 0)
    c.rect(5, 5, 18, 22, "ink")
    c.rect(6, 7, 17, 20, "cream")
    c.rect(6, 7, 8, 20, "concrete")
    c.rect(14, 7, 17, 20, "blue_dark")
    c.rect(8, 8, 12, 10, "white")
    c.rect(6, 12, 17, 15, "red_dark")
    c.rect(7, 12, 15, 14, "red")
    c.rect(6, 20, 17, 22, "ink")
    c.rect(7, 20, 16, 21, "blue_dark")
    c.rect(7, 2, 16, 6, "ink")
    c.rect(8, 3, 15, 5, "blue")
    c.rect(10, 1, 13, 2, "ink")
    c.rect(10, 4, 13, 4, "cyan_light")
    c.save(ART / "Weapons/TermoBalistico.png")


def boleadora():
    c = Canvas(24)
    # Two weighted ends and a visible cord, symmetric while flying straight.
    c.line(7, 7, 17, 17, "ink")
    c.line(8, 7, 18, 17, "cream")
    c.ellipse(6, 6, 5, 5, "ink")
    c.ellipse(6, 6, 3, 3, "concrete")
    c.rect(4, 3, 6, 4, "white")
    c.ellipse(18, 18, 5, 5, "ink")
    c.ellipse(18, 18, 3, 3, "concrete")
    c.rect(16, 15, 18, 16, "white")
    c.save(ART / "Weapons/Boleadora.png")


def chancleta():
    c = Canvas(24)
    # Broad sole and V strap remain legible at the existing flat scale.
    c.poly([(2, 12), (5, 8), (17, 7), (22, 10), (21, 15),
            (16, 18), (5, 17), (2, 15)], "ink")
    c.poly([(3, 12), (6, 9), (17, 8), (21, 10), (20, 14),
            (16, 17), (5, 16), (3, 14)], "cream")
    c.line(7, 10, 12, 14, "red_dark")
    c.line(18, 9, 12, 14, "red_dark")
    c.rect(11, 13, 13, 15, "red")
    c.rect(4, 15, 18, 16, "blue_dark")
    c.save(ART / "Weapons/ChancletaTeledirigida.png")


def fernet():
    c = Canvas(24)
    # A short tumbler with dark drink, pale foam and one ice cube.
    c.poly([(4, 4), (20, 4), (18, 21), (6, 21)], "ink")
    c.poly([(6, 6), (18, 6), (17, 19), (7, 19)], "cream")
    c.poly([(7, 10), (17, 10), (16, 18), (8, 18)], "pavement_dark")
    c.rect(9, 11, 12, 14, "concrete")
    c.rect(10, 11, 11, 12, "white")
    c.rect(7, 7, 17, 9, "white")
    c.rect(9, 20, 15, 22, "ink")
    c.save(ART / "Weapons/FernetOrbital.png")


def bolsa():
    c = Canvas(48)
    # Open handles and an irregular hem keep the bag readable at game scale.
    c.poly([(12, 15), (18, 13), (17, 7), (20, 4), (26, 4), (29, 7),
            (28, 13), (35, 15), (39, 21), (40, 36), (36, 43),
            (29, 41), (24, 45), (17, 42), (11, 44), (8, 36), (9, 22)], "ink")
    c.poly([(13, 17), (19, 15), (20, 7), (26, 7), (27, 15), (34, 17),
            (37, 22), (38, 35), (35, 40), (29, 39), (24, 42),
            (17, 40), (12, 41), (10, 35), (11, 23)], "ochre")
    c.rect(22, 7, 24, 14, "ink")
    c.poly([(13, 18), (19, 17), (20, 23), (17, 32), (13, 34),
            (11, 29)], "yellow")
    c.poly([(27, 18), (34, 18), (37, 25), (35, 34), (30, 31)], "cream")
    c.poly([(17, 24), (30, 23), (32, 27), (28, 30), (19, 29)], "ink")
    c.rect(19, 25, 29, 26, "shadow")
    c.line(14, 35, 19, 39, "shadow")
    c.line(32, 34, 28, 39, "shadow")
    c.rect(23, 34, 25, 39, "cream")
    c.save(ART / "Enemies/BolsaVoladora.png")


def jefa():
    c = Canvas(48)
    # Large coat, dark hair and bright collar distinguish the miniboss.
    c.poly([(14, 10), (19, 5), (29, 5), (35, 11), (36, 28),
            (32, 34), (15, 34), (11, 26)], "ink")
    c.poly([(15, 11), (20, 7), (28, 7), (33, 12), (34, 27),
            (29, 31), (17, 31), (13, 25)], "plum")
    c.rect(18, 17, 30, 27, "skin")
    c.rect(19, 18, 29, 20, "cream")
    c.rect(19, 22, 21, 23, "ink")
    c.rect(27, 22, 29, 23, "ink")
    c.rect(23, 26, 27, 27, "red_dark")
    c.poly([(14, 29), (19, 27), (24, 31), (29, 27), (35, 29),
            (42, 40), (37, 46), (11, 46), (6, 40)], "ink")
    c.poly([(15, 31), (19, 29), (24, 35), (29, 29), (34, 31),
            (39, 40), (36, 44), (12, 44), (9, 40)], "red_dark")
    c.poly([(18, 30), (24, 35), (30, 30), (27, 42), (21, 42)], "violet")
    c.poly([(21, 31), (24, 35), (27, 31), (25, 41), (23, 41)], "cream")
    c.rect(22, 37, 26, 39, "yellow")
    c.poly([(8, 34), (12, 33), (16, 40), (12, 44), (6, 41)], "plum")
    c.poly([(36, 33), (40, 34), (42, 41), (36, 44), (32, 40)], "plum")
    c.save(ART / "Enemies/LaJefa.png")


def gema():
    # Keep the placeholder's 16 px canvas and 64 PPU: same world footprint.
    c = Canvas(16)
    c.poly([(8, 0), (14, 5), (13, 10), (8, 15), (2, 10), (1, 5)], "ink")
    c.poly([(8, 1), (13, 5), (12, 10), (8, 14), (3, 10), (2, 5)], "blue")
    c.poly([(8, 2), (12, 6), (8, 12), (4, 9), (3, 6)], "cyan")
    c.poly([(8, 2), (12, 6), (8, 7), (4, 5)], "cyan_light")
    c.rect(7, 3, 8, 5, "white")
    c.save(ART / "Pickups/GemaExperiencia.png")


if __name__ == "__main__":
    documented = set(re.findall(r"#[0-9A-F]{6}", (ROOT / "Docs/ArtDirection.md").read_text()))
    assert all("#" + value in documented for value in PALETTE.values())
    for draw in (player, mosquito, paloma, termo, boleadora, chancleta, fernet,
                 bolsa, gema, jefa):
        draw()
    print("Generated ten original sprites from the approved 22-color palette.")
