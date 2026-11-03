# Rediseño de La Piba del Club — 2026-11-03

El usuario pidió que La Piba tuviera un diseño comparable al Pibe. Se conservaron identidad, colores de club, cola de pelo y boleadora; se añadieron volumen de cabello, rostro definido, cuello/cierre/bolsillos, escudo, puños, pliegues, pantalón y zapatillas con más contraste.

## Arte integrado

- Sprite base, seis poses de caminata y retrato reemplazados en sus rutas originales, conservando GUID, pivotes y referencias.
- Nuevo `Art/Animations/Frames/PibaIdle2.png`. `PibaIdle.anim` alterna dos poses cada 0,3 segundos en un ciclo de 0,6 s; caminata conserva seis cuadros a 10 fps y su controlador existente.
- Todos los cuadros son 48×48, 48 PPU, pivote inferior (24,0), Point, sin mipmaps ni compresión. Última fila visible y=45 en coordenadas de imagen: mismo apoyo en los ocho cuadros. Alfa estrictamente 0/255.
- Retrato de 132×132 con el mismo recorte y ampliación entera 3× usados por el proyecto.
- Se conservan vida 4, velocidad 5,5, Boleadora inicial, primera oferta de Termo y controles. No se modificaron escenas, prefabs, datos de personaje, cámara, colliders ni paquetes.

La fuente es [source-sheet.png](source-sheet.png), generada con la herramienta integrada `image_gen` a partir de los personajes propios. [Prompt completo](prompt.txt). No se usó un personaje de un pack ni se atribuye CC0 al nuevo arte.

La salida de 1774×887 se separó en cuatro columnas/dos filas iguales, se muestreó por vecino más próximo a 48×48, se alineó el apoyo y se redujo a 19 colores opacos. Quince tonos provienen de la paleta existente; se añadieron `#9F654D`, `#E1A77A`, `#5C4244`, `#7A5851` para piel/cabello. La adaptación fue técnica, sin pintar una reinterpretación distinta por cuadro. El exportador y los asserts de conversión quedan en `TestResults/PibaRedesign/export.swift`; los PNG nativos integrados son los archivos editables autoritativos.

`Tools/generate_club_art.py` deja de sobrescribir La Piba con el antiguo dibujo procedural. Se verificó en un directorio temporal que conserva sus nueve PNG y sigue produciendo los mismos recursos de pelota, Canchita y retrato del Pibe.

## Comparación y movimiento

- [Selector anterior](selector-before.png)
- [Selector nuevo](selector-after.png)
- [Caminata: seis cuadros a 10 fps, ampliación nearest 6×](walk.gif)

Las capturas del selector vienen directamente de Play Mode. El GIF es una vista ampliada de los PNG reales; no es una captura del juego ni una ilustración adicional.

## Validación

Referencia y candidato en Unity 6000.3.23f1/Metal, copia aislada `/tmp/conurbano-piba-redesign-20261103`. `CharactersCheck` comprueba dimensiones, pivotes, importación, alfa y apoyo de los ocho cuadros, dos poses de idle y seis de caminata, además de sus casos existentes de ambos perfiles, selección/cancelación/retorno, movimiento, pausa, mejoras, reinicio y ShortRun para ambos personajes. Evidencia: `TestResults/PibaRedesign/`.

La referencia (`baseline.log`) y el candidato final (`characters-final.log`) terminaron con `CHARACTERS_CHECK_PASS`; el candidato añadió `PIBA_ART_PASS` y `PIBA_IDLE_RUNTIME_PASS` para las dos cargas de La Piba. Se observaron las dos poses de idle en ejecución. ShortRun pasó para ambos personajes; selección y captura en cuatro resoluciones. La excepción conocida de UnityEditor.Search.SearchDatabase apareció una vez en referencia y candidato; no se detectaron errores nuevos de juego ni compilación.

Se inspeccionaron las capturas reales de selector y gameplay a 1280×720. La comparación SHA-256 con el comienzo de esta intervención confirmó únicamente diez archivos existentes modificados en Assets (ocho PNG, el clip idle y su comprobación), más el nuevo cuadro idle y su meta. Todos los meta existentes, incluido el del clip, se conservaron sin cambios. Escenas, prefabs, datos, ProjectSettings y Packages coinciden con la referencia. Al terminar, Assets del original y de la copia validada son idénticos. `git diff --check` aprobado.

No se generó una build ni se probó Windows o mouse de extremo a extremo. Las comprobaciones automáticas no sustituyen la evaluación artística del usuario.

## Recursos compatibles recomendados

Siguen disponibles los recursos gratuitos CC0 de [Ninja Adventure](https://pixel-boy.itch.io/ninja-adventure-asset-pack). Los siguientes usos son recomendaciones y no se incorporaron en esta intervención:

1. Puffs pequeños de `FX/Smoke/Smoke/SpriteSheet.png` para movimiento/acciones, discretos para no ocultar enemigos.
2. Brillos breves para experiencia y completar Kiosco/Parrilla/Canchita, usando los efectos de `FX/Magic/` tras revisar su alineación y ajustar tamaño/paleta.

Se prioriza feedback de acciones del juego sobre incorporar más personajes genéricos. Cada efecto necesita una prueba a escala real y con la densidad actual de enemigos antes de integrarse.
