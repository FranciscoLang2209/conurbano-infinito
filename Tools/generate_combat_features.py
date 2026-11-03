"""Original combat sprites, using the same pixel canvas and palette as the game."""

from generate_visual_slice import ART, Canvas
from generate_animation_frames import FRAMES, read_sprite


def player_stride(index):
    source = read_sprite(ART / "Player/ElPibeDelBarrio.png")
    result = Canvas(48)
    result.ellipse(24, 45, 18, 2, "ink", 120)
    poses = ((14, 27, 0), (17, 24, 1), (21, 20, 2),
             (25, 16, 0), (23, 18, 1), (19, 22, 2))
    left, right, rise = poses[index]
    for x, color in ((left, "blue_dark"), (right, "blue")):
        result.rect(x + 1, 34 - rise, x + 6, 42, "ink")
        result.rect(x + 2, 35 - rise, x + 5, 41, color)
        result.rect(x, 42, x + 8, 44, "ink")
        result.rect(x + 1, 42, x + 7, 43, "concrete")
    for y in range(37):
        for x in range(48):
            pixel = source.pixels[y][x * 4:x * 4 + 4]
            if pixel[3] and y >= rise:
                sway = (1 if index < 3 else -1) if y < 22 else 0
                result.pixels[y - rise][(x + sway) * 4:(x + sway) * 4 + 4] = pixel
    return result


def dog_pose(phase, frame):
    source = read_sprite(ART / "Enemies/PerroCallejero.png")
    result = Canvas(48)
    for y in range(33):
        for x in range(48):
            pixel = source.pixels[y][x * 4:x * 4 + 4]
            if not pixel[3]:
                continue
            shift = 3 if phase in ("Aim", "Rest") else (-1 if frame else 0)
            dx = -2 if phase == "Aim" and x > 27 else 0
            result.pixels[y + shift][(x + dx) * 4:(x + dx) * 4 + 4] = pixel
    stride = 4 if phase == "Dash" else (2 if phase == "Run" else 0)
    for x, sign in ((15, 1), (29, -1)):
        foot = x + stride * sign * (1 if frame else -1)
        result.poly([(x, 32), (x + 4, 32), (foot + 5, 41), (foot - 1, 41)], "ink")
        result.line(x + 2, 34, foot + 2, 39, "skin")
    if phase == "Rest":
        result.rect(41, 29, 43, 33 + frame, "red")
    return result


def neighbor(aim=False, step=0):
    c = Canvas(48)
    c.rect(17 - step, 33, 23 - step, 43, "ink")
    c.rect(18 - step, 34, 22 - step, 41, "blue_dark")
    c.rect(27 + step, 33, 33 + step, 43, "ink")
    c.rect(28 + step, 34, 32 + step, 41, "blue")
    c.rect(15 - step, 42, 23 - step, 44, "ink")
    c.rect(27 + step, 42, 35 + step, 44, "ink")
    c.poly([(14, 18), (31, 18), (35, 25), (33, 36), (14, 36), (11, 25)], "ink")
    c.poly([(15, 20), (30, 20), (33, 25), (31, 34), (16, 34), (13, 25)], "grass_dark")
    c.rect(19, 21, 29, 33, "grass")
    c.rect(23, 21, 24, 34, "cream")
    c.rect(15, 28, 20, 31, "grass_light")
    c.ellipse(24, 13, 9, 10, "ink")
    c.ellipse(24, 13, 7, 8, "skin")
    c.rect(17, 8, 20, 13, "pavement_dark")
    c.rect(30, 8, 31, 14, "pavement_dark")
    c.rect(22, 4, 27, 5, "cream")
    c.rect(27, 12, 28, 13, "ink")
    c.rect(23, 18, 30, 20, "ink")
    c.rect(24, 18, 29, 18, "pavement_dark")
    if aim:
        c.rect(14, 23, 39, 27, "ink")
        c.rect(15, 24, 27, 26, "grass")
        c.rect(29, 24, 38, 26, "skin")
        c.line(22, 24, 39, 18, "red")
        c.line(22, 25, 39, 24, "red")
        c.rect(39, 21, 41, 30, "ink")
        c.rect(40, 22, 40, 29, "ochre")
        c.line(40, 23, 36, 17, "ink")
        c.line(40, 23, 44, 17, "ink")
    else:
        c.rect(12, 24, 17, 34, "ink")
        c.rect(13, 25, 16, 31, "grass")
        c.rect(13, 32, 16, 34, "skin")
        c.rect(32, 24, 37, 34, "ink")
        c.rect(33, 25, 36, 31, "grass")
        c.rect(33, 32, 36, 34, "skin")
        c.line(39, 34, 39, 40, "ochre")
        c.line(39, 35, 35, 30, "ochre")
        c.line(39, 35, 43, 30, "ochre")
        c.line(35, 30, 43, 30, "red")
    return c


def main():
    frames = {}
    for i in range(6):
        frames[f"PlayerStride{i + 1}"] = player_stride(i)
    for phase in ("Run", "Aim", "Dash", "Rest"):
        for i in range(2):
            frames[f"Dog{phase}{i + 1}"] = dog_pose(phase, i)
    frames["VecinoWalk1"] = neighbor(step=-2)
    frames["VecinoWalk2"] = neighbor(step=2)
    frames["VecinoAim"] = neighbor(aim=True)
    frames["VecinoRest"] = neighbor()
    for name, canvas in frames.items():
        assert any(row[3::4].count(255) for row in canvas.pixels), name
        assert all(len(row) == canvas.size * 4 for row in canvas.pixels), name
        canvas.save(FRAMES / (name + ".png"))
    for group in (("PlayerStride", 6), ("DogRun", 2), ("DogDash", 2), ("VecinoWalk", 2)):
        signatures = {b"".join(frames[group[0] + str(i + 1)].pixels) for i in range(group[1])}
        assert len(signatures) == group[1], group[0]
    stone = Canvas(16)
    stone.ellipse(8, 8, 6, 5, "ink")
    stone.ellipse(8, 8, 4, 3, "concrete")
    stone.rect(6, 6, 9, 7, "white")
    stone.save(ART / "Enemies/PiedraGomera.png")
    print(f"COMBAT_ART_PASS frames={len(frames)} stone=1 unique_cycles=True")


if __name__ == "__main__":
    main()
