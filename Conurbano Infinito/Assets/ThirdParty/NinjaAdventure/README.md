# Ninja Adventure: focused integration

Authors: Pixel-Boy and AAA. License: CC0 1.0 Universal, included verbatim in LICENSE.txt.
Source: https://pixel-boy.itch.io/ninja-adventure-asset-pack
Downloaded free on 2026-11-03: upload 16981275, pack dated 2026-03-29.
SOURCE-README.md is the unmodified author README.

Only two unmodified source PNGs are included:

| Local file | Original ZIP path | Use |
| --- | --- | --- |
| Resources/NinjaAdventure/focus.png | Ui/Arrow.png | Keyboard focus hand in buttons and character selector. 13×13, displayed at 2× with a 90-degree rotation and small pixel-step bob. |
| Resources/NinjaAdventure/impact.png | FX/Attack/CutX/SpriteSheet.png | Enemy hit feedback, four 32×32 frames from left to right, centered pivots, 48 PPU. |

Point filtering, no mipmaps, no compression, no NPOT resizing. Focus uses the existing BarrioUiImport linear-color conversion for IMGUI. Impact retains sRGB for SpriteRenderer. HitFlash caches four runtime Sprite objects shared by enemies; no per-hit textures, sprites or coroutines. Existing 0.16 s hit-effect duration is preserved (40 ms/frame); the pack preview uses 100 ms/frame. Pause freezes the impact; pooling hides it and the next hit restarts at frame zero.

CC0 permits use, modification and redistribution, including commercial games; attribution is optional. Pack characters, fonts, music, Godot code and other artwork are not included. No new Unity package or setup is required.
