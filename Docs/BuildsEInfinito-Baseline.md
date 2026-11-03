# Builds e infinito — etapa 0: auditoría y baseline

Fecha: **2026-11-03**. Estado: **auditoría completada, con límites de validación**. No se implementó gameplay de las etapas 1–9.

## Referencia, instrucciones y entorno

- Repositorio/remoto verificado: `FranciscoLang2209/conurbano-infinito`, `git@github.com:FranciscoLang2209/conurbano-infinito.git`.
- Rama: `codex/demo-cuatro-minutos`. Commit: `7014ba49f0a8058fe394bb5d2c6a5efb837e34a0` (`Save gameplay, Club characters, UI and art updates`). `git status --short` inicial vacío; no había cambios ajenos versionables que preservar. Existen resultados y builds locales ignorados de intervenciones anteriores.
- Raíz Unity comprobada por `Assets/`, `Packages/manifest.json` y `ProjectSettings/ProjectVersion.txt`: **`Conurbano Infinito/`**. El `Assets/` exterior es un esqueleto y no se utilizó.
- No se encontró `AGENTS.md` ni `CLAUDE.md` dentro del repositorio ni en sus directorios ancestros. Se aplicaron las instrucciones de esta sesión, `Docs/UnityProjectContext.md`, `Docs/PartidaCorta.md`, las guías de UI/arte y los checks reales. No se inventó una guía local ausente.
- `Docs/BuildsEInfinito.md` no existía. Se incorporó el adjunto `conurbano-infinito-builds-infinito-prompts.md` como **contrato de diseño**; sus prompts futuros no son evidencia de implementación ni autorización para ejecutar otras etapas. Su referencia original a `606639c4...` no es el commit auditado aquí.
- Unity instalado: `/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity`, revisión `09d2ecc7fb28`. No había proceso Editor abierto al comenzar. No se instalaron herramientas.
- Ejecución: macOS **26.5.2 (25F84)**, arm64, modelo **Mac15,12 / Apple M3**, **8 GiB RAM**, Metal. Es evidencia del Editor, no de una build del jugador.
- URP **17.3.0**, Input System **1.20.0**, `activeInputHandler=1`, Unity Test Framework **1.6.0**. Manifest y lock conservados. No hay asmdefs/asmrefs propios: `Assembly-CSharp` y `Assembly-CSharp-Editor`.
- No se detectaron puente/configuración Unity MCP en el proyecto ni herramientas Unity MCP disponibles en esta sesión. Editor local/batchmode sí disponible; consola y Play Mode comprobados mediante sus logs. No se probaron capacidades remotas de Editor/profiler.

## Diferencias entre diseño, documentación y código

| Tema | Estado real en este commit | Interpretación para las próximas etapas |
| --- | --- | --- |
| Duración | `GameManager.DefaultDurationSeconds=240`; Main serializa 240. Victoria por reloj, aunque La Jefa siga viva. | Clásico de cuatro minutos confirmado; conservarlo. |
| Oleadas | Seis fases a 0/30/60/120/180/210 s; Jefa una vez a 180 s. | No hay director infinito ni jefes periódicos. |
| GDD 0.1 | El plan adjunto refiere diez minutos y evoluciones futuras. No se encontró el GDD original en este checkout ni se proporcionó su contenido íntegro. | Esa descripción del GDD es indirecta; no se presenta como cotejo de un original leído. `PartidaCorta.md` y el código coinciden en 240 s. Adenda del GDD corresponde a etapa 8. |
| Personajes | Pibe 5 vida / 5 velocidad / Termo / primera oferta Aerosol; Piba 4 / 5,5 / Boleadora / primera oferta Termo. | No hay afinidades. El contexto histórico que dice siempre Aerosol describe al Pibe; el perfil actual manda. |
| Armas y niveles | Siete armas, cuatro espacios. Nivel visual = 1 + suma de rangos de cartas dirigidas, sin máximo global. | Nivel global 8 es una regla nueva con efecto de balance, no comportamiento actual. |
| Pasivas | Cuatro cartas de cinco rangos; `selectedPassives` alimenta el HUD, sin inventario limitado. | Antena, Bolsa y cuatro espacios de pasivas no existen todavía. |
| Elecciones | Hasta tres cartas distintas, máximos por carta y cupo de armas; cola de niveles y pausa. Catálogo agotado descarta la cola y continúa. | Faltan revalidación al aplicar, exclusión de doble input y fallback explícito de curación/continuar. |
| Sinergias/evoluciones | No hay recetas, familias, contadores ni logro runtime de jefe derrotado. | Diseño de etapas 3/4, no implementación. |
| Quilombo | Tres POIs, umbrales 10/25, sin élites; con todos: Q=35 y presión ×1,7. | Puntaje futuro usa otra fórmula (×1,35 con Q=35); no reutilizar el bonus de presión para puntuar. |
| Modos/resultados | Sin selector de modo, puntaje, resultado inmutable o récords. | Etapas 5–7 pendientes. PlayerPrefs actual guarda volumen, no resultados. |
| Arte/UI | IMGUI/GameUi, Atkinson Hyperlegible, mínimo físico de 16 px; plaza con objetos irregulares. | No migrar UI, regenerar plaza ni sustituir arte para incorporar builds. |

Los intervalos base actuales son **2,4 / 1,55 / 1,05 / 0,75 / 1,15 / 0,57 s**, topes **18 / 26 / 36 / 46 / 40 / 55**. Con Q=35 y bonus=0,35, el último límite resulta `ceil(55 × 1,7)=94` comunes más Jefa. Vidas actuales: Mosquito 3, Paloma 15, Bolsa 6, Perro 10, Gomero 12, Jefa 100. Las evaluaciones humanas históricas de Aerosol no constituyen balance verificado por esta auditoría.

## Dueños del estado y puntos de extensión

En esta sección las rutas parten de `Conurbano Infinito/Assets/_Game/`.

| Archivos | Definición / estado y flujo actual | Extensión acotada prevista |
| --- | --- | --- |
| `Scripts/Player/CharacterData.cs`, `Resources/Characters/*.asset` | SO define perfil; selección/catálogo son estáticos y se limpian en `SubsystemRegistration`. `Selected` persiste entre recargas de escena. | Agregar identidad estable al perfil; conservar nombres de campos, vida, animador y primeras ofertas. Afinidad sólo en etapa 3. |
| `Scripts/Player/PlayerController.cs` | `Awake` aplica perfil, vida y Animator, busca `Player/Move`; `OnEnable/OnDisable` habilitan/deshabilitan input. Velocidad mejorada vive en el componente. | Integrar cálculo de velocidad sin escribir el SO; conservar WASD/flechas, normalización y física. |
| `Scripts/Player/WeaponController.cs`, `Scripts/Combat/WeaponData.cs` | SO inmutable de arma; `equipped` y `EquippedWeapon` contienen bonus, próximo ataque y orbitales. `Awake` toma arma del personaje y equipa. API pública de mejoras y getters legacy. | Estado runtime de build/nivel y cálculo común; mantener APIs utilizadas por checks. No agregar afinidades, pasivas nuevas o recetas en etapa 1. |
| `Scripts/Player/UpgradeData.cs`, `UpgradeSelection.cs` | SO define efecto/cantidad/target; `CanOffer` filtra, `Apply` despacha. Selection posee `levels`, `selectedPassives`, `options`, `pendingSelections`; escucha/desuscribe `ExperienceSystem.LevelUp`. | Fuente única de nivel de arma; después elegibilidad, cupos/fallback. Revalidar y consumir una vez cada elección, incluida cola. |
| `Scripts/Player/ExperienceSystem.cs`, `ExperienceGem.cs`, `Scripts/Enemy/ExperienceDrop.cs` | XP y nivel pertenecen al jugador: requisito 5 + 3 por nivel anterior. `AddExperience` usa long intermedio y puede emitir varias subidas síncronas. Drop se suscribe a muerte; gema se recoge una vez y vuelve al pool. | Conservar cola/pausa; presupuesto y consolidación de XP en etapa 5, sin perder valor. No convertir XP en puntaje. |
| `Scripts/Player/GameManager.cs` | Estado Playing/Paused/Defeat/Victory y reloj de sesión; `StopGame` deshabilita director/spawn/armas y congela tiempo. Recarga Main para reiniciar; Menu para salir. | Política de modo en etapa 5, cierre idempotente/práctica en 6. No duplicar Main. |
| `Scripts/Enemy/WaveData.cs`, `WaveDirector.cs` | SO de fases/jefe; director posee fase, próximo spawn, lista de comunes, bossSpawned/ActiveBoss y bonus de presión. `Tick` se llama desde GameManager. | Director compartido con política de modo; curvas desde base inmutable y calendario futuro. No acumular otra aplicación de Quilombo. |
| `Scripts/Enemy/SpawnManager.cs`, `Scripts/Combat/ObjectPool.cs`, `PooledObject.cs` | Pool instancia si falta y resetea Health/cuerpo antes de reactivar reutilizados. Spawn asigna blanco después de `Spawn`. El pool retiene el pico de instancias; no tiene cupo global. | Configurar vida/velocidad escaladas antes de combatir, contar por aparición y separar muerte de despawn. Revisar primer Instantiate/OnEnable además de la reutilización. |
| `Scripts/Combat/Health.cs` | Vida float con tolerancia 0,0001, invulnerabilidad y bonus runtime; `TakeDamage` emite Damaged/Died una sola vez por vida. HitFlash puede retrasar el retorno. `ResetForReuse` limpia bonus y daño. | Punto de muerte válida para puntaje, junto con identidad/categoría de aparición; evitar mutar bases serializadas al escalar. |
| `Scripts/Combat/Projectile.cs` | Snapshot de daño/velocidad/rango al inicializar; modos, blancos golpeados, cooldown orbital y visual runtime se limpian en `OnDisable`. `Hit` estático carece de payload, sirve al audio. | Eventos mínimos de ataque/impacto con arma/familia antes de sinergias; no inferir arma a partir del sprite. No introducirlos por anticipado en etapa 1. |
| `Scripts/Player/QuilomboSystem.cs`, `QuilomboThresholdController.cs` | Current runtime clamp 0–100. Listener aplica umbrales una vez, 0,15 y 0,35 que reemplazan el bonus previo. | Preservar presión y reinicio; puntaje consulta Q al morir, sin sumar el bonus del director. |
| `Scripts/Player/QuilomboManager.cs`, `RiskZone.cs`, `RiskZoneData.cs` | Manager coloca tres zonas en ocho puntos en `Start`. SO contiene recompensa; zona posee progreso/estado/uso y tiempos. Parrilla +20% daño permanente; Kiosco radio temporal; Canchita cura hasta 2, una vez. | Evento explícito de POI completado en etapa 6, sin repetir recompensa ni resetear por tramo. |
| `Scripts/Player/GameHud.cs`, `MenuScreen.cs`, `GameUi.cs` | HUD consulta nivel a Selection, reloj a GameManager, jefe/fases a director. Menú elige personaje. GameUi comparte paleta, fuente, foco, escalado y controles. | Sustituir fuente del nivel, luego añadir estados/mode/resultados sin romper teclado/mouse. Preservar tipografía y área segura. |
| `Scripts/Player/AudioDirector.cs`, `PerformanceOverlay.cs` | Audio mantiene suscripciones equilibradas; `Projectile.Hit` no identifica ataques. F3 muestra FPS muestreados y enemigos actuales, sin picos ni GC/memoria. | Conservar eventos legacy; instrumentación específica de estabilidad sólo cuando llegue etapa 5/8. |

### Inicialización y reinicio que hay que preservar

1. Menu abre selector; `CharacterData.Select` confirma y carga Main. Abrir Main directamente usa el primer perfil del catálogo.
2. `Awake` de Player aplica perfil/vida; WeaponController toma el mismo perfil, busca pool y equipa; Selection toma primera oferta y valida referencias. GameManager restaura `timeScale=1`. No hay orden explícito de ejecución propio: no depender de un orden entre `Awake` de distintos componentes al integrar estado nuevo.
3. `OnEnable` suscribe XP/muerte/Quilombo/audio; `Start` coloca POIs. GameManager avanza tiempo sólo Playing y `timeScale!=0`; director configura fase y agenda spawn. Armas atacan usando `Time.time`; física/POIs/efectos usan tiempo escalado.
4. Una ráfaga de XP encola selecciones y congela tiempo sin cambiar GameState. Esc lleva a Paused; continuar mantiene el tiempo congelado si sigue pendiente una elección.
5. Muerte/victoria detienen combate. Reiniciar recarga escena: XP, mejoras, armas, POIs, director y pool se reconstruyen; personaje elegido se conserva. Los checks lo ejercitaron para ambos perfiles.
6. `StateChanged` sólo se emite en derrota/victoria; pausa no lo emite. No suponer que ese evento representa toda transición. La prioridad muerte/victoria del mismo frame no está definida independientemente del orden de Update/contactos; resolver en etapa 6.

## Referencias serializadas que no se deben recrear

- `Scenes/Main.unity`: GameManager **1400000002**, SpawnManager **1400000003**, GameHud **1400000004**, UpgradeSelection **1400000005**, WaveDirector **1400000006**, QuilomboSystem **1400000010**, ThresholdController **1400000011**, QuilomboManager **1400000012**. Director apunta a `Data/Oleadas.asset`, spawner y Quilombo; `elitePrefab` es nulo.
- Main referencia al Player instanciado: Health **1634118243**, WeaponController **1634118244**, ExperienceSystem **1634118245**, PlayerController **1634118246**; proceden de `Prefabs/Player.prefab`, GUID **6f646f35764349d5b2dcc69d1f51dc8e**. No convertir los objetos stripped en componentes nuevos.
- Player mantiene `weapon` → `Data/Termo Balístico.asset`, GUID **06025649deea45fd962e2c2c5a755b6a**, y `projectilePrefab` → `Prefabs/TermoProjectile.prefab`, GUID **1231ec4eba43481582de54ee105c59e1**, fileID **9876543211**. Perfil runtime reemplaza el arma inicial sin borrar esa referencia legacy.
- Main serializa **32 cartas únicas**, todas las UpgradeData actuales: cuatro generales, siete adquisiciones y veintiuna dirigidas. `firstWeaponUpgrade` serializado apunta a Equipar Aerosol; `Awake` lo reemplaza por el perfil. Las referencias dirigidas usan el SO, no su nombre visible.
- `Resources/Characters/{ElPibeDelBarrio,LaPibaDelClub}.asset` enlazan arma/primera carta/retrato/controlador; preservar también GUIDs de animaciones/arte. Main/Menu son las dos escenas habilitadas en `ProjectSettings/EditorBuildSettings.asset`; SampleScene está deshabilitada.
- `RiskZone` de Main enlaza `Data/{Parrilla del Barrio,Kiosco 24hs,Canchita del Club}.asset`, QuilomboSystem y cámara. Mantener registro de tres elementos y ocho puntos de colocación.
- `WeaponPattern`: **Straight=0, Boleadora=1, Returning=2, Orbiting=3, Explosive=4, Ricochet=5**. `UpgradeEffect`: Damage=0, FireInterval=1, MovementSpeed=2, MaximumHealth=3, EquipWeapon=4, WeaponDamage=5, WeaponFireInterval=6, WeaponProjectileCount=7, WeaponProjectileSpeed=8, WeaponExplosionRadius=9, WeaponRicochets=10. `RiskZoneReward`: Damage=0, CollectionRadius=1, Healing=2. Añadir al final, nunca reordenar.

Inventario detallado de GUIDs/campos/fileIDs inspeccionados: `TestResults/BuildsEInfinito/Etapa0-20261103/catalog-and-wiring.json`. No hay GUIDs duplicados entre los meta de Assets. Este inventario y los checks de integración no equivalen a validar todo fileID externo de cada asset de terceros.

### Herramientas de regeneración: leer, no ejecutar para integrar

En `Assets/_Game/Editor/`: `VisualSlice2Setup.Apply`, `VisualSlice3Setup.Apply`, `VisualSlice4Setup.Apply`, `Hito11Setup.Prepare`, `CombatFeaturesSetup.Apply`, `ClubFeaturesSetup.Apply/ApplyBall/ApplyPitch` guardan escenas, prefabs, datos o importación. Ninguna se ejecutó. `Hito11Setup.Check` sí es una comprobación y se ejecuta dentro de checks existentes.

En `Tools/`: `generate_visual_slice.py`, `generate_animation_frames.py`, `generate_expansion_art.py`, `generate_combat_features.py`, `generate_hito11_assets.py`, `generate_club_art.py`, `generate_ui_art.py`, `generate_plaza.py` producen/sobrescriben recursos. El generador del Club ahora preserva los PNG autoritativos de La Piba; eso no autoriza regenerar el resto. `BarrioUiImport` es un AssetPostprocessor acotado, no un constructor de escenas. UiRedesignCheck cambia temporalmente `isReadable` de enamel y lo restaura: por eso también se ejecutó sólo en copia.

## Estadísticas y siete armas: baseline verificable

Datos en `Conurbano Infinito/Assets/_Game/Data/`. Los valores son **sin mejoras**; cantidad y pierce no son intercambiables.

| Arma / asset | Patrón | Daño / intervalo s | Velocidad / cantidad / pierce / alcance | Cartas dirigidas: máximo individual | Rangos compatibles disponibles / nivel visual anterior máximo |
| --- | --- | --- | --- | --- | --- |
| Termo Balístico | Straight 0 | 1 / 0,8 | 8 / 1 / 0 / 6 | Daño 3, Cadencia 3, Cantidad 2 | **8 / 9** |
| Boleadora | Boleadora 1 | 1 / 1,5 | 7 / 2 / **99** / 5 | Daño 3, Cadencia 3, Cantidad 2 | **8 / 9** |
| Chancleta Teledirigida | Returning 2 | 2 / 2 | 6 / 1 / 0 / 5 | Daño 3, Cadencia 3, Cantidad 2 | **8 / 9** |
| Fernets Orbitales | Orbiting 3 | 1 / 0,5 | 200 grados/s / 2 / 0 / 1,5 | Daño 3, Giro 3, Cantidad 2 | **8 / 9** |
| Aerosol | Straight 0 | 1 / 0,8 | 5 / 5 / **4** / 2,8 | Daño 3, Cadencia 3, Cobertura 3 | **9 / 10** |
| Petardo | Explosive 4 | 2 / 3 | 6 / 1 / 0 / 6 | Daño 3, Cadencia 3, Radio 3 | **9 / 10** |
| Pelota de Potrero | Ricochet 5 | 2 / 1,8 | 8 / 1 / **2** / 5 | Daño 3, Cadencia 3, Rebotes 3 | **9 / 10** |

**Las siete permiten siete mejoras válidas para llegar a nivel 8.** Se verificaron targets, presencia única en el catálogo Main y compatibilidad de patrón. Adquirir arma no suma rango dirigido. El tope futuro deja sin consumir 1 o 2 rangos del máximo anterior; no reducir máximos individuales para ocultarlo.

- Daño actual: `(base + bonus general + bonus dirigido) × multiplicador de Parrilla`. Chimichurri +1 plano, cinco rangos; cada carta de daño dirigida +1, tres rangos. El daño efectivo del proyectil es **float**; `WeaponController.Damage` redondea sólo el getter legacy. Preservar daño fraccional (p. ej. 1,2), no usar el getter entero como daño real.
- Intervalo: `base × 0,9^rangosYerba × 0,9^rangosCadenciaDirigida`. Yerba tiene cinco rangos; cadencia dirigida tres donde existe. Reducir intervalo también adelanta proporcionalmente el ataque pendiente. No hay clamp runtime global actual.
- Cantidad: base +1 por rango de cantidad/cobertura. Giro de Fernets ×1,2 por rango; Petardo radio base **1,5**, ×1,2 por rango; Pelota añade +1 a sus rebotes por rango (base tres impactos, máximo actual seis blancos únicos).
- Zapatillas multiplica velocidad ×1,1 hasta cinco rangos. Alfajor aumenta máximo y vida actual +1 hasta cinco; no revive. El bonus general se aplica también a armas adquiridas después.
- Proyectiles activos conservan snapshot; orbitales existentes se actualizan con `RefreshOrbitals` al cambiar daño/intervalo/velocidad. Cantidad orbital se reconcilia en Update, sin recreación por frame. `OnDisable` libera orbitales.
- Los clamps orientativos 0,12 s / 12 proyectiles / velocidad 10 no cambian armas base ni los máximos actualmente alcanzables de esas magnitudes: intervalo mínimo actual 0,295245 s (Fernets con Yerba 5), cantidad máxima 8 (Aerosol), velocidad máxima 8,857805 (Piba con Zapatillas 5). El nuevo nivel global sí cambia builds. Cifras derivadas estáticamente; probar cálculo/migración runtime en etapa 1.

## Riesgos y preparación de etapa 1

| ID / prioridad | Evidencia y confianza | Consecuencia / acción en su etapa | Verificación necesaria |
| --- | --- | --- | --- |
| B01 / media | **Confirmado por código y check:** `BallCheck.CheckAcquisitionAndUpgrades` aplica 9 rangos y exige `GetWeaponLevel(ball)==10`. Expansion/Petardo también agotan las tres cartas. | El tope 8 romperá fixtures previos. Adaptar escenarios/rangos en etapa 1, preservando pruebas de daño, física, capas y pool; no eliminar aserciones para obtener verde. | Nivel 1→8 por cada arma, rechazo de octava mejora, conservar máximos de carta y casos de rebote/pool separados. |
| B02 / media | **Riesgo por inspección, sin reproducción en esta etapa:** `Select` sólo valida índice/estado; abre otra selección inmediatamente si hay cola y no revalida elegibilidad. | Dos llamadas en un frame pueden consumir dos elecciones sucesivas. Resolver aplicación única exigida por etapa 1 y elegibilidad detallada en 2. | Cola de XP con mouse+teclado simultáneos, pausa y muerte. |
| B03 / media | **Confirmado:** estado de modificadores repartido entre WeaponController, Selection, movimiento, Health y POIs. Checks usan APIs públicas y reflexión sobre `equipped`, `Data`, bonus, `GetInterval`, `Fire`, `ShowNext`, reloj y mazo. | Refactor mínimo de etapa 1 debe actualizar consumidores/checks juntos, mantener nombres serializados y evitar dos fuentes de nivel. No introducir orden frágil de Awake. | Igualdad de estadísticas base, bonus antes/después de adquirir, orbitales, reinicio y perfiles. |
| B04 / media | **Confirmado como límite estructural, impacto de rendimiento no medido:** pool retiene picos, gemas no expiran ni se agrupan, proyectiles/VFX sin presupuesto; búsqueda de blanco usa `OverlapCircleAll`. | Aceptable como baseline corto, insuficiente para prometer infinito estable. Etapa 5: preservar valor XP, instrumentar picos, medir antes de optimizar búsqueda. | Soak ≥30 min simulados más medición real; no confundir F9 con acelerar combate. |
| B05 / media | **Riesgo de identidad por inspección:** `WaveDirector.activeEnemies` conserva referencias Health y sólo limpia nulos/muertos al spawnear; una referencia reutilizada antes de purgarse podría quedar duplicada. | Al integrar infinito/puntaje revisar vida de aparición y listas sin duplicados. No corregido ni demostrado como regresión aquí. | Matar, devolver, reutilizar antes de próximo Tick y comprobar conteos/cupos/puntaje por aparición. |
| B06 / media | **Confirmado:** `Projectile.Hit` no distingue arma/ataque/blanco; F9 sólo multiplica reloj ×10, no es marca persistente de práctica; POIs no emiten evento de completado. | Eventos mínimos en 3; cierre, categorías, práctica irreversible y prioridad muerte/victoria en 6. No construir ranking encima de eventos de audio. | Multiproyectil/efecto secundario/pool; F9 activado y desactivado; doble cierre. |
| B07 / baja | **Confirmado:** no hay IDs de contenido; selección/íconos/checks usan referencias o nombres (`GameUi.WeaponIcon`, `CharacterData.All`). | En etapa 1 agregar IDs editables estables con validación de unicidad y sin derivarlos del nombre visible en cada partida. Mantener referencias e iconos actuales; guardado futuro usa IDs. | Cambiar nombre visible sin cambiar identidad, IDs ausentes/duplicados y reinicio. |

**Etapa 1 lista para comenzar**, sin bloqueo técnico observado. No está implementada. Alcance: IDs, estado runtime de armas/nivel/modificadores, cálculo común, nivel 1–8, guards de selección, compatibilidad de APIs y fixtures. Reutilizar MonoBehaviours/SO existentes; nuevos scripts/datos/meta sólo los necesarios. No incluye aún afinidades, nuevas pasivas, sinergias, evoluciones, modo infinito ni guardado.

## Ejecución y evidencia conservada

Copia APFS independiente creada con `cp -cR` (sin hardlinks): **`/tmp/conurbano-builds-baseline-20261103-aJE7pW/Project`**. Se clonó el proyecto con cache local y se permitió su importación/recompilación en la copia. Cada suite arrancó un proceso Editor nuevo, secuencial, con gráficos, sin `-nographics` ni `-quit` anticipado. Los runners existentes entran/salen de Play Mode y llaman `EditorApplication.Exit`.

Raíz de evidencia local: **`TestResults/BuildsEInfinito/Etapa0-20261103/`**. Contiene logs completos `*.log`, capturas por suite, `results.json` (argv/env/ruta/salida/duración), `run_checks.py`, `catalog-and-wiring.json`, `source-before.json`, `integrity.json`. TestResults está ignorado por git; los logs se conservan en este equipo y el informe versionable resume sus resultados.

| Entrada real / log | Resultado ejecutado | Salida / duración de proceso |
| --- | --- | --- |
| `CharactersCheck.RunBatch` / `characters.log` | `CHARACTERS_CHECK_PASS`; selección/cancelación/retorno, perfiles/animación/pausa, mejoras/reinicio. Incluye **dos `SHORT_RUN_CHECK_PASS`**, Piba y Pibe: primera oferta, seis fases, jefe único, victoria 240, derrota, reinicio y Menu. | **0 / 39,61 s** |
| `CombatFeaturesCheck.RunBatch` / `combat.log` | `COMBAT_FEATURES_CHECK_PASS`, `ENEMY_HEALTH_POOL_PASS` (6 prefabs / 12 vidas), `EXPANSION_CHECK_PASS`; Gomero/perro, hit flash, física/pausa/muerte/XP/pool. Incluye Hito9 (Hito4), Hito11.Check, Petardo, VisualSlice5 y ShortRun. | **0 / 14,55 s** |
| `BallCheck.RunBatch` / `ball.log` | `BALL_CHECK_PASS`; cartas para ambos, cadenas base de 3 y mejoradas de 6 blancos, alcance/capas/no repetir/muertos, física real, pausa y reuso ball→straight→ball. | **0 / 12,52 s** |
| `PitchCheck.RunBatch` / `pitch.log` | `PITCH_CHECK_PASS`; curación/no resurrección, dos resistencias físicas de 6 s, salir/pausar, seis órdenes con fixtures de reloj, Q35/umbrales/reinicio, 100 semillas. Incluye `PoiIndicatorsCheck.Run` y `QuilomboSliceCheck.Run`. | **0 / 30,97 s** |
| `UiRedesignCheck.RunBatch` / `ui.log` | `UI_REDESIGN_PASS`, `UI_RUNTIME_PASS`, `UI_UPGRADE_COPY_PASS cards=32 resolutions=4`; teclado/foco, Esc/R/M, cola con pausa, cuatro armas, nivel N1/N2, menú/créditos/victoria/derrota/F3. `text-layout.txt` vacío. Incluye POIs y Hito11.Check. | **0 / 42,01 s** |

Resoluciones comprobadas: **1024×768, 1280×720, 1920×1080 y 2560×1080**. Son checks automáticos, no partidas humanas completas ni prueba de mouse de extremo a extremo. ShortRun adelanta fases/reloj por fixture; no demuestra supervivencia real durante cuatro minutos.

Se inspeccionaron visualmente las capturas reales `ui/hud-1280x720.png` y `ui/selector-1024x768.png`: HUD/equipo/POIs y perfiles conservan su presentación legible; la plaza mantiene mobiliario irregular. Esta muestra no cubre cada estado visual ni cada resolución.

Compilación comprobada en `characters.log`: ejecución **Csc** para **Assembly-CSharp.dll** y **Assembly-CSharp-Editor.dll**, importación completada y Play Mode posterior. Sin errores C#, excepciones de gameplay ni asserts reportados; cinco procesos con salida 0. No se ejecutó una suite NUnit ni se generó build.

Mensajes ambientales preservados: cada arranque informa `Access token is unavailable; failed to update`, después resuelve entitlement y ejecuta los checks; también `Ignoring depth surface load action as it is memoryless` en el Editor/Metal (5/1/1/3/4 apariciones en characters/combat/ball/pitch/ui), avisos de threads al cerrar. No se ocultaron ni corrigieron. La excepción histórica `UnityEditor.Search.SearchDatabase` **no se reprodujo**: cero en los cinco logs y `knownEditorSearchErrors=0`. No atribuirla al gameplay ni declararla resuelta por una corrida.

Integridad SHA-256: **602 archivos** de Assets/Packages/ProjectSettings del original idénticos antes/después; la copia al finalizar también coincide en los 602. Incluye meta, escenas, prefabs, catálogos, paquetes y ajustes. El esqueleto exterior no se editó. No se ejecutaron generadores ni métodos Setup de regeneración.

### Reproducción

Desde la raíz del repo, crear una copia nueva (en macOS APFS; en otro filesystem usar copia independiente normal), con Editor de la versión instalada:

```sh
baseline_copy=$(mktemp -d /tmp/conurbano-baseline-XXXXXX)
cp -cR "Conurbano Infinito" "$baseline_copy/Project"
baseline_output="$PWD/TestResults/BuildsEInfinito/repeticion-$(date +%Y%m%d-%H%M%S)"
mkdir -p "$baseline_output"
CONURBANO_CHARACTERS_OUTPUT="$baseline_output/characters" \
  /Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$baseline_copy/Project" \
  -executeMethod CharactersCheck.RunBatch -logFile "$baseline_output/characters.log"
```

Repetir **secuencialmente** en procesos nuevos sustituyendo el par método/variable por `CombatFeaturesCheck.RunBatch` / `CONURBANO_COMBAT_OUTPUT`, `BallCheck.RunBatch` / `CONURBANO_BALL_OUTPUT`, `PitchCheck.RunBatch` / `CONURBANO_PITCH_OUTPUT`, `UiRedesignCheck.RunBatch` / `CONURBANO_UI_OUTPUT`, y sus carpetas/logs. No establecer `CONURBANO_ART_SAMPLE=1`: esta auditoría ejecuta la validación normal. Esperar salida del proceso y marcador final, no sólo existencia de capturas. Los argv exactos de esta ejecución están en `results.json`; `run_checks.py` permite repetirla mientras exista su copia, pero sobrescribe esos logs.

`ShortRunCheck.Run` **no tiene RunBatch**: requiere Play Mode y es `async void`; se ejecutó y esperó a través de CharactersCheck para ambos perfiles. Alternativa manual: elegir personaje en Menu, iniciar Play Mode y usar **Tools → Conurbano Infinito → Comprobar partida corta en Play Mode**. Cambia/reinicia la sesión y termina en Menu.

## Orden de trabajo y puerta de verificación

| Etapa | Entrega posterior a este baseline | Verificación mínima antes de seguir |
| --- | --- | --- |
| 0 | Este informe y contexto actualizado; gameplay intacto. | Checks anteriores + integridad + `git diff --check`. |
| 1 | IDs, estado runtime, estadísticas y nivel global 8. | Base idéntica por arma/personaje, fórmulas/orden/clamps/fracciones, bonus en adquisiciones posteriores, orbitales/reinicio, nivel 8 en siete armas, doble input; adaptar fixtures y repetir Characters/Combat/Ball/UI. |
| 2 | Seis pasivas, cuatro espacios, elegibilidad/fallback y cola. | Mazos 0/1/2/3, cupos, máximos, compatibilidad, adquisición tardía, XP múltiple, pausa/muerte/reinicio; cuatro resoluciones/teclado/mouse. |
| 3 | Dos afinidades y tres sinergias visibles. | IDs/niveles/orden inverso, aplicación única, ataque vs proyectil/impacto, carga/consumo, secundarios sin recursión, pool/reinicio y pausa legible. |
| 4 | Dos evoluciones y logro de jefe previo. | Requisitos en ambos órdenes, recetas simultáneas, cuatro espacios, herencia/familia, pausa/cancelación de pulsos, pool/blancos muertos/retorno limitado; una por personaje. |
| 5 | Selector de modo, infinito, escalado y presupuestos. | 239,9/240/240,1, tramos 120 s, jefes 180/420/660, jefe vivo, POIs, pool/clásico, XP conservada; soak ≥30 min de simulación y medición real declarada. |
| 6 | Puntaje y resultado inmutable/práctica/cierre único. | Ejemplo 3240, centipuntos/saturación, Q al morir, muerte vs despawn por aparición, pausa, doble cierre/abandono, muerte-victoria y snapshots. |
| 7 | Top20/último resultado locales y navegación. | Reinicio de app, versiones/modos, empates/duplicados, exclusión práctica, JSON viejo/corrupto/fallo de escritura, teclado/mouse/foco y cuatro resoluciones. |
| 8 | Validación/balance/build y adenda del GDD. | Matriz de personajes/modos/tres builds, regresiones, medición FPS/GC/memoria en hardware declarado, build y ejecución fuera del Editor; registrar Windows por separado. |
| 9 opcional | Especificación/ADR de ranking casual online. | Contrato/amenazas/idempotencia/offline/moderación/borrado y decisiones de proveedor/costo; sin servicios ni despliegue en esa etapa de diseño. |

## Prueba humana de este baseline y límites

No hay comportamiento nuevo que activar. Abrir la raíz Unity correcta, cargar Menu e iniciar Play Mode. Con Pibe confirmar Termo/vida 5, adquirir Aerosol si se desea; reiniciar y repetir con Piba, Boleadora/vida 4 y primera oferta Termo. Mover con WASD/flechas, elegir cartas con 1–3 o foco/Enter y mouse, pausar con Esc durante una selección y continuar sin perderla. Probar Canchita herido (seis segundos, hasta +2), Kiosco y Parrilla una vez; salir antes debe reiniciar el desafío. Jefa a 03:00; sobrevivir hasta 04:00 debe ganar aunque quede viva. R reinicia conservando personaje y limpiando progreso; M vuelve a Menu. No usar F9 para evaluar dificultad: acelera sólo el reloj.

No se midieron FPS/frame time, GC, crecimiento de memoria ni estabilidad de 30 minutos; no se generó build nueva, no se ejecutó fuera del Editor ni en Windows y no se evaluó balance/sonido/juego humano. No se probó exhaustivamente mouse ni simultaneidad de inputs. Estos límites no impiden empezar etapa 1; sí impiden declarar lista la entrega completa de infinito o competencia.
