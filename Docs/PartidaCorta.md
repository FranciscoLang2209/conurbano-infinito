# Primera partida completa — cuatro minutos

Objetivo: sobrevivir hasta 04:00. La Jefa llega a 03:00; matarla ayuda, pero no termina la partida antes del tiempo. Ganar o morir permite reiniciar con R o volver al menú con M; Esc pausa y continúa.

| Tiempo | Encuentro | Intervalo base / límite base |
| --- | --- | --- |
| 00:00 | Mosquitos; aprender a moverse y recoger XP | 2,4 s / 18 |
| 00:30 | Se suman palomas | 1,55 s / 26 |
| 01:00 | Se suman perros; esquiva lateral | 1,05 s / 36 |
| 02:00 | Mezcla con bolsas y Gomero; salir de la línea roja | 0,75 s / 46 |
| 03:00 | La Jefa; baja la presión de enemigos comunes | 1,15 s / 40 |
| 03:30 | Última resistencia | 0,57 s / 55 |

Candidato actual del 2026-10-23: intervalos reducidos un 10–15% desde los 30 s respecto de la primera densidad, conservando sus topes. Francisco encontró mucho mejor la resistencia, pero dejó de correr peligro al adquirir Aerosol; por eso se prueba más frecuencia. Se conservan las vidas de etapa 2 (Paloma 15, Bolsa 6, Perro 10, Gomero 12; Mosquito 3 y La Jefa 100), composición, daño, XP, armas y duración. Falta probar este nuevo candidato. Checkpoints y evidencia en `PlanDificultad.md`, sección 12; las validaciones de octubre 16 de abajo corresponden al balance anterior.

Quilombo conserva su multiplicador de frecuencia y límite. Las cartas de la primera subida de nivel incluyen adquirir Aerosol; sigue siendo opcional y ocupa uno de los cuatro espacios. Las demás elecciones mantienen su azar y filtrado.

## Quilombo de la demo

El Kiosco otorga 10 puntos y la Parrilla 15, una sola vez por partida. Desde el 2026-10-30, la Canchita del Club agrega otros 10: resistir seis segundos seguidos dentro del círculo amarillo recupera hasta dos puntos de vida. Salir antes reinicia el progreso; completarla con vida llena consume igualmente la recompensa y suma Quilombo. No revive al jugador ni aumenta su vida máxima.

Los umbrales de presión se conservan en **10 y 25**: el primero añade un bonus de 0,15 al multiplicador de oleadas y el segundo lo reemplaza por 0,35. Kiosco + Canchita suman 20; Parrilla más cualquiera de los otros lugares suma 25. Con los tres se llega a 35. El multiplicador es `1 + Quilombo / 100 + bonus`: 1,6 para Kiosco + Parrilla y 1,7 para los tres. El intervalo base se divide por ese valor y el límite base se multiplica, redondeando hacia arriba. El HUD cambia a naranja en 10 y rojo en 25; la escala general sigue siendo 0–100.

Esta demo usa los enemigos existentes: los umbrales no habilitan élites. El soporte opcional de élites de WaveDirector queda sin prefab asignado. QuilomboSliceCheck conserva la comprobación de los dos lugares originales; PitchCheck cubre los seis órdenes de los tres POIs, recompensas únicas y reinicio, usando sus contactos y recompensas reales, con fixtures de reloj para las permutaciones. También prueba dos resistencias físicas completas de seis segundos, una por personaje.

El perro anuncia su dirección en amarillo y con un sonido breve; la franja gana opacidad durante los 0,65 s de preparación. No corrige su dirección durante la embestida. Una barra celeste debajo del perro indica los 1,1 s de descanso: el Aerosol de alcance 2,8 sirve para contraatacar cerca, a cambio de exponerse; Termo y Petardo permiten jugar a distancia. No se agregaron multiplicadores especiales de daño. El candidato de resistencia sólo aumenta su vida a 10.

Los avisos de La Jefa también emiten el sonido nuevo. Sus ataques se detienen con la pausa y se cancelan al morir ella o su blanco. AudioDirector limita la repetición de avisos a uno cada 0,2 s, conserva el volumen general y desuscribe eventos al recargar escenas. El destello, anillo de impacto, desvanecimiento y audio de golpes existentes se reutilizan.

El HUD anuncia cada oleada, calcula el cronómetro y sus diez segmentos desde la duración real y marca en rojo los últimos treinta segundos. El menú explica objetivo, controles y contraataque. Aviso.wav es audio sintético original, reproducible con la entrada Aviso de Tools/generate_hito11_assets.py.

## Comprobación

En Play Mode ejecutar **Tools > Conurbano Infinito > Comprobar partida corta en Play Mode**. La prueba abre Main de cero, verifica la primera elección repetida veinte veces, pausa/reanudación, límites de fase, jefe único a 03:00, avisos y cancelación al morir, victoria a 04:00, reinicio limpio, derrota y retorno a Menu. Cambia la sesión; salir de Play Mode al terminar.

ExpansionCheck añade comprobación de la barra de recuperación y su reinicio en el pool. Hito 4 y Hito 9 comprueban ahora los tiempos de esta partida corta. No se reejecutan menús de preparación que regeneren assets.

## Validación del 2026-10-16

Unity 6000.3.23f1 compiló. En copia aislada, batchmode y sesiones nuevas pasaron Hito 9 (incluye Hito 4), Hito 11 y seis comprobaciones de Play Mode: partida corta, Aerosol/Perro, Petardo, Hito 8, Hito 10 y quinta slice visual. La versión final de ShortRunCheck también pasó en el Editor abierto, incluyendo pausa del ataque de La Jefa, audio conectado y cancelación al morir.

Una simulación adicional recorrió **240 segundos de gameplay**, las seis fases, la aparición de La Jefa y ocho elecciones de mejora hasta victoria. Resultado: `SHORT_RUN_SOAK_PASS gameplaySeconds=240 phases=6 bossSeen=True choices=8 peakEnemies=6 accelerated=8 healthBoost=10000`. Se aceleró toda la simulación con timeScale=8, se aumentó la vida y se otorgó XP periódicamente para ejercitar el recorrido; esto verifica integración y cierre, no dificultad ni supervivencia con los valores normales.

Game View confirmó lectura del objetivo 04:00, anuncio de primera oleada, menú con controles y consejo de contraataque. ShortRunCheck terminó en Menu sin errores ni warnings nuevos en el Editor; se salió de Play Mode. La barra del perro se comprobó funcionalmente, no se inspeccionó visualmente durante una embestida.

Las dos corridas batch terminaron con código 0. Cada arranque registró la excepción preexistente `ArgumentOutOfRangeException` de `UnityEditor.Search.SearchDatabase`; se contabilizó separadamente. No hubo errores de gameplay ni aserciones. Logs: `/var/folders/mc/vpskr0r53pl721jwxq_srywm0000gn/T/conurbano-four-steps-nm_ew_zr/validation.log` y `/var/folders/mc/vpskr0r53pl721jwxq_srywm0000gn/T/conurbano-four-steps-nm_ew_zr/soak.log` (temporales).

No se generó build Windows ni se evaluaron sonido de oído, balance o diversión en una partida humana de cuatro minutos. Los intervalos y límites son una primera calibración.
