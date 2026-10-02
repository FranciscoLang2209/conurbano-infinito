# Hito 10 — prueba de 150 enemigos

El medidor de la esquina superior derecha aparece en el Editor y en Development Builds. Muestra FPS promedio del último segundo y enemigos vivos activos, incluida La Jefa. Las oleadas normales tienen un máximo de 90; no se modifica ese balance para la prueba.

1. En Unity, duplicar `Assets/_Game/Scenes/Main.unity` y `Assets/_Game/Data/Oleadas.asset` como copias temporales. Abrir la escena copiada y asignar la copia de `Oleadas` al `WaveDirector`.
2. En la copia de `Oleadas`, poner la **primera** fase en `maximumAlive = 150` y `spawnIntervalSeconds = 0.02`. Mantener el resto de fases y prefabs. En la escena de prueba, desactivar `WeaponController` del jugador y subir `Health.maximumHealth` a `10000` antes de iniciar Play Mode. No guardar estos valores en los originales.
3. Usar siempre la misma PC, resolución, calidad, estado de VSync y tipo de ejecución (Editor o Development Build). Cerrar aplicaciones pesadas, iniciar Play Mode sin F9 y dejar quieto al jugador. Esperar hasta que el medidor marque **150 enemigos**.
4. Con 150 enemigos visibles en el medidor, registrar los FPS de cada muestra de 1 segundo durante 30 segundos. Anotar promedio y mínimo de esas 30 muestras, CPU Main Thread y GC Alloc/frame en el Profiler, y cualquier error de Console. Repetir tres veces desde cero y comparar corridas con idénticos ajustes.
5. Aceptar la prueba de carga cuando las tres corridas lleguen a 150, sostengan el conteo durante los 30 segundos y no haya errores ni bloqueos. El hito no fija un umbral de FPS porque no hay hardware objetivo definido; conservar los números medidos para decidir si hace falta otra optimización.
6. Salir de Play Mode y borrar las dos copias temporales. Confirmar que `Main` y `Oleadas` originales conservan sus valores y que la partida normal sigue llegando a victoria a los 10 minutos.

Para comprobar la reutilización, iniciar Play Mode en `Main` y ejecutar **Tools > Conurbano Infinito > Comprobar hito 10**. La Console debe registrar la comprobación sin excepciones.
