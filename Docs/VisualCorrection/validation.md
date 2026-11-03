# Validación de la corrección visual

Unity 6000.3.23f1, URP, macOS/Metal. Validación automatizada con gráficos habilitados en `/tmp/conurbano-visual-correction-20261030`, copia aislada del estado local. El proyecto original estaba abierto; se preservaron sus escenas y ajustes. Referencia Git: `797de66` más cambios locales anteriores, registrados antes de editar en `TestResults/VisualCorrection/preexisting.patch` y `preexisting-status.txt`.

La última muestra (`sample-delivery.log`) terminó con ART_SAMPLE_PASS y ART_CHECK_PASS, errores 0. La carpeta de entrega contiene 11 capturas Before y 45 After; se verificaron las dimensiones de cada PNG. Tras esa ejecución se compararon los 561 archivos de Assets/Packages del proyecto original con la copia validada: idénticos. `git diff --check` pasó.

## Resultados

| Criterio | Evidencia | Resultado |
| --- | --- | --- |
| Compilación Unity | Ejecución de UiRedesignCheck, PitchCheck y CombatFeaturesCheck. | Sin errores de compilación. |
| HUD, inventario, mejoras, pausa, menú, créditos, derrota y victoria | `ui-final-2.log`: UI_REDESIGN_PASS y UI_RUNTIME_PASS. Daño/XP/Quilombo, armas nivel 1/2, cuatro slots, F3, flechas/Tab/Enter, Esc, R y M. | Aprobado. |
| Todas las cartas | `UI_UPGRADE_COPY_PASS cards=32 resolutions=4`. El fixture muestra las cartas reales y mide sus GUIStyles reales en OnGUI. | 32 cartas × 4 resoluciones sin recortes. |
| Lectura y escalado | Capturas y medidas de ancho/alto en 1280×720, 1024×768, 1920×1080 y 2560×1080. Muestra Main/Menu y panel de Canchita. | Sin desbordes detectados en las pantallas ejercitadas. Capturas a tamaño nativo; lectura visual inspeccionada en PNG, sin aumento 2×. |
| Tildes, ñ y signos españoles | Fuentes Regular y Bold: `áéíóúüñÁÉÍÓÚÜÑ¿¡` solicitados y HasCharacter comprobado. | Aprobado. |
| Indicadores y superposición entre POIs | PoiIndicatorsCheck: ocho direcciones, tres proporciones lógicas, 1536 distribuciones de tres indicadores; PitchCheck: tres indicadores sobre el mismo borde en cuatro resoluciones. Se reserva espacio superior para HUD/avisos también en las etiquetas del mundo. | Aprobado. |
| Objetivos | `pitch-final.log`: resistencia física real de 6 s con ambos personajes, salir reinicia progreso, pausa, curación limitada, no revivir ni reiniciar invulnerabilidad, uso único, seis órdenes de visita, 100 semillas, reinicio. | PITCH_CHECK_PASS, errores de juego 0. |
| Combate y circulación existente | `combat.log`: seis cuadros del jugador, cuatro fases del perro, aviso/dirección fija/barrido/daño/pausa/pool del Gomero; 12 ciclos de vida para seis prefabs; ExpansionCheck, PetardoCheck, VisualSlice5Check y ShortRunCheck. | COMBAT_FEATURES_CHECK_PASS, errores de juego 0. |
| Reglas de partida | ShortRunCheck: primera adquisición, pausa, seis fases, jefe único, victoria a 240 s, reinicio, derrota y menú. | Aprobado. |
| Terreno y regeneración | `python3 Tools/generate_plaza.py`: generación doble idéntica, opacidad, uniones y cruce libre. Comparación textual del bloque de generación de pasto/caminos con el inicio: idéntico. Canchita conserva forma 192×192, pivote, PPU e importación. | Aprobado. |
| Escenas, prefabs y datos | 110 archivos de Data/Prefabs/Scenes comparados byte por byte con la copia de validación artística anterior `/tmp/conurbano-art-20261030`: sin diferencias. También se compararon los diffs protegidos con el registro tomado al iniciar esta intervención. | Sin cambios propios en esas rutas. |
| Proyecto original | Se entró en Play Mode desde Main: cargan nueva plaza/HUD y actores. Se salió con Cmd+P; Main se conserva, sin guardar cambios de ejecución. | Comprobación de integración; la vista nativa seguía en su zoom previo 2× y no se usa como prueba de legibilidad a 1×. |
| Clic y hover de mouse | La herramienta nativa devolvió `noWindowsAvailable`; los controles mantienen GUI.Button y sus handlers. | No validado de extremo a extremo. La navegación de teclado sí fue ejercitada. |
| Build Windows / rendimiento / balance humano | Fuera de esta intervención visual. | No ejecutado. |

Las medidas de texto detectaron y permitieron corregir recortes en el selector, reloj de pausa, ayudas y dos títulos largos de Chancleta. No prueban automáticamente todas las posibles superposiciones del mundo; se combinaron con inspección visual de capturas y las pruebas de límites/separación de POIs.

La primera ejecución de PitchCheck falló porque QuilomboSliceCheck comparaba literalmente los dos avisos antiguos en mayúsculas. Se actualizaron únicamente esas dos expectativas al texto nuevo y se repitió la prueba completa con éxito. No se modificaron sus aserciones de umbrales, presión ni recompensas. Los primeros intentos de captura también registran inestabilidad temporal del tamaño de Game View; el fixture ahora reutiliza tamaños del grupo activo, aplica la selección mediante el callback de Unity que actualiza el render target y espera resolución estable. Se conservan esos logs, sin contarlos como pasadas. La diferencia entre asignar el índice y aplicar la selección está documentada en el [código de referencia de GameView de Unity](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/GameView/GameView.cs).

Las sesiones finales de UI, muestra, objetivos y combate registran `knownEditorSearchErrors=0`. No se limpiaron ni ocultaron errores de Console para obtener estos resultados.

## Reproducción

Desde una copia aislada y con Unity 6000.3.23f1 (gráficos habilitados, sin `-nographics`):

```sh
CONURBANO_UI_OUTPUT=/ruta/capturas Unity -batchmode -projectPath /ruta/copia -executeMethod UiRedesignCheck.RunBatch -logFile /ruta/ui.log
CONURBANO_ART_SAMPLE=1 CONURBANO_UI_OUTPUT=/ruta/muestra Unity -batchmode -projectPath /ruta/copia -executeMethod UiRedesignCheck.RunBatch -logFile /ruta/sample.log
CONURBANO_PITCH_OUTPUT=/ruta/objetivos Unity -batchmode -projectPath /ruta/copia -executeMethod PitchCheck.RunBatch -logFile /ruta/pitch.log
CONURBANO_COMBAT_OUTPUT=/ruta/combate Unity -batchmode -projectPath /ruta/copia -executeMethod CombatFeaturesCheck.RunBatch -logFile /ruta/combat.log
python3 Tools/generate_plaza.py
```

Logs y capturas auxiliares locales: `TestResults/VisualCorrection/` (ignorado por Git). Capturas de entrega: `Docs/VisualCorrection/Before/` y `After/`. `After/text-layout.txt` queda vacío cuando pasa la medición; la ejecución normal y la muestra lo escriben por separado, cada una tras validar todas sus pantallas.

## Límites

No se hizo una partida humana completa ni se probó una build de Windows. El mouse requiere una comprobación manual de clic en botones y tarjetas. El piso continúa repitiéndose cada 10,6667 unidades y la cámara conserva movimiento suave/escalado fraccional; la basura se integró a ese sistema existente. La validación de lectura cubre las cuatro resoluciones indicadas, no ventanas arbitrariamente pequeñas.
