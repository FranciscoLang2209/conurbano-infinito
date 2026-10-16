"""Original Aerosol and Perro Callejero sprites using the project's palette."""

from generate_visual_slice import ART, Canvas


def aerosol():
    canvas = Canvas(24)
    canvas.rect(7, 7, 16, 21, "ink")
    canvas.rect(8, 8, 15, 20, "grass_dark")
    canvas.rect(9, 9, 14, 19, "grass_light")
    canvas.rect(8, 12, 15, 16, "cream")
    canvas.rect(10, 13, 13, 15, "grass")
    canvas.rect(9, 4, 14, 7, "ink")
    canvas.rect(10, 5, 13, 7, "concrete")
    canvas.rect(13, 5, 15, 6, "cream")
    canvas.dot(18, 5, "cyan_light")
    canvas.dot(20, 3, "cyan_light")
    canvas.dot(21, 6, "cyan_light")
    return canvas


def mist():
    canvas = Canvas(24)
    for horizontal, vertical, radius in ((9, 9, 5), (15, 11, 5), (11, 16, 4)):
        canvas.ellipse(horizontal, vertical, radius, radius, "grass_light", 155)
        canvas.ellipse(horizontal, vertical - 1, radius - 2, radius - 2, "cyan_light", 210)
    canvas.dot(3, 13, "cyan_light", 150)
    canvas.dot(19, 4, "grass_light", 150)
    return canvas


def dog():
    canvas = Canvas(48)
    canvas.poly([(11, 26), (5, 19), (4, 13), (8, 14), (10, 21), (16, 26)], "ink")
    canvas.poly([(10, 27), (15, 22), (31, 22), (36, 27), (32, 35), (14, 35)], "ink")
    canvas.rect(15, 25, 30, 32, "ochre")
    canvas.rect(15, 25, 22, 29, "pavement_dark")
    canvas.rect(14, 33, 19, 41, "ink")
    canvas.rect(15, 33, 18, 39, "skin")
    canvas.rect(28, 33, 33, 41, "ink")
    canvas.rect(29, 33, 32, 39, "skin")
    canvas.poly([(27, 23), (28, 13), (32, 8), (35, 15), (39, 13), (41, 23), (45, 25), (44, 30), (33, 30)], "ink")
    canvas.poly([(29, 23), (30, 15), (32, 12), (34, 19), (38, 16), (39, 24), (43, 26), (42, 28), (33, 28)], "ochre")
    canvas.rect(33, 24, 41, 27, "cream")
    canvas.rect(41, 24, 43, 25, "ink")
    canvas.dot(37, 21, "ink")
    canvas.rect(29, 29, 36, 31, "red")
    return canvas


if __name__ == "__main__":
    for relative, canvas in (("Weapons/Aerosol.png", aerosol()),
                             ("Weapons/NubeAerosol.png", mist()),
                             ("Enemies/PerroCallejero.png", dog())):
        assert any(channel for row in canvas.pixels for channel in row[3::4])
        assert all(len(row) == canvas.size * 4 for row in canvas.pixels)
        canvas.save(ART / relative)
