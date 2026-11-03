# Plan de dificultad — Conurbano Infinito

Fecha: 2026-10-23. Estado: nueva ronda de frecuencia sobre las vidas de etapa 2. Francisco informó que la resistencia está mucho mejor, pero adquirir Aerosol elimina el peligro; propone que falta cantidad. Se reducen sólo los intervalos desde los 30 s. Las tablas de valores anteriores se conservan como referencia del plan; los valores actuales y su evidencia están en la sección 12.

Francisco confirmó que las animaciones, armas y enemigos locales funcionan bien y pidió aumentar la dificultad de forma ordenada. Este plan parte del contenido actual del workspace, incluidos los archivos nuevos todavía sin seguimiento por Git, y conserva la partida de cuatro minutos.

## 1. Qué hay hoy y qué implica

| Sistema | Estado local confirmado | Implicación para el balance |
| --- | --- | --- |
| Oleadas | Seis fases a 0, 30, 60, 120, 180 y 210 s; una aparición por oportunidad | Agregar una especie cambia las probabilidades, no la cantidad total de apariciones |
| Gomero | Peso 1 desde 120 s; conserva los intervalos y límites anteriores | Aporta otra amenaza, pero no aumenta por sí solo la densidad |
| Quilombo | Multiplica frecuencia y límite de enemigos | Hay que evaluar cada cambio con y sin lugares visitados |
| Armas | Seis disponibles, hasta cuatro equipadas; Termo inicial y Aerosol ofrecido en la primera elección | La fuerza real depende del equipo y las mejoras elegidas, no sólo del catálogo |
| Progresión | XP necesaria: 5, 8, 11, 14…; gemas de 1–3 XP por enemigo común | Más apariciones pueden dar más mejoras y compensar parte de la dificultad añadida |
| Jugador | 5 de vida, 1 s de invulnerabilidad, velocidad 5 antes de mejoras | Duplicar el daño enemigo cambia mucho el castigo; no usarlo como primer ajuste |
| Jefa | Llega a 180 s, 100 de vida; matarla no es requisito para ganar | Aumentar su vida no modifica el objetivo de supervivencia y puede tener poco efecto si ya permanece viva |
| Pool | La vida se reinicia desde el prefab en cada reutilización | Ajustar prefabs basta para vida fija; un escalado temporal futuro debe aplicarse otra vez tras cada spawn |
| Cambios locales | UI, animaciones, gomero y validación están mezclados en el árbol de trabajo | Preservar un punto de recuperación que incluya los archivos nuevos y sus `.meta` antes de ajustar |

La revisión del diff confirma que el balance anterior de intervalos, límites y estadísticas se conservó al integrar al Gomero. Los cambios de UI muestran más información, pero no aumentan por sí mismos el daño de las armas.

El multiplicador efectivo de oleadas es:

`M = 1 + Quilombo / 100 + bonus de umbral`

El intervalo efectivo es `intervalo base / M`; el límite de comunes es `ceil(límite base × M)`. La Jefa se cuenta aparte. Con los lugares actuales:

| Visitas completadas | Quilombo | Bonus | M |
| --- | ---: | ---: | ---: |
| Ninguna | 0 | 0 | 1,00 |
| Sólo Kiosco | 10 | 0,15 | 1,25 |
| Sólo Parrilla | 15 | 0,15 | 1,30 |
| Ambos | 25 | 0,35 | 1,60 |

Los lugares también dan recompensas: Kiosco aumenta el radio de recolección temporalmente y Parrilla añade 20% de daño. Comparar rutas distintas sin registrarlas confundiría el resultado.

## 2. Objetivo de esta iteración

Buscar una curva con aprendizaje durante los primeros 30 s, presión creciente desde el minuto uno, decisiones de movimiento y equipo desde el minuto dos, y un cierre exigente durante los últimos 30 s.

Como referencia inicial para Francisco, apuntar a unas 2–3 victorias en cinco intentos cuando ya conoce la versión. Es un objetivo de diseño provisional, no una tasa medida ni una conclusión estadística. Cada derrota debería poder explicarse por un riesgo visible: cerco, mala posición, aviso ignorado o elección de equipo.

Mantener durante las primeras rondas: duración, momentos de introducción, cuatro slots, armas y mejoras, XP, vida e invulnerabilidad del jugador, daño enemigo, velocidades, distancias de aparición, avisos de ataque y recompensas de los lugares. Así se puede atribuir el resultado al grupo de parámetros que se está probando.

## 3. Etapa 0: preservar y medir la referencia

1. Guardar un checkpoint del estado local aceptado, incluyendo archivos nuevos de arte, prefabs, scripts, documentación y `.meta`. No basta un parche de `git diff`, que omite los archivos sin seguimiento. No incluir cachés ni builds.
2. Registrar los valores actuales de las tablas siguientes. Mantener cada ronda de balance como un cambio separado y reversible.
3. Jugar dos partidas de referencia: una sin visitar lugares y otra buscando ambos. Registrar equipo, mejoras, nivel y minuto de las visitas; no exigir una victoria para que una partida cuente.
4. Medir con F3 en Editor/Development Build y usar siempre la misma máquina, resolución y configuración gráfica. No usar F9, F6/F7, vida extra ni XP artificial en las pruebas de balance.

La devolución humana actual ya valida que el contenido se entiende. Estas partidas sirven para medir dificultad y progresión, sin reabrir una ronda de onboarding.

## 4. Etapa 1: aumentar la densidad

Cambiar sólo los intervalos y límites de `Assets/_Game/Data/Oleadas.asset`. Conservar los pesos de cada especie para aislar la primera prueba. Esta es la primera implementación recomendada.

| Tramo | Intervalo actual → propuesta | Límite base actual → propuesta | Aumento de frecuencia | Intervalo con ambos lugares | Límite con ambos lugares |
| --- | ---: | ---: | ---: | ---: | ---: |
| 00:00–00:30 | 3,00 → 2,40 s | 15 → 18 | 25% | 1,500 s | 29 |
| 00:30–01:00 | 2,60 → 1,80 s | 20 → 26 | 44% | 1,125 s | 42 |
| 01:00–02:00 | 1,80 → 1,20 s | 28 → 36 | 50% | 0,750 s | 58 |
| 02:00–03:00 | 1,40 → 0,85 s | 35 → 46 | 65% | 0,531 s | 74 |
| 03:00–03:30 | 2,20 → 1,30 s | 30 → 40 | 69% | 0,813 s | 64 |
| 03:30–04:00 | 1,10 → 0,65 s | 40 → 55 | 69% | 0,406 s | 88 |

La suma teórica `duración de cada fase / intervalo` pasa de aproximadamente 139 a 219 oportunidades: **58% más** sin Quilombo. No son apariciones garantizadas: influyen el límite de vivos, el reinicio del temporizador al cambiar de fase y la frecuencia de actualización. Tampoco describen cuántos enemigos se ven simultáneamente en cámara.

Con ambos lugares, el máximo previsto de la última fase es 88 comunes más la Jefa si sigue viva: 89 enemigos vivos. No es un límite de todas las instancias: además existen proyectiles, gemas, efectos de muerte y objetos inactivos retenidos en el pool. Al bajar el límite de una fase, los enemigos sobrantes no desaparecen; se bloquean nuevas apariciones hasta liberar espacio.

El límite sólo importa cuando se alcanza. Si el jugador mata más rápido de lo que aparecen enemigos, aumentarlo sin reducir el intervalo apenas cambia el juego.

**Criterio para avanzar:** probar tres partidas sin lugares y tres con ambos, registrar las visitas y comparar con la referencia. Si esta ronda ya da el desafío buscado, conservarla y no subir vida por obligación. Si el arranque castiga antes de la primera mejora, corregir sólo los dos primeros tramos. Si el cierre queda vacío, corregir los tramos finales.

## 5. Etapa 2: aumentar resistencia si todavía hace falta

Aplicar después de evaluar la densidad, manteniendo los valores de la ronda 1 aceptada. Cambiar la vida de los prefabs de enemigos, sin alterar el comportamiento de `Health`.

| Enemigo | Vida actual → candidata | Motivo |
| --- | ---: | --- |
| Mosquito | 3 → 3 | Mantener las bajas y la primera mejora accesibles con el Termo inicial |
| Paloma | 12 → 15 | Reforzar su papel de enemigo resistente y lento |
| Bolsa | 5 → 6 | Aumento pequeño para conservar su papel de presión móvil |
| Perro | 8 → 10 | Darle más oportunidades de completar su secuencia y ofrecer contraataque |
| Gomero | 10 → 12 | Exigir una decisión de acercamiento o priorización sin volverlo una esponja |
| Jefa | 100 → 100 inicialmente | Evaluar primero si muere demasiado pronto y si eso realmente vacía el último minuto |

Para la Jefa, considerar 100 → 125 únicamente si se observa que el equipo la elimina antes de que su presencia aporte presión. Si ya queda viva hasta el final, aumentar su vida no resuelve la dificultad.

Estos son candidatos de prueba. El efecto de la vida ocurre por umbrales de impactos: pasar de tres a cuatro impactos es un cambio real aunque el porcentaje parezca pequeño. Registrar también cómo se sienten las bajas con una mejora de daño y con el bonus de Parrilla. No asumir un DPS constante para armas de área, perforación o regreso.

No sumar simultáneamente otro aumento de frecuencia: por ejemplo, +60% de frecuencia y +25% de vida implican cerca de **2 veces la vida enemiga que entra por segundo**, antes de considerar composición, XP y daño de área.

**Criterio para avanzar:** repetir las dos rutas de prueba. Conservar sólo aumentos que prolonguen decisiones interesantes; reducirlos si se acumulan enemigos que absorben impactos sin añadir interacción o si la progresión se frena de forma excesiva.

## 6. Etapa 3: ajustar mezcla y comportamiento según lo observado

Elegir una sola intervención por problema; esta etapa no es una lista de aumentos obligatorios.

| Observación | Primer ajuste a probar |
| --- | --- |
| Hay mucha gente pero poco peligro | Cambiar un peso de la mezcla en el tramo afectado hacia perros o gomeros, manteniendo la frecuencia |
| Se superponen demasiadas líneas rojas | Reducir el peso del Gomero; conservar los avisos y no añadir un gestor de límites por especie todavía |
| Se puede ganar corriendo siempre en línea recta | Revisar por qué las amenazas no interceptan al jugador; velocidades y aproximación son una ronda separada |
| El jugador consigue mejoras demasiado pronto y vuelve a vaciar la pantalla | Comparar nivel y bajas por minuto antes de cambiar la curva de XP o las armas |
| La Jefa muere enseguida y desaparece la presión final | Probar primero su vida candidata; conservar su aviso de 1,2 s |
| Las derrotas ocurren por daño difícil de leer | Corregir mezcla o presentación antes de aumentar daño o acortar avisos |

Mantener el daño normal en 1 y el de la Jefa en 2 durante esta iteración. Con 5 de vida inicial, pasar de 1 a 2 duplica el castigo de cada impacto. Conservar el aviso de 0,65 s y descanso de 1,1 s del perro y el aviso de 0,9 s del Gomero; el jugador acaba de aprender esas ventanas.

La velocidad inicial del jugador ya supera la de persecución de los comunes. Por eso aumentar sólo vida y cantidades puede no resolver una estrategia de huida sostenida; se debe observar ese caso antes de proponer cambios de IA o velocidad.

## 7. Alcance técnico y fuentes de valores

| Archivo o grupo | Trabajo previsto |
| --- | --- |
| `Data/Oleadas.asset` | Etapa 1: intervalos y límites. Etapa 3 opcional: pesos |
| `Prefabs/PalomaDePlaza.prefab`, `BolsaVoladora.prefab`, `PerroCallejero.prefab`, `VecinoGomero.prefab` | Etapa 2: vida candidata |
| `Prefabs/LaJefa.prefab` | Sólo si los datos justifican su cambio de vida |
| `Editor/ExpansionCheck.cs` | Si cambia el perro, actualizar la expectativa literal de vida 8; verificar el reinicio contra la vida configurada del prefab |
| `Editor/CombatFeaturesSetup.cs` | Si cambia el Gomero, sincronizar su valor de creación actualmente fijado en 10 para que una reintegración no lo revierta |
| `Docs/PartidaCorta.md`, `Docs/CombateYVecinoGomero.md`, `Docs/PruebaDemo.md` | Registrar números aceptados y resultados de las rondas |
| `Docs/Hito10Performance.md` | Mantener el procedimiento de carga; actualizar su descripción del balance y aclarar F3 si cambian los límites |

`WaveDirector`, `SpawnManager` y `Health` ya permiten la primera iteración. No hace falta otro manager, un multiplicador global, duplicar enemigos ni crear modos Fácil/Normal/Difícil. Reutilizar los ScriptableObjects y prefabs existentes como fuente de configuración.

Los scripts de integración visual son herramientas de autoría, no el mecanismo cotidiano para balancear. Revisar cualquier menú antiguo antes de ejecutarlo: algunos reescriben valores. En esta ronda no regenerar escenas, prefabs de otras categorías, paquetes ni ajustes del proyecto.

Un escalado de vida por minuto sólo se consideraría si las pruebas muestran que la misma especie debe tener otra resistencia al final. En ese caso sí habría que ampliar `WaveData` y aplicar el valor desde el flujo de spawn después del reinicio de vida del pool; también probar la reutilización entre fases. No se necesita para probar los candidatos actuales.

## 8. Validación y registro de partidas

Antes de jugar cada candidato:

1. Importar y compilar con Unity 6000.3.23f1. Conservar la evidencia de errores previos y distinguirlos de los nuevos.
2. Ejecutar `CombatFeaturesCheck.RunBatch` en una copia aislada. Incluye referencias, estados del perro, Gomero, pausa, daño, cancelación, reutilización y regresiones de Aerosol, Petardo y cierre de partida.
3. Ejecutar `QuilomboSliceCheck` con ambos órdenes de visita en sesiones independientes: su comprobación funcional de 10/25 y recompensas no está incluida en `CombatFeaturesCheck.RunBatch`.
4. Verificar los intervalos y topes efectivos de las seis fases con Q=0 y Q=25, además de Q=10 y Q=15 para las visitas individuales. Mantener las introducciones del perro y Gomero y el jefe único a 180 s.
5. Si cambia vida, confirmar daño, muerte, XP única y vida restaurada al reutilizar el mismo enemigo. Actualizar las expectativas de configuración que cambien, conservando las pruebas de comportamiento.

Para las partidas humanas, registrar al menos una muestra por minuto de nivel, vida y enemigos activos con F3; al terminar, completar una fila. No cambiar los parámetros a mitad de una ronda.

| Versión / intento | Ruta y minuto de visitas | Victoria o minuto de derrota | Equipo y mejoras principales | Nivel final | Pico observado de enemigos / FPS mínimo observado | Tramo vacío, saturado o golpe ilegible |
| --- | --- | --- | --- | --- | --- | --- |
| Referencia / 1 | Sin lugares | Pendiente | Pendiente | Pendiente | Pendiente | Pendiente |
| Referencia / 2 | Ambos | Pendiente | Pendiente | Pendiente | Pendiente | Pendiente |
| Densidad / 1–3 | Sin lugares | Pendiente | Pendiente | Pendiente | Pendiente | Pendiente |
| Densidad / 4–6 | Ambos | Pendiente | Pendiente | Pendiente | Pendiente | Pendiente |
| Resistencia, si se aplica / 1–6 | Mismas dos rutas | Pendiente | Pendiente | Pendiente | Pendiente | Pendiente |

Las muestras manuales son picos observados, no máximos exactos de toda la partida. Revisar por separado una combinación de corto alcance y otra de alcance/área, registrando las elecciones reales; el azar del mazo impide atribuir diferencias de una sola partida exclusivamente al balance.

Después de elegir números, repetir la prueba de 150 enemigos de `Docs/Hito10Performance.md` y añadir un escenario representativo del cierre con mezcla de especies, cuatro armas, piedras y gemas. Una prueba sólo con mosquitos no cubre esa carga. Registrar tiempos de frame y GC en el Profiler junto a los FPS; la comparación debe usar las mismas condiciones. Como objetivo provisional, buscar 60 FPS sostenidos a 1280×720 en la máquina de prueba y registrar su hardware; ese objetivo no certifica rendimiento en otra PC.

Si aparecen asignaciones o pausas al aumentar la carga, perfilar `WeaponController.FindNearestEnemy` (`OverlapCircleAll` crea un array en cada búsqueda) y el pico retenido del pool. Optimizar sólo si la medición lo justifica.

## 9. Reglas de decisión y orden de entrega

- Si el candidato funciona y produce el desafío buscado, aceptarlo sin aplicar las etapas opcionales.
- Si un tramo es excesivo, aumentar su intervalo 10–15%; si es insuficiente y la pantalla se vacía, reducirlo 10–15%. No compensar a la vez con vida, XP y daño.
- Si se alcanza el límite y se necesitan más enemigos, revisar ese límite; si nunca se alcanza, tocarlo no aporta presión.
- Si los FPS caen o los avisos se vuelven ilegibles, detener el aumento de carga y resolver el problema observado antes de seguir.
- Si se rechaza una ronda, restaurar sólo sus archivos/valores desde el checkpoint aceptado; evitar resets generales sobre el trabajo local.

Entregas previstas: **A)** checkpoint y referencia; **B)** densidad y resultados; **C)** resistencia sólo si hace falta; **D)** mezcla/comportamiento sólo según evidencia; **E)** validación de carga y registro final. Ninguna entrega incorpora nuevos enemigos o armas.

Esta revisión es estática sobre código, datos, prefabs y diffs locales. Se comprobaron los cálculos de frecuencia, límites y presupuesto teórico; no se ejecutó Unity ni se midió dificultad o rendimiento de los candidatos. La experiencia humana positiva informada corresponde al balance actual, no a los valores propuestos.

## 10. Registro de implementación — 2026-10-23

Esta entrega inicial aplicó únicamente la etapa 1 como candidato. El párrafo anterior describe la revisión original del plan; la ejecución posterior de Unity se registra aquí. En ese momento no se habían aceptado los números como balance final ni aplicado las etapas opcionales 2/3. La devolución humana posterior y el candidato de resistencia se registran en la sección 11; los resultados humanos no se reemplazan con simulaciones.

### Checkpoint y alcance

Antes de editar se guardaron **596 archivos** de código, datos, arte, documentación, herramientas y sus `.meta`, incluidos los archivos sin seguimiento por Git. Archivo: `/Users/franciscolang/.codex/backups/videojuegos/dificultad-2026-10-23/source.tar.gz` (13.605.712 bytes). Junto al archivo se conservan `manifest.json` con SHA-256 por archivo, `git-status.txt`, `tracked.patch` y `HEAD.txt` (`797de66`). Se verificaron el inventario y el hash de cada archivo del tar. Se excluyeron cachés, builds, reportes generados y `__pycache__`; no se cambió el índice, la rama ni los cambios locales previos.

Para probar la referencia, extraer ese tar en una carpeta vacía y abrir su subcarpeta `Conurbano Infinito` con Unity 6000.3.23f1. Para rechazar sólo esta ronda, recuperar únicamente `Conurbano Infinito/Assets/_Game/Data/Oleadas.asset` del archivo, después de revisar si tuvo modificaciones posteriores. No extraer todo sobre el workspace ni aplicar un reset general.

El diff de gameplay contra ese checkpoint contiene exactamente **doce escalares** de `Oleadas.asset`: los intervalos y topes de la tabla de etapa 1. Se comprobaron sin cambios los pesos, referencias, GUIDs, títulos, comienzos de fase y jefe. No se tocaron scripts runtime, prefabs, escenas, paquetes ni ProjectSettings. La documentación actualiza tablas, F3, condiciones de carga y registro de intentos; se conserva la prueba humana histórica sin atribuirla al candidato.

### Evidencia técnica

Todas las pruebas se ejecutaron con Unity **6000.3.23f1**, batchmode y gráficos habilitados, en una copia aislada. Sus Assets, Packages y ProjectSettings se compararon byte a byte con el candidato local: sin diferencias en archivos originales. El fixture adicional sólo existe en la copia y en `TestResults/Difficulty`, ignorado por Git.

| Criterio | Estado | Evidencia |
| --- | --- | --- |
| Referencia técnica anterior al ajuste | Pasó | `TestResults/Difficulty/baseline/validation.log`, salida 0, `COMBAT_FEATURES_CHECK_PASS` |
| Compilación y regresiones de combate del candidato | Pasó | `TestResults/Difficulty/candidate/validation.log`, salida 0, `COMBAT_FEATURES_CHECK_PASS` y `SHORT_RUN_CHECK_PASS` |
| Quilombo, recompensas únicas, umbrales y reinicio | Pasó | `QuilomboSliceCheck.Run`, escenas nuevas para ambos órdenes, en `TestResults/Difficulty/density/validation.log` |
| Seis fases con Q=0/10/15/25 y ambos órdenes para Q=25 | Pasó | 30 casos runtime, 24 configuraciones únicas, `DENSITY_CHECK_PASS`, salida 0 |
| Intervalos, topes, reposición tras muerte, jefe único y sobrantes al bajar el tope | Pasó | Aserciones del mismo fixture; tope final 88 comunes + 1 jefe con Q=25 |
| Partidas humanas de referencia y candidato | No ejecutado | Filas individuales pendientes en `PruebaDemo.md` |
| 150 enemigos y cierre representativo con Profiler | No ejecutado | Procedimiento en `Hito10Performance.md`; después de aceptar los números |

CombatFeaturesCheck ejercitó animaciones, perro, Gomero, daño, pausa, cancelación y pool, además de Hito 4/9/11, ExpansionCheck, PetardoCheck, VisualSlice5Check y ShortRunCheck (victoria, derrota, reinicio y menú). Las tres corridas finales registraron cero errores/warnings de C# y ningún error nuevo de gameplay. Cada arranque repitió una excepción interna de `UnityEditor.Search.SearchDatabase`, ya presente en la referencia y contabilizada aparte (`knownEditorSearchErrors=1`).

El primer intento del fixture adicional falló al comprobar el intervalo porque Main ya había ejecutado un frame automático antes del primer sondeo. Se corrigió el fixture para forzar la entrada de fase antes de medirla; no se cambió el juego. El log y código de salida 1 se conservan en `TestResults/Difficulty/density-initial-fixture-failure/`. La corrida corregida pasó los 30 casos. Los sondeos controlan reloj y temporizadores para alcanzar los topes sin esperar una partida; no miden densidad orgánica, dificultad ni rendimiento.

Fixture reproducible: `TestResults/Difficulty/DensityValidationBatch.cs`. Copiarlo sólo a `Assets/_Game/Editor/` de una copia aislada y ejecutar Unity con `-batchmode -projectPath /ruta/copia -executeMethod DensityValidationBatch.Run -logFile /ruta/density.log`. La comprobación de combate usa el comando de `CombateYVecinoGomero.md`. El path de la copia usada está en `TestResults/Difficulty/workspace.txt`; las capturas funcionales del candidato están en su subcarpeta `candidate/`. No se generó una build de entrega nueva.

**Decisión pendiente de aquella entrega:** evaluar las partidas humanas antes de aceptar la densidad o aumentar vida. La devolución posterior de Francisco se registra a continuación. El plan no autoriza deducir una victoria humana, FPS sostenidos o necesidad de resistencia de las pruebas técnicas.

## 11. Candidato de resistencia — devolución humana del 2026-10-23

Francisco informó: «Las partidas jugadas por mi parte las gane todas sin mucho esfuerzo». Confirmó que jugó **el candidato actual de densidad, con rutas variadas**. Es evidencia cualitativa de dificultad insuficiente para él; no se informó cantidad exacta de intentos, equipos, niveles, visitas por partida ni métricas. No se completan las filas detalladas con datos inventados ni se afirma una tasa medida. Se conserva la densidad como base y se activa la etapa 2, sin sumar otro aumento de frecuencia.

Se modificaron sólo cuatro valores de vida: Paloma 12→15, Bolsa 5→6, Perro 8→10 y Gomero 10→12. Mosquito permanece en 3 y La Jefa en 100; no hay observación de muerte prematura que justifique subir a La Jefa. Mezcla, intervalos, límites, daño, XP, velocidad y ventanas de aviso/descanso no cambian.

Antes del cambio se creó otro checkpoint completo de 596 archivos, con hashes SHA-256 verificados, en `/Users/franciscolang/.codex/backups/videojuegos/dificultad-2026-10-23-pre-resistencia/`. Incluye la etapa de densidad y sus documentos. Para rechazar sólo resistencia, recuperar los cuatro prefabs, `Editor/ExpansionCheck.cs`, `Editor/CombatFeaturesSetup.cs` y `Editor/CombatFeaturesCheck.cs` de ese checkpoint después de revisar cambios posteriores; no restaurar las oleadas desde el checkpoint inicial.

ExpansionCheck compara la vida restaurada del perro con la configuración del prefab. CombatFeaturesSetup crea ahora al Gomero con 12 de vida para que una reintegración no revierta el candidato; no se ejecutó ese menú ni se regeneraron recursos. CombatFeaturesCheck añade dos ciclos por cada uno de los seis prefabs: vida inicial esperada, daño no letal, ausencia de XP antes de morir, muerte única, una gema con el valor configurado y reutilización de la misma instancia con vida completa.

Unity 6000.3.23f1 compiló la copia aislada sin errores ni warnings de C#. `TestResults/Difficulty/Resistance/combat/validation.log` terminó con salida 0, `ENEMY_HEALTH_POOL_PASS prefabs=6 lifecycles=12` y `COMBAT_FEATURES_CHECK_PASS`; pasaron también las regresiones de ExpansionCheck, PetardoCheck, VisualSlice5Check y ShortRunCheck. `TestResults/Difficulty/Resistance/density/validation.log` terminó con salida 0 y `DENSITY_CHECK_PASS`: se conservaron los 30 casos runtime de fases y Quilombo, con ambos órdenes de visita. No hubo errores nuevos de gameplay; cada arranque volvió a registrar la excepción interna conocida de UnityEditor.Search, contabilizada aparte.

Los archivos originales de Assets, Packages y ProjectSettings se compararon byte a byte con la copia validada. El diff contra el checkpoint previo a resistencia contiene sólo cuatro escalares de prefabs y las tres herramientas de Editor indicadas, más documentación; `Health`, oleadas y scripts runtime siguen intactos. El parche de esta ronda está en `TestResults/Difficulty/Resistance/candidate.patch`.

Al cerrar esta entrega faltaba jugar el candidato de resistencia en las dos rutas. La devolución posterior de Francisco se registra en la sección 12. Las partidas de referencia detalladas y la medición de carga con Profiler siguen pendientes; no se cambia mezcla o comportamiento sin una observación que lo justifique.

## 12. Nueva frecuencia después de adquirir armas — 2026-10-23

Francisco informó que la resistencia está «mucho mejor», pero que al adquirir otra arma pierde mucha dificultad: con Aerosol dejó de correr peligro. Considera que la vida puede estar bien y la cantidad es baja. No informó minuto de adquisición, visitas, nivel, bajas ni enemigos activos en esta prueba. Se conservan las vidas de etapa 2 y se prueba sólo frecuencia, aplicando la regla de reducir intervalos un 10–15% por ronda.

La inspección de WeaponController, Projectile y Aerosol.asset confirma cinco nubes cada 0,8 s, con hasta cinco blancos por nube (pierce=4); no es una medición de DPS real. El daño depende de posición, impactos, mejoras y enemigos. WaveDirector intenta generar un solo enemigo por oportunidad. Aumentar únicamente el máximo de vivos no aporta enemigos si las bajas mantienen espacio libre; no hay medición que demuestre que actualmente se alcanza el tope. Por eso primero se reduce el intervalo, sin modificar topes, armas, XP, composición ni vida.

| Inicio | Intervalo anterior → candidato | Reducción de intervalo | Intervalo con Q=25 | Tope base / Q=25 |
| --- | --- | --- | --- | --- |
| 00:00 | 2,40 → 2,40 s | Sin cambio; aprendizaje inicial | 1,50000 s | 18 / 29 |
| 00:30 | 1,80 → 1,55 s | 13,9% | 0,96875 s | 26 / 42 |
| 01:00 | 1,20 → 1,05 s | 12,5% | 0,65625 s | 36 / 58 |
| 02:00 | 0,85 → 0,75 s | 11,8% | 0,46875 s | 46 / 74 |
| 03:00 | 1,30 → 1,15 s | 11,5% | 0,71875 s | 40 / 64 |
| 03:30 | 0,65 → 0,57 s | 12,3% | 0,35625 s | 55 / 88 |

Las oportunidades teóricas sin Quilombo pasan de 218,986 a 247,716 (+13,12%); no son apariciones garantizadas ni una medida de enemigos en pantalla. El arranque de 30 s se conserva para aprender y llegar a la primera mejora; la caída de peligro posterior a un Aerosol adquirido antes de ese punto puede seguir siendo perceptible durante ese tramo. Registrar su minuto en la siguiente prueba permite distinguirlo.

Checkpoint previo a esta ronda: `/Users/franciscolang/.codex/backups/videojuegos/dificultad-2026-10-23-pre-frecuencia/`, con 596 archivos y hashes verificados. Conserva las vidas de etapa 2. Para rechazar sólo frecuencia, recuperar únicamente Oleadas.asset de ese checkpoint tras revisar cambios posteriores. El diff de gameplay de esta ronda contiene exactamente cinco escalares de ese asset; no se ejecutaron herramientas de regeneración ni se cambiaron scripts del juego.

Unity 6000.3.23f1 validó la copia aislada sin errores ni warnings de C#: `TestResults/Difficulty/Frequency/combat/validation.log` y `density/validation.log` terminaron con salida 0. Pasaron `COMBAT_FEATURES_CHECK_PASS`, `ENEMY_HEALTH_POOL_PASS` (12 ciclos de seis prefabs), `SHORT_RUN_CHECK_PASS` y los 30 casos de fases/Quilombo de `DENSITY_CHECK_PASS`, usando un fixture con los nuevos intervalos conservado en esa misma carpeta. Cada arranque repitió la excepción interna conocida de UnityEditor.Search, registrada aparte; no hubo errores nuevos de gameplay. Los archivos originales de Assets, Packages y ProjectSettings se compararon con la copia validada sin diferencias. El parche de esta ronda está en `TestResults/Difficulty/Frequency/candidate.patch`.

La aceptación humana del candidato y la medición final de carga siguen pendientes. En la siguiente partida, registrar minuto de adquisición de Aerosol, enemigos activos antes/después y lugares visitados; si se llega al tope y falta cantidad, entonces evaluar ese tope. No se presupone una necesidad de debilitar Aerosol ni modificar XP a partir de una sola observación.
