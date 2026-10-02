"""Generate original, dependency-free placeholder sprites and audio for hito 11."""

from pathlib import Path
import math
import struct
import wave
import zlib


ROOT = Path(__file__).resolve().parents[1] / "Conurbano Infinito/Assets/_Game"
ART = ROOT / "Art/Placeholders"
AUDIO = ROOT / "Audio"
ART.mkdir(parents=True, exist_ok=True)
AUDIO.mkdir(parents=True, exist_ok=True)


def png(path, pixels):
    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data))

    raw = b"".join(b"\0" + b"".join(bytes(pixel) for pixel in row) for row in pixels)
    header = struct.pack(">2I5B", 16, 16, 8, 6, 0, 0, 0)
    path.write_bytes(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", header) +
                     chunk(b"IDAT", zlib.compress(raw)) + chunk(b"IEND", b""))


def sprite(kind):
    result = []
    for y in range(16):
        row = []
        for x in range(16):
            dx, dy = x - 7.5, y - 7.5
            if kind == "Jugador":
                inside = (dx * dx + (dy + 3) ** 2 < 12) or (abs(dx) < 4 and -1 <= dy <= 6)
            elif kind == "Paloma":
                inside = (abs(dx) + abs(dy) < 7) or (abs(dx) < 2 and 4 <= dy <= 7)
            elif kind == "Bolsa":
                inside = (2 <= x <= 13 and 5 <= y <= 14) or (5 <= x <= 10 and 2 <= y <= 5)
            elif kind == "Mosquito":
                inside = (dx * dx / 7 + dy * dy / 25 < 1) or (abs(dx) < 7 and 3 <= y <= 6)
            elif kind == "Jefa":
                inside = (3 <= x <= 12 and 4 <= y <= 14) or (5 <= x <= 10 and 1 <= y <= 5)
            elif kind == "Gema":
                inside = abs(dx) + abs(dy) <= 6
            else:
                inside = abs(dx) <= 3 and abs(dy) <= 6
            row.append((255, 255, 255, 255) if inside else (0, 0, 0, 0))
        result.append(row)
    return result


for name in ("Jugador", "Paloma", "Bolsa", "Mosquito", "Jefa", "Gema", "Proyectil"):
    png(ART / f"{name}.png", sprite(name))


RATE = 22050


def write_wav(name, seconds, sample):
    with wave.open(str(AUDIO / f"{name}.wav"), "wb") as output:
        output.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        frames = bytearray()
        for i in range(int(RATE * seconds)):
            value = max(-1.0, min(1.0, sample(i / RATE)))
            frames.extend(struct.pack("<h", int(value * 32767)))
        output.writeframes(frames)


def tone(t, frequency):
    return math.sin(2 * math.pi * frequency * t) + 0.18 * math.sin(4 * math.pi * frequency * t)


def music(notes, bass):
    def sample(t):
        beat = t * 2
        step = int(beat * 2) % len(notes)
        gate = min(1, (beat * 2) % 1 * 9) * max(0, 1 - ((beat * 2) % 1) * 0.5)
        melody = 0 if notes[step] == 0 else tone(t, notes[step]) * gate
        low = tone(t, bass[int(beat // 4) % len(bass)])
        tick = math.sin(2 * math.pi * 900 * t) * max(0, 1 - (beat % 1) * 18)
        return (0.21 * melody + 0.12 * low + 0.045 * tick) / 1.3
    return sample


write_wav("Menu", 8, music([392, 0, 494, 0, 587, 494, 440, 0] * 4, [196, 220, 174.61, 196]))
write_wav("Partida", 8, music([330, 392, 440, 392, 330, 294, 330, 0] * 4, [164.81, 146.83, 174.61, 196]))


def effect(start, end, seconds, character=0):
    def sample(t):
        progress = t / seconds
        frequency = start + (end - start) * progress
        envelope = (1 - progress) ** 2 * min(1, t * 100)
        return envelope * (0.55 * tone(t, frequency) + character * math.sin(2 * math.pi * 93 * t))
    return sample


for name, start, end, duration, noise in (
    ("Impacto", 230, 95, 0.11, 0.2),
    ("Experiencia", 620, 900, 0.12, 0),
    ("SubirNivel", 480, 960, 0.34, 0),
    ("Daño", 260, 80, 0.28, 0.3),
    ("Victoria", 520, 1040, 0.75, 0),
    ("Derrota", 360, 95, 0.75, 0.2),
):
    write_wav(name, duration, effect(start, end, duration, noise))
