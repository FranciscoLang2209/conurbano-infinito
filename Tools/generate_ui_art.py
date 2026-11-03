"""Original UI sprites; same pixel grid and palette as the game's existing art."""
from generate_visual_slice import Canvas, ROOT

OUT = ROOT / "Conurbano Infinito/Assets/_Game/Resources/BarrioUi"


def surface(name, face, edge="ink"):
    c = Canvas(32)
    c.poly([(3, 0), (28, 0), (32, 4), (32, 28), (28, 32),
            (3, 32), (0, 28), (0, 4)], edge)
    c.poly([(4, 1), (27, 1), (31, 5), (31, 27), (27, 31),
            (4, 31), (1, 27), (1, 5)], face)
    c.line(6, 2, 25, 2, "white" if face == "cream" else "shadow")
    c.line(7, 29, 24, 29, "concrete" if face == "cream" else "ink")
    for x, y in ((3, 5), (28, 26)):
        c.dot(x, y, edge)
    c.save(OUT / (name + ".png"))


# Native HUD sizes: one authored pixel per reference-screen pixel, never a 16 px enlargement.
ICON_SIZES = {"heart": 24, "xp": 16, "flame": 30, "clock": 32, "crest": 48,
              "shoe": 24, "fist": 24, "rapid": 24, "arrow": 24, "pin": 16, "mate": 32,
              **dict.fromkeys(("weapon_thermos", "weapon_aerosol", "weapon_boleadora",
                               "weapon_slipper", "weapon_fernet", "weapon_firecracker", "weapon_ball"), 40)}


def icon(name):
    c = Canvas(ICON_SIZES[name])
    if name == "heart":
        c.poly([(2, 5), (5, 2), (9, 2), (12, 5), (15, 2), (19, 2),
                (22, 5), (22, 11), (19, 15), (12, 22), (5, 15), (2, 11)], "ink")
        c.poly([(3, 6), (6, 3), (9, 3), (12, 7), (15, 3), (18, 3),
                (21, 6), (21, 11), (18, 15), (12, 20), (6, 14), (3, 10)], "red_dark")
        c.poly([(3, 6), (6, 3), (9, 3), (12, 7), (15, 3), (18, 3),
                (20, 6), (19, 11), (12, 18), (6, 13), (3, 10)], "red")
        c.rect(6, 5, 8, 6, "white")
        c.rect(5, 6, 6, 8, "white")
        c.dot(16, 5, "skin")
        c.line(13, 18, 18, 13, "skin")
    elif name == "xp":
        c.poly([(8, 1), (10, 5), (15, 5), (12, 9), (13, 15),
                (8, 12), (3, 15), (4, 9), (1, 5), (6, 5)], "ink")
        c.poly([(8, 3), (10, 6), (13, 6), (10, 9), (11, 12),
                (8, 10), (5, 12), (6, 9), (3, 6), (7, 6)], "cyan")
        c.line(8, 4, 8, 8, "white")
        c.line(4, 6, 7, 7, "cyan_light")
        c.dot(11, 2, "yellow")
    elif name == "flame":
        # Battered barrio loudspeaker: cabinet + cone + radiating noise.
        c.poly([(3, 7), (17, 5), (19, 7), (19, 25), (4, 27), (2, 25), (2, 9)], "ink")
        c.poly([(4, 8), (16, 6), (18, 8), (18, 24), (4, 26), (3, 24), (3, 10)], "ochre")
        c.poly([(4, 8), (15, 7), (15, 24), (4, 25)], "red_dark")
        c.line(5, 8, 14, 7, "red")
        c.ellipse(9, 12, 3, 3, "ink")
        c.ellipse(9, 12, 2, 2, "cream")
        c.dot(8, 11, "white")
        c.ellipse(10, 20, 5, 5, "ink")
        c.ellipse(10, 20, 4, 4, "cream")
        c.ellipse(10, 20, 2, 2, "shadow")
        c.dot(9, 19, "white")
        c.line(21, 11, 23, 13, "yellow")
        c.line(23, 13, 23, 19, "yellow")
        c.line(23, 19, 21, 21, "yellow")
        c.line(25, 8, 27, 11, "red")
        c.line(27, 11, 27, 21, "red")
        c.line(27, 21, 25, 24, "red")
        c.line(19, 2, 21, 4, "yellow")
        c.line(23, 3, 24, 1, "cream")
    elif name == "clock":
        c.rect(12, 1, 19, 5, "ink")
        c.rect(13, 2, 18, 3, "ochre")
        c.rect(15, 4, 16, 6, "ink")
        c.line(24, 6, 27, 9, "ink")
        c.line(25, 6, 27, 8, "cream")
        c.ellipse(15, 18, 12, 12, "ink")
        c.ellipse(15, 17, 11, 11, "ochre")
        c.ellipse(15, 17, 10, 10, "cream")
        c.ellipse(15, 17, 9, 9, "white")
        c.line(9, 9, 18, 8, "cyan_light")
        for x, y in ((15, 9), (7, 17), (23, 17), (15, 25)):
            c.dot(x, y, "ink")
        c.rect(14, 11, 15, 18, "ink")
        c.rect(15, 17, 21, 18, "red_dark")
        c.dot(15, 17, "yellow")
    elif name == "crest":
        # Club pennant with an uninterrupted cream field behind the live number.
        c.poly([(2, 5), (10, 5), (14, 2), (34, 2), (38, 5), (46, 5),
                (44, 30), (36, 39), (24, 46), (12, 39), (4, 30)], "ink")
        c.poly([(3, 6), (11, 6), (15, 3), (33, 3), (37, 6), (45, 6),
                (43, 29), (35, 38), (24, 44), (13, 38), (5, 29)], "ochre")
        c.poly([(5, 7), (12, 7), (16, 4), (32, 4), (36, 7), (43, 7),
                (41, 28), (34, 36), (24, 42), (14, 36), (7, 28)], "yellow")
        c.poly([(8, 10), (40, 10), (38, 28), (32, 34), (24, 39), (16, 34), (10, 28)], "ink")
        c.poly([(9, 11), (39, 11), (37, 27), (31, 33), (24, 37), (17, 33), (11, 27)], "cream")
        c.poly([(10, 11), (37, 11), (35, 26), (30, 31), (24, 35), (17, 31), (12, 26)], "white")
        c.rect(13, 6, 34, 8, "cyan")
        c.rect(14, 6, 31, 6, "cyan_light")
        c.dot(24, 40, "white")
        for x in (5, 42): c.rect(x, 12, x, 18, "white")
    elif name == "shoe":
        c.poly([(3, 4), (10, 4), (12, 10), (17, 13), (21, 14), (23, 17),
                (23, 21), (2, 21), (1, 18), (3, 12)], "ink")
        c.poly([(4, 5), (9, 5), (11, 11), (17, 14), (21, 15), (22, 18), (3, 18)], "blue")
        c.poly([(4, 7), (8, 7), (10, 12), (7, 16), (3, 16)], "cyan")
        c.rect(3, 19, 21, 20, "cream")
        c.rect(4, 19, 20, 19, "white")
        for x, y in ((10, 10), (12, 12), (14, 13)):
            c.line(x, y, x + 2, y, "white")
        c.rect(4, 5, 8, 6, "ink")
    elif name == "fist":
        c.poly([(3, 9), (6, 8), (6, 3), (9, 2), (12, 3), (15, 2), (20, 4),
                (22, 7), (21, 15), (17, 18), (17, 22), (7, 22), (7, 17), (2, 14)], "ink")
        c.poly([(7, 4), (10, 3), (12, 5), (15, 3), (19, 5), (21, 7),
                (20, 14), (16, 17), (16, 21), (8, 21), (8, 16), (4, 13), (4, 10), (7, 10)], "ochre")
        c.rect(8, 5, 19, 12, "yellow")
        c.rect(5, 10, 8, 13, "yellow")
        c.rect(9, 17, 15, 19, "yellow")
        c.line(8, 5, 10, 5, "white")
        for x in (11, 15, 19): c.line(x, 5, x, 9, "ochre")
        c.line(9, 12, 17, 12, "ochre")
    elif name == "rapid":
        c.poly([(11, 1), (21, 1), (15, 9), (22, 9), (7, 23),
                (10, 14), (3, 14)], "ink")
        c.poly([(12, 2), (19, 2), (12, 10), (19, 10), (9, 19),
                (12, 13), (5, 13)], "ochre")
        c.poly([(12, 2), (18, 2), (11, 10), (17, 10), (10, 16),
                (13, 12), (6, 12)], "yellow")
        c.line(12, 3, 15, 3, "white")
        c.line(2, 6, 6, 6, "cyan")
        c.line(2, 9, 4, 9, "cyan_light")
    elif name == "arrow":
        c.poly([(3, 8), (12, 8), (12, 3), (22, 12), (12, 21), (12, 16), (3, 16)], "white")
        c.line(4, 14, 14, 14, "cream")
        c.line(14, 17, 19, 12, "cream")
    elif name == "pin":
        c.poly([(5, 1), (10, 1), (13, 4), (13, 8), (8, 15), (2, 8), (2, 4)], "ink")
        c.poly([(5, 2), (10, 2), (12, 4), (12, 8), (8, 13), (3, 8), (3, 4)], "yellow")
        c.ellipse(7, 5, 3, 3, "white")
        c.ellipse(7, 5, 2, 2, "ink")
    elif name == "mate":
        c.poly([(18, 2), (24, 2), (24, 4), (21, 4), (17, 17), (14, 17)], "ink")
        c.line(19, 3, 15, 17, "cyan_light")
        c.line(20, 3, 23, 3, "white")
        c.poly([(7, 12), (24, 12), (27, 18), (26, 25), (22, 29), (10, 29), (6, 25), (5, 18)], "ink")
        c.poly([(7, 15), (24, 15), (26, 19), (25, 24), (21, 28), (11, 28), (7, 24), (6, 19)], "ochre")
        c.poly([(8, 16), (16, 16), (16, 27), (11, 27), (8, 23)], "skin")
        c.rect(8, 12, 23, 16, "cream")
        c.rect(9, 12, 22, 13, "white")
        c.rect(10, 14, 21, 15, "grass_dark")
        c.dot(11, 14, "grass_light")
        c.line(9, 19, 9, 23, "cream")
    elif name == "weapon_thermos":
        # Argentine carry thermos: cup cap, pouring stopper, tall steel body, open side handle.
        c.poly([(24, 11), (30, 11), (33, 14), (33, 29), (30, 32), (24, 32)], "ink")
        c.poly([(25, 12), (30, 12), (32, 14), (32, 29), (30, 31), (25, 31),
                (25, 28), (29, 28), (29, 15), (25, 15)], "grass_dark")
        c.rect(30, 15, 31, 27, "cyan_light")
        c.rect(26, 16, 28, 27, "ink", 0)
        c.poly([(11, 9), (23, 9), (26, 13), (26, 35), (24, 38), (10, 38), (8, 35), (8, 13)], "ink")
        c.rect(9, 14, 25, 34, "concrete")
        c.rect(10, 14, 23, 34, "cream")
        c.rect(11, 14, 14, 33, "white")
        c.rect(23, 14, 24, 34, "shadow")
        c.rect(10, 24, 24, 28, "red_dark")
        c.rect(10, 24, 21, 27, "red")
        c.rect(11, 24, 14, 26, "skin")
        c.rect(10, 35, 24, 36, "grass_dark")
        c.line(11, 35, 21, 35, "cyan_light")
        c.rect(12, 9, 22, 12, "ink")
        c.rect(13, 10, 21, 11, "ochre")
        c.poly([(11, 2), (23, 2), (24, 4), (24, 8), (22, 10), (12, 10), (10, 8), (10, 4)], "ink")
        c.rect(11, 4, 23, 7, "grass_dark")
        c.rect(12, 3, 21, 4, "cyan_light")
        c.rect(12, 5, 14, 7, "cyan")
        c.line(12, 8, 22, 8, "shadow")
    elif name == "weapon_aerosol":
        c.rect(16, 3, 23, 7, "ink")
        c.rect(17, 4, 22, 6, "red")
        c.rect(22, 4, 23, 5, "white")
        c.poly([(13, 8), (26, 8), (29, 12), (29, 35), (26, 38), (12, 38), (10, 35), (10, 12)], "ink")
        c.rect(12, 12, 27, 35, "cyan")
        c.rect(12, 13, 15, 34, "cyan_light")
        c.rect(25, 13, 27, 35, "blue")
        c.rect(12, 19, 27, 29, "cream")
        c.poly([(19, 20), (23, 24), (20, 28), (16, 26)], "grass_dark")
        c.line(17, 26, 22, 22, "yellow")
        c.rect(14, 9, 25, 11, "cream")
        c.line(13, 36, 26, 36, "shadow")
        for x, y in ((28, 4), (32, 3), (34, 6)):
            c.rect(x, y, x + 1, y + 1, "cyan_light")
    elif name == "weapon_boleadora":
        for end in ((7, 10), (31, 10), (20, 32)):
            c.line(19, 19, *end, "ink")
            c.line(20, 19, end[0] + 1, end[1], "ochre")
        c.rect(18, 17, 21, 20, "ink")
        c.dot(19, 18, "cream")
        for x, y in ((7, 9), (31, 9), (20, 32)):
            c.ellipse(x, y, 6, 6, "ink")
            c.ellipse(x, y, 5, 5, "concrete")
            c.ellipse(x - 1, y - 1, 4, 4, "cream")
            c.line(x - 3, y - 2, x - 1, y - 3, "white")
            c.line(x - 4, y + 2, x + 3, y + 2, "ochre")
            c.dot(x + 3, y + 3, "shadow")
    elif name == "weapon_slipper":
        c.poly([(20, 2), (29, 3), (33, 7), (34, 13), (29, 25), (23, 35),
                (18, 38), (10, 37), (6, 33), (7, 27), (12, 15), (15, 6)], "ink")
        c.poly([(20, 3), (28, 4), (32, 8), (33, 13), (28, 25), (22, 34),
                (17, 37), (11, 36), (7, 32), (8, 27), (13, 15), (16, 6)], "ochre")
        c.poly([(20, 4), (27, 5), (30, 8), (31, 13), (26, 24), (20, 33),
                (16, 35), (10, 33), (9, 30), (15, 16), (17, 7)], "yellow")
        for y in (24, 27, 30): c.line(12, y, 21, y + 2, "ochre")
        c.poly([(17, 10), (20, 11), (21, 15), (28, 16), (27, 21), (20, 19),
                (14, 24), (11, 22), (17, 16)], "ink")
        c.line(18, 12, 19, 17, "cyan_light")
        c.line(19, 17, 13, 22, "cyan")
        c.line(20, 17, 27, 18, "cyan")
        c.line(21, 5, 26, 6, "white")
    elif name == "weapon_fernet":
        c.rect(16, 2, 23, 5, "ink")
        c.rect(17, 3, 22, 4, "red")
        c.rect(17, 6, 22, 12, "ink")
        c.rect(18, 6, 20, 12, "grass_dark")
        c.line(18, 6, 18, 10, "cyan_light")
        c.poly([(16, 11), (23, 11), (28, 16), (28, 35), (26, 38),
                (13, 38), (11, 35), (11, 16)], "ink")
        c.poly([(16, 12), (23, 12), (27, 17), (27, 35), (25, 37),
                (14, 37), (12, 35), (12, 17)], "grass_dark")
        c.rect(13, 17, 15, 33, "grass")
        c.rect(14, 18, 15, 23, "cyan_light")
        c.rect(13, 24, 26, 32, "cream")
        c.rect(14, 25, 25, 26, "white")
        c.rect(18, 27, 22, 31, "red_dark")
        c.rect(19, 27, 21, 29, "red")
        c.line(14, 35, 24, 35, "shadow")
        for x, y in ((5, 10), (32, 23)):
            c.line(x - 2, y, x + 2, y, "yellow")
            c.line(x, y - 2, x, y + 2, "yellow")
    elif name == "weapon_firecracker":
        c.line(21, 12, 22, 7, "ink")
        c.line(22, 7, 27, 5, "ink")
        c.line(22, 11, 23, 7, "cream")
        c.line(23, 7, 27, 6, "cream")
        c.poly([(13, 13), (24, 11), (28, 14), (29, 34), (26, 38),
                (15, 38), (12, 35), (11, 17)], "ink")
        c.poly([(13, 16), (24, 14), (27, 16), (28, 34), (25, 37),
                (16, 37), (13, 34)], "red_dark")
        c.poly([(14, 17), (22, 15), (24, 17), (25, 34), (23, 36),
                (17, 36), (14, 33)], "red")
        c.line(15, 18, 15, 31, "skin")
        c.line(13, 16, 24, 13, "cream")
        c.line(15, 34, 27, 32, "ochre")
        c.poly([(29, 2), (30, 5), (34, 4), (32, 7), (35, 9), (31, 9),
                (30, 13), (28, 10), (25, 11), (27, 8), (25, 5), (28, 5)], "yellow")
        c.rect(29, 7, 30, 8, "white")
    elif name == "weapon_ball":
        from generate_club_art import ball
        c = ball(40)
    else:
        raise ValueError(name)
    # Catch clipped silhouettes, accidental backgrounds and antialiasing at source.
    assert all(row[3] == row[-1] == 0 for row in c.pixels), name
    assert not any(c.pixels[0][3::4]) and not any(c.pixels[-1][3::4]), name
    assert {a for row in c.pixels for a in row[3::4]} == {0, 255}, name
    c.save(OUT / (name + ".png"))


if __name__ == "__main__":
    for args in (("paper", "cream"), ("enamel", "blue_dark"),
                 ("button_normal", "blue_dark"), ("button_hover", "cyan"),
                 ("button_selected", "yellow"), ("button_pressed", "ochre"),
                 ("button_disabled", "shadow"), ("slot", "shadow", "concrete")):
        surface(*args)
    for name in ICON_SIZES:
        icon(name)
    print("UI art: 8 unchanged surfaces and 18 native-size icons; transparent RGBA, original palette.")
