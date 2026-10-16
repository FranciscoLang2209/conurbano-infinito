# Demo de cuatro minutos — validación del 2026-10-16

Base preservada en `0a2d39f`, cambios de gameplay guardados en `ad51a31`, rama `codex/demo-cuatro-minutos`. Los cambios de esta ronda ajustan Quilombo a los dos lugares existentes, sus colores en el HUD y la documentación de entrega. No se cambiaron daño, vida, XP ni las seis fases de la partida. Estado: builds preparadas para probar; aceptación final pendiente de partidas humanas y otra PC Windows.

## Comprobaciones realizadas

Unity 6000.3.23f1, copia aislada del proyecto. Se comparó el contenido de todos los archivos de Assets con el proyecto original: coincidieron. Los experimentos temporales y el ejecutor batch quedaron fuera del proyecto original.

| Criterio | Resultado | Evidencia |
| --- | --- | --- |
| Configuración de Quilombo, fases, escenas/recursos e indicadores | Pasó | Cuatro checks: QuilomboSliceCheck, Hito9Check, Hito11Setup.Check y PoiIndicatorsCheck |
| Ambos órdenes de visita, recompensas únicas, umbrales 10/25 y reinicio | Pasó | QuilomboSliceCheck, sin sumar puntos artificiales para alcanzar la segunda etapa |
| Contactos físicos con Kiosco y Parrilla y pausa de su progreso | Pasó | `DEMO_POI_RUNTIME_PASS`, 25 puntos y multiplicador final de spawn 1,6 |
| Primera mejora, pausa, fases, jefe, victoria, derrota y menú | Pasó | `SHORT_RUN_CHECK_PASS`; acelera el recorrido mediante saltos de reloj, no mide dificultad |
| Aerosol/Perro y Petardo | Pasó | ExpansionCheck y PetardoCheck, incluidos mejoras, daño, física y reutilización |
| Cuatro armas, pooling y efectos de combate | Pasó | Hito8Check, Hito10Check y VisualSlice5Check en corridas separadas |
| Reinicios y retornos al menú con cuatro armas y orbitales | Pasó | `DEMO_LIFECYCLE_PASS reloads=12 weapons=4 includesOrbitals=True` |
| Lectura del HUD y pausa | Pasó | Inspección en Game View del Editor abierto; rojo en 25 y naranja en 15 usando F6/F7 de desarrollo; se salió de Play Mode |
| Compilación Windows x86_64/Mono de entrega | Pasó | BuildOptions.None, Menu → Main; ejecutable PE32+ x86-64 y carpeta de datos presentes |
| Compilación macOS de entrega y apertura local | Pasó | BuildOptions.None, Menu → Main; proceso de la app nueva y gameplay/pausa visibles, Player.log sin errores ni excepciones en la inspección inicial |
| Tres partidas humanas con valores normales | Pendiente | Completar PruebaDemo.md |
| Rendimiento con 150 enemigos en hardware objetivo | Pendiente | Seguir Hito10Performance.md; su check funcional solo prueba pooling |
| Ejecución en otra PC Windows | Pendiente | Seguir Hito11Entrega.md |

Logs conservados localmente bajo `TestResults/`: `demo-core.log`, `demo-petardo-baseline.log`, `demo-hito8-reversed.log`, `demo-hito10.log`, `demo-visual5.log` y `demo-lifecycle-baseline.log`. Los archivos de TestResults y Builds están excluidos de Git.

## Incidencias previas del Editor

Cada arranque batch produjo la excepción conocida de `UnityEditor.Search.SearchDatabase`. Se registró por separado y no se ocultó del log.

Una tanda combinada y una corrida aislada de Hito8 reprodujeron `t.GetParent() == nullptr` al salir de Play Mode. Petardo aislado, una repetición de Hito8 con el código original y los doce cambios de escena pasaron sin esa aserción. Un experimento sin reparentar objetos del pool también pasó, pero revertirlo no reprodujo consistentemente el fallo: la causa sigue sin confirmar y **no se cambió ObjectPool**. Los intentos fallidos se conservan en `demo-validation.log` y `demo-hito8.log`.

## Builds

Ambas usan Menu → Main y BuildOptions.None, sin Development Build. No se elevaron vida ni XP ni se aceleró el reloj en las builds.

- Windows x86_64/Mono: `Builds/Windows/Demo-2026-10-16/ConurbanoInfinito.exe`. BuildPipeline: éxito, 103.582.437 bytes, 52,8 s. ZIP: `Builds/Windows/ConurbanoInfinito-Demo-2026-10-16-Windows.zip`.
- macOS: `Builds/Mac/Demo-2026-10-16/ConurbanoInfinito.app`. BuildPipeline: éxito, 121.305.847 bytes, 89,1 s. Se abrió esta ruta exacta y se observó la partida pausada a 00:16 con 10 de Quilombo; no se cuenta esa inspección como una partida humana completa. La aplicación quedó abierta para probar.
- Cada carpeta incluye Docs, LEEME.txt y build-info.json. Los símbolos Burst `DoNotShip` quedan fuera del ZIP de Windows.

Resultados de compilación: `TestResults/demo-build-windows.log` y `TestResults/demo-build-mac.log`; captura del log de inicio local: `TestResults/demo-mac-player.log`. Una compilación exitosa no reemplaza el recorrido en la plataforma de destino ni las partidas humanas.
