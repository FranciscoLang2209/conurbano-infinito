# Corrección visual de Conurbano Infinito — 2026-10-30

Esta intervención parte del estado local previo, incluidos sus cambios sin commit. Se conserva el terreno de la muestra artística y se extiende el criterio de lectura a todas las pantallas. No requiere cambios de Inspector ni regenerar escenas.

## Resultado

- Atkinson Hyperlegible Regular para lectura y Bold para títulos/valores; caracteres españoles comprobados en Unity. Fuente estática incluida, con licencia OFL. Texto dinámico rasterizado al tamaño de salida, filtro bilinear sólo en la fuente; sprites siguen Point. Escala uniforme y safe area existentes, referencia 1280×720. El tamaño mínimo es 18 unidades de diseño y 16 píxeles físicos; no hay autoajuste según la longitud del texto.
- IMGUI, no Canvas ni CanvasScaler. Carbón con texto crema en HUD; crema con tinta oscura en objetivos, cartas y menús. Azul gastado en bordes/botones; amarillo en selección, recompensas y acentos. Se eliminaron las superficies azules lavadas detrás de los textos, sombras de paneles y el uso generalizado de mayúsculas. XP conserva el celeste en su barra, con cifras crema.
- Valores de vida, tiempo y nivel con mayor presencia; títulos y cuerpos sin contorno ni sombra. Márgenes ampliados. Las cartas admiten los títulos largos de Chancleta en tres líneas con la misma tipografía y tamaño.
- Plaza: cinco bancos y cuatro cestos con separaciones desiguales, nueve árboles y 42 objetos decorativos por textura: bolsas, papeles, botellas acostadas/de pie, cajones y conos. Grupos cargados cerca de cestos y mobiliario, sectores más despejados, detalles junto a la fuente y cuatro objetos adicionales junto a la Canchita. Variantes de orientación, tamaño y tonos apagados distinguen basura de actores/pickups.
- El bloque que genera pasto y caminos se conservó byte por byte respecto al inicio de esta intervención. El generador comprueba que el cruce completo conserva sus píxeles, las uniones de textura, opacidad y reproducción determinista. Se conservan material, tiling, cámara y colisiones. Ninguna decoración recibe collider.

## Capturas comparables

PNG directos de `ScreenCapture` en Play Mode, sin retoque ni ampliación. Abrir a 100% para juzgar la lectura. El zoom de la ventana del Editor no forma parte de estas imágenes.

Before es el estado local inicial de esta intervención, no HEAD ni la plaza anterior al nuevo pasto. La muestra fija cámara, jugador, actores, POIs, vida 4/5, XP 2/5, Quilombo 25, cuatro armas y reloj 01:15. Sólo el fixture de captura congela/acomoda la escena; esos cambios no se guardan en Main.

| Vista | Antes | Después |
| --- | --- | --- |
| Plaza + HUD 1280×720 | [PNG](Before/plaza-hud-1280x720.png) | [PNG](After/plaza-hud-1280x720.png) |
| Plaza + HUD 1024×768 | [PNG](Before/plaza-hud-1024x768.png) | [PNG](After/plaza-hud-1024x768.png) |
| Plaza + HUD 1920×1080 | [PNG](Before/plaza-hud-1920x1080.png) | [PNG](After/plaza-hud-1920x1080.png) |
| Plaza + HUD 2560×1080 | [PNG](Before/plaza-hud-2560x1080.png) | [PNG](After/plaza-hud-2560x1080.png) |
| Selector 1280×720 | [PNG](Before/selector-1280x720.png) | [PNG](After/selector-1280x720.png) |
| Selector 1024×768 | [PNG](Before/selector-1024x768.png) | [PNG](After/selector-1024x768.png) |
| Selector 1920×1080 | [PNG](Before/selector-1920x1080.png) | [PNG](After/selector-1920x1080.png) |
| Selector 2560×1080 | [PNG](Before/selector-2560x1080.png) | [PNG](After/selector-2560x1080.png) |

La ampliación de la validación incorpora capturas nuevas del [objetivo](After/objetivo-1280x720.png), [mejoras](After/mejoras-1280x720.png), [menú](After/menu-1280x720.png), [pausa](After/pausa-1280x720.png), [créditos](After/creditos-1280x720.png), [derrota](After/derrota-1280x720.png) y [victoria](After/victoria-1280x720.png), también en las otras tres resoluciones. Estas pantallas adicionales no tienen una nueva captura Before de esta intervención. La captura de objetivo fija el progreso a 3/6 s para mostrar el panel; la resistencia real se comprueba por separado en PitchCheck.

## Archivos de esta intervención

Rutas relativas al repositorio. Los demás cambios que muestra git status son anteriores; no se revirtieron.

| Archivos | Cambio |
| --- | --- |
| `Conurbano Infinito/Assets/_Game/Scripts/Player/GameUi.cs` | Fuente compartida, rasterización al tamaño físico, superficies limpias, botones, comprobación de texto sólo en Editor. |
| `GameHud.cs` en la misma carpeta | Vida/XP/nivel/reloj/Quilombo, inventario, oleadas, boss e indicadores de lugares. |
| `RiskZone.cs`, `RiskZoneData.cs` | Panel y etiquetas de objetivos; sólo presentación y redacción de recompensas. |
| `MenuScreen.cs`, `GameManager.cs`, `UpgradeSelection.cs`, `PerformanceOverlay.cs`, `QuilomboThresholdController.cs` | Menú, selector, pausa/finales, cartas, diagnóstico y avisos con tipografía/jerarquía compartidas. |
| `Conurbano Infinito/Assets/_Game/Editor/UiRedesignCheck.cs` | Medición de recortes reales, 32 cartas, capturas en cuatro resoluciones y objetivo representativo; espera al tamaño estable de Game View y reutiliza tamaños existentes. |
| `Conurbano Infinito/Assets/_Game/Editor/QuilomboSliceCheck.cs` | Actualiza las dos expectativas de texto al nuevo uso de mayúsculas; conserva las aserciones de gameplay. |
| `Tools/generate_plaza.py` | Recuperación de objetos del historial, composición irregular, variantes y comprobación del cruce libre. |
| `Tools/generate_club_art.py` | Detalles decorativos de la Canchita; se regeneró únicamente ese sprite. |
| `Conurbano Infinito/Assets/_Game/Art/Sprites/Environment/PlazaPiso.png`, `CanchitaDelClub.png` | Recursos originales regenerados; GUIDs/importación preservados. |
| `Conurbano Infinito/Assets/_Game/Resources/BarrioUi/AtkinsonHyperlegible-{Regular,Bold}.ttf`, `Atkinson-OFL.txt`, sus `.meta` | Fuentes locales y licencia. VT323 anterior se conserva. |
| `Docs/VisualCorrection/`, `Docs/Licencias.md`, `Docs/UnityProjectContext.md`, `Docs/ArtDirection.md` | Evidencia, alcance y créditos. |

Los métodos de gameplay, estadísticas, tiempos y cantidades de recompensa se conservan. No se editaron escenas, prefabs, ScriptableObjects, paquetes ni ProjectSettings en el proyecto original.

## Validación y límites

El detalle de pruebas y resultados está en [validation.md](validation.md). No hay build distribuible nueva. Se conserva el piso repetido cada 10,6667 unidades: cambia la composición interior, pero un recorrido largo permite reconocer la repetición. No se introdujo un sistema procedural nuevo ni una Pixel Perfect Camera. El movimiento suave del mundo conserva su comportamiento anterior.
