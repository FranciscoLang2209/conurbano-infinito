# Iconos — segunda iteración, 2026-10-16

Se conserva la dirección visual: colores, VT323, paneles y posiciones del HUD. Se redibujan los once iconos de interfaz y se incorporan seis iconos de armas. Todos son PNG RGBA originales, sin emojis, Unicode ni fuentes de iconos. La flecha de lugares también usa el PNG propio.

- Vida: corazón con dos lóbulos, punta clara, sombra inferior derecha y brillo superior izquierdo.
- Experiencia: estrella celeste de cinco puntas en lugar del rombo.
- Nivel: insignia de club con borde dorado, cabecera celeste y campo crema libre para el número.
- Tiempo: cronómetro circular con corona, pulsador y agujas separadas.
- Quilombo: parlante de barrio, dos conos, ondas y chispas de ruido. Conserva la clave `flame` para sus consumidores.
- Armas: termo argentino con tapa/taza, cuello y manija abierta; lata de aerosol; tres bolas atadas; chancleta con tira; botella de fernet y petardo con mecha. HUD y mejoras comparten los nuevos iconos. Se mantienen los sprites de combate y sus referencias.

## Tamaño y coherencia

Cuadrícula de un píxel por píxel de referencia a 1280×720, contorno tinta de 1 px, luces arriba a la izquierda y colores de la paleta existente. Lienzos de 16 px para XP/ubicación, 24 para vida/pasivas/flecha, 30 para Quilombo, 32 para reloj/mate, 40 para armas y 48 para nivel. La diferencia de lienzo corresponde al espacio disponible; no se agranda un dibujo de 16 px para rellenar un slot mayor. Las cartas centran el recurso nativo dentro del espacio existente.

Importación: Point, FullRect, sin compresión ni mipmaps, Clamp y transparencia. `GameUi.Icon` conserva proporciones, limita el tamaño al original y alinea el origen a píxeles de pantalla. El ajuste global existente del HUD sigue siendo proporcional; a escalas fraccionarias, nearest-neighbor puede repartir un píxel de origen entre uno y dos píxeles físicos. No se cambió el escalado global del HUD.

## Evidencia visual

[Comparativa](comparativa-hud.png): recortes de capturas reales, **sin reescalar**. Los estados de prueba son nivel 3 antes y nivel 7 después; la segunda prueba además equipa las cuatro pasivas para comprobar sus iconos.

[Familia completa](familia-sprites.png): cada sprite a tamaño nativo sobre panel azul y crema, más zoom 2× por vecino más cercano para revisar el dibujo.

Capturas de Play Mode en [Before/](Before/) y [After/](After/). El segundo equipo muestra también chancleta y fernet en el HUD a 1280×720 y 1024×768. Las imágenes son fixtures de validación, no una partida humana.

## Validación ejecutada

Unity **6000.3.23f1**, Metal/macOS, copia aislada del proyecto en `/tmp/conurbano-icons-validation`; el Editor de trabajo no cambia de escena ni entra en Play Mode. Punto de partida `797de66` con los cambios locales de la primera iteración preservados.

```sh
python3 Tools/generate_ui_art.py
CONURBANO_UI_OUTPUT=/ruta/absoluta/capturas Unity -batchmode -projectPath /ruta/copia -executeMethod UiRedesignCheck.RunBatch -logFile /ruta/validacion.log
```

Resultados antes y después: salida **0**. La prueba final compila el C# e importa 26 texturas (17 iconos, ocho superficies y la ilustración). Comprueba tamaños nativos, mapeo de las seis armas a iconos distintos del proyectil, Point, ausencia de mipmaps/compresión, conversión de paleta y caracteres españoles. El capturador espera una escritura nueva al repetir la prueba en la misma carpeta.

- Pasó: estados de vida/XP/Quilombo, niveles N1/N2 de armas, cuatro slots, cuatro pasivas, cartas, pausa y navegación, reinicio limpio, menú, créditos, derrota y victoria. Sin errores de Console registrados durante la prueba.
- Pasó: HUD y pausa a **1024×768, 1280×720, 1920×1080 y 2560×1080**; 15 capturas finales.
- Revisado visualmente: siluetas, contraste sobre paneles, lectura del número, seis armas y pasivas; sin recortes ni estiramiento de proporciones. Muestreo Point sin bordes interpolados.
- Pasó: comprobación de PNG originales: tamaños, colores contenidos en la paleta, alfa exclusivamente 0/255 y margen transparente en los 17 iconos. Los ocho PNG de superficies y `plaza.png` son idénticos byte a byte a la primera iteración.
- No ejecutado: nuevas builds de distribución ni prueba humana prolongada. Se valida el HUD en Play Mode de macOS; no se afirma validación en hardware Windows.

Marcadores de la ejecución final en [validation.log](validation.log). Fuente reproducible del arte: `Tools/generate_ui_art.py`. Integración: `GameUi`, `GameHud`, `UpgradeSelection`; comprobación: `UiRedesignCheck`. No requiere cambios de Inspector, escenas o prefabs, ni nuevas dependencias.
