"""Derive small pixel-art motion frames from the existing original sprites."""

from pathlib import Path
import struct
import zlib

from generate_visual_slice import Canvas


ROOT = Path(__file__).resolve().parents[1]
SPRITES = ROOT / "Conurbano Infinito/Assets/_Game/Art/Sprites"
FRAMES = ROOT / "Conurbano Infinito/Assets/_Game/Art/Animations/Frames"


def read_sprite(path):
    data = path.read_bytes()
    assert data[:8] == b"\x89PNG\r\n\x1a\n"
    offset, compressed, size = 8, bytearray(), None
    while offset < len(data):
        length = struct.unpack_from(">I", data, offset)[0]
        kind = data[offset + 4:offset + 8]
        chunk = data[offset + 8:offset + 8 + length]
        if kind == b"IHDR":
            width, height, depth, color = struct.unpack_from(">2I2B", chunk)
            assert width == height and depth == 8 and color == 6
            size = width
        elif kind == b"IDAT":
            compressed.extend(chunk)
        offset += 12 + length
    raw = zlib.decompress(compressed)
    stride = size * 4 + 1
    assert len(raw) == size * stride
    canvas = Canvas(size)
    for y in range(size):
        row = raw[y * stride:(y + 1) * stride]
        assert row[0] == 0  # The project's art generator writes filter-0 RGBA PNGs.
        canvas.pixels[y][:] = row[1:]
    return canvas


def copy(canvas):
    result = Canvas(canvas.size)
    for y, row in enumerate(canvas.pixels):
        result.pixels[y][:] = row
    return result


def bob(canvas, pixels):
    result = Canvas(canvas.size)
    for y in range(pixels, canvas.size):
        result.pixels[y - pixels][:] = canvas.pixels[y]
    return result


def walk(canvas, stride, raised):
    result = bob(canvas, raised)
    for y in range(37, 45):
        for x in range(15, 34):
            result.pixels[y][x * 4:x * 4 + 4] = b"\0\0\0\0"
    left, right = (15, 26) if stride < 0 else (19, 22)
    for x in (left, right):
        result.rect(x + 2, 36 - raised, x + 6, 41 - raised, "ink")
        result.rect(x + 3, 37 - raised, x + 5, 40 - raised, "blue_dark")
        result.rect(x, 41 - raised, x + 8, 44 - raised, "ink")
        result.rect(x + 1, 42 - raised, x + 7, 43 - raised, "concrete")
    return result


def main():
    player = read_sprite(SPRITES / "Player/ElPibeDelBarrio.png")
    bob(player, 1).save(FRAMES / "PlayerIdle2.png")
    for index, (stride, raised) in enumerate(((-1, 0), (1, 1), (1, 0), (-1, 1)), 1):
        walk(player, stride, raised).save(FRAMES / f"PlayerWalk{index}.png")
    for name in ("MosquitoGigante", "PalomaDePlaza", "BolsaVoladora", "LaJefa"):
        bob(read_sprite(SPRITES / f"Enemies/{name}.png"), 2).save(FRAMES / f"{name}Move2.png")
    assert read_sprite(FRAMES / "PlayerWalk1.png").pixels != player.pixels


if __name__ == "__main__":
    main()
