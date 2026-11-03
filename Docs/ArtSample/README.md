# Muestra artística: plaza, HUD y selección

Implementada el 2026-10-30 sobre los cambios locales existentes. Alcance: una composición representativa reutilizando el piso repetido de Main, el HUD y el selector. No se reemplazó la arquitectura ni se añadieron paquetes. No se extendió el rediseño a enemigos, VFX y pantallas secundarias.

## Inspección y decisiones

- Unity 6000.3.23f1, URP 17.3.0, Input System 1.20.0 y UI IMGUI. Menu y Main son las escenas habilitadas; el selector pertenece a MenuScreen, el HUD a GameHud y los estilos a GameUi. No hay CanvasScaler ni UI Toolkit en estas pantallas.
- Main usa cámara ortográfica de tamaño 5 con seguimiento suave. El terreno es un Quad de 20000 unidades con un material Unlit que repite PlazaPiso 1875 veces: cada textura de 512 px ocupa 10,6667 unidades, equivalente a **48 PPU**. No se cambió cámara, transform, material, colisión ni navegación.
- Actores y cuadros originales a 48 PPU, pivote inferior central. La gema conserva 64 PPU y los proyectiles las escalas de WeaponData; cambiarlos alteraría su relación con los colliders. Se conservaron esos tamaños.
- Player.controller usa Walking y los estados PlayerIdle/PlayerWalk; La Piba los reutiliza mediante AnimatorOverrideController. Se mantienen las poses, clips, transiciones y parámetros; sólo se agrega la sombra a los PNG activos de ambos protagonistas.
- VT323-Regular.ttf con licencia OFL incluida localmente. Se verificaron áéíóúüñÁÉÍÓÚÜÑ¿¡ en Unity. UI con referencia 1280×720, ajuste uniforme por el lado limitante y Screen.safeArea.
- Se comprobaron Point, sin mipmaps ni compresión efectiva, transparencia e importación de UI. Los paneles existentes usan bordes de 6 px y GUIStyle.border para nueve secciones. Los overrides Standalone desactivados no sustituyen la configuración predeterminada.

## Resultado

El césped usa tres tonos cercanos con contornos periódicos; desaparece la cuadrícula de parches y disminuye el detalle suelto. Los caminos tienen juntas de menor contraste y bordes desgastados. Bancos, árboles, cestos, fuente y carteles PLAZA forman grupos; se retiraron bolsas y botellas decorativas para no confundirlas con enemigos o pickups. El cruce central permanece libre. Todos esos elementos del piso son decoración sin collider; las zonas reales conservan RiskZone, su nombre y su feedback de interacción. No se inventaron obstáculos físicos.

Ambos protagonistas reciben una sombra pequeña semitransparente en sus cuadros de idle y caminata. Su contorno oscuro, colores, silueta corporal y animación existente se conservan; el suelo menos contrastado mejora su separación. Se reutilizan los siete iconos propios de armas y los indicadores transparentes; sin emojis ni iconos de un pack ajeno.

El HUD mantiene vida, nivel, reloj y Quilombo en tres grupos fuera del centro. La XP incorpora etiqueta explícita y barra más gruesa; se agrandan el valor de Quilombo y los niveles NV de armas. Los slots ocupados tienen borde y los vacíos un guion. GameUi concentra colores, fuente, margen, separación y contornos; se corrigió allí que los labels heredaran texto blanco al hacer hover.

El selector amplía los retratos de 132 a 176 px de referencia: el dibujo original de 44 px pasa de 3× a 4×. Selección: amarillo y ELEGIDO; hover: borde celeste interior; foco: flecha y FOCO. Vida, velocidad y arma inicial están alineadas para comparación. Tab recorre personajes, Volver y A la plaza; arriba devuelve al personaje y abajo enfoca A la plaza. Enter/Espacio activa, Esc cancela. El clic conserva las rutas GUI.Button existentes. A la plaza es el botón principal amarillo.

## Capturas comparables

PNG capturados por ScreenCapture en Play Mode, sin retoque. Before corresponde al estado local inicial, no a HEAD; After al resultado. Misma cámara, posición del jugador, enemigos y POIs, 01:15, vida 4/5, XP 2/5, Quilombo 25 y cuatro armas. El fixture congela la simulación sólo para las capturas y no guarda la escena. La posición de los POIs es una composición de prueba; la distribución aleatoria de la partida se conserva.

| Vista | Antes | Después |
| --- | --- | --- |
| Plaza + HUD 1280×720 | [PNG](Before/plaza-hud-1280x720.png) | [PNG](After/plaza-hud-1280x720.png) |
| Selector 1280×720 | [PNG](Before/selector-1280x720.png) | [PNG](After/selector-1280x720.png) |
| Plaza + HUD 1024×768 | [PNG](Before/plaza-hud-1024x768.png) | [PNG](After/plaza-hud-1024x768.png) |
| Selector 1024×768 | [PNG](Before/selector-1024x768.png) | [PNG](After/selector-1024x768.png) |
| Plaza + HUD 1920×1080 | [PNG](Before/plaza-hud-1920x1080.png) | [PNG](After/plaza-hud-1920x1080.png) |
| Selector 1920×1080 | [PNG](Before/selector-1920x1080.png) | [PNG](After/selector-1920x1080.png) |
| Plaza + HUD 2560×1080 | [PNG](Before/plaza-hud-2560x1080.png) | [PNG](After/plaza-hud-2560x1080.png) |
| Selector 2560×1080 | [PNG](Before/selector-2560x1080.png) | [PNG](After/selector-2560x1080.png) |
| La Piba seleccionada | [PNG](Before/selector-piba-1280x720.png) | [PNG](After/selector-piba-1280x720.png) |

Foco en acciones: [Volver](After/selector-foco-volver-1280x720.png), [A la plaza](After/selector-foco-jugar-1280x720.png).

## Recursos y referencias evaluadas

Se buscaron packs en el repositorio y Downloads, incluidos los ZIP disponibles. Ninguno de los cuatro packs de referencia está disponible localmente. No se descargó ni integró arte de esas páginas.

| Referencia | Evaluación | Archivo faltante, sólo si se decide integrarlo |
| --- | --- | --- |
| [Modern Exteriors, LimeZu](https://limezu.itch.io/modernexteriors) | Buena perspectiva urbana y mobiliario agrupado, disponible a 16/32/48 px. Requeriría adaptar paleta y densidad. Permite uso/modificación en juegos, exige crédito y prohíbe redistribuir el pack. | Descarga oficial **Modern_Exteriors_v42.3**, con su licencia TXT. |
| [Modern User Interface, LimeZu](https://limezu.itch.io/modernuserinterface) | Marcos y retratos aprovechables; la UI original ya ofrece coherencia sin agregar otro lenguaje gráfico. Misma necesidad de crédito y restricción de redistribución. | Descarga oficial **Modern_UI_v1.2**, con su licencia TXT. |
| [UI Pack Pixel Adventure, Kenney](https://kenney.nl/assets/ui-pack-pixel-adventure) | CC0; buena referencia de paneles escalables, pero su dirección de aventura requiere adaptación al barrio. | **kenney_ui-pack-pixel-adventure.zip**, incluida la licencia del pack. |
| [Input Prompts Pixel, Kenney](https://kenney.nl/assets/input-prompts-pixel) | CC0, base de 16×16. Útil si se amplían las ayudas de entrada. Las teclas textuales actuales son suficientes para esta muestra. | ZIP oficial de **Input Prompts Pixel**, con licencia. Su endpoint de descarga devolvió 403 y no se confirmó el nombre del archivo; no se inventa uno. |

Ningún pack faltante bloquea esta muestra. Créditos utilizados: arte y generadores originales de Conurbano Infinito, Francisco Lang y Jerónimo Engelhard; fuente VT323, The VT323 Project Authors / Peter Hull, SIL Open Font License 1.1. La ilustración previa de menú/pausa fue creada con OpenAI y no se modificó. Ver también [Licencias](../Licencias.md).

## Archivos de esta intervención

Rutas relativas al repositorio; no confundir con todos los cambios locales previos de git status.

- Tools/generate_plaza.py: terreno, distribución y cartelería; aserciones de repetición y opacidad.
- Tools/generate_visual_slice.py: tonos de terreno compartidos y sombra base del Pibe.
- Tools/generate_combat_features.py y Tools/generate_club_art.py: misma sombra en las poses activas de ambos personajes.
- Conurbano Infinito/Assets/_Game/Art/Sprites/Environment/PlazaPiso.png.
- Conurbano Infinito/Assets/_Game/Art/Sprites/Player/ElPibeDelBarrio.png y LaPibaDelClub.png.
- Conurbano Infinito/Assets/_Game/Art/Animations/Frames/PlayerIdle2.png, PlayerStride1–6.png y PibaStride1–6.png.
- Conurbano Infinito/Assets/_Game/Scripts/Player/GameUi.cs, GameHud.cs y MenuScreen.cs.
- Conurbano Infinito/Assets/_Game/Editor/UiRedesignCheck.cs: modo de muestra reproducible, navegación adicional y capturas.
- Docs/ArtSample/, Docs/ArtDirection.md, Docs/Licencias.md y Docs/UnityProjectContext.md: evidencia y documentación.

Se conservaron GUIDs y fileIDs. Las escenas, prefabs y datos de gameplay no se modificaron en esta intervención. Comparación byte a byte contra la copia previa /tmp/conurbano-club-20261030: 110 rutas de escenas/prefabs/datos sin diferencias.

## Validación y límites

La evidencia completa y el procedimiento se registran en validation.md. No se generó una build distribuible. No se midió rendimiento ni se calibró dificultad. El terreno conserva su repetición cada 10,6667 unidades: variar la composición a escala de todo el mundo requeriría otra etapa. La cámara suave y el escalado fraccional existentes no garantizan pixel perfect en movimiento; no se cambiaron para preservar el comportamiento. El selector continúa diseñado para dos personajes.
