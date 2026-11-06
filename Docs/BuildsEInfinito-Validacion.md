# Builds e infinito — validación de entrega, etapa 8

Fecha: 2026-11-06. **Estado de entrega completa: Blocked** por balance humano pendiente y Windows sin módulo/equipo. Validación técnica macOS ejecutada dentro del alcance detallado. No declarar la entrega terminada por pasar checks.

## Fuentes y alcance

Repositorio FranciscoLang2209/conurbano-infinito, rama main, HEAD `3e43deb1bed42e853fe635960f9f373414d92bcc`. El árbol recibido incluía etapas 6 y 7 sin commit: scripts, seis prefabs, checks y documentación; se preservan. El adjunto es contrato de diseño, no autorización para ejecutar etapas adicionales. El usuario confirmó explícitamente **etapa 8** en esta sesión. No se implementa etapa 9.

Raíz comprobada: `Conurbano Infinito/` por Assets, Packages/manifest.json y ProjectSettings/ProjectVersion.txt. No hay AGENTS.md/CLAUDE.md en repositorio ni ancestros. Guías: contexto del proyecto, BuildsEInfinito, PartidaCorta, UiRedesign/README y ArtDirection. No se encontró el GDD original; [la adenda](BuildsEInfinito-GDD-Adenda.md) expresa el alcance vigente sin atribuirle modificaciones a un documento ausente.

Revisión de flujo: MenuScreen → selección CharacterData/RunRules → GameManager/RunConfiguration; BuildState/BuildStatistics y WeaponController separan SO/estado; UpgradeSelection conserva cola y revalidación; WaveDirector/SpawnManager/Health/ExperienceDrop/ObjectPool cierran cada vida; ScoreSystem → RunResult congelado → LocalRecords → RecordsView. Sin sistemas nuevos, cambios de balance o gameplay justificados por esta revisión. No se actualiza motor/paquetes ni se regeneran escenas, prefabs o catálogo.

Nuevo código persistente: `Assets/_Game/Editor/DeliveryCheck.cs` y su meta. Comprobación explícita de doce builds y doce recorridos simulados; entrada de build macOS. Los scripts de sonda pertenecen exclusivamente a la evidencia y a una copia de QA; no a la build limpia ni al runtime del repositorio.

## Entorno y evidencia

Unity **6000.3.23f1**, URP **17.3.0**, Input System **1.20.0**, Test Framework **1.6.0**. MacBook Air / Apple M3, 8 GiB, macOS **26.5.2**, Metal. Sin Unity MCP disponible; CLI batchmode con gráficos. El Editor nunca abrió el proyecto original.

Evidencia local ignorada por git: `TestResults/BuildsEInfinito/Etapa8-20261106/`. `head.txt`, `initial-status.txt`, `initial-sha256.json`, `environment.txt` y `project-path.txt` identifican el árbol recibido, entorno y copia. `results.json`, `delivery-results.json`, `delivery-verified-results.json` y resultados de build/player conservan argv, variables, salidas y duración reales. No confundir estos artefactos locales con archivos versionados.

La copia se creó en directorio nuevo copiando únicamente Assets, Packages y ProjectSettings. Los checks cambian escenas, Game View y preferencias de esa copia; nunca se usan Setup/Apply/Generate. Cada entrada corre en proceso nuevo, excepto la fase interna propia de cada check. Runtime y Editor compilaron en estas ejecuciones.

## Regresiones

| Entrada real | Salida | Duración del proceso (s) |
| --- | ---: | ---: |
| `RecordsCheck.RunBatch` | 0 | 105.50 |
| `RecordsCheck.ReloadBatch` | 0 | 10.94 |
| `ScoreCheck.RunBatch` | 0 | 21.86 |
| `EndlessCheck.RunBatch` | 0 | 18.60 |
| `EvolutionCheck.RunBatch` | 0 | 21.49 |
| `SynergyCheck.RunBatch` | 0 | 22.91 |
| `PassiveSelectionCheck.RunBatch` | 0 | 23.66 |
| `BuildStateCheck.RunBatch` | 0 | 12.64 |
| `CharactersCheck.RunBatch` | 0 | 18.53 |
| `CombatFeaturesCheck.RunBatch` | 0 | 15.37 |
| `BallCheck.RunBatch` | 0 | 12.59 |
| `PitchCheck.RunBatch` | 0 | 32.49 |
| `UiRedesignCheck.RunBatch` | 0 | 44.92 |

Las trece entradas aprobaron sobre las fuentes recibidas. CharactersCheck incluye ShortRunCheck para ambos personajes; CombatFeaturesCheck incluye Expansion/Petardo/VisualSlice5/ShortRun; Pitch cubre Quilombo/POIs e indicadores. Conservan aserciones anteriores y las adaptaciones históricas al nivel global 8, afinidades y cierre en LateUpdate. No se quitó ni debilitó ninguna aserción anterior en esta etapa.

Cobertura: clásico 239,9/240/240,1 y victoria a 240; infinito sin victoria a 240; fronteras de tramo y jefes 180/420/660/900, omisión sin deuda; tres POIs únicos; doce ciclos de seis prefabs, despawn sin XP/puntos; 6.000 drops conservados y cupos; pausa/cola/doble input/fallback; dos evoluciones, tres sinergias, afinidades y snapshots; ejemplo de 3.240 puntos, centipuntos/saturación; muerte en deadline, cierre único/reentrante; práctica permanente y abandono excluidos; top20 por modo/reglas, empates, idempotencia incluso expulsados, backup/corrupción/incompatibilidad y escritura fallida; recarga desde segundo proceso. Registros detallados en logs y fuentes de cada check.

UI: checks anteriores recorren 1280×720, 1024×768, 1920×1080 y 2560×1080, teclado/foco, mouse sintético de RecordsView y texto. Sus informes de layout finales se conservan; la inspección visual y recorrido del player se registran más abajo. No equivalen a mouse físico en Windows ni a una partida humana.

## Matriz: integración y progresión simulada

**Integración dirigida: 12/12 aprobadas.** Dos personajes × dos modos × tres builds. Cada caso usa cartas/XP controlados a través de UpgradeSelection, comprueba su sinergia y pasiva máxima, y para Termo/Pelota fuerza la muerte real de la Jefa, elige la receta y conserva N8/espacio/familia. La build Pelota/Petardo/Antena comprueba Festejo sin inventar una tercera evolución. Todos verifican frontera de modo, snapshot/cierre único, guardado/recarga y exclusión de práctica. Los SO permanecen idénticos y no hay scripts faltantes en prefabs propios/Main. Esto prueba integración, no tiempo natural para conseguir una build.

**Progresión con catálogo, salud y XP normales:** doce sesiones con una ruta cuadrada automática por WASD, semillas 8100–8212 (identificadas por fila en delivery-verified/matrix.json), selección preferente del objetivo y sin curación ni XP inyectada. Física/combate completos acelerados con captureDeltaTime=1/30 y timeScale=8. Se verifica movimiento real (>2 unidades recorridas por caso), pausa que congela reloj/puntos, cero ofertas inválidas y presupuestos. Es un bot sencillo, no jugador humano, ni replay determinista entre plataformas.

| Personaje | Modo | Objetivo | Segundos activos | Elecciones / fuera del objetivo | Puntos | Puntos/min |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Pibe | Clásico | Termo/Yerba | 45.10 | 1 / 1 | 175 | 232.84 |
| Pibe | Clásico | Pelota/Zapatillas | 38.42 | 1 / 1 | 148 | 231.11 |
| Pibe | Clásico | Pelota/Petardo/Antena | 21.62 | 0 / 0 | 71 | 197.01 |
| Pibe | Infinito | Termo/Yerba | 27.49 | 0 / 0 | 97 | 211.70 |
| Pibe | Infinito | Pelota/Zapatillas | 20.83 | 0 / 0 | 60 | 172.87 |
| Pibe | Infinito | Pelota/Petardo/Antena | 30.42 | 1 / 1 | 120 | 236.65 |
| Piba | Clásico | Termo/Yerba | 13.09 | 0 / 0 | 33 | 151.26 |
| Piba | Clásico | Pelota/Zapatillas | 14.42 | 0 / 0 | 44 | 183.04 |
| Piba | Clásico | Pelota/Petardo/Antena | 18.69 | 0 / 0 | 68 | 218.30 |
| Piba | Infinito | Termo/Yerba | 24.56 | 0 / 0 | 84 | 205.22 |
| Piba | Infinito | Pelota/Zapatillas | 15.76 | 0 / 0 | 55 | 209.41 |
| Piba | Infinito | Pelota/Petardo/Antena | 14.69 | 0 / 0 | 44 | 179.70 |

**Resultados de progresión:** las doce terminaron en derrota. Primera sinergia/evolución: **no alcanzadas** en las doce (−1 en JSON significa ausencia, no tiempo cero). No atribuirles rendimiento de sus builds objetivo: en general sólo se mantuvo el arma inicial y hubo 0–1 elecciones. `weapons`, `passives`, `synergies`, semilla, distancia y picos por fila conservan el equipo realmente obtenido. Causa observable: Health.Died; el atacante/tipo de golpe no está instrumentado. No se deduce a partir del enemigo más cercano.

**Opciones inútiles:** cero ofertas inválidas según elegibilidad/máximos. Tres elecciones quedaron fuera del objetivo estratégico; eso no significa que fueran mecánicamente inútiles. No hay evidencia humana suficiente para cuantificar cartas poco atractivas, tiempo hasta sinergia/evolución natural o utilidad de Bolsa N1/N2. No existe instrumentación acumulada de daño por arma; no se agregan contadores de combate sólo para inventar ese dato. Puntos/minuto corresponde a recorridos breves del bot y no permite ordenar builds ni comparar Clásico/Infinito a largo plazo.

**Intentos conservados:** delivery-attempt1 pasó sin comprobar desplazamiento y no es evidencia válida de ruta. Al agregar la aserción de movimiento, delivery-final falló: faltaba configurar entrada en segundo plano/Game View. Se aplicó en el nuevo fixture la misma configuración IgnoreFocus/AllDeviceInputAlwaysGoesToGameView de los checks existentes; delivery-focus pasó con distancia medida. Una última repetición delivery-verified aprobó las fuentes finales tras precisar sólo los rótulos de diagnóstico (jefe forzado y soporte Windows); su matrix.json es la tabla de arriba. Los tiempos varían entre ejecuciones aun con semilla: no se afirma determinismo de física/input. No se cambió PlayerController ni se eliminó la aserción. Se descartan los tiempos de esos intentos para balance.

## Soak de infinito

EndlessCheck.RunBatch con CONURBANO_ENDLESS_SOAK=1 aprobado: **1.800,726 s activos simulados**, 7.295 frames, 125 elecciones, tramo 14; tiempo interno de soak **23,84 s** (proceso completo 44,22 s). Jugador quieto, salud 1.000.000, XP controlada cada 15 s, cuatro armas; captureDeltaTime=1/30 y timeScale=8. No es F9/salto exclusivo del reloj: también avanzan combate, física, spawn, drops y pool.

Picos: **37 enemigos / 256 gemas / 19 proyectiles**; **335** instancias de pool al final. No excedió 150 comunes + un jefe ni ganó a 240. El pool creció de 322 a 331 entre muestras 1560–1740 s y terminó en 335: no se afirma una meseta absoluta ni ausencia de fugas. CSV por minuto (`soak/soak.csv`) y colección diagnóstica final: managed 801.677.312 → 772.624.384 bytes del proceso Editor, que incluye herramientas/fixtures. No usarlo como memoria del player. La prueba de cupo extremo 151 está separada; este soak no mantuvo esa densidad.

## Build y player

**Build limpia disponible:** `Builds/Mac/Etapa8-20261106/ConurbanoInfinito.app`. Release (BuildOptions.None), Mono, universal x86_64 + arm64 confirmado por `lipo -info`, Unity 6000.3.23f1, versión 1.0, Menu → Main. BuildPipeline devolvió **Succeeded / 0 errores / 1 warning / 127.109.916 bytes / 59,01 s internos**; proceso 68,83 s. No contiene DeliveryRuntimeProbe ni scripts de QA runtime. Firma ad hoc confirmada por codesign, sin identidad de distribución ni notarización; sin publicación. Los directorios BackUpThisFolder generados por Unity no forman parte de la app para probar.

**Warning:** token vacío de símbolos Unity Cloud; el postproceso no pudo subir símbolos por credenciales ausentes. Se conserva también el diagnóstico ambiental de licencia. No se agregaron credenciales ni se publicó nada. `build-mac-result.json`, `build-mac.log` y manifiesto SHA-256 identifican el artefacto exacto.

**Player limpio por UI nativa CUA:** menú/Récords, alternar modos, mouse con pulsación 180 ms, elegir La Piba/Infinito, A la plaza, Esc, R, M, último abandono, cerrar/relanzar bundle y leer ese mismo resultado excluido del top. AvPág desplaza detalle sin recorte. No es una partida humana completa ni una marca competitiva. Wheel nativa no se afirma: su llamada CUA falló durante el cierre; se conserva esa limitación y la cobertura sintética de RecordsCheck. `native-ui-observations.md`, `clean-player-first.log`, `clean-player-reload.log` y copia del JSON del abandono registran el recorrido. Se inspeccionó además `records/records-evolved-build-1024x768.png`: cuatro armas/dos evoluciones, cuatro pasivas y tres sinergias legibles; los informes de layout finales de regresión están vacíos.

**QA independiente:** `Builds/Mac/Etapa8-20261106-QA/ConurbanoInfinitoQA.app`, Development/Mono/universal, **Succeeded / 0 errores / 1 warning / 333.451.024 bytes / 39,81 s internos**, proceso 56,59 s. Sólo en la copia se incorporaron DeliveryRuntimeProbe/DeliveryBuildQa y se cambió productName a “Conurbano Infinito Etapa8 QA” para aislar guardados. Proyecto original y build limpia conservan su configuración. Las sondas fueron retiradas de la copia después de ejecutarse y sus fuentes siguen en la evidencia local. No distribuir esta app como entrega.

Sonda visible, M3/8 GiB/Metal, 1280×720, VSync=1, timeScale=1/captureDeltaTime=0, salud elevada, Q100, reloj preparado en 1800, cuatro armas mejoradas/dos evoluciones, 150 comunes iniciales + jefe, jugador quieto y elecciones deshabilitadas. Después de 5 s de calentamiento se midieron 120 s reales con combate/física/spawn completos. La población disminuye por el combate; no se sostuvieron 150 enemigos. La instrumentación, escritura CSV y observación de pantalla forman parte del coste medido.

| Métrica del player QA | Resultado |
| --- | ---: |
| Ventana / frames | 120,022 s / 7.190 |
| FPS promedio | 59,91 |
| Frame p50 / p95 / p99 | 16,665 / 17,663 / 17,786 ms |
| Mayor frame | 166,660 ms |
| Enemigos durante la ventana (por frame) | 20–94 |
| Picos desde preparación: enemigos / gemas / proyectiles | 151 / 256 / 33 |
| Pool al inicio / fin de ventana | 246 / 459 |
| Pool en últimos 30 s muestreados | 459, sin nuevas instancias |
| GC Allocated In Frame: total / media / máximo | 59.495.748 / 8.274,79 / 77.344 bytes |
| GC.CollectionCount: delta en cada generación 0/1/2 | 78 / 78 / 78 (Mono; no sumar como 234 colecciones independientes) |
| Memoria Unity asignada inicio / fin | 103.979.787 / 104.652.711 bytes |
| Memoria managed inicio / fin / colección diagnóstica final | 5.427.200 / 6.123.520 / 5.754.880 bytes |
| Reloj activo inicio / fin | 1805,006 → 1925,035 s |

`standalone/standalone.json` y CSV por segundo conservan resultados; ProfilerRecorder de GC fue válido. No certifica 60 FPS sostenidos, memoria indefinidamente estable, 30 minutos reales ni balance. No se comparan estas cifras como una mejora/regresión frente a la sonda histórica de 60 s, que tenía distinta carga. OverlapCircleAll permanece: el GC agregado no atribuye un cuello de botella a ese método y no justifica reemplazarlo a ciegas.

**Persistencia en player real:** al iniciar el player normal sin batch, Score aún no era práctica. La sonda llama MarkPractice antes de alterar estado. Su derrota queda como práctica, guardada una vez y excluida del top; un fixture explícito separado de ScoreSystem sin práctica verifica que la misma implementación Mono sí acepta una marca elegible. Es un fixture, no partida natural. Dos archivos propios de la sonda conservan ambos casos; un segundo proceso del player los relee y comprueba identidad/tablas. Ambos procesos terminan con salida 0 (133,97 / 3,07 s), `DELIVERY_PLAYER_PROBE_PASS` y `DELIVERY_PLAYER_RELOAD_PASS`. No se introdujo esa marca artificial en el guardado del jugador.

**Diagnóstico conocido:** las dos ejecuciones QA y el player limpio emiten al cerrar “GarbageCollector disposing of ComputeBuffer”. El mismo texto está en `TestResults/BuildsEInfinito/Etapa5-20261104/old-demo-player.log` de la demo de octubre; antecede al candidato. Su causa no está resuelta; no se confunde con excepción de gameplay. SearchDatabase queda sólo en Editor.

**Windows: no generado ni ejecutado.** La instalación sólo tiene MacStandaloneSupport y no tiene WindowsStandaloneSupport. IsBuildTargetSupported devuelve true en este Editor; ese indicador no demuestra presencia del módulo. Se corrigió el rótulo diagnóstico de la nueva entrada para mostrar por separado targetSupported=True/moduleDirectoryPresent=False. No se instalaron módulos ni se cambió plataforma para forzar una build. Falta generar Windows x86_64 con el mismo Unity y probarlo en Windows.

**Estado de aceptación:** checks de reglas/integración, soak simulado, build macOS y recorrido/persistencia local del player aprobados dentro del alcance descrito. Balance humano **no validado**; Windows **bloqueado por módulo/equipo ausentes**; 60 FPS sostenidos y 30 minutos reales **no comprobados**. No se declara Ready ni entrega de balance terminada.

## Incidencias y aceptación pendiente

| Prioridad | Hallazgo / confianza | Acción |
| --- | --- | --- |
| P1 | Balance humano sin validar; confirmado. El bot no alcanza especialización. | Ejecutar la guía de 15–20 minutos, registrar oportunidades y tiempos; no ajustar estadísticas a partir de estas muertes automáticas. |
| P2 | Frame p95 17,66 ms/pico 166,66 ms medidos; 60 FPS sostenidos no certificados. | Medir sesiones representativas más largas y atribuir picos antes de optimizar. |
| P2 | SearchDatabase: excepción preexistente del Editor, reproducida en baseline. | Registrar aparte; no atribuir a gameplay ni declarar resuelta. |
| P2 | Atribución de daño/muerte y daño por arma no disponibles. | Registrar causa humana en la guía; instrumentar sólo si una decisión de balance concreta lo necesita. |
| P2 | 30 minutos reales / memoria de sesiones muy largas sin validar. | Soak y medición corta no demuestran estabilidad indefinida. |
| P2 | Índice local registeredRunIds crece una cadena por cierre; límite ya documentado. | Revisar compactación si el uso lo exige y se acuerda su contrato de idempotencia. |

Sin defecto runtime nuevo confirmado en este alcance; no se cambia gameplay para hacer pasar una prueba.

## Reproducción

Crear una copia vacía de Assets, Packages y ProjectSettings desde la raíz Unity correcta. Unity 6000.3.23f1 con gráficos; no usar -nographics ni -quit (las entradas cierran el Editor). Usar directorio nuevo para cada ejecución de DeliveryCheck para que no haya fixtures previos.

```sh
UNITY_BIN=/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity
QA_PROJECT=/ruta/absoluta/copia-unity
QA_OUTPUT=/ruta/absoluta/resultados-nuevos
mkdir -p "$QA_OUTPUT/matrix" "$QA_OUTPUT/soak"
CONURBANO_DELIVERY_OUTPUT="$QA_OUTPUT/matrix" "$UNITY_BIN" -batchmode -projectPath "$QA_PROJECT" -executeMethod DeliveryCheck.RunBatch -logFile "$QA_OUTPUT/matrix.log"
CONURBANO_ENDLESS_OUTPUT="$QA_OUTPUT/soak" CONURBANO_ENDLESS_SOAK=1 "$UNITY_BIN" -batchmode -projectPath "$QA_PROJECT" -executeMethod EndlessCheck.RunBatch -logFile "$QA_OUTPUT/soak.log"
CONURBANO_DELIVERY_APP=/ruta/absoluta/salida-nueva/ConurbanoInfinito.app "$UNITY_BIN" -batchmode -projectPath "$QA_PROJECT" -executeMethod DeliveryCheck.BuildMac -logFile "$QA_OUTPUT/build.log"
```

Para repetir la sonda, copiar DeliveryRuntimeProbe.cs y DeliveryBuildQa.cs desde la evidencia **sólo a otra copia de QA**, en Scripts/Player y Editor respectivamente. Ejecutar DeliveryBuildQa.BuildMac con CONURBANO_DELIVERY_QA=1 y destino nuevo. Lanzar el ejecutable con `-delivery-probe /ruta/resultados-nuevos -logFile /ruta/player.log`, mantener la ventana visible y dejarlo terminar; relanzar con los mismos argumentos más `-delivery-reload`. Fuentes completas, configuración de carga y argv reales: run_build.py, run_player.py, build-qa-result.json y player-results.json. No usar -batchmode para la medición visible; el nombre de producto QA mantiene aparte los guardados.

`run_checks.py` conserva las trece entradas/variables reales; RecordsCheck.ReloadBatch usa el mismo directorio de RecordsCheck.RunBatch. No ejecutar pruebas sobre una sesión con trabajo sin guardar. La sonda del player y su constructor de QA están sólo en la evidencia local; la reproducción exacta del player se registra con sus argv. No añadirlos a una entrega limpia.

## Guía humana de 15–20 minutos

Usar la build limpia. No usar F9, relojes/vida de QA ni la app con sufijo QA. Anotar personaje, modo, build realmente lograda, tiempo hasta primera sinergia/evolución, cartas que no aportan una opción útil, golpe/ataque que mató y puntos/minuto. Si no aparece una receta, registrar “no alcanzada”, no cero.

1. **0–2 min:** Menu → Récords con mouse; cambiar modo, volver con Esc. Jugar → El Pibe → Clásico. Verificar foco con Tab/flechas/Enter y elegir las primeras cartas con 1–3 y mouse.
2. **2–6 min:** intentar Termo/Yerba; alternar distancia y recogida de XP. Esc debe congelar ataques/reloj/puntos. Completar un lugar y comprobar +250, presión y multiplicador. Registrar Mate al alcanzar N4/N3. Si se llega a 04:00 vivo, victoria aunque la Jefa viva; si se muere, registrar causa y reintentar. Anotar tiempos reales, sin forzar que esta ventana alcance los cuatro minutos activos.
3. **6–11 min:** cambiar a La Piba/Infinito e intentar Pelota/Zapatillas. Observar rebote propio, Potrero al N4/N3. Superar 04:00 si es posible, pausar con una elección pendiente, consumir la cola. Jefa N3:00 y otra a N7:00 sólo si la anterior murió. Para evolucionar: Pelota N8/Zapatillas N5, un jefe derrotado y otra subida; nunca evolución automática por matar al jefe.
4. **11–16 min:** reintentar Pelota/Petardo/Antena con el personaje que falte probar. Buscar Festejo N4/N4, observar carga después de cinco impactos y consumo por el próximo Petardo; Antena sólo escala Petardo. Registrar si las ofertas permiten el plan o fuerzan otra build.
5. **16–20 min:** morir/ganar, mirar desglose/build, abrir Récords con L, detalle con Enter/mouse y desplazamiento rueda/RePág/AvPág. R conserva personaje/modo y limpia build; M permite cambiarlos. Cerrar/reabrir la aplicación y comprobar la marca comparable. Abandonar desde pausa debe figurar sólo como último resultado. La app QA sirve aparte para probar F9 permanente; la build limpia Release no incluye ese atajo.

Una sesión breve no cubre doce combinaciones humanas ni garantiza conseguir evoluciones. Repetir con los pares restantes y ampliar el tiempo si el azar/progresión lo requiere. No declarar diversión, balance terminado o 60 FPS sostenidos a partir de checks.

## Integridad final

SHA-256: **635 archivos Assets/Packages idénticos a la copia probada**, **332 GUIDs únicos**. De los **833 archivos recibidos** registrados al inicio se preservan 831 byte por byte; sólo cambian los dos documentos de contexto/etapas solicitados. Se agregan DeliveryCheck/meta e informe/adenda. Runtime, scripts/checks previos, escenas, prefabs, SO, arte, metas previos, paquetes, ProjectSettings, Tools y Assets exterior permanecen idénticos al árbol recibido. El snapshot de fuentes de la build limpia difiere del check final únicamente en dos rótulos de log de Editor; no cambia código del player. Manifiestos de build y hashes de fuentes prueban el alcance. Ocho informes de layout finales vacíos. `git diff --check` aprobado. No se hizo commit, push, merge, publicación o despliegue.
