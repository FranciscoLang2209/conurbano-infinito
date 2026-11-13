# Cierre de entrega — 13/11/2026

**Estado vigente: LISTA CON LIMITACIONES para Mac.** Funciones congeladas; revisión cerrada a pedido del usuario. Evidencia nueva en `TestResults/Candidata-20261113-1635/`; matriz PASS/FAIL/NO EJECUTADO y paquete al final, en «Candidata de entrega — validación independiente». Los cierres y paquetes anteriores son históricos.

**Estado de la ampliación solicitada:** ver «Ampliación acotada de POIs — revisión vigente» al final. Sus pruebas corresponden al código actual en Editor; las apps y ZIP anteriores conservan su alcance histórico.

**Revisión posterior de bugs:** el cierre y ZIP descritos más abajo son históricos. El estado vigente de las correcciones y su validación está en «Revisión focalizada de bugs» al final; no atribuir esos cambios al ZIP anterior.

Estado: **lista con limitaciones para revisión en Mac**; aceptación humana de la entrega todavía pendiente. Destino confirmado por Francisco: **Mac, dentro de una hora**. Inicio 09:07–09:10 ART; congelar funciones a las 09:50 y dedicar al menos 18 minutos a validación y empaquetado. El adjunto de Downloads se leyó como referencia; no autoriza publicación ni etapas online.

## Estado inicial

- HEAD/main `7a430289fa3dd51c903da9e438aa6e64afe8a530`, idéntico a la referencia revisada; `git status --porcelain=v1 -uall` vacío. Sin reset ni descarte. No se encontró AGENTS.md en ancestros, repo, Docs ni raíz Unity.
- Raíz `Conurbano Infinito/`; Editor instalado 6000.3.23f1 (09d2ecc7fb28), coincide con ProjectVersion.txt. macOS 26.5.2/arm64, Apple M3 según informe previo (hardware se vuelve a comprobar). Sin proceso Unity abierto al inicio; sin herramientas Unity MCP expuestas ni dependencia MCP en manifest. No actualizar motor/paquetes.
- URP 17.3.0, Input System 1.20.0, Test Framework 1.6.0; sin asmdefs propios/NUnit propio. Checks de Editor con assertions y Play Mode.
- Escenas habilitadas en EditorBuildSettings: `Assets/_Game/Scenes/Menu.unity` y `Assets/_Game/Scenes/Main.unity`; SampleScene deshabilitada. MenuScreen → selector → Main. Modos reales: Clásico (240 s; Jefa 180 s, victoria sin exigir matarla) e Infinito (sin victoria a 240 s). Récords locales existentes. Etapa 9 sólo planificación.
- Build limpia más reciente identificada: `Builds/Mac/Etapa8-20261106/ConurbanoInfinito.app`; hay QA separada. La build Windows de octubre no representa el código actual. Recuperación: conservar ambas carpetas existentes.
- Entrada real Mac: `DeliveryCheck.BuildMac`, variable `CONURBANO_DELIVERY_APP`. Windows documentado en `Docs/Hito11Entrega.md`: Build Profiles/Windows/x86_64/Mono/Release; no hay entrada BuildWindows. MacStandaloneSupport instalado. WindowsStandaloneSupport ahora existe bajo `6000.3.23f1/PlaybackEngines` y modules.json marca windows-mono seleccionado; falta confirmar capacidad real/equipo Windows, fuera del destino Mac acordado.
- Evidencia de esta sesión: `TestResults/EntregaHoy-20261113/` (ignorado por git), hashes iniciales y copia aislada en project-path.txt. Checks y build sólo sobre esa copia; nunca Setup/Apply/Generate generales.

## Rutas reales y alcance

Todas las rutas siguientes son relativas a `Conurbano Infinito/Assets/_Game/`, salvo Tools/Docs.

| Sistema | Ubicación real |
| --- | --- |
| Colocación POIs | Scripts/Player/QuilomboManager.cs; ocho spawnPoints y tres elements en Scenes/Main.unity |
| Desafío/recompensa | Scripts/Player/RiskZone.cs, RiskZoneData.cs; Data y referencias de Main |
| Suelo/calles | Tools/generate_plaza.py; Art/Sprites/Environment/PlazaPiso.png y .mat; quad PlazaPiso en Main, escala 20000, UV repetidas 1875 |
| Cancha | Art/Sprites/Environment/CanchitaDelClub.png, 192×192/48 PPU, pivote central, escala 1; footprint 4×4 unidades |
| Progreso/modificadores | Scripts/Player/BuildState.cs, BuildStatistics.cs, WeaponController.cs, PlayerController.cs, ExperienceSystem.cs |
| Estados/score/récords | Scripts/Player/GameManager.cs, RunRules.cs, ScoreSystem.cs, LocalRecords.cs |
| Textos/UI | Scripts/Player/GameHud.cs, RiskZone.cs, MenuScreen.cs, UpgradeSelection.cs, RecordsView.cs y datos SO |
| Validación | Editor/PitchCheck.cs, QuilomboSliceCheck.cs, PoiIndicatorsCheck.cs, BuildStateCheck.cs, ScoreCheck.cs, UiRedesignCheck.cs y DeliveryCheck.cs |

Mapa fijo mediante textura repetida; no se encontraron chunks/regiones semánticas ni generación de secciones en runtime. El sorteo actual sólo verifica distancias. Tres POIs: Kiosco (+25% radio, 10 s, +10 Q), Parrilla (+20% daño permanente en la partida, +15 Q), Canchita (6 s, cura hasta 2, +10 Q). Cada instancia recompensa una sola vez y suma 250 puntos vía GameManager.

## Prioridad y etapas verificables

1. Baseline Unity/Console en copia aislada y reproducción de cancha/calles.
2. Fix geométrico compartido con regiones válidas y footprint completo; 100 semillas, conservar tres POIs activos y distancias.
3. Evaluar seis POIs y beneficios temporales separados del progreso permanente. Sólo incorporarlos si queda margen para expiración, combinación, pausa, HUD y récords por reglas. Si no, recortar contenido nuevo explícitamente.
4. Mensajes breves y coherentes, tabla antes/después; no reemplazos masivos.
5. Congelar, checks afectados/regresión, build Mac Release, ZIP completo con licencias/instrucciones, extraer y ejecutar fuera de Editor.

## Incidencias

| ID | Reproducción / actual / esperado | Causa y estado |
| --- | --- | --- |
| MAP-01 (P1) | Iniciar Main con distintas semillas: la cancha puede invadir caminos; debe entrar completa en una sección de césped. | QuilomboManager sólo filtra distancias; no conoce UV ni footprint. Confirmación geométrica y visual pendiente. No corregir por sorting. |
| QA-01 | Excepción histórica UnityEditor.Search.SearchDatabase; ComputeBuffer al cerrar Player, según informe 06/11. | Conservar traza exacta y separar de errores nuevos; no asumir que cualquier excepción es conocida. |

## Checklist al inicio (histórico)

| Caso | Estado inicial |
| --- | --- |
| Compilación runtime/Editor y Console | NO EJECUTADO en esta sesión |
| Cancha: 100 semillas, footprint y visual | NO EJECUTADO |
| Tres POIs originales sin regresión | NO EJECUTADO |
| Seis POIs y buffs | CONDICIONAL al plazo |
| Regresión reglas/modos/récords/UI | NO EJECUTADO |
| Build Mac Release y paquete extraído | NO EJECUTADO |
| Recorrido Player menú/partida/pausa/reinicio/menú | NO EJECUTADO |
| Partida humana completa/15 minutos reales Infinito | PENDIENTE; automatización no prueba balance |
| Rendimiento sostenido en equipo objetivo | PENDIENTE; no afirmar 60 FPS |
| Windows | FUERA del destino confirmado; no generado ni probado hoy |

Riesgos: tiempo de import/build; firma ad hoc sin notarización; no sobrescribir récords reales con QA; tests alteran escenas/preferencias sólo en copia. Ningún PASS histórico valida el cambio de hoy.

## Mensajes previstos (antes de implementarlos)

| Contexto/ruta Scripts/Player | Antes | Después |
| --- | --- | --- |
| GameManager, resultado | La build de esta partida | Tu equipo de esta partida |
| GameManager, derrota/victoria | Fin de partida / Victoria · Cuatro minutos | Hasta acá llegamos / ¡El barrio aguantó! |
| GameManager, reinicio | Otra partida | Otra vuelta |
| GameManager, práctica | Práctica · no participa en récords | Partida de práctica: sin récord |
| UpgradeSelection, título | Elegí una mejora | Elegí cómo seguirla |
| RecordsView, detalle/orden | Build; Run ID ordinal | Equipo; explicación por puntos y tiempo sin claves internas |
| RiskZone, desafío | Resistí 2/6 s · +10 Q | Quedate en la zona: 2 de 6 s; coste y puntos separados |
| RiskZone, ya usado | Sin rótulo | Ya pasaste por acá |
| RiskZoneData, Parrilla | +20% de daño | +20% de daño en esta partida |
| GameHud, beneficios | Un panel grande podía tapar otro beneficio | Hasta cuatro líneas compactas con efecto y segundos restantes |
| RecordsView/LocalRecords, detalle | Run ID, Personaje ID, reglas como clave, Build | Personaje por nombre, reglas con nombre, Versión del juego; IDs conservados sólo en datos |

## Etapas 1–3: evidencia y decisiones

- Baseline real: `baseline-pitch.log`, Unity salió 0; runtime y Editor compilados. Tres POIs/check original aprobados. La excepción ArgumentOutOfRangeException incluye `UnityEditor.Search.SearchDatabase.EnumerateAll`/GetDefaultSearchDatabase/IndexationOnStartup, igual a la histórica; se cuenta aparte, no se ignoran otras excepciones.
- Reproducción nueva: `repro-pitch.log`, salida 1, semilla 0, footprint de Parrilla en (8,−20) invade una calle. El mismo defecto de colocación afecta a Canchita; no era sólo sorting. Se conserva la evidencia del fallo.
- Fix MAP-01: ocho candidatos reubicados en centros de césped según el quad real, escala 20000 y tiling 1875 (período 10⅔ unidades). Cada candidato define una región de césped de 8×8; el colocador rechaza sprites cuyo footprint completo +0,25 de margen no cabe. Todos los candidatos quedan a ≥15 del inicio y a ≥10 entre sí. No hay restricciones por tipo: los ocho admiten los seis POIs, por lo que el sorteo secuencial no consume candidatos exclusivos. Búsqueda acotada; ningún POI se desactiva en las semillas probadas. Suelo/arte previo/material intactos.
- `candidate-pitch-v3.log`, salida 0: 100 semillas 0–99, seis zonas activas, distancias y footprint sin calles. CSV de 600 ubicaciones. Cancha con dos resistencias físicas reales de seis segundos; tres POIs originales y seis órdenes conservados (Q35). Nuevos POIs con contactos reales y temporizadores adelantados sólo para completar desafíos del fixture: recompensa/evento único, no reentrada, +5 Q por nuevo tipo y +250 puntos cada uno.
- Se incorporan Mural (+20% daño/20 s tras resistir 5 s), Ronda de mate (intervalo ×0,85/20 s tras 4 s) y Bicicletería (+15% movimiento/15 s tras 4 s). Total de seis lugares: Q50 y 1500 puntos por POIs; modifica presión/puntos alcanzables. Reglas nuevas `entrega-pois-2` en ambos perfiles; IDs de modo y formato de guardado intactos, tablas anteriores conservadas.
- Estado temporal en BuildState/PlayerController, reloj escalado: pausa y elección congelan duración. Daño suma puntos porcentuales con Parrilla/afinidad (20% +20% =40%, más afinidad si corresponde); intervalo y movimiento multiplican progreso permanente y mantienen clamps. Mismo efecto renueva el plazo desde ahora, conserva la intensidad activa mayor y nunca suma intensidades; al vencer se retira sólo ese aporte. Efectos distintos coexisten. Fin de partida limpia beneficios; reinicio reconstruye visitas. No hay chunks ni pooling de POIs: deshabilitar/reactivar la misma instancia conserva su uso; cargar Main inicia una partida nueva.
- Orbitales existentes actualizan daño/cadencia al aplicar/vencer; disparos y segundo pulso ya emitidos conservan snapshot de lanzamiento, igual al contrato previo de proyectiles. Próximo ataque se reescala al cambiar cadencia, incluido vencimiento. Sin mutar ScriptableObjects runtime.
- `TEMPORARY_BENEFITS_PASS`: cálculo real, renovación de distinta potencia, pausa, selección, mejora permanente durante beneficio, expiración, orbital, coexistencia, score único, SO inmutables, fin/reinicio. Capturas en cuatro resoluciones. `PITCH_INDICATORS_PASS`: seis en el mismo borde; además se conservan las 1536 combinaciones originales de tres y se añaden seis por cada dirección/formato.
- Revisión visual: `candidate-pitch-v3/canchita-resistiendo-1280x720.png` muestra cancha completa en césped; `indicadores-6-1280x720.png` muestra seis flechas y cuatro beneficios. `beneficios-4-1024x768.png` muestra todos los tiempos. Se estrecha después la franja de beneficios para evitar que su fondo toque el aviso de oleada; la candidata final se vuelve a comprobar.
- Intentos conservados: candidate-pitch falló por un esperado incorrecto del fixture (afinidad del Pibe se suma, no se multiplica); v2 falló porque la prueba buscaba orbitales en Aerosol, cuyo patrón real es Straight (disparo en abanico). Se corrigió el fixture a Fernets Orbitales; v3 pasó. No se rebajaron comprobaciones de runtime para obtener PASS.

Funciones congeladas: empieza regresión final; no se añaden más contenidos. Sigue pendiente validar el paquete extraído, recorrido fuera del Editor, balance humano y rendimiento sostenido.

## Regresión y revisión de fuentes

La primera matriz final aprobó Pitch (41,56 s), Score (22,01 s), BuildState (13,09 s), UiRedesign (44,16 s), Characters (21,85 s), CombatFeatures (16,62 s), Synergy (24,29 s), Evolution (22,45 s) y Endless (33,40 s), todos salida 0. Los logs/argv están en regression-results.json. UiRedesign produjo text-layout.txt vacío. Se mantienen los recorridos ShortRun y las verificaciones de física/pool incluidas por esos checks.

Records falló por un fixture que aún creaba las marcas en builds-infinito-1 mientras abría por defecto entrega-pois-2: la tabla nueva estaba vacía y no tenía segunda página. Se conserva records.log/salida 1. Se adapta el fixture para usar las reglas vigentes por defecto y se mantiene una prueba explícita de guardados de builds-infinito-1 en tabla separada; no se cambia persistencia para acomodar la prueba. Se repiten RunBatch y ReloadBatch sobre directorio nuevo.

Integridad estática: todos los GUIDs/meta previos, fileIDs previos de Main, paquetes y ProjectSettings preservados. Sin dependencias nuevas ni scripts/runtime QA en fuentes de entrega. Tres sprites nuevos originales; ejecución de delivery_pois() no alteró ningún byte de arte previo. Nueva escena sólo suma tres objetos de cinco bloques, registro/raíces y mueve los ocho candidatos. No se usó Setup/Apply/Generate general.

Fuente congelada: release-source-sha256.json (673 archivos Unity), source-snapshot.tar.gz y source.patch en evidencia. Copia release nueva indicada en release-project-path.txt; sin Library/Temp de la copia de pruebas. DeliveryCheck.BuildMac sigue siendo la única entrada utilizada. La consulta Windows de ese método mira la ubicación histórica dentro de Unity.app; su False no descarta el módulo ahora presente en PlaybackEngines fuera del bundle. Windows no es el destino acordado y no se probará como si Mac lo certificara.


## Candidata, paquete y verificación final

**Plataforma entregable:** Mac universal x86_64 + arm64, Mono/Release, objetivo macOS 12.0; bundle 1.0, edición Entrega-20261113, reglas entrega-pois-2. Fuente base 7a43028 más cambios locales identificados por snapshot/diff, sin commit/push. `DeliveryCheck.BuildMac` terminó **Succeeded, 0 errores, 1 warning, 127.231.580 bytes, 131,60 s internos / 207,43 s de proceso** en copia recién creada. Advertencia: no se pudieron subir símbolos a Unity Cloud por falta de token; no se agregaron credenciales. `lipo -info` confirmó ambas arquitecturas; codesign verificó firma ad hoc, sin identidad de distribución ni notarización. Archivo build-mac.log completo conservado.

**Once entradas finales aprobadas:** Pitch, Score, BuildState, UiRedesign, Characters, CombatFeatures, Synergy, Evolution, Endless, Records y Reload. Records final 44,90 s y Reload 7,86 s, ambos salida 0; informes UI/Records de layout vacíos. Todos los Assets/Packages probados coinciden byte por byte con el árbol actual; los 673 archivos de la copia Release siguen idénticos después del import/build. No hay paquetes/ProjectSettings/meta anteriores alterados.

**Paquete final:** `Builds/Mac/ConurbanoInfinito-Mac-Entrega-20261113.zip`, carpeta `Entrega-20261113/` con `.app` completa, README-ENTREGA.md, este informe, Fuente.json y Licencias.md más textos de licencias. Los directorios BurstDebug/DoNotShip se conservaron fuera del paquete, en la evidencia. No incluye Library, Temp, proyecto, herramientas, sondas QA ni guardados. ZIP extraído aparte y contenido de la app comparado por SHA-256 con la app ejecutada; integridad/firma verificados. Hash externo y resultados exactos en `TestResults/EntregaHoy-20261113/packaging-results.json` y archivo `.sha256` junto al ZIP. Sin publicación ni enlace público inventado.

**Player real fuera del Editor:** la aplicación del ZIP candidato, con los mismos bytes de la app final, abrió Menu/selector. Tres partidas cortas consecutivas Pibe/Clásico verificaron movimiento, pausa/reanudación, dos reinicios y limpieza de vida/XP/arma/Q/reloj; M regresó al menú. Mouse eligió La Piba/Infinito; 19,3 s activos con combate real: cuatro bajas, XP2/5, vida2/4, 59 puntos. No se inyectaron atributos. Se cerraron las pruebas por abandono, excluidas del top; persistencia y detalle con mouse/AvPág comprobados tras cerrar (exit 0) y relanzar. Tabla actual sin marcas elegibles. La aplicación queda abierta en el último detalle para revisión. Observaciones exactas: native-ui-observations.md; logs player-native.log/player-relaunch.log y resumen de guardados player-records-summary.json.

El primer intento batch no se usa como evidencia de interacción gráfica; la prueba observada usó el Player normal. Los checks de Editor guardan QA separado. El Player normal añadió sólo abandonos/último resultado, **cero marcas nuevas elegibles**; no se borraron ni reemplazaron guardados reales. El aviso ComputeBuffer al cerrar se reprodujo con salida 0: preexistente y sin resolver; no hubo excepciones de gameplay ni asserts en los logs revisados.

### Matriz final

| Caso | Resultado y alcance |
| --- | --- |
| Compilación runtime/Editor | PASS Unity real; no errores C#; SearchDatabase histórico separado |
| Serialización/escenas/referencias | PASS estático + import/build + Play Mode; GUIDs/fileIDs previos conservados; Menu primero |
| Cancha/calles, todos los candidatos | PASS geometría 8 candidatos × 6 tipos, margen 0,25; rechazo de footprint sobredimensionado |
| Sorteo y capacidad | PASS 100 semillas 0–99 / CSV 600 colocaciones, seis activos y distancias; sin candidatos exclusivos |
| Curación/POIs originales | PASS físicas reales 6 s por personaje, visitas únicas/seis órdenes, Q35 previo conservado |
| Beneficios nuevos | PASS Editor/Play Mode: aplicación, renovación, coexistencia, expiración, permanente durante buff, pausa/selección, orbital y fin/reinicio |
| Score/récords | PASS seis ×250 una vez, tablas por reglas, recarga/corrupción/backup y práctica/abandono excluidos |
| UI | PASS comprobaciones de texto y capturas 1024×768, 1280×720, 1920×1080, 2560×1080; seis indicadores y cuatro beneficios; revisión visual representativa |
| Modos/reglas | PASS checks de Clásico 240 s e Infinito sin victoria a 240, calendario de jefes/colas/pool; reloj controlado de QA, no partida humana |
| Build Mac Release | PASS build nueva, universal, firma ad hoc válida |
| Paquete extraído/Player | PASS integridad y recorrido corto descrito, dos personajes/modos, teclado/mouse, tres inicios consecutivos y persistencia |
| Player: mejora, seis desafíos/buffs, derrota/victoria | NO EJECUTADO manualmente; cubierto en Editor, pendiente en paquete |
| Clásico humano completo / Infinito 15 min reales | NO EJECUTADO; no certificar balance |
| Rendimiento sostenido / audio humano / otro Mac | NO EJECUTADO; no afirmar 60 FPS ni calibración final |
| Windows | NO EJECUTADO, fuera del destino confirmado; ninguna build antigua valida esta edición |

MAP-01: **corregido y verificado** dentro del alcance geométrico/Play Mode/visual descrito. No se observaron nuevos bloqueos o errores de gameplay en las pruebas realizadas. Excepción SearchDatabase y aviso ComputeBuffer siguen como incidencias preexistentes abiertas.

**Siguiente cambio concreto:** sin más funciones antes de la entrega. Ejecutar en el paquete extraído una partida humana completa de Clásico (Jefa 180 s, victoria 240 s), completar los seis lugares con una mejora permanente durante un beneficio y comprobar derrota/reinicio; después extender Infinito a 15 minutos reales. Registrar resultados aquí antes de declarar aceptación definitiva o ajustar balance. Si falla un caso, corregir sólo ese bloqueo y reconstruir/repetir las pruebas afectadas.

Recuperación intacta: `Builds/Mac/Etapa8-20261106/ConurbanoInfinito.app`. No se actualizó Unity/paquetes, no se implementó online, no se publicaron archivos y no se regeneraron escenas/prefabs de forma general.

Detalle visual menor pendiente (UI-02/P3): en el fixture `final-pitch/canchita-resistiendo-1280x720.png`, el rótulo sobre la cancha coincide parcialmente con el indicador superior de Ronda de mate. No tapa vida, selección ni desafío y no afecta colocación/recompensa. Se conserva como límite de los rótulos sobre el mundo; no se afirma ausencia absoluta de superposiciones entre nombres e indicadores. Ajustar su prioridad en el siguiente pase de UI y repetir capturas antes de otra build; no bloquea el recorrido corto verificado.


## Revisión focalizada de bugs — 13/11/2026

Pedido actual: investigar cancha/calles y las incidencias de esta entrega usando el árbol local, sin repetir auditoría general. El adjunto de Downloads es referencia: su cronograma y propuestas de contenido/publicación no son instrucciones de este pedido. No se recibió una lista adicional: el marcador «PEGAR ACÁ» queda **pendiente de relevamiento**, sin inventar errores.

Baseline: HEAD `7a430289fa3dd51c903da9e438aa6e64afe8a530` más los cambios locales ya existentes al iniciar esta revisión. Se preservaron todos. Copias de los archivos tocados, hashes y status en `TestResults/BugsEntrega-20261113/baseline*`; Unity 6000.3.23f1 / Metal en Mac. Pruebas en la copia aislada identificada por `project-path.txt`, no sobre los guardados reales. No se ejecutó Setup/Apply/Generate general.

### Geometría comprobada

- `QuilomboManager.Start` registra seis RiskZone y ocho Transform. Los objetos de los POIs están en Main, sin prefab instanciado ni hijos visuales en la configuración actual. El código ahora también incluye sprites hijos al calcular el footprint.
- PlazaPiso es un **quad XY**, vértices locales ±0,5, UV = local XY + 0,5; posición mundial `(0,0,5)`, rotación identidad, escala `(20000,20000,1)`, sin padre. Los ocho candidatos son hijos de un Transform identidad; se usa su posición **mundial**, no local. No hay grilla, Tilemap, chunks, regeneración de terreno ni pooling de POIs en runtime; el pooling encontrado corresponde a combate. Se comprueban todos los candidatos del mapa fijo.
- Material URP/Unlit opaco, `_BaseMap` y `_MainTex` con tiling `(1875,1875)` y offset cero. PNG 512×512, Point/Repeat. Por eso `u = 937,5 + x × 0,09375`, igual para v/y: período mundial `10⅔`, caminos centrados en múltiplos de ese período, césped continuo entre caminos. El origen de la repetición **no** es el origen de una grilla de 8 unidades.
- `generate_plaza.floor()` pinta caminos con semianchos 43/39 píxeles, onda máxima 3 y borde 4. Se reservan conservadoramente 50/46 píxeles a cada lado del centro del cruce. Las regiones semánticas mínimas son los rectángulos de césped entre cruces, derivados del quad/material reales; todos admiten los seis tipos en los ocho candidatos actuales. No se agregaron categorías ni un generador de mapa.
- Canchita: rectángulo completo 192×192 / 48 PPU = **4×4 unidades**, pivote `(96,96)`, escala 1, rotación identidad. Trigger circular radio 1,1, insuficiente para representar el dibujo completo. Se validan bounds visuales completos + **0,25 unidades por lado**, los cuatro vértices contra el suelo, límites de la malla y reservas de otros POIs. Rotación/escala/pivote e hijos afectan esos bounds; no se toma sólo el punto central ni el trigger.
- Canchita y suelo conservan layer/sorting layer Default y sortingOrder 0; cancha z=1, suelo z=5. Los caminos forman parte de la misma textura del suelo, no son una capa independiente. El sprite sobre el suelo es correcto como renderizado: la invasión histórica era una **ubicación lógica** inválida. No se tocó sorting, malla, material, UV, textura ni decoración para esconder el problema.

### Registro de incidencias

| ID / gravedad / origen | Reproducción y esperado / actual antes del fix | Causa | Fix y resultado |
| --- | --- | --- | --- |
| MAP-01 / P1 mapa / histórico, ya corregido parcialmente al iniciar | Colocar el sprite real en cada uno de los ocho puntos de HEAD: `(-20,-8),(-18,12),(-8,20),(8,20),(20,12),(20,-8),(8,-20),(-8,-20)`. Esperado: footprint dentro de césped; actual de esa referencia: **8/8** invaden caminos. Los ocho puntos reubicados del árbol actual ya pasaban la comprobación antes de esta revisión. | Sorteo original sólo sabía distancias, sin relación entre mundo y textura. No era sorting. | Se conservan los ocho candidatos actuales. Se repitió la contraprueba con las coordenadas reales de HEAD (`baseline-repro.log`). La protección geométrica de MAP-02 completa el arreglo; no se presenta MAP-01 como una regresión nueva del árbol actual. |
| MAP-02 / P1 mapa / preexistente en el intento local de fix | Mover un candidato configurado a `(0,20)` y consultar `FitsLawn(Canchita, point.position)`: devuelve **true**, pero la comprobación UV rechaza el footprint. Esperado: rechazarlo o elegir otro candidato. | El rectángulo sintético de 8×8 se centraba otra vez en cada posición propuesta; se desplazaba junto con el error y no representaba una sección real. | Referencia serializada al MeshFilter de PlazaPiso y validación de la región real, transformaciones, material, bordes, footprint y margen. Caso repetido después: rechazado. También pasa la colocación cercana al borde a +2,0 y se rechaza el desborde a +2,1; el trigger todavía entraría en ese último caso. |
| MAP-03 / P1 mapa / riesgo latente del colocador anterior | Fixture reversible: tres candidatos en `(-18.5,-16),(-16,-16),(-13.5,-16)`, otros cuatro centros válidos alejados y un octavo duplicado; distancia mínima 4,5. Hay una asignación de seis, usando ambos extremos y dejando libre el medio. Esperado: seis activos; colocador previo: **31/100 semillas 0–99** terminan con cinco activos (reproducción mínima: semilla **4**). El mapa actual sin alterar candidatos no fallaba por capacidad. | Elegir secuencialmente un punto central podía consumir dos alternativas necesarias para los POIs siguientes. | Barajar candidatos y buscar una asignación completa mediante backtracking, con máximo **71.000 comprobaciones** (alcanza para agotar ocho candidatos/seis POIs). Aplicar posiciones sólo al encontrarla, reservando footprint además de distancias. Mismo fixture con nuevo colocador: **0/100 fallos** y footprints válidos. Además, 100 semillas donde sólo el candidato 0 admite cancha: seis activos y cancha en ese candidato. |
| UI-02 / P3 legibilidad / preexistente y documentado al cerrar | Captura anterior `EntregaHoy-20261113/final-pitch/canchita-resistiendo-1280x720.png`: el nombre de Canchita y la flecha de Ronda de mate comparten espacio. Esperado: ambos mensajes legibles; actual: texto superpuesto. | Rótulos del mundo y paneles de orientación se dibujaban desde callbacks distintos sin compartir sus reservas. | Dibujar los nombres después de calcular los indicadores en el mismo pase del HUD; el nombre cede si invade un panel o su margen de 6. La prueba comprueba que desaparece al colisionar y vuelve al retirar el indicador; no se desactiva el POI ni su desafío/recompensa. |
| QA-02 / P2 validación / fixture preexistente, expuesto al cambiar la secuencia aleatoria | PitchCheck elige `Options[0]` al comprobar pausa por mejora. En `repro-fixture.log` eligió **Termo Balístico · Cadencia**, y después esperaba intervalo 0,72 aunque la mejora legítima lo había reducido a **0,648**. Esperado: comparar un estado controlado; actual: falso fallo de expiración. | La prueba suponía que una carta aleatoria no alteraba las estadísticas que comprobaría después. Se manifestó tras cambiar el consumo/restauración de Random del colocador; no prueba una regresión del buff. | Elegir explícitamente la adquisición de arma garantizada para esa primera oferta y comprobar que es EquipWeapon. Se mantienen pausa, selección real, mejora permanente, expiración, orbitales y reinicio. No se modifica gameplay para acomodar el fixture. |
| QA-01 / P3 Editor/cierre / preexistente, abierto | El nuevo Play Mode vuelve a registrar ArgumentOutOfRangeException con traza `UnityEditor.Search.SearchDatabase`; el cierre histórico del Player registró ComputeBuffer. Esperado: Editor/cierre sin incidencias; actual: persiste la primera, la segunda no se da por arreglada. | Incidencias externas al flujo de colocación, ya registradas antes de este pedido. | Sin cambio especulativo de motor/paquetes ni supresión de errores nuevos. Se cuenta SearchDatabase por separado; no se afirma arreglado ComputeBuffer. |

Si la configuración se vuelve imposible, el colocador termina, emite un error explícito y omite la asignación completa, sin aplicar posiciones parciales. La prueba fuerza una cancha sobredimensionada, exige un solo diagnóstico, comprueba que no hubo movimientos y después restaura el fixture. **Ese camino de error no es el arreglo:** los seis POIs previstos permanecen activos en todas las configuraciones válidas probadas.

### Verificación de esta revisión

- `PitchCheck.CheckPlacementSeeds` es compartido con `QuilomboSliceCheck`: cada entrada recorre **las mismas semillas reales 0–99**, registra 600 colocaciones y comprueba seis activos, pertenencia al candidato, distancias, footprint completo, margen, regiones y reservas entre POIs. Se validan también los **48 pares candidato/tipo** del mapa fijo; no se sustituyen estas comprobaciones por distancias.
- Fixtures adicionales registran 100 semillas con candidato exclusivo para cancha y 100 con candidatos cercanos/duplicados. Casos negativos/positivos: borde válido/desborde, fuera de la malla, sprite sobredimensionado, pivote desplazado, rotación, sprite hijo, traslación/rotación/escala no uniforme del suelo, escala negativa, cambio de offset y tiling, rechazo controlado y recuperación del fixture.
- Evidencia nueva separada en `TestResults/BugsEntrega-20261113/`: baseline-repro.log; prior-allocation.log/.csv (**31 fallos**); fixed-allocation.log/.csv (**cero**); repro-fixture.log y los intentos de Pitch conservados. El harness reversible se conserva como `AllocationRepro.cs.txt` fuera de Assets; no forma parte de la entrega.
- Verificación final de Play Mode, UI y build: registrar abajo el resultado sobre las fuentes definitivas, sin trasladar PASS de intentos anteriores.

Pendientes: no hay una lista adicional concreta de errores del usuario. Balance humano, partida larga, Windows/otro Mac, rendimiento sostenido y aceptación final conservan los límites del cierre histórico. No se ejecutó otra auditoría general ni se implementó contenido nuevo.


### Resultado final de la revisión focalizada

**Estado: corregido y validado en Editor/Play Mode, build Mac generada; prueba manual del nuevo Player pendiente.**

| Criterio | Evidencia nueva / resultado |
| --- | --- |
| Compilación y recorrido focalizado | `accepted-pitch.log`, salida **0**, `PITCH_CHECK_PASS`. Dos personajes con resistencia física real de seis segundos, curación y consumo único; seis órdenes de visita originales, pausa, buffs, expiración, progreso permanente, muerte y reinicio aprobados. |
| Suelo y capacidad | `accepted-pitch/placement-seeds.csv`: **600** filas / 100 semillas; `scarce-candidate-seeds.csv` y `capacity-seeds.csv`: **100** semillas cada uno, seis activos. 48 combinaciones fijas candidato/tipo y todos los fixtures de borde/transformación aprobados. Un único error **esperado** al forzar configuración imposible; no se cuenta como error de gameplay. |
| Reproducibilidad tras recarga | `quilombo-placement-seeds.csv` conserva la repetición independiente de QuilomboSliceCheck. Las 600 tuplas semilla/tipo/candidato/posición coinciden exactamente con PitchCheck; comprobación en `verification-summary.json`. |
| UI-02 y apariencia | `WORLD_LABEL_PASS`: reserva ocupada → nombre cede, reserva liberada → nombre visible. Capturas revisadas `accepted-pitch/ui-02-label-ceding-1280x720.png` y `ui-02-label-clear-1280x720.png`; la cancha conserva una posición válida real, se mueve la cámara del fixture. Revisión adicional de `canchita-resistiendo-1280x720.png`; suelo y decoración de barrio conservados. |
| UI alrededor del cambio | `ui.log`, salida **0**, `UI_REDESIGN_PASS`; HUD, mejora, pausa, reinicio, menú, derrota/victoria y capturas en cuatro resoluciones. `ui/text-layout.txt` vacío. No se repite la auditoría completa de combate/récords/rendimiento. |
| Errores preexistentes | Una excepción SearchDatabase identificada por traza en cada proceso Play Mode; sigue abierta y separada. No hay nuevas excepciones de gameplay ni asserts en los recorridos aprobados. El fallo de expectativa de buffs queda explicado/corregido en QA-02; se conservan logs fallidos. |
| Build Mac nueva | `DeliveryCheck.BuildMac`, `build-mac.log`, salida **0**: **Succeeded**, 0 errores, 1 warning, **127.233.628 bytes**, **63,79 s** internos. Warning: símbolos nativos no subidos a Unity Cloud por falta de token; no se añadieron credenciales. |
| Artefacto e integridad | `Builds/Mac/BugsEntrega-20261113/ConurbanoInfinito.app`, Release/Mono, **x86_64 + arm64** confirmado con lipo; versión 1.0, macOS mínimo 12.0, firma ad hoc verificada con codesign. Los **673 archivos** de Assets/Packages/ProjectSettings coinciden entre fuentes y copia después del build (`source-sha256.json`, `build-results.json`). |
| Alcance del diff | Siete archivos Unity cambiados respecto del estado recibido: manager, referencia de suelo de Main, RiskZone/GameHud y tres checks. En Main sólo se sustituye lawnSize por la referencia MeshFilter; posiciones, GUIDs/fileIDs/raíces/sorting intactos. Todos los bytes de arte/materiales/meta/datos/paquetes/ProjectSettings coinciden con la entrega previa. Se actualizan este informe y UnityProjectContext. |
| Nuevo Player / paquete | **No ejecutado manualmente** el Player nuevo, no hay ZIP nuevo ni sustitución del ZIP de entrega anterior. La validación visual/runtime de estas correcciones es Play Mode real del Editor; no atribuirle la interacción manual de la build histórica. |

Para probar estas correcciones usar la **app de BugsEntrega-20261113** o el proyecto actual. `ConurbanoInfinito-Mac-Entrega-20261113.zip` conserva los bytes del cierre anterior y **no contiene este fix**. No se publicó nada ni se alteraron récords reales.

Pendientes concretos: prueba manual del nuevo Player, las pruebas humanas largas y plataformas ya pendientes, QA-01 y relevamiento de la lista adicional cuando exista. El modelo de regiones corresponde al quad XY y al cruce pintado actual; si se cambia esa geometría/arte, los checks exigen actualizar las regiones. No se promete eliminar toda posible superposición entre rótulos del mundo; UI-02 protege su conflicto con los indicadores del HUD.


## Ampliación acotada de POIs — revisión vigente

Solicitud de esta revisión: completar seis tipos sobre la arquitectura local y sus verificaciones. El documento de Downloads se usó como referencia; sus prompts de publicación, empaquetado u otras etapas no amplían esta solicitud. Al comenzar ya había cambios sin confirmar, seis tipos, arte/datos nuevos y la colocación corregida. Se conservaron. HEAD sigue siendo `7a430289fa3dd51c903da9e438aa6e64afe8a530`.

### Tipos, efectos y distribución

| Tipo | Desafío | Beneficio | Quilombo | Puntos únicos |
| --- | --- | --- | --- | --- |
| Kiosco 24hs (original) | Inmediato | +25% radio de recolección durante 10 s | 10 | 250 |
| Parrilla del Barrio (original) | Resistir 5 s | +20% daño durante toda la partida | 15 | 250 |
| Canchita del Club (original) | Resistir 6 s | Cura hasta 2, sin superar el máximo ni revivir | 10 | 250 |
| Mural del barrio | Resistir 5 s | +20% daño durante 20 s | 5 | 250 |
| Ronda de mate | Resistir 4 s | Intervalo de ataques ×0,85 durante 20 s | 5 | 250 |
| Bicicletería | Resistir 4 s | +15% movimiento durante 15 s | 5 | 250 |

`QuilomboManager.Start` coloca una instancia de cada tipo al iniciar Main; no crea POIs por frame. Baraja ocho candidatos y busca una asignación completa válida. Los ocho admiten los seis tipos: `(-16,-16)`, `(-26,6667;5,3333)`, `(-16,16)`, `(-5,3333;26,6667)`, `(16,16)`, `(26,6667;-5,3333)`, `(16,-16)`, `(5,3333;-26,6667)`. Se conservan ≥15 unidades al inicio, ≥10 entre centros, footprint visual completo de hijos/rotaciones/pivotes +0,25 de margen, césped real de PlazaPiso y ausencia de superposición entre footprints. No se regeneraron escena ni arte en esta revisión.

### Composición y ciclo de vida

- No se reemplazaron estadísticas por copias antiguas ni se tocaron SO durante juego. `BuildState.DamageBoost/AttackBoost`, `PlayerController.movementBoost` y el vencimiento de `ExperienceSystem` aportan estado temporal separado del progreso.
- Daño real: `(daño base + mejoras planas generales + mejoras del arma) × (1 + Parrilla + Mural + afinidad directa aplicable)`. Parrilla y Mural suman 40 puntos porcentuales; con afinidad directa de 10%, 50. Parrilla conserva su contrato persistente y su guardia original.
- Cadencia real: `max(0,12; intervalo base × progreso general × progreso del arma × (1 − bonus temporal))`. Mate usa 0,15: ×0,85, aproximadamente +17,65% ataques/s sin llegar al límite. Movimiento: `min(10; velocidad base × progreso permanente × (1 + bonus temporal))`. Radio: base × multiplicador temporal mientras esté vigente.
- Mismo efecto renueva a **ahora + duración entrante**, conserva la mayor potencia todavía activa y nunca suma intensidades; incluso una duración entrante menor reemplaza el plazo. Después de vencer, una activación menor comienza con su propia potencia. Efectos distintos coexisten. El Kiosco ahora usa la misma regla de mayor potencia/renovación, conservando +25%/10 s en sus datos. Se rechazan duraciones infinitas.
- Reloj escalado (`Time.time`): pausa y selección congelan los cuatro beneficios. Al vencer se retira sólo el aporte temporal; las mejoras permanentes recibidas durante él siguen vigentes. `NextAttackTime` se reescala al aplicar/vencer/cambiar cadencia, respetando su mínimo.
- **Corrección reproducida aquí:** `Projectile.SetOrbitStats` actualizaba el intervalo del orbital, pero conservaba los deadlines de contactos anteriores. Ahora reescala también cada espera pendiente, sin recrear orbitales. Sólo copia las claves al cambiar el intervalo, fuera del camino por frame. Proyectiles ya emitidos y el segundo pulso de una evolución mantienen el daño capturado al disparar; los disparos posteriores usan el estado vigente. No se borran afinidades, evoluciones ni contadores de sinergias.
- Una activación por instancia/partida; `rewardStarted` precede a la recompensa y al único evento. `GameManager.OnPoiReward` además deduplica por instancia y `ScoreSystem` conserva 250 puntos. No hay `OnTriggerStay` que emita avisos repetidos.
- Desactivar/reactivar el mismo POI conserva su uso. **No existen chunks descargables ni pooling de POIs**: son objetos de Main; no se destruyen y reconstruyen durante una partida. Por tanto no hay descarga que pueda reponer un POI. El pooling real de proyectiles limpia su estado al liberar. Si se incorpora streaming en el futuro, se necesita identidad lógica de POI y registro de visitas por partida antes de permitir recreaciones; no se agregó ese sistema inexistente.
- Derrota, victoria y abandono limpian los cuatro beneficios y llaman a `RiskZone.CloseRun` en todas las zonas, incluso desactivadas: sin progreso, referencias al jugador, avisos ni nuevas activaciones. Reinicio/cambio Main→Menu destruyen la escena; una nueva Main empieza disponible. No se reutilizan SO para guardar visitas.

### HUD y balance

El HUD existente muestra simultáneamente radio, daño temporal, espera y movimiento con porcentajes y segundos. Oculta esos elementos y los indicadores durante pausa/selección; los avisos de RiskZone también ceden al estar pausado. Los rótulos muestran nombre y «Disponible»/«Usado en esta partida», sin depender sólo del color. Se reutilizan paneles, iconos y sprites existentes; sin paquetes ni framework nuevo.

Indicadores y rótulos comparten el área libre del HUD: reservan los cuatro buffs, vida/equipo y el rectángulo real del aviso prioritario. Se corrigió además que el panel del desafío tapara nombres de indicadores. Se conservan las 1536 combinaciones originales de tres indicadores y se ejercitan seis en el mismo borde con/sin aviso y cuatro beneficios.

Acumulado: originales **35 Q y 750 puntos**, catálogo completo **50 Q y 1500 puntos**; los nuevos agregan 15 Q y 750 puntos. Con bonus de umbral 0,35 ya vigente, la presión pasa de `1+0,35+0,35=1,70` a `1+0,50+0,35=1,85`: aproximadamente +8,82% frecuencia y factor del cupo común antes del redondeo/límites; en la última fase inicial el intervalo pasa de `0,57/1,70=0,3353 s` a `0,57/1,85=0,3081 s`. Puntos por bajas pasan de ×1,35 a ×1,50 (+11,11%). El controlador actual sólo tiene umbrales 10 y 25: Q50 **no activa élites**. Se mantienen los valores propuestos; balance humano pendiente, sin afirmar equilibrio por automatización.

Ambos perfiles conservan `entrega-pois-2`. No se vuelve a aumentar la versión por estas correcciones; la edición de tres lugares permanece en `builds-infinito-1`. Tablas separadas por modo/versión, guardados anteriores preservados, sin borrar archivos reales. Editor/batch usa `local-records-qa.json`.

### Evidencia de esta revisión

Carpeta: `TestResults/POIsAmpliacion-20261113/` (ignorada por git). Editor **6000.3.23f1**, Mac, sin conexión Unity MCP. Ejecución real en copia aislada indicada por `project-path.txt`, sin Setup/Apply/Generate generales. Los 673 archivos de Assets/Packages/ProjectSettings de la copia coinciden con los del proyecto (`source-sha256.json`); sólo ocho scripts cambiaron respecto del estado recibido, más este documento. No se cambiaron escena, paquetes, perfiles, arte ni metadatos en esta revisión.

Comando base: `/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath <copia> -executeMethod <Check.RunBatch> -logFile <log>`. Variables `CONURBANO_PITCH_OUTPUT`, `CONURBANO_SCORE_OUTPUT`, `CONURBANO_RECORDS_OUTPUT` y `CONURBANO_EVOLUTIONS_OUTPUT` apuntan a subcarpetas de esa evidencia.

| Criterio | Resultado / evidencia actual |
| --- | --- |
| Baseline anterior a los nuevos arreglos | PASS, salida 0: `baseline-pitch.log`; conserva invariantes de tres y seis POIs. |
| Reproducción del fallo de contacto orbital | Fallo esperado, salida 1: `repro-pitch.log`, «Existing orbital contact cooldown did not follow cadence». No se ocultó ni se atribuyó a SearchDatabase. |
| Compilación runtime/Editor y POIs | PASS, salida 0: `accepted-pitch.log`, `PitchCheck.RunBatch`; sin errores nuevos de juego/compilación. |
| Ubicaciones | PASS: 100 semillas normales (600 ubicaciones), 100 con candidato escaso para cancha y 100 con candidatos cercanos; límites, transformación, rotación, pivote, hijos, márgenes, calles y fallo controlado. CSVs bajo `accepted-pitch/`. |
| Originales | PASS: Canchita resistida seis segundos reales con ambos personajes, curación acotada, no revivir, no resetear invulnerabilidad; Kiosco +25%/10 s; seis órdenes de visita de los tres, Q35 y Parrilla permanente. |
| Beneficios temporales | PASS: renovación fuerte/débil de los cuatro, vencimiento, coexistencia, porcentajes, clamps, progreso permanente durante buff, `NextAttackTime`, cooldown orbital, pausa y selección de los cuatro, una activación, disable/enable y SO inmutables. Se usan duraciones cortas o progreso inyectado sólo en fixtures donde se indica. |
| Fin/reinicio | PASS: derrota, victoria, abandono a Menu y nuevas escenas; beneficios, avisos y activaciones cerrados, visitas nuevas disponibles al reiniciar. |
| Score único | PASS, salida 0: `score.log`, `ScoreCheck.RunBatch`, 72 vidas/72 despawns, listeners al reactivar, seis POIs ×250, pausa/cierre y dos modos/personajes. Q50/presión1,85 también en PitchCheck. |
| Guardados y reglas | PASS, salida 0: `records.log`, `RecordsCheck.RunBatch`; tabla anterior conservada, tablas por modo/versión, top20, idempotencia, escritura atómica/backup, recuperación y flujo menú→partida→récord→reinicio/abandono. |
| Evoluciones/sinergias y proyectiles persistentes | PASS, salida 0: `evolution.log`, `EvolutionCheck.RunBatch`. Se extendió el ataque del Termo evolucionado con daño/cadencia temporales que vencen entre el primer y segundo pulso: disparos emitidos conservan snapshot, segundo pulso conserva daño capturado, Mate sigue contando sólo el primer pulso y la mejora permanente recibida se preserva. También pasan las dos evoluciones y tres familias de sinergias, cola, pausa, muerte y reciclado de proyectiles. |
| Estadísticas/progreso compartidos | PASS, salida 0: `build-state.log`, `BuildStateCheck.RunBatch`; 46 definiciones inmutables, siete armas N1–N8, fórmulas, orden de mejoras, armas futuras, límites, snapshots, orbitales, cola y reinicio con ambos personajes. |
| UI visual | PASS en Play Mode: capturas y aserciones 1024×768, 1280×720, 1920×1080 y 2560×1080. Inspeccionadas cuatro líneas, seis indicadores con aviso, vida, selección, y rótulo usado. Archivos `accepted-pitch/beneficios-4-*`, `indicadores-6-con-aviso-*`, `seleccion-con-beneficios-1280x720.png`, `poi-usado-1280x720.png`. |

Incidencias de verificación: `candidate-pitch.log` terminó por un fallo de resolución del Game View (3420×1910 en vez de 1024×768); no se contó como PASS. La pasada siguiente generó las cuatro resoluciones, y `accepted-pitch.log` pasó completo con las reservas de avisos finales. `final-pitch.log` expuso un error del fixture de victoria: se verificaba antes de LateUpdate; se corrigió esperando el resultado real, sin cambiar el flujo del juego. Se conserva la excepción conocida de `UnityEditor.Search.SearchDatabase` en los logs y se contabiliza aparte; no se afirma Console global sin excepciones.

No se generó ni lanzó un Player nuevo, ni se actualizó un ZIP en esta solicitud. No se probó Windows ni balance humano prolongado. Los artefactos anteriores no contienen estas correcciones. La validación declarada aquí es compilación y Play Mode real automatizado más inspección de imágenes, no una prueba humana de build final.

Resultado final de esta revisión: **implementación validada en Editor**, cinco checks completos con salida 0 y comprobación visual. Resumen legible por máquina: `verification-summary.json`. `git diff --check` sin errores. Nueva build y aceptación humana siguen sin realizarse.

## Revisión de textos — 13/11/2026

Alcance: la solicitud de textos de esta conversación. El documento de Downloads es referencia; sus otras etapas no amplían esta solicitud. Se preservan los cambios locales recibidos, IMGUI/GameUi, controles, claves, reglas y datos de juego. Sin migración de localización, generadores ni publicación. Rutas de la tabla relativas a `Conurbano Infinito/Assets/_Game/`; nombres de clases abreviados bajo `Scripts/Player/`.

### Antes / después implementado

Los porcentajes y tiempos de ejemplo corresponden a los datos actuales: RiskZoneData forma sus textos desde esos campos. Los valores dinámicos conservan su precisión.

| Ubicación y contexto | Antes recibido | Después implementado |
| --- | --- | --- |
| MenuScreen · controles | WASD / flechas · Ataque automático | Movete con WASD / flechas; El ataque es automático. Pausa, mejoras y navegación en otras líneas |
| MenuScreen · foco del selector | Foco | Elegí |
| CharacterData · afinidad de La Piba | +1 rebote: sólo armas Ricochet (Pelota). | +1 rebote con la Pelota de Potrero. |
| RiskZone · lugar usado | Usado en esta partida | Ya pasaste por acá |
| RiskZone · premio y coste | +{Q} de Quilombo / +{Q} Quilombo · +250 puntos · más presión | +{Q} Quilombo · +250 puntos; separado del desafío y del efecto |
| RiskZone · duración restante | {TimerLabel}: {segundos} s | Quedan {segundos} s |
| RiskZoneData · mensaje por defecto | ¡DESAFÍO COMPLETADO! | ¡Desafío completo! |
| RiskZoneData · Kiosco | Radio +25% · 10 s | +25% de radio de recolección por 10 s |
| RiskZoneData · Canchita | Hasta +2 de vida | Al completar: recuperás hasta 2 de vida |
| RiskZoneData · Mural | +20% de daño · 20 s | +20% de daño por 20 s |
| RiskZoneData · Mate | 15% menos espera · 20 s | 15% menos espera entre ataques por 20 s |
| RiskZoneData · Bicicletería | +15% de movimiento · 15 s | +15% de velocidad por 15 s |
| GameHud · buffs | Kiosco: radio / Mate: espera | Radio de XP / Ataques: espera; con porcentaje y segundos restantes |
| GameHud · sinergia lista | Festejo listo · Petardo +25% | Petardo: radio +25% listo |
| GameHud · aceleración de práctica | DEV x10 · F9 NORMAL | Práctica · Reloj ×10 |
| GameManager · explicación en pausa | Cuando quieras, seguimos. | Más Quilombo: más enemigos y más puntos por baja. |
| GameManager · desglose | Lugares: {puntos} · Tiempo: {puntos} | Lugares: {puntos} pt · Tiempo: {puntos} pt |
| GameManager · tiempo | {segundos} s activos · Quilombo {Q} | Jugaste {segundos} s · Quilombo {Q} |
| GameManager · aviso de guardado | Guardado: revisar aviso en Récords | Hay un aviso de guardado en Récords |
| GameManager / LocalRecords · práctica | Partida de práctica: sin récord | Partida de práctica: no cuenta para los récords |
| GameManager / LocalRecords · abandono | Abandono · sin récord / Abandono: sin récord | Dejaste la partida: no cuenta para los récords / Esta partida no cuenta para los récords |
| UpgradeSelection · sin opciones, vida llena | Continuar; Vida llena. Seguís sin recompensa. | Seguir jugando; Ya tenés toda la vida. No quedan mejoras disponibles. |
| UpgradeSelection · respaldo | Catálogo agotado | Seguí con tu equipo |
| RecordsView · tabla vacía | Todavía no hay marcas en este modo y versión. Práctica y abandono sólo figuran como último resultado. | Todavía no hay marcas con estas reglas. Las prácticas y las partidas que dejaste sólo aparecen en Último resultado. |
| RecordsView · personaje anterior | Clave interna cuando no coincide con un personaje actual | Personaje anterior |
| LocalRecords · cierre | Cierre: Victoria / Derrota / Abandono | Resultado: ¡El barrio aguantó! / Hasta acá llegamos / Dejaste la partida |
| LocalRecords · fecha interna | Inicio UTC: {marca ISO} | Se retira de la vista; permanece en el guardado |
| LocalRecords · formato no compatible | Formato de récords incompatible; archivo conservado. | No se pueden abrir estos récords. Conservamos el archivo. |
| LocalRecords · respaldo no compatible | Respaldo incompatible; archivos conservados. | No se puede abrir el respaldo. Conservamos los archivos. |
| LocalRecords · lectura fallida | Archivo dañado; se conserva antes del próximo guardado. / No se pudieron leer los récords; archivos conservados. | No se pudieron leer los récords. Podés seguir jugando. |
| LocalRecords · validación o escritura fallida | Resultado inválido; no se guardó. / No se pudo guardar el resultado en este equipo. | No se pudo guardar esta partida. Podés jugar otra vuelta. |
| Data/Aerosol - Cadencia.asset · description | 10% menos de intervalo entre ráfagas. | 10% menos espera entre ráfagas. |
| Data/Boleadora - Cadencia.asset · description | -10% de intervalo de esta arma. | 10% menos espera entre ataques de Boleadora. |
| Data/Canchita del Club.asset · completionMessage | ¡TERCER TIEMPO! | ¡Tercer tiempo! |
| Data/Chancleta Teledirigida - Cadencia.asset · description | -10% de intervalo de esta arma. | 10% menos espera entre chancletazos. |
| Data/Equipar Boleadora.asset · description | Equipá Boleadora. | Lanzá boleadoras que atraviesan enemigos y vuelven. |
| Data/Equipar Chancleta Teledirigida.asset · description | Equipá Chancleta Teledirigida. | Lanzá una chancleta que sigue a su blanco. |
| Data/Equipar Fernets Orbitales.asset · description | Equipá Fernets Orbitales. | Dos fernets giran a tu alrededor y dañan al tocar. |
| Data/Equipar Termo Balístico.asset · description | Equipá Termo Balístico. | Equipá el Termo: dispara hacia el enemigo más cercano. |
| Data/Kiosco 24hs.asset · completionMessage | KIOSCO 24HS | Algo para el camino |
| Data/Parrilla del Barrio.asset · completionMessage | ¡ASADO LISTO! | ¡Asado listo! |
| Data/Petardo - Cadencia.asset · description | -10% de intervalo entre petardos. | 10% menos espera entre petardos. |
| Data/Termo Balístico - Cadencia.asset · description | -10% de intervalo de esta arma. | 10% menos espera entre ataques del Termo. |
| Data/Yerba Premium.asset · description | -10% de intervalo entre disparos por nivel. | 10% menos espera entre ataques por nivel. |
| Resources/Evolutions/PelotaDeFinal.asset · description | Dos rebotes extra. Vuelve una vez al primer blanco vivo. | +2 rebotes. Vuelve una vez al primer enemigo si sigue vivo. |
| Resources/Evolutions/TermoDeAcero.asset · description | 2 pulsos a 0,12 s. Segundo al 50%. Mate sólo en el primero. | 2 pulsos a 0,12 s. Segundo al 50% de daño. Mate suma al primero. |
| Data/Equipar Aerosol.asset · description | Rocía un abanico de corto alcance que atraviesa grupos. | Rociá un abanico corto que atraviesa enemigos. |
| Data/Equipar Pelota de Potrero.asset · description | Hasta 2 rebotes entre enemigos cercanos. | 2 rebotes de base entre enemigos cercanos. |
| QuilomboThresholdController → GameHud · umbral 25 | Se está yendo todo al carajo | Se picó la plaza: llegan más enemigos |

Se revisaron y conservaron: «Jugar», «A la plaza», «Tomate un respiro», «Seguir en la plaza», «Elegí cómo seguirla», «Tu equipo de esta partida», «Hasta acá llegamos», «¡El barrio aguantó!», «Otra vuelta» y «Volver al menú». Parrilla ya decía «+20% de daño en esta partida»: se conserva. El progreso mantiene «Quedate en la zona: {progreso} de {duración} s». La victoria sigue condicionada al cierre Clásico; Infinito conserva reloj, tramos y derrota. No se promete regeneración en Canchita. Se preservan nombres propios y las mejoras que ya explicaban su efecto, compatibilidades, límites y porcentajes reales.

### Inspección y verificación de esta revisión

Evidencia nueva: `TestResults/Textos-20261113/`. Baseline en `baseline-status.txt`, `baseline-diff.patch` y `baseline-sha256.json`; textos recibidos en `before/`. Unity 6000.3.23f1, macOS; copia aislada indicada por `project-path.txt`. No se usan PASS históricos para validar estos textos.

**Inspeccionado en código:** fuentes Regular/Bold de Atkinson Hyperlegible; escala GameUi (referencia 1280×720 y mínimo de 16 píxeles físicos); fondos opacos de menú/resultados/cartas y oscuros de HUD; reserva del aviso de POI frente a indicadores. Sólo se amplían los espacios necesarios de descripciones, instrucciones y avisos para expresar el efecto. Los errores de lectura/escritura no atribuyen una causa: excepción y ruta se guardan en warnings del log. Se conservan respaldo, escritura atómica y formato de datos.

**Ejecutado en Unity y Play Mode real automatizado (Mac):**

| Comprobación | Estado y evidencia nueva |
| --- | --- |
| Compilación runtime/Editor y recorrido de UI | PASS, salida 0: `verified-ui.log`, `UiRedesignCheck.RunBatch` (41,9 s). Menú, selector, cartas, HUD con equipo lleno, pausa/reanudar, derrota, victoria Clásico, reinicio y vuelta al menú. |
| Acentos y glifos | PASS: ambos pesos comprueban áéíóúüñ y mayúsculas, ¿¡, punto medio, %, signos y guiones. No se cambió la fuente. |
| Desbordes de cartas y textos largos | PASS: 36 cartas (34 mejoras + 2 evoluciones), opciones agotadas con vida parcial/llena, en 1024×768, 1280×720, 1920×1080 y 2560×1080. `verified-ui/text-layout.txt` vacío y 90 capturas. Se conservan las comprobaciones de elección, niveles, pausa y reinicio. |
| Seis indicadores y cuatro buffs, con aviso | PASS, salida 0: `verified-pitch-resumed.log`, `PitchCheck.RunBatch` (46,7 s). Cuatro resoluciones, 24 enemigos visibles y proyectiles/orbitales/efectos de cuatro armas. Indicadores sin superposición entre sí ni con el aviso; vida, equipo y buffs reservados. Se registran las 68 capturas en `verified-pitch-resumed/`. |
| POIs y efectos reales | PASS en el mismo PitchCheck: ambos personajes, Canchita resistida 6 s reales, curación acotada y sin revivir, una recompensa, Parrilla permanente, Mural/Mate/Bici temporales, renovación, coexistencia, pausa/elección, expiración, progreso permanente y limpieza al cerrar/reiniciar. Además conserva colocación y semillas, aunque no se cambió el mapa. |
| Texto de previa y premio de seis POIs | PASS: capturas `poi-previa-*` y `poi-premio-*` en cuatro tamaños, con medida de ancho y alto del texto. Las previas/premios de esta matriz son estados de presentación inyectados; los efectos reales se prueban por separado en el mismo check. Kiosco es inmediato: su previa de duración cero no es un estado accesible jugando. Canchita y Parrilla completadas no muestran un temporizador temporal. |
| Récords, errores y datos privados | PASS, salida 0: `accepted-records.log`, `RecordsCheck.RunBatch` (44,8 s), 64 capturas y `text-layout.txt` vacío. Incluye lectura fallida, formato no compatible y escritura fallida en las cuatro resoluciones. Verifica además que el resumen no muestra runId, clave de personaje desconocido, motivo QA ni fecha UTC interna. Mantiene top20, desempate, exclusión de práctica/abandono, atomicidad, backup y preservación de archivos. |
| Recarga de récords | PASS, salida 0: `records-reload.log`, `RecordsCheck.ReloadBatch` (10,4 s), reutiliza los fixtures de la fase anterior en un proceso nuevo. |
| Clásico/Infinito | PASS, salida 0: `endless.log`, `EndlessCheck.RunBatch` (19,8 s). Clásico termina a 240 s; Infinito sigue jugando a 240 s y más adelante, sin victoria por plazo; modos, jefes, pausa/cola, selector y HUD con layouts en cuatro formatos. No se ejecutó soak prolongado. |
| Integridad del alcance | PASS: los 645 archivos de Assets coinciden con la copia probada (`source-sha256.json`, `copy-differences.json` vacío). Respecto del estado recibido, 31 archivos del proyecto cambian más este documento; los assets sólo cambian descripciones/mensajes. Sin cambios nuevos de escena, paquetes, metadatos, cifras, IDs o formato de guardado. `git diff --check` sin errores. |

**Verificado visualmente sobre capturas reales:** HUD con los seis indicadores, cuatro buffs y aviso en las cuatro resoluciones (`verified-pitch-resumed/indicadores-6-con-aviso-*`), con enemigos y efectos detrás. Se inspeccionaron menú en los cuatro tamaños, pausa estrecha/16:9/ultrawide tras separar título, explicación y botones, selector, cartas largas de ambas evoluciones, resultados de derrota/victoria, aviso de lectura y resumen de equipo completo con scroll. Se leen tildes, signos, cifras y segundos; las áreas revisadas conservan fondos legibles y no tapan vida/equipo ni se pisan entre sí. La inspección visual es de muestras representativas; el chequeo automático de límites recorre todas las cartas y tamaños, no equivale a inspeccionar a mano cada captura.

**Contraste calculado por código, separado de la inspección visual:** `contrast-palette.json`. Tinta/crema 13,74:1, petróleo/crema 8,29:1, dorado/tinta 9,61:1, crema/azul noche 9,17:1. Es cálculo sobre la paleta sRGB de fondos opacos, no medición de pantalla ni de cada píxel de paneles translúcidos. Sus fondos sobre enemigos/efectos se comprobaron en las capturas anteriores.

**Incidencias conservadas:** `baseline-ui.log` no pudo fijar 1280×720 (Game View quedó en 3420×1910), y `final-ui.log` falló después al fijar 1920×1080. No cuentan como PASS. `candidate-ui.log` encontró dos desbordes nuevos en 1024×768 (instrucciones y explicación de Quilombo): se ampliaron sus recuadros. La revisión visual posterior detectó poco espacio entre título y explicación de pausa: se separaron y se bajaron sus botones. `verified-ui.log` valida el ajuste final sin desbordes. `verified-pitch.log` quedó interrumpido entre fases y no se cuenta como aprobado; `verified-pitch-resumed.log` repite la prueba completa con salida 0. La excepción histórica con traza `UnityEditor.Search.SearchDatabase` se registra aparte (una por sesión); no se afirma Console global sin excepciones ni se excluyen otras excepciones. Los warnings de lectura/escritura de RecordsCheck son fallos provocados en fixtures, con causa técnica en el log y aviso neutro para el jugador.

Los checks de texto se adaptaron sin eliminar sus verificaciones de comportamiento: rótulo usado, ocultamiento de buffs al elegir, umbral 25/cola sin repeticiones, estadísticas, efectos y navegación. Se añadió cobertura de cartas de evolución, opción de respaldo y avisos de error; no se incorporaron dependencias ni otro sistema de UI.

Estado: **revisión implementada y validada en Editor**. Las capturas y pruebas son de Play Mode automatizado en macOS. No se construyó ni probó un Player nuevo, Windows, una partida humana prolongada o rendimiento sostenido. Esta revisión no actualiza las apps/ZIP históricos. El riesgo pendiente de entrega es comprobar estos textos en la próxima build final y en su equipo destino; no se declara aceptación humana por pasar checks.

## Candidata de entrega — validación independiente

Revisión del **13/11/2026, desde 16:27 ART**. Este apartado reemplaza como estado vigente a los cierres anteriores. El adjunto de Downloads se usó como contexto; no se ejecutaron sus prompts de nuevas funciones, generación general, online ni publicación.

**Funciones congeladas.** Se recibió HEAD `7a430289fa3dd51c903da9e438aa6e64afe8a530` más los cambios locales enumerados en `initial-status.txt`/`source.patch`. No se descartaron cambios ni se cambió código de producción, assets, escenas, paquetes o configuración del proyecto original durante esta validación. La fuente candidata consta de **673 archivos** de Assets/Packages/ProjectSettings, identificados en `source-sha256.json` y preservados completos en `candidate-source.tar.gz`; `source-integrity-final.json` verifica que siguen idénticos. Las apps/ZIP de las revisiones anteriores no validan esta fuente.

**Estado global al cierre solicitado por el usuario: LISTA CON LIMITACIONES para Mac.** El usuario pidió cerrar si el recorrido funcionaba; se cierra sin ampliar pruebas ni modificar el juego. Esto no aprueba los casos humanos ni las repeticiones completas pendientes, que conservan su estado NO EJECUTADO. Una aceptación sin esas limitaciones sigue pendiente. No se observó crash, imposibilidad de jugar/reiniciar, mapa que rompa la partida ni paquete que no inicie en el Mac probado. No se convierte la aprobación técnica automatizada en aprobación de balance humano.

### Entorno y reproducción

- Unity **6000.3.23f1 (09d2ecc7fb28)**, URP 17.3.0, Input System 1.20.0, Test Framework 1.6.0. macOS 26.5.2, Apple M3, 8 GiB, GPU M3/Metal; pantalla interna 2560×1664. Ejecución del Player a 1280×720, ventana. La edición de entrega documentada es **Mac universal, Mono Release, versión 1.0, reglas entrega-pois-2**. Runtime probado en Apple Silicon, no en Intel ni otro Mac.
- No había Editor abierto al iniciar y no se detectó Unity MCP. Se usó el binario instalado por CLI; no se añadió un puente ni se actualizó ningún paquete. No había Console activa que limpiar. No se borraron logs ni evidencia histórica.
- Copia nueva de Assets/Packages/ProjectSettings, sin Library/Temp inicial, en la ruta de `project-path.txt`. Cada check usa un **proceso nuevo con gráficos**. Argumentos exactos y exit codes en `regression-results.json`; no se añadió `-nographics` ni `-quit`. Las entradas cierran por sí mismas el Editor.
- WindowsStandaloneSupport existe en `/Applications/Unity/Hub/Editor/6000.3.23f1/PlaybackEngines/WindowsStandaloneSupport`; MacStandaloneSupport está dentro del bundle del Editor. El diagnóstico de DeliveryCheck mira sólo la ruta Windows dentro del bundle y puede imprimir False pese al módulo externo existente. **No hay equipo Windows probado**. No se usa una build Mac ni una Windows antigua como evidencia de Windows actual.
- Evidencia nueva: **`TestResults/Candidata-20261113-1635/`**, fuera del control de versiones por la regla existente. Conserva fuentes de las sondas, scripts de reproducción, argumentos, hashes, CSV, capturas y todos los intentos. Los helpers de QA sólo se copiaron a proyectos temporales, **después** de generar la app limpia; no están en ella ni en Assets del original (`clean-assembly-inspection.json`). La ruta temporal de `project-path.txt` terminó instrumentada para QA: para reconstruir una app limpia hay que crear otra copia desde la fuente congelada, no reutilizar esa copia instrumentada sin restaurarla.

### Checks ejecutados, en orden

| Entrada real | Estado | Exit | Duración de proceso |
| --- | --- | --- | --- |
| PitchCheck.RunBatch | PASS |0|105,28s, incluido import inicial |
| ScoreCheck.RunBatch | PASS |0|20,45s |
| BuildStateCheck.RunBatch | PASS |0|11,68s |
| UiRedesignCheck.RunBatch | PASS |0|34,67s |
| CharactersCheck.RunBatch | PASS |0|17,03s |
| CombatFeaturesCheck.RunBatch | PASS |0|12,23s |
| SynergyCheck.RunBatch | PASS |0|20,60s |
| EvolutionCheck.RunBatch | PASS |0|18,90s |
| EndlessCheck.RunBatch | PASS |0|16,54s |
| RecordsCheck.RunBatch | PASS |0|42,22s |
| RecordsCheck.ReloadBatch | PASS |0|7,08s |
| DeliveryCheck.RunBatch | PASS |0|10,63s |

Se leyeron las entradas antes de invocar: **QuilomboSliceCheck.Run y PoiIndicatorsCheck.Run** ya son llamadas por PitchCheck; PoiIndicators también está integrado en UiRedesign. No se inventaron RunBatch ni se lanzaron matrices separadas de esos helpers. No se repitieron Ball/ShortRun/Expansion/Petardo/VisualSlice5/selección como matrices independientes: los checks principales ya invocan la cobertura relevante; BuildState prueba selección/cola y Hito6, Characters incluye ShortRun para ambos personajes, CombatFeatures incluye sus regresiones históricas y Evolution/Synergy ejercitan las armas relacionadas.

Las dependencias reales justifican la regresión amplia: cambió estado compartido de estadísticas/proyectiles, UI y fin de partida, además de mapas/POIs/récords. Delivery añadió inspección de scripts en todos los prefabs y objetos de Main, doce integraciones controladas de personaje×modo×especialización y doce recorridos con movimiento medido, ofertas y presupuestos. **Las doce rutas del bot terminaron en derrota**; sus builds objetivo no se alcanzaron naturalmente. La matriz no acredita balance ni rendimiento. `delivery/matrix.json` identifica equipo realmente obtenido, semillas, distancia y resultado por fila.

Records.Reload usa el mismo directorio `records/` y los datos producidos por RunBatch, en otro proceso. No se recrearon fixtures para simular persistencia.

### Matriz de aceptación vigente

PASS significa ejecución nueva dentro del alcance indicado; las observaciones de código o GUIDs por sí solas no aprueban runtime.

| Caso solicitado | Estado | Evidencia nueva, alcance y límite |
| --- | --- | --- |
|1. Compilación Unity runtime/Editor | **PASS** | Doce sesiones reales, sin errores C# ni excepciones nuevas de gameplay. Logs completos por entrada; import inicial incluido. |
|1. Referencias, prefabs y escenas | **PASS** | `serialized-inspection.json`:71 assets serializados, sin GUIDs ausentes, scripts vacíos ni duplicados de Assets. Se complementa con carga real/Play Mode, inspección de scripts de Delivery y build. Escenas habilitadas: Menu primero y Main después; SampleScene deshabilitada. La app extraída arranca efectivamente en Menu. No se afirma un barrido semántico de cada campo opcional. |
|2. Recorrido crítico en Editor | **PASS** | UiRedesign, Characters, Score, Records y Delivery: menú/selector, juego, movimiento, ataques, XP/elección, pausa/reanudar, derrota, reinicio y menú. Son fixtures automatizados, algunos controlan XP/tiempo. |
|2/8. Recorrido en app limpia fuera del Editor | **PASS** | `native-ui-observations.md`, player-native.log/player-reload.log y resultados de procesos: menú→selector Pibe/Clásico→movimiento/ataque→XP→mejora natural de Termo con mouse→pausa/reanudar→derrota→reinicios→menú, cierre y relanzamiento. Sin inyecciones de salud, XP o reloj en esta app. |
|2. Tres partidas consecutivas / estado residual | **PASS** | Tres derrotas naturales:24,7s/74pt;16,4s/56pt;39,8s/169pt. Dos reinicios restituyeron5vida,XP0,Q0,puntos0,reloj0,TermoN1. En la tercera se alcanzó nivel2/TermoN2. |
|2. Repetir también la rama de mejora en las tres partidas del paquete | **NO EJECUTADO** | Las primeras dos terminaron con XP3/5, sin elección; sólo la tercera cubre toda esa rama. No se presenta como tres recorridos completos de mejora. Requiere dos repeticiones adicionales. |
|3. Reglas de Clásico y Jefa 180/victoria 240 con jefe vivo | **PASS** | EndlessCheck/ShortRun y límites de Delivery, reloj controlado. Se ejercitan jefes y cierre por plazo sin depender de matar al jefe. No equivale a sobrevivir cuatro minutos a velocidad normal. |
|3. Clásico humano completo a velocidad normal | **NO EJECUTADO** | Requiere una persona jugando la candidata y llegar a 180/240s. Las partidas CUA y los bots no se rotulan humanas. |
|3. Infinito no gana a 240; jefes posteriores y escalado | **PASS** | EndlessCheck: límites239,9/240/240,1, tramos0/1/2/14, jefes180/420/660/900, jefe único sin deuda, escalado sólo a nuevas vidas, caps y pool. |
|3. Infinito: soak técnico de 15 min reales | **PASS** |900,01s medidos a timeScale=1/captureDeltaTime=0,150–151 enemigos; salida0. Sonda con salud/equipo QA y reloj inicial1800s; no es supervivencia humana ni una partida natural desde cero. |
|3. Infinito humano 15 min desde cero | **NO EJECUTADO** | Se conserva como aceptación humana pendiente; el soak anterior no lo sustituye. |
|4. Seis POIs y temporizadores | **PASS** | PitchCheck: Kiosco, Parrilla, Canchita, Mural, Mate y Bici; configuración, distribución, contactos, desafío/recompensa única, feedback, duración, expiración, coexistencia, pausa/elección y limpieza en victoria/derrota/abandono/reinicio. Canchita:6s físicos reales por personaje; otros desafíos usan además temporizadores de fixture. |
|4. Buff + mejora permanente; expiración y no reactivación | **PASS** | `TEMPORARY_BENEFITS_PASS` y `BUFF_EDGES_PASS`: mejora permanente durante buff preservada al vencer, renovación sin acumular potencia, cuatro efectos coexistentes, orbital/contacto/cadencia/disparo pendiente, disable-enable y reentrada de zona usada sin segunda recompensa. Score único6×250 / Q50. |
|5. Cancha y demás POIs dentro del césped, sin calles | **PASS** | `placement-seeds.csv`:600 colocaciones, semillas0–99, seis activos por semilla; ocho candidatos, footprint completo y margen. `placement-distribution.json`: todos los tipos aparecieron en los ocho candidatos; entre 7 y 20 apariciones por par tipo/candidato. Límites/escasez/capacidad también ejecutados; sin chunks. |
|5. Casos visuales representativos de colocación | **PASS** | Capturas nuevas de Canchita resistiendo/recuperación/con Kiosco, ambas visitas de personaje y estado de zona usada; inspección de cancha completa sobre césped. Geometría y sorteo se prueban por CSV/Unity, no se afirma inspección manual de 600 imágenes. |
|6. Estados y cuatro resoluciones soportadas | **PASS** | UiRedesign/Pitch/Records/Endless y layout:1024×768,1280×720,1920×1080,2560×1080. Informes text-layout.txt vacíos; menú, selector, cartas/evoluciones/respaldo, HUD, pausa, derrota/victoria y récords/avisos. Revisión visual de muestras nuevas, seis indicadores/cuatro buffs/aviso, texto y controles legibles. Player limpio1280×720 inspeccionado por CUA; no mouse físico Windows ni todas las capturas revisadas a mano. |
|7. Alta carga y comparación con base | **PASS** | Base 120,02s y candidata 900,01s reales,150–151 enemigos en ambas,1280×720/M3/Metal/Release. Resultados y límites abajo; PASS de ejecución/medición, no del objetivo de FPS mínimos. |
|7. 60 FPS mínimos constantes en este ensayo | **FAIL** | Promedio 59,91FPS, p99 de17,70ms y pico de1018,55ms. Hubo frames lentos; no se certifican60FPS constantes ni se atribuye la causa sin investigar. |
|7. Objetivo GDD60FPS con 150 enemigos en todos los equipos de destino | **NO EJECUTADO** | La medida local no certifica otros Mac ni Windows. No hay medición GPU por frame ni garantía de 60FPS mínimos en todo momento. |
|8. Build Mac y paquete que inicia | **PASS** | DeliveryCheck.BuildMac salida0; Succeeded/0errores/1warning,127.235.788bytes,59,72s internos/66,46s de proceso. Bundle universal x86_64+arm64 y firma ad hoc verificados. ZIP extraído:186 archivos de app idénticos por SHA-256; inicio y recorrido reales descritos arriba. |
|8. Log del Player | **PASS** | Ambos procesos limpios salieron0; sin excepciones de gameplay/asserts observados. ComputeBuffer de cierre preexistente sigue abierto y se clasifica abajo. |
|9. Score y resultado congelado | **PASS** | ScoreCheck: ciclos de muertes/despawns, listeners, seis POIs×250 una vez, pausa/cierre y no recontar después. Resultado nativo169 permanece igual en resultado, tabla y detalle recargado. |
|9. Guardado/recarga, datos anteriores y tablas por reglas | **PASS** | Records/Reload: top20, modos/versiones incluida builds-infinito-1 frente a entrega-pois-2, IDs/desempate/idempotencia, schema admitido, atomicidad/backup/recuperación y formatos desconocidos preservados. Player nuevo releyó169/74/56 con los equipos correctos en proceso nuevo. No se declara compatible un esquema desconocido. |
|9. Práctica/abandono excluidos y QA aislada | **PASS** | Score/Records/Delivery verifican exclusión; Editor usa local-records-qa.json, fixtures con rutas de evidencia. Player limpio usa home aislado comprobado. Las sondas marcan práctica antes de operar y no crean fixtures elegibles. Tres cierres naturales por UI se guardaron sólo en el home aislado; sin insertar puntajes ficticios en récords reales. |
|9. Windows actual / Intel / otro equipo | **NO EJECUTADO** | El destino documentado es Mac; se probó Apple Silicon. Módulo Windows detectado, falta generar y ejecutar una candidata Windows en equipo Windows si pasa a ser destino. Ningún histórico certifica esta versión. |

### Console e incidencias conservadas

`log-classification.json` y los archivos `*-search-trace.txt` separan la coincidencia **exacta** histórica:

```text
ArgumentOutOfRangeException: Index was out of range. Must be non-negative and less than the size of the collection.
Parameter name: index
System.Collections.Generic.List`1[T].get_Item
UnityEditor.Search.SearchDatabase+<EnumerateAll>d__81.MoveNext — SearchDatabase.cs:449
UnityEditor.Search.SearchDatabase.GetDefaultSearchDatabase — SearchDatabase.cs:424
UnityEditor.Search.SearchInit.IndexationOnStartup — SearchInit.cs:109
UnityEditor.EditorApplication.Internal_CallDelayFunctions — EditorApplication.cs:407
```

La traza íntegra conserva también los frames System.Linq y rutas del motor. Una coincidencia por cada check con Play Mode; Reload cierra antes de la indexación. No se filtraron todas las excepciones del Editor: no se encontraron otras excepciones no esperadas ni errores C# en las sesiones aprobadas. Pitch provoca un rechazo de colocación para comprobar recuperación; Records provoca errores de lectura/escritura/formato sólo en sus fixtures y conserva sus warnings.

Otros diagnósticos:25 avisos de dependencias de shaders Terrain/SpeedTree del paquete durante import inicial, sin material rosa observado ni error de build; origen en import de paquetes, no se atribuyen a cambios nuevos sin comparación equivalente. «Access token is unavailable» del cliente de licencias no impidió resolver la licencia. El warning de build es subida de símbolos a Unity Cloud sin credenciales; no se añadieron secretos. No se afirma Console global limpia.

Player: «GarbageCollector disposing of ComputeBuffer. Please use ComputeBuffer.Release() or .Dispose() to manually release the buffer.» al cerrar, reproducido en ambos procesos con exit0 y documentado en la demo de octubre. Sigue sin resolver; no se convierte en una excepción de gameplay ni se oculta. SearchDatabase es del Editor, no del Player.

### Rendimiento y tiempo real

**Medición final completada**, con entrada deshabilitada sólo dentro de la sonda autónoma. Se probaron controles en la app limpia por separado. `performance150-input-results.json`, `performance-summary.json` y CSV por segundo conservan carga, frames, reloj, pool, memoria y exit codes. Ambos procesos terminaron0; no hubo excepciones ni cambios de resolución después de fijar 1280×720. La candidata permaneció900,01s reales en medición y siguió en Infinito. Su reloj activo pasó de1804,99 a2703,04s: **898,05s activos**, diferencia conservada en el informe; no se presentan900s activos ni supervivencia humana.

| Medida | Base HEAD | Candidata |
| --- | --- | --- |
| Duración medida |120,02s|900,01s|
| Enemigos activos |150–151|150–151|
| FPS promedio |59,975|59,915|
| Frame p95 |17,613ms|17,612ms|
| Frame p99 |17,781ms|17,703ms|
| Peor frame |34,480ms|**1018,555ms**|
| Frames medidos |7198|53924|
| Pool creado inicio/final |253/609|267/622|

Ventanas comparables de ~119s desde el CSV: base 59,9758FPS, candidata 59,9756FPS. Diferencia descriptiva −0,0003FPS, sin evidencia suficiente para afirmar mejora/regresión causal. Es una sola muestra por fuente; semilla de combate y temperatura no controladas, escritorio compartido. Los percentiles completos tienen ventanas distintas (120/900s). El pico de1,02s de la candidata queda registrado y sin causa atribuida: **no se cumple ni se declara 60FPS mínimos constantes**. No hubo crash o excepción durante ese pico. No se añadió un arreglo especulativo al cierre.

Sonda reutilizada de etapa8, adaptada sólo en copias QA. Release/Mono,1280×720,Metal,vSync1. Jugador quieto con salud QA y cuatro armas mejoradas; elección deshabilitada,Q100,reloj inicial controlado1800s, luego timeScale=1/captureDeltaTime=0. Se reponen enemigos regulares por WaveDirector para mantener150; este coste extra forma parte de ambas mediciones. No representa balance humano ni una partida desde cero. No crea fixtures elegibles. La API GC Allocated In Frame no es válida en este Release; los ceros de esos campos no significan 0GC. Memoria administrada final de candidata: 5.78MiB, después de colección explícita fuera de la ventana: 5.54MiB. No hay medición separada de CPU/GPU por frame.

Primer intento: base120,02s/59,98FPS pero sólo22–80 enemigos activos después del pico151. No cubre carga 150. La candidata inicial se detuvo voluntariamente conSIGTERM a 46,2s, exit −15, para reemplazar el fixture insuficiente; se conserva CSV/log y `performance-replaced.txt`. No es crash observado del juego ni un PASS de soak. Se añadió reposición mediante WaveDirector hasta 150 activos cada frame, con aserción de carga; se reconstruyeron ambas variantes QA y se repitió la medición, con los intentos conservados. La app limpia y sus verificaciones no cambian.

Intento posterior de alta carga (`performance150-base`, base HEAD): **FAIL**, exit 1 a 110,36 s de proceso, después de ~99 s medidos. La sonda lanzó `Exception: Realtime scenario interrupted.` y el log registró un cambio de superficie a 3420×2146; no dejó registrado cuál de estado/timeScale/captureDelta cambió, por lo que su causa queda indeterminada. Al abortar también apareció `Internal: There are remaining Allocations on the JobTempAlloc. This is a leak, and will impact performance`. Son diagnósticos adicionales, **no** SearchDatabase ni una medición aprobada. Se conserva todo. Para repetir se deshabilitó la entrada sólo dentro del Player autónomo QA y se amplió el diagnóstico de la aserción; se reconstruyeron ambas variantes. Las pruebas de controles de la app limpia permanecen separadas. En la repetición completa con entrada aislada no reaparecieron la aserción ni JobTempAlloc; ComputeBuffer sí volvió a aparecer al cerrar. La causa del primer cambio de estado no quedó demostrada.

### Artefacto y pasos manuales pendientes

Candidata de revisión: `Builds/Mac/ConurbanoInfinito-Mac-Candidata-20261113-1635.zip`, `.sha256` al lado. Contiene app limpia, instrucciones y licencias; no incluye QA, guardados, Library/Temp ni directoriosDoNotShip. `packaging-results.json` identifica ZIP, hash y extracción verificada. Firma ad hoc, sin notarización. No se publicó ni se subió a ningún servicio.

A pedido del usuario no se lanzan más pruebas. Se preparó una copia aislada del guardado real del 6/11 (`compatibility-home`), pero **no se abrió en Player**: no se cuenta como PASS adicional. La compatibilidad aprobada arriba es la de RecordsCheck/Reload y las versiones de fixtures descritas.

Pasos concretos para una aceptación posterior sin limitaciones:

1. Extraer ese ZIP en el Mac de entrega y abrir la app. Jugar **una persona** Pibe oPiba/Clásico a velocidad normal, sin herramientas QA: mover/atacar, recogerXP, elegir mejora, pausar/reanudar; registrar Jefa al llegar180s y victoria a 240s aunque siga viva. Guardar Player.log, personaje, resolución y observaciones de dificultad. Si muere antes, repetir; no alterar salud para aprobar balance.
2. Completar dos recorridos adicionales del paquete que alcancen una mejora, derrota y reinicio; verificar vida/XP/equipo/POIs/buffs/reloj reiniciados. La revisión actual ya tiene tres partidas consecutivas, pero sólo una con mejora.
3. Jugar Infinito desde0 al menos15 min reales, confirmar que240s no gana, observar jefes posteriores y escalado; registrar pausas, duración activa y real, dificultad y log. El soak QA no sustituye esta aceptación humana.
4. Como contraste manual de integración, completar los seis lugares en la app: salir/volver a uno usado, combinar buff con mejora permanente y comprobar la estadística al vencer; pausar el temporizador y reiniciar. La cobertura automatizada deEditor ya pasó, esta comprobación manual en paquete queda pendiente.
5. Si se exige Windows, usar Unity 6000.3.23f1 y el módulo detectado: copia aislada limpia, Build Profiles/Windows/x86_64/Mono/Release, Menu y Main habilitadas con Menu primero; generar a destino nuevo y ejecutar enWindows. Repetir el recorrido, persistencia y carga; conservar log `%USERPROFILE%\AppData\LocalLow\<empresa>\<producto>\Player.log` según la identidad efectiva de esa build. No usar la app Mac ni el ejecutableWindows antiguo.

Cualquier cambio posterior en fuentes/datos/configuración invalida los checks afectados y exige reconstruir la candidata. En esta revisión no hubo una corrección de producción que requiriera repetir matrices ya aprobadas. Se omitieron funciones nuevas y retoques cosméticos para mantener congelado el alcance.

