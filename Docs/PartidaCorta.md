# Primera partida completa — cuatro minutos

Objetivo: sobrevivir hasta 04:00. La Jefa llega a 03:00; matarla ayuda, pero no termina la partida antes del tiempo. Ganar o morir permite reiniciar con R o volver al menú con M; Esc pausa y continúa.

| Tiempo | Encuentro | Intervalo base / límite base |
| --- | --- | --- |
| 00:00 | Mosquitos; aprender a moverse y recoger XP | 3 s / 15 |
| 00:30 | Se suman palomas | 2,6 s / 20 |
| 01:00 | Se suman perros; esquiva lateral | 1,8 s / 28 |
| 02:00 | Mezcla con bolsas | 1,4 s / 35 |
| 03:00 | La Jefa; baja la presión de enemigos comunes | 2,2 s / 30 |
| 03:30 | Última resistencia | 1,1 s / 40 |

Quilombo conserva su multiplicador de frecuencia y límite. Las cartas de la primera subida de nivel incluyen adquirir Aerosol; sigue siendo opcional y ocupa uno de los cuatro espacios. Las demás elecciones mantienen su azar y filtrado.

El perro anuncia su dirección en amarillo y con un sonido breve; la franja gana opacidad durante los 0,65 s de preparación. No corrige su dirección durante la embestida. Una barra celeste debajo del perro indica los 1,1 s de descanso: el Aerosol de alcance 2,8 sirve para contraatacar cerca, a cambio de exponerse; Termo y Petardo permiten jugar a distancia. No se agregaron multiplicadores especiales de daño. Sus números previos se conservan.

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
