# Créditos y licencias de recursos

## Créditos

Diseño y desarrollo: **Francisco Lang y Jerónimo Engelhard**.

## Recursos del MVP

Hasta el hito 11, los sprites provisionales de `Assets/_Game/Art/Placeholders/` y los clips de `Assets/_Game/Audio/` son originales del proyecto. Se generan con `Tools/generate_hito11_assets.py` usando solamente la biblioteca estándar de Python. Los demás elementos visuales del juego usan componentes y texturas estándar de Unity. No hay recursos externos de terceros incorporados que requieran atribución adicional.

El efecto `Assets/_Game/Audio/Aviso.wav`, agregado el 2026-10-16, es audio sintético original generado por el mismo script para los avisos del Perro y La Jefa.

## Recursos externos futuros

Los sprites `Aerosol.png`, `NubeAerosol.png` y `PerroCallejero.png`, incorporados el 2026-10-16, son arte original del proyecto generado con `Tools/generate_expansion_art.py` y la paleta propia. No incorporan recursos de terceros.

El sprite `Assets/_Game/Art/Sprites/Weapons/Petardo.png` se generó el 2026-10-09 con la herramienta integrada de imágenes de OpenAI (`image_gen`), a partir de una descripción propia y la paleta del proyecto. El prompt y la importación se registran en `Docs/Petardo.md`; no se descargó de un pack de terceros. Su anillo de explosión se genera con código del proyecto.

Registrar cada recurso **antes de incluirlo en una entrega**. Adjuntar el texto de la licencia cuando lo exija su autor y confirmar que permite distribución dentro de un videojuego para PC.

| Recurso y ruta en el proyecto | Autor/titular | Fuente | Licencia y versión | Atribución requerida | Fecha de incorporación |
| --- | --- | --- | --- | --- | --- |
| Sin recursos externos hasta el hito 11 | — | — | — | — | — |

## Interfaz de barrio (2026-10-16)

Los 25 PNG de iconos y superficies de `Assets/_Game/Resources/BarrioUi/`, salvo `plaza.png`, son dibujos originales generados por `Tools/generate_ui_art.py` con la paleta y las herramientas del proyecto. La segunda iteración redibuja once indicadores y agrega seis iconos de armas a su tamaño nativo, sin emojis, glifos de fuentes ni packs. `plaza.png` es una ilustración original creada mediante `image_gen` de OpenAI para esta entrega; prompt y procedencia registrados en `Docs/UiRedesign/README.md`. No provienen de un pack ni se les atribuye una licencia de terceros inventada.

| Recurso y ruta | Autor/titular | Fuente | Licencia | Distribución |
| --- | --- | --- | --- | --- |
| `Assets/_Game/Resources/BarrioUi/VT323-Regular.ttf` | The VT323 Project Authors (Peter Hull) | https://github.com/google/fonts/tree/main/ofl/vt323 | SIL Open Font License 1.1, sin modificar | Conservar copyright y licencia; texto completo adjunto en `OFL.txt`. Permite incorporar y distribuir la fuente dentro del juego. |

El archivo de licencia también viaja en Resources dentro del juego. Los créditos del menú mencionan la fuente y su licencia.

## Animaciones y Vecino Gomero (2026-10-23)

Los 18 cuadros `PlayerStride*`, `Dog*` y `Vecino*` de `Assets/_Game/Art/Animations/Frames/` y `Art/Sprites/Enemies/PiedraGomera.png` son arte original del proyecto generado con `Tools/generate_combat_features.py`, reutilizando Canvas, paleta y sprites propios del Pibe y del perro. No incorporan recursos de terceros ni dependencias nuevas.

## Personajes del Club (2026-10-30)

`LaPibaDelClub.png`, los seis cuadros `PibaStride*` y los dos retratos de `Resources/CharacterPortraits/` son arte original generado con `Tools/generate_club_art.py`, Canvas y la paleta del proyecto. El retrato del Pibe deriva de su sprite propio existente. No incorporan recursos de terceros ni dependencias nuevas.

`Art/Sprites/Weapons/PelotaDePotrero.png` y `Resources/BarrioUi/weapon_ball.png`, incorporados el mismo día, son dibujos originales de 48×48 y 40×40 generados con la función `ball` de Tools/generate_club_art.py. Tools/generate_ui_art.py reutiliza esa función para reproducir el icono. Usan la paleta y Canvas propios, sin recursos de terceros.

`Art/Sprites/Environment/CanchitaDelClub.png` es arte original de 192×192 generado el 2026-10-30 con la función `canchita` del mismo generador. Cancha, arcos, banco, bandera del club y marcas de resistencia usan Canvas y la paleta del proyecto; no incorpora recursos externos.

## Muestra artística de plaza — 2026-10-30

PlazaPiso.png se redibujó con el generador original Tools/generate_plaza.py y tonos propios compartidos en Tools/generate_visual_slice.py. Las sombras añadidas al Pibe, La Piba y sus cuadros activos son originales del proyecto. Se reutilizan los iconos transparentes, superficies y retratos existentes, y VT323 con su OFL adjunta. No se incorporó arte externo ni se cambió la autoría previa.

Se evaluaron las cuatro referencias solicitadas de LimeZu y Kenney; no están disponibles localmente ni son dependencias del resultado. Licencias publicadas, archivos faltantes y decisiones de integración en [ArtSample/README.md](ArtSample/README.md). No se atribuye CC0 al arte original propio ni se redistribuyen los packs comerciales.

## Tipografía legible — 2026-10-30

La interfaz utiliza **Atkinson Hyperlegible Regular y Bold**, © 2020 Braille Institute of America, Inc., SIL Open Font License 1.1. Archivos estáticos originales de [Google Fonts](https://github.com/google/fonts/tree/main/ofl/atkinsonhyperlegible), incluidos en `Assets/_Game/Resources/BarrioUi/`; licencia completa en `Atkinson-OFL.txt`. Se incluye el archivo de fuente en el player para conservar los caracteres españoles. VT323 se conserva como recurso anterior, pero ya no se usa para dibujar la interfaz.

## Recursos gratuitos Ninja Adventure — 2026-11-03

| Recurso y ruta | Autor/titular | Fuente | Licencia | Uso y modificaciones |
| --- | --- | --- | --- | --- |
| `Assets/ThirdParty/NinjaAdventure/Resources/NinjaAdventure/focus.png` | Pixel-Boy y AAA | [Ninja Adventure](https://pixel-boy.itch.io/ninja-adventure-asset-pack), ZIP gratuito del 2026-03-29, upload 16981275; original `Ui/Arrow.png` | CC0 1.0 Universal | PNG sin modificar; mano de foco IMGUI, rotada/escalada al dibujar. |
| `Assets/ThirdParty/NinjaAdventure/Resources/NinjaAdventure/impact.png` | Pixel-Boy y AAA | Mismo ZIP; original `FX/Attack/CutX/SpriteSheet.png` | CC0 1.0 Universal | PNG sin modificar; cuatro cuadros de impacto, duración existente de 0,16 s. |

Licencia íntegra y README original en `Assets/ThirdParty/NinjaAdventure/`. CC0 permite incorporación y redistribución comercial; atribución opcional. Se importaron únicamente estos dos PNG, sin personajes, fuentes, música ni código Godot. Procedencia, hashes y validación en `Docs/FreeAssetPass.md` y `TestResults/FreeAssetPass/`.

## Rediseño de La Piba — 2026-11-03

Se reemplazaron `LaPibaDelClub.png`, los seis `PibaStride*` y su retrato, y se añadió `PibaIdle2.png`. El nuevo arte se creó con la herramienta integrada `image_gen` de OpenAI, tomando como referencias los dos personajes propios del proyecto. No proviene de Ninja Adventure ni se le atribuye CC0. La hoja generada, el prompt, la adaptación técnica a 48×48/48 PPU, la paleta y las capturas se conservan en `Docs/PibaRedesign/`. Se conservaron los GUID de los archivos reemplazados y las estadísticas del personaje.

Los antiguos cuadros de La Piba descritos en la sección del Club fueron reemplazados por este diseño. `Tools/generate_club_art.py` conserva los PNG editados de La Piba y continúa generando el resto del arte procedural del Club.
