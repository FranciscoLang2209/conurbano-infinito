# Primer pase de recursos gratuitos — 2026-11-03

El usuario compartió `unity-top-down-pixel-animation-resources.md` y eligió trabajar solamente con recursos gratuitos. El documento es una lista de referencias, no un paquete de arte ni una instrucción para instalar herramientas. Se comprobó el proyecto real antes de elegir: Unity 6000.3.23f1, IMGUI, URP, protagonistas originales de 48 PPU y Aseprite Importer 3.0.2 ya instalado.

## Integración

Se descargó gratuitamente Ninja Adventure desde [la página del autor](https://pixel-boy.itch.io/ninja-adventure-asset-pack). Su README y LICENSE confirman CC0 1.0 Universal. Se incorporaron únicamente dos PNG originales, sin retocarlos, a `Assets/ThirdParty/NinjaAdventure/Resources/NinjaAdventure/`.

- **Foco de teclado:** `Ui/Arrow.png` del pack reemplaza la flecha de foco de botones y del selector. Mano de 13×13, dibujada a 2× y girada 90 grados, con desplazamiento de dos píxeles basado en tiempo no escalado. Sigue visible durante la pausa. Se conservan textos, contraste, distribución y controles de IMGUI; las flechas de navegación a lugares del mundo conservan el dibujo propio.
- **Impactos:** `FX/Attack/CutX/SpriteSheet.png`, cuatro cuadros de 32×32 de izquierda a derecha, pivote central a 48 PPU. Reemplaza el anillo generado por HitFlash, sin cambiar daño, colisiones, recompensas ni la duración de 0,16 segundos. El preview del pack usa 0,1 segundos por cuadro; aquí se conserva la respuesta breve del juego, 0,04 segundos por cuadro. El efecto avanza también durante la muerte, se congela en pausa, se oculta al devolver al pool y empieza desde el primer cuadro en el siguiente golpe.
- Los cuatro sprites de impacto se crean una sola vez y se comparten. Cada enemigo conserva su renderer de efecto reutilizable. No se genera una textura por enemigo ni objetos nuevos por golpe.
- Importación Point, sin mipmaps, compresión ni redimensionado NPOT. La mano reutiliza BarrioUiImport para la conversión de paleta IMGUI. La textura de impactos conserva sRGB para SpriteRenderer.

La copia aislada de validación es `/tmp/conurbano-free-assets-20261103`. No se modificaron escenas, prefabs, estadísticas, paquetes ni ProjectSettings del original. No hay pasos de Inspector pendientes.

## Archivos

| Ruta | Responsabilidad |
| --- | --- |
| `Assets/ThirdParty/NinjaAdventure/` | Dos PNG, metadata Unity, README de integración, README original y licencia íntegra. |
| `Assets/_Game/Scripts/Enemy/HitFlash.cs` | Reproducción de los cuatro cuadros de impacto, incluida la fase de muerte. |
| `Assets/_Game/Scripts/Player/GameUi.cs` y `MenuScreen.cs` | Mano de foco compartida. |
| `Assets/_Game/Editor/BarrioUiImport.cs` | Importación de la mano con el criterio IMGUI existente. |
| `Assets/_Game/Editor/UiRedesignCheck.cs` | Recursos, dimensiones/importación y captura del selector, además de sus comprobaciones existentes. |
| `Assets/_Game/Editor/CombatFeaturesCheck.cs` | Cuatro cuadros, pivotes/escala, pausa, fin, golpe letal y reutilización en seis enemigos. |
| `Docs/Licencias.md`, `Docs/UnityProjectContext.md`, `TestResults/FreeAssetPass/` | Procedencia y evidencia. |

Las rutas Assets son relativas a `Conurbano Infinito/`. Otros cambios sin commit ya existían y se conservaron; su registro previo está en `TestResults/FreeAssetPass/preexisting.patch` y `preexisting-status.txt`.

## Recursos de la guía y próximos usos

| Recurso | Decisión para este proyecto |
| --- | --- |
| Ninja Adventure | Integrados UI/impactos CC0; no se importaron sus personajes, música ni proyecto Godot. |
| LPC | Reservar para una evaluación separada: personajes genéricos y licencias por pieza no sustituyen automáticamente a los protagonistas propios. |
| LimeZu | Excluido de este pase por la elección de usar sólo recursos gratuitos. |
| TDSM / PixelLab | Sin instalar ni contratar; no hacen falta para estos dos recursos. |
| Aseprite Importer | Ya está instalado. Para futuras animaciones originales, conservar PPU/pivotes y probar idle/caminata por dirección antes de ampliar acciones. |

Un reemplazo completo de personajes requeriría animaciones compatibles para ambos perfiles, idle/caminata y los estados existentes del perro/Gomero; no se presenta este pase como esa sustitución.

## Validación

Estado: **listo con límites de validación**. Unity compiló y ejecutó las comprobaciones en la copia aislada, con gráficos Metal habilitados.

| Comprobación | Evidencia | Resultado |
| --- | --- | --- |
| Referencia anterior a la edición | `TestResults/FreeAssetPass/baseline.log` | UI_REDESIGN_PASS y UI_RUNTIME_PASS. |
| Recursos y UI final | `TestResults/FreeAssetPass/ui-retry.log` | UI_REDESIGN_PASS, UI_RUNTIME_PASS y UI_UPGRADE_COPY_PASS: 32 cartas en cuatro resoluciones; importación y dimensiones de ambos PNG comprobadas. |
| Combate y animación | `TestResults/FreeAssetPass/combat.log` | COMBAT_FEATURES_CHECK_PASS: cuatro cuadros, pausa/fin, golpe letal y reinicio del efecto en doce ciclos de vida de seis prefabs. Regresiones existentes de movimiento, perro, Gomero, Expansion, Petardo, VisualSlice5 y ShortRun aprobadas. |
| Navegación y lectura | `TestResults/FreeAssetPass/After/` | Capturas directas de menú, selector, pausa, mejoras, HUD y resultados en 1280×720, 1024×768, 1920×1080 y 2560×1080. `text-layout.txt` vacío: no se detectaron recortes. Se inspeccionaron selector y pausa a 1280×720. |
| Efecto en escena | `TestResults/FreeAssetPass/Combat/muerte-enemigo.png` | Impacto visible durante la muerte; captura directa inspeccionada. |
| Integridad | Comparación binaria al finalizar | Los archivos de Assets del original coinciden con la copia validada. Escenas (4 archivos), prefabs (20), datos (86), Packages (2) y ProjectSettings (26) no difieren de la copia inicial. `git diff --check` aprobado. |
| Procedencia | `source-sha256.txt`, `imported-sha256.txt` | Hash del ZIP y de cada recurso incluido; licencia/README originales conservados. |

La primera ejecución candidata de UI (`ui.log`) falló al cambiar Game View: informó 642×1156 cuando el fixture solicitó 1280×720. Se conserva el log, no se relajaron aserciones ni se modificó el producto para sortearlo. La repetición completa pasó. La excepción conocida de UnityEditor.Search.SearchDatabase apareció una vez en referencia y en ejecuciones candidatas; se distingue de errores de juego.

No se generó una build nueva ni se probó Windows, mouse de extremo a extremo, rendimiento bajo carga o balance humano. Estas comprobaciones automatizadas no sustituyen una partida humana. No se detectó MCP Unity; se usó el Editor local en batchmode y no se instaló un puente.
