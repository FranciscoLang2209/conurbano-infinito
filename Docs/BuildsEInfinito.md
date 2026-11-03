# Conurbano Infinito: builds, modo infinito y competencia

Plan de diseño e implementación para Codex. Fecha: 3 de noviembre de 2026.

## 1. Punto de partida comprobado

Repositorio: https://github.com/FranciscoLang2209/conurbano-infinito

Inspección de `main`, commit `606639c4bea1fd7d2469df2277f62646388449ea`. Se revisaron código, configuración, árbol de archivos y documentación mediante GitHub; no se ejecutó Unity ni se comprobó una build en esta revisión. Las cifras de diseño de las secciones siguientes son propuestas iniciales, pendientes de pruebas humanas.

| Sistema | Estado observado | Consecuencia para el plan |
| --- | --- | --- |
| Proyecto | Unity 6000.3.23f1, URP 17.3.0, Input System 1.20.0 | Mantener motor, paquetes y controles |
| Raíz real | `Conurbano Infinito/` | No trabajar en el esqueleto `Assets/` de la raíz del repo |
| Escenas de build | Menu → Main | Reutilizar el flujo para elegir personaje y modo |
| Duración | GameManager limita el reloj a 240 s y declara victoria | Agregar un modo explícito; no simular infinito con una duración enorme |
| Personajes | El Pibe del Barrio y La Piba del Club | Profundizar sus diferencias antes de agregar personajes |
| Armas | Catálogo de siete: Termo, Boleadora, Chancleta, Fernets, Aerosol, Petardo, Pelota | Aprovechar contenido existente |
| Inventario | WeaponController permite cuatro armas simultáneas | Mantener cuatro espacios |
| Mejoras | Tres ofertas por nivel, filtros por arma y niveles máximos por carta | Ampliar elegibilidad y progreso de build |
| Pasivas | Cuatro assets: Chimichurri, Yerba, Zapatillas y Alfajor | Convertirlas en un inventario con identidad y cupo explícitos |
| Nivel de arma | UpgradeSelection suma las mejoras de cartas dirigidas al arma | No existe todavía una progresión única de arma con nivel máximo global |
| Quilombo | Modifica frecuencia y límite de enemigos; POIs de uso único | Reutilizarlo como riesgo que también aumenta recompensa |
| Oleadas | WaveDirector recorre fases y genera una única Jefa a 180 s | Necesita extensión posterior a 240 s y política de jefes repetidos |
| Pool | Reutiliza enemigos y proyectiles; Health se reinicia | El escalado, las suscripciones y los puntos deben reiniciarse por aparición |
| UI | IMGUI centralizado en GameUi | Extenderlo respetando tipografía y legibilidad actuales |
| Verificación | Test Framework instalado; checks propios de Editor | Reutilizar checks y sumar pruebas de reglas e integración pertinentes |

El GDD adjunto, versión 0.1, proponía partidas de diez minutos y dejaba evoluciones como ampliación. El código actual y `Docs/PartidaCorta.md` describen una demo de cuatro minutos. Este plan toma **cuatro minutos como modo clásico actual** y agrega infinito como un segundo modo. El GDD sigue siendo referencia para identidad, ataques automáticos, humor y caos legible, pero deberá recibir una adenda de alcance.

Archivos centrales revisados:

- `Docs/UnityProjectContext.md` y `Docs/PartidaCorta.md`.
- `Conurbano Infinito/ProjectSettings/{ProjectVersion.txt,EditorBuildSettings.asset}` y `Packages/manifest.json`.
- `Assets/_Game/Scripts/Player/{CharacterData,WeaponController,UpgradeData,UpgradeSelection,GameManager,ExperienceSystem,RiskZone}.cs`, relativos a la raíz Unity.
- `Assets/_Game/Scripts/Enemy/{WaveDirector,SpawnManager}.cs`.
- `Assets/_Game/Scripts/Combat/{WeaponData,Health,ObjectPool}.cs`.

## 2. Experiencia que buscamos

El jugador elige un personaje, empieza con un arma y va construyendo una especialización. Las elecciones deben cambiar posición, cobertura y prioridades: atacar desde lejos, jugar entre grupos, aprovechar rebotes o arriesgarse cerca. Una build es una combinación que el jugador descubre durante la partida, no una clase cerrada que compra antes de empezar.

La secuencia deseada es: personaje → arma inicial → primeras mejoras → especialización → sinergia visible → evolución → sostener la build ante una presión creciente. En infinito, el puntaje premia eliminar enemigos y aceptar riesgo. La derrota muestra qué combinación logró el récord y facilita volver a intentarlo.

### Alcance recomendado de primera entrega

- Dos personajes actuales con un rasgo sencillo y distinto por personaje.
- Siete armas actuales y cuatro espacios de armas.
- Seis pasivas disponibles y cuatro espacios de pasivas.
- Tres sinergias explícitas y dos evoluciones.
- Modo clásico de cuatro minutos y modo infinito en la plaza actual.
- Puntaje, resumen de partida y tabla de récords local.
- Estadísticas y versiones suficientes para conectar un ranking online después.

Dejar para otra entrega personajes adicionales, fusiones que consuman dos armas, árboles de habilidades permanentes, desafíos diarios, mapas nuevos y ranking online. La primera versión permite competencia en una misma computadora y comparación manual de resultados; no ofrece una clasificación compartida entre instalaciones.

## 3. Reglas de builds

### 3.1 Personajes

Conservar vida, velocidad, arma inicial, retratos y animaciones actuales. Propuesta de rasgos adicionales:

| Personaje | Rasgo propuesto | Estilo favorecido |
| --- | --- | --- |
| El Pibe del Barrio | +10% de daño para armas etiquetadas como proyectil directo | Termo y presión a distancia |
| La Piba del Club | +1 rebote exclusivamente para armas con patrón Ricochet | Pelota y control de grupos |

Son afinidades, no restricciones: ambos pueden equipar todas las armas. La etiqueta de proyectil directo se asigna explícitamente a Termo y Boleadora en esta primera versión; no se deduce del sprite. El rasgo de La Piba no modifica Boleadora, Returning ni pierce de otras armas.

Las primeras ofertas actuales se mantienen para reducir regresiones: Aerosol para El Pibe y Termo para La Piba. La Piba puede conseguir Pelota después. Si las pruebas muestran que esa afinidad tarda demasiado en aparecer, cambiar la primera oferta de La Piba será una decisión de balance documentada.

### 3.2 Armas y niveles

- Cuatro espacios incluyendo el arma inicial; una evolución reemplaza su arma y conserva el espacio.
- Identificadores estables para armas, pasivas, personajes, recetas y perfiles de reglas. El nombre visible puede cambiar sin invalidar récords.
- Cada arma empieza en nivel 1 y llega a nivel 8. Cada elección de una carta de mejora de esa arma suma un nivel; adquirirla solo la deja en nivel 1.
- Conservar inicialmente las mejoras actuales de daño, cadencia, cantidad, radio y rebotes y sus máximos individuales. Agregar el tope global de nivel 8.
- Filtrar tanto por el máximo individual de la carta como por el máximo global del arma. Mostrar ambos conceptos de forma comprensible.
- Un arma al máximo sale de las mejoras normales. Su receta de evolución puede aparecer cuando cumpla requisitos.
- No editar WeaponData durante una partida. Nivel, bonus, cadencia, estado de evolución y contadores pertenecen al estado runtime.

El límite global de 8 cambia el balance respecto de sumar todos los rangos actuales; no debe presentarse como una refactorización sin efecto jugable. Se valida que todas las armas tengan suficientes mejoras compatibles para llegar a 8.

### 3.3 Pasivas

Las cuatro pasivas actuales pasan a ocupar espacios. Mejorar una ya adquirida no ocupa otro. Con cuatro espacios llenos se ofrecen mejoras de las propias, no una quinta pasiva. Los máximos existentes se conservan inicialmente.

| Pasiva | Efecto propuesto | Regla de aplicación |
| --- | --- | --- |
| Chimichurri Casero | Daño general | Migrar el efecto actual al cálculo común |
| Yerba Premium | Menor intervalo entre ataques | Preservar sus rangos, con límite mínimo global de intervalo |
| Zapatillas Flecha | Velocidad de movimiento | Preservar sus rangos, con máximo de velocidad configurado |
| Alfajor Triple | Vida máxima | Preservar curación asociada actual al aumentar vida |
| Antena de Televisión, nueva | +10% de radio de explosión por nivel, 5 niveles | Primera versión: Petardo; no escala todos los tamaños visuales |
| Bolsa del Supermercado, nueva | +1 proyectil a nivel 3, máximo 3 | Primera versión: Termo y Boleadora; no agrega explosiones superpuestas a Petardo |

Antena y Bolsa solo se ofrecen si hay al menos un arma compatible equipada. La compatibilidad se expresa en datos y aparece en la descripción. Nuevas armas adquiridas después reciben inmediatamente los efectos de las pasivas propias.

### 3.4 Sinergias

Primera versión: reglas explícitas y editables que se activan una vez al cumplirse requisitos; no un motor genérico de scripting. Deben funcionar igual sin importar el orden de adquisición.

| Sinergia | Requisitos | Efecto inicial propuesto |
| --- | --- | --- |
| Mate de Guardia | Termo nivel 4 + Yerba nivel 3 | Cada cuarto ataque del Termo lanza un proyectil adicional |
| Potrero Veloz | Pelota nivel 4 + Zapatillas nivel 3 | Pelota gana +20% de velocidad de proyectil |
| Festejo del Barrio | Pelota nivel 4 + Petardo nivel 4 | Cada quinto impacto directo de Pelota potencia +25% el radio del próximo ataque de Petardo |

Reglas precisas: un ataque es una activación del arma, no cada proyectil individual. Los impactos de Festejo son impactos válidos sobre enemigos vivos; rebotes cuentan, efectos secundarios no. Se almacena como máximo una carga de Festejo, se consume por ataque completo de Petardo y no puede volver a generarse por sus explosiones. La UI indica carga lista. Los contadores empiezan en cero y se limpian al reiniciar.

Las sinergias no dan puntos por sí mismas. Su ventaja se refleja en sobrevivir y eliminar enemigos. Mostrar un aviso breve al activarse y su estado en pausa; evitar otro panel permanente grande en combate.

### 3.5 Evoluciones

| Evolución | Requisitos | Cambio jugable propuesto |
| --- | --- | --- |
| Termo de Acero Inoxidable | Termo nivel 8 + Yerba nivel máximo + derrotar un jefe en esa partida | Dispara una ráfaga de dos pulsos separados por 0,12 s; mismo intervalo entre inicios de ráfaga; segundo pulso con 50% del daño |
| Pelota de Final de Barrio | Pelota nivel 8 + Zapatillas nivel máximo + derrotar un jefe | Añade dos rebotes y permite un retorno al primer blanco si sigue vivo, tras visitar al menos otro blanco |

Al estar lista, la evolución compite como carta explícita en la siguiente subida de nivel y ocupa una opción garantizada. Si hay dos recetas listas, priorizar por ID estable y ofrecer la otra en la próxima selección. No evolucionar silenciosamente al matar un jefe. La receta solo se puede consumir una vez.

Conservar las mejoras ya obtenidas; el arma evolucionada queda al máximo. Las sinergias reconocen el ID de su familia: no vuelven a activarse ni se pierden al evolucionar. La ráfaga del Termo cuenta como un ataque para Mate de Guardia: su proyectil adicional se agrega al primer pulso. Festejo reconoce la Pelota evolucionada, pero solo se permite un retorno al primer blanco por proyectil, sin ciclos infinitos.

Reutilizar arte existente e identificar la evolución con nombre, color y efecto sencillo. Nuevos sprites pueden agregarse después de validar la mecánica.

### 3.6 Elecciones y final de progresión

- Mantener tres opciones distintas cuando existan tres candidatas válidas. Con menos candidatas, mostrar solo las válidas; no duplicar cartas para rellenar.
- Revalidar una elección antes de aplicarla y aplicar el cambio una sola vez aunque coincidan mouse y teclado.
- Garantizar la evolución lista sin eliminar las reglas de compatibilidad y cupos.
- Si se agotan todas las mejoras, ofrecer curar 10% de vida máxima, mínimo 1, sin aumentar estadísticas permanentes. A vida llena, mostrar una opción explícita de continuar sin recompensa.
- Mantener la cola de varias subidas de nivel y la pausa existente. Nunca dejar la partida bloqueada con cero cartas.
- No recompensar las subidas sobrantes con puntos: evita que la fórmula dependa de mejoras gratuitas repetibles.

### 3.7 Cálculo común de estadísticas

Separar definición, estado de partida y resultado calculado. Para daño, un esquema suficiente es `(base + bonus planos) × (1 + suma de porcentajes)`, con un redondeo final consistente. Para intervalos, conservar inicialmente los factores multiplicativos existentes y aplicar un mínimo explícito al final. Documentar qué efectos son planos y cuáles multiplicadores.

Límites iniciales orientativos: intervalo mínimo 0,12 s, hasta 12 proyectiles por ataque y 10 de velocidad del jugador. Son parámetros de balance, no constantes repartidas. Los límites no deben cambiar el resultado actual de armas sin mejoras. Verificar orbitales existentes y proyectiles ya activos cuando cambian estadísticas. El daño y otros valores de un proyectil usan un snapshot al crearlo; los orbitales permanentes se refrescan sin recrearse cada frame.

## 4. Modo infinito

### 4.1 Dos modos explícitos

**Clásico:** conservar 240 segundos, fases actuales y Jefa a 180 segundos. Cambia la profundidad de builds, pero se conserva su regla de victoria y la plaza.

**Infinito:** selección independiente desde menú. Usa los primeros cuatro minutos como apertura, continúa al superar 240 segundos y termina al morir o abandonar. Abandonar no produce récord. Reiniciar conserva personaje y modo; volver al menú permite cambiarlos. El reloj es tiempo de simulación activo y se congela en pausa y elecciones.

No hace falta un mapa infinito: el modo puede durar sin límite dentro de la plaza actual. No ampliar el mapa ni crear chunks para esta entrega.

### 4.2 Director posterior a cuatro minutos

Propuesta de primer perfil, ajustable desde datos:

- Cada 120 segundos posteriores a 240 empieza un nuevo tramo: `c = 1 + floor((t - 240) / 120)`.
- Vida de enemigos nuevos: `1 + 0,20 × c` sobre la base original de cada prefab.
- Factor de velocidad: `min(1 + 0,02 × c, 1,30)`.
- Frecuencia: intervalo base final dividido por `min(1 + 0,08 × c, 2)` y luego por el factor actual de Quilombo; nunca menor a 0,20 s.
- Límite de comunes: límite de la última fase, escalado por tramo y Quilombo, con tope global inicial de 150 comunes activos y un jefe adicional. Este tope requiere medición en el equipo objetivo.
- Daño por contacto: mantenerlo inicialmente; priorizar densidad, vida, composición y lectura de ataques sobre muertes de un golpe.
- Jefa a 180 s; luego a 420 s y cada 240 s. Si ya hay un jefe vivo, omitir esa aparición, sin acumular una deuda de jefes.
- No crear variantes élite sin prefab y comportamiento válidos. Reutilizar enemigos y patrones existentes.

Evitar multiplicar la vida ya escalada de un objeto reutilizado. Al aparecer, calcular desde la base inmutable; aplicar configuración antes de que pueda combatir. Enemigos vivos conservan sus estadísticas al cambiar de tramo.

Son curvas para comenzar pruebas, no garantía de dificultad adecuada: el Aerosol ya aparece documentado como una fuente de poder que necesita evaluación. La presión no debería desaparecer cuando se agota el catálogo de mejoras.

### 4.3 Quilombo y POIs

En primera entrega los POIs conservan sus recompensas de una vez por partida. No se reinician por tramo. El Quilombo adquirido mantiene la presión y aumenta los puntos por enemigos. Informar ambas consecuencias antes de completar el lugar. No venderlo como una dificultad que crece sola: el progreso temporal lo lleva el director infinito.

### 4.4 Duración y rendimiento

Una partida larga no puede acumular objetos indefinidamente. Perfilar enemigos, proyectiles, drops, efectos, contadores y pools. Agregar presupuesto configurable de gemas activas, agrupación de XP y/o recolección consolidada que conserve el valor total; registrar la política para gemas lejanas. No devolver enemigos vivos al pool para liberar cupo otorgando XP o puntos.

Revisar `Physics2D.OverlapCircleAll` en búsqueda de blancos: asigna arrays por consulta. Optimizar con buffers reutilizables si el perfil lo justifica; manejar saturación del buffer sin romper la búsqueda. Registrar picos y comportamiento estable, además de FPS promedio. La meta de 60 FPS se comprueba en hardware declarado y con una build real.

## 5. Puntos y competencia

### 5.1 Fórmula inicial

| Evento | Valor base propuesto |
| --- | ---: |
| Enemigo común | 10 |
| Enemigo especial: Perro o Gomero | 30 |
| Élite, solo cuando exista una variante identificada | 150 |
| Jefe | 1.000 |
| POI completado por primera vez | 250 |
| Segundo completo de supervivencia activa | 1 |

Multiplicador por enemigo: `1 + min(Quilombo, 100) / 100`, de ×1 a ×2. Se toma el Quilombo inmediatamente anterior al evento de muerte. No usar nivel del personaje, daño bruto ni experiencia como puntos adicionales: eso premiaría varias veces el mismo resultado.

Representación exacta: guardar centipuntos en `long`; para Quilombo fraccionario, redondear primero el multiplicador a centésimas con una política documentada. Por ejemplo, Quilombo entero 35 da factor entero 135 y una baja común agrega `10 × 135 = 1350` centipuntos. POIs y segundos agregan su valor multiplicado por 100. Mostrar `floor(centipuntosTotales / 100)`; no redondear cada baja por separado. Proteger desbordamientos de forma saturada.

Ejemplo: 100 comunes y un jefe, todos con Quilombo 25, dos POIs y 240 segundos activos: `(1000 + 1000) × 1,25 + 500 + 240 = 3240 puntos`.

No sumar por desaparición, reciclaje o despawn. Una muerte válida puntúa una sola vez por vida del enemigo, incluso con daño simultáneo y pool. Los enemigos invocados/divididos futuros deberán definir expresamente si generan puntaje para evitar granjas.

### 5.2 Récord local y resumen

Separar tablas por modo y `rulesetVersion`; conservar historial de versiones anteriores sin mezclar posiciones. Dentro de cada tabla: puntos descendentes, tiempo activo descendente y `runId` ordinal ascendente como desempate estable.

Guardar top 20 y último resultado: runId, personaje, modo, versión de reglas, versión de build, inicio UTC, tiempo activo, puntos, desglose, bajas por categoría, jefes, Quilombo final, armas y niveles, pasivas, sinergias, evoluciones y motivo de cierre. Guardar IDs y versión de esquema; no referencias de Unity.

Escribir una vez al cierre válido mediante guardado atómico con respaldo y recuperación ante JSON ausente, viejo o corrupto. No repetir registro al reabrir derrota. Pausa no termina la partida. Pruebas, F9, alteración de reloj, invulnerabilidad de QA o herramientas equivalentes marcan la sesión como práctica y excluyen el récord; desactivar el truco después no recupera elegibilidad.

Mostrar en derrota puntaje, mejor marca comparable, build utilizada y opciones Reintentar/Menú/Récords. La tabla local sirve para competir entre amigos en el mismo equipo. Las capturas permiten comparación manual entre equipos, sin garantía contra manipulación.

### 5.3 Ranking online: segunda entrega opcional

Si querés posiciones compartidas automáticamente entre jugadores, se necesita un servicio remoto. El guardado local y un hash del cliente no verifican una partida.

Antes de elegir proveedor, definir alcance: ranking casual con controles básicos y resultados no verificados, o competencia con simulación/validación autoritativa. Para un proyecto de dos personas recomiendo primero ranking casual, indicando sus límites. Un seed común no hace determinista la física de Unity entre equipos ni versiones.

Contrato mínimo futuro: crear sesión de partida; devolver ID y versión de reglas; enviar resultado con clave de idempotencia; consultar top y posición propia. El servidor asigna identidad y tiempos, valida esquema, duración y cotas razonables, aplica límites de frecuencia y rechaza sesiones duplicadas o de práctica. Estas comprobaciones detectan anomalías, pero no prueban que el cliente jugó honestamente. Una clasificación verificada exige un diseño adicional de autoridad/replay verificable.

No incluir secretos de servidor en la build. Definir identidad pública, moderación de nombres, funcionamiento sin conexión y borrado de datos antes de publicar. Mantener un `ILeaderboardService` pequeño si aporta valor al reemplazo local/remoto; evitar infraestructura de red en la primera entrega.

## 6. Orden de implementación

| Etapa | Entregable jugable o verificable | Dependencia |
| --- | --- | --- |
| 0 | Auditoría actualizada y baseline registrado | Ninguna |
| 1 | Estado de build y estadísticas sin regresión | 0 |
| 2 | Cupos, seis pasivas y ofertas válidas | 1 |
| 3 | Afinidades y tres sinergias visibles | 2 |
| 4 | Dos evoluciones completas | 3 |
| 5 | Infinito con presión y objetos acotados | 4 |
| 6 | Puntaje único y resumen reproducible | 5 |
| 7 | Récords locales persistentes y menú completo | 6 |
| 8 | Pruebas, balance, rendimiento y build | 7 |
| 9, opcional | Diseño concreto de ranking online | 8 |

Una etapa por rama/PR o por cambio revisable. No pegar todos los prompts a la vez: terminar, verificar y jugar el resultado antes de continuar. Si aparece una regresión, corregirla en esa etapa. Primero integrar lo anterior antes de comenzar otra tarea que edite Main.

## 7. Prompts para Codex

Copiá este documento al repositorio como `Docs/BuildsEInfinito.md`. Los siguientes prompts lo usan como contrato de diseño. Las reglas y nombres nuevos son propuestas de este plan; Codex debe contrastarlos con el estado actual antes de implementarlos.

### Instrucción común para cada sesión

```text
Trabajamos en FranciscoLang2209/conurbano-infinito. Leé AGENTS.md y la guía local aplicable, Docs/UnityProjectContext.md y Docs/BuildsEInfinito.md. La raíz Unity esperada es "Conurbano Infinito/"; verificála antes de editar. No trabajes sobre el esqueleto Assets de la raíz.

Implementá solamente la etapa indicada y sus dependencias ya acordadas. Revisá git status y preservá cambios ajenos. No actualices Unity/paquetes, no migres IMGUI y no regeneres Main, Menu, prefabs o el catálogo completo con herramientas históricas. Conservá GUIDs, fileIDs, valores de enums serializados y nombres de campos; agregá valores al final y usá una migración compatible cuando corresponda. Permitido: cambios acotados de integración, nuevos datos/scripts y los meta necesarios.

Separá definiciones ScriptableObject de estado runtime. Evitá sistemas genéricos, dependencias o infraestructura que esta etapa no necesite. Mantené teclado, mouse, pausa y selección en cola. Preservá el estilo legible de GameUi y los objetos irregulares de la plaza.

Completá implementación e integración y ejecutá las verificaciones pertinentes disponibles. Si falta Unity, informá exactamente qué no pudiste ejecutar y dejá pasos reproducibles; no afirmes compilación, Play Mode, FPS o build sin evidencia. No hagas push, merge, publicación ni despliegue como parte de estos prompts.

Al terminar: explicá el comportamiento nuevo, archivos relevantes, pruebas realmente ejecutadas, límites pendientes y cómo probarlo jugando. Actualizá el contexto del proyecto y registrá la etapa en Docs/BuildsEInfinito.md, distinguiendo diseño de implementación comprobada.
```

### Prompt 0 — Auditoría y baseline

```text
Aplicá la instrucción común. Ejecutá la etapa 0: auditoría previa, sin modificar gameplay.

Contrastá Docs/BuildsEInfinito.md con la rama actual. Inspeccioná GameManager, WaveDirector/WaveData, SpawnManager/ObjectPool/Health, Projectile, WeaponController/WeaponData, CharacterData/PlayerController, UpgradeSelection/UpgradeData, ExperienceSystem, Quilombo y RiskZone, HUD/MenuScreen/GameUi y checks del Editor. Identificá referencias serializadas y herramientas que regeneran contenido. Revisá las siete armas y que cada una tenga rangos compatibles suficientes para llegar al nivel global 8 propuesto.

Documentá puntos concretos de extensión, dueños de estado, flujo de inicialización y riesgos, con rutas reales. Marcá diferencias entre GDD, documentación y código; el clásico actual debería durar 240 segundos. Registrá commit, cambios locales y capacidades de Editor/MCP disponibles, sin instalar herramientas.

Si existe Unity y es seguro, ejecutá en copia aislada los checks pertinentes que establezcan el baseline: ShortRunCheck para ambos personajes, CharactersCheck, CombatFeaturesCheck, BallCheck y checks actuales de UI/POIs según sus entradas reales. No supongas nombres de métodos ni regeneres assets para probar. Conservá logs y separá fallos preexistentes.

Entregá Docs/BuildsEInfinito-Baseline.md y actualizá Docs/UnityProjectContext.md preservando el contenido manual. Listá orden de trabajo y verificación de cada etapa. Si no hay impedimentos, dejá lista la etapa 1; no implementes otras etapas todavía.
```

### Prompt 1 — Estado de build y estadísticas

```text
Aplicá la instrucción común. Implementá etapa 1 según las secciones 3.2 y 3.7.

Agregá IDs estables y un estado runtime de build con armas, niveles y modificadores, separado de assets. Centralizá cálculo de estadísticas y compatibilidades. Conservá o adaptá de forma compatible las APIs públicas usadas por checks actuales. Migrá los modificadores actuales sin aplicar todavía afinidades ni sinergias nuevas. Los valores sin mejoras deben coincidir con el baseline; documentá las diferencias que introduzcan los límites acordados.

Reemplazá el nivel visual calculado sumando cartas por una fuente única de nivel de arma: empieza en 1, cada mejora dirigida suma 1 y máximo 8. El máximo individual de cada carta sigue existiendo. No alteres el patrón serializado de ninguna arma, especialmente Ricochet ni su uso actual de pierce.

Comprobá fórmula de daño/cadencia, orden de aplicación, clamps, aplicación de bonus a armas adquiridas después, refresco de orbitales y reinicio de sesión. Comprobá que cada arma puede alcanzar nivel 8 y que una carta no se aplica dos veces por input simultáneo. Ejecutá regresiones pertinentes de combate/personajes. La selección detallada y nuevas pasivas corresponden a la etapa siguiente.
```

### Prompt 2 — Pasivas y elecciones

```text
Aplicá la instrucción común. Implementá etapa 2 y reglas 3.3/3.6.

Convertí Chimichurri, Yerba, Zapatillas y Alfajor en pasivas con cuatro espacios. Agregá Antena y Bolsa con niveles y compatibilidades definidos en el plan. Las mejoras de una pasiva propia no consumen espacios. Con inventario lleno filtrá adquisiciones nuevas, pero seguí ofreciendo mejoras válidas de lo equipado. En armas, respetá cuatro espacios, nivel global 8 y máximos individuales por carta.

Reestructurá la elegibilidad para ofrecer hasta tres cartas distintas válidas, conservando primeras ofertas por personaje y cola de niveles. Mostrá adquisición versus mejora, nivel resultante y compatibilidad. No garantices sinergias todavía. Al agotarse opciones, aplicá el fallback de curación/continuar del plan, sin otorgar puntos.

Probá cupos llenos, catálogos con 0/1/2/3 candidatas, mejoras máximas, pasivas incompatibles, adquirir un arma después de una pasiva, ráfaga de XP con varias elecciones, pausa durante elección, muerte y reinicio. Revisá UI en las cuatro resoluciones ya usadas por el repo. Dejá una partida donde se puedan construir dos especializaciones distintas.
```

### Prompt 3 — Afinidades y sinergias

```text
Aplicá la instrucción común. Implementá etapa 3, afinidades de la sección 3.1 y las tres sinergias de 3.4.

Usá requisitos explícitos por IDs/niveles y estado runtime. No hagas lógica por nombres visibles ni un framework de scripting. Resolvé activación al cambiar la build, con idéntico resultado en cualquier orden de adquisición. Respetá el patrón Ricochet exclusivamente para la afinidad de La Piba.

Implementá Mate de Guardia, Potrero Veloz y Festejo del Barrio con contadores, definición de ataque/impacto, carga máxima, consumo por ataque completo y prevención de procs recursivos del plan. Si hacen falta eventos de combate, distinguí arma/familia, ataque, proyectil, impacto directo y efecto secundario con el mínimo estado necesario.

Agregá aviso breve de activación y consulta de efectos/requisitos en pausa. Mostrá carga lista de Festejo sin tapar combate. Probá umbrales, orden inverso, aplicación única, múltiples proyectiles por ataque, blancos muertos, pool y reinicio; comprobá que el daño secundario no genera otra carga. Validá las dos afinidades sin cambiar vida/velocidad/arte del personaje.
```

### Prompt 4 — Evoluciones

```text
Aplicá la instrucción común. Implementá etapa 4 y las dos evoluciones de 3.5.

Creá recetas editables: arma nivel 8, pasiva requerida al máximo y al menos un jefe derrotado durante esta sesión. Guardá el logro de jefe antes de reunir los otros requisitos. Al completarse una receta, garantizá su carta en la siguiente elección; si hay varias, orden estable por ID. Aplicala una vez y reemplazá el arma en su espacio, conservando mejoras y familia. No debe seguir ofreciéndose el arma base ni sus cartas incompatibles.

Implementá la ráfaga de Termo y el retorno limitado de Pelota del plan. Congelá pulsos pendientes en pausa y cancelalos al morir/reiniciar; no dejes callbacks apuntando a objetos reutilizados. Conservá sinergias, afinidades y reglas de conteo al evolucionar. Reutilizá arte existente con indicación visual discreta y nombre nuevo.

Probá requisitos en ambos órdenes, cuatro espacios llenos, dos recetas listas, jefe previo, elección doble, orbitales ajenos, blancos muertos durante ráfaga/rebote y pool. La Pelota nunca entra en ciclos infinitos. Validá una evolución por personaje en Play Mode si está disponible y registrá cómo reproducirla sin contaminar futuras marcas.
```

### Prompt 5 — Modo infinito y estabilidad

```text
Aplicá la instrucción común. Implementá etapa 5 y sección 4 completa.

Agregá selección explícita Clásico/Infinito con perfil de reglas y configuración runtime por partida. Clásico conserva victoria a 240 s y Jefa a 180 s. Infinito supera 240 s, usa tramos de 120 s, curvas/topes y calendario de jefes del plan. No dupliques Main ni uses una duración artificialmente enorme. Reiniciar conserva modo/personaje; abandonar queda distinguido de derrota.

Separá política de modo y director compartido lo suficiente para no alterar el clásico. Componé Quilombo exactamente una vez. Escalá nuevos enemigos desde su base inmutable; el pool no acumula multiplicadores. Conservá enemigos activos al cruzar tramos y como máximo un jefe. POIs siguen siendo de un uso por partida.

Mostrá tiempo ascendente, tramo y próximo evento útil en infinito, eliminando mensajes de victoria y barras finitas inaplicables. Revisá todos los consumidores de DurationSeconds. Acotá gemas/XP y objetos activos conservando valor y sin puntuar despawns. Incluí presupuesto de proyectiles/VFX y registro de picos; optimizá búsquedas de blancos si la medición lo exige.

Probá 239,9/240/240,1 s, fronteras de tramos, 180/420/660 s, jefe anterior vivo, pausa, elecciones, pool, muerte, reinicio y cambio de modo. Hacé simulación extendida para lógica y un soak de al menos 30 minutos de simulación para estabilidad; medí además ejecución normal en build/hardware declarado cuando sea posible. Acelerar solo el reloj no valida combate, supervivencia ni rendimiento reales. Reportá diferencias entre pruebas aceleradas y reales.
```

### Prompt 6 — Puntaje y cierre de partida

```text
Aplicá la instrucción común. Implementá etapa 6, fórmula y registro de resultado de sección 5.1/5.2; todavía sin guardado de tabla.

Creá ScoreSystem con desglose y centipuntos long, multiplicador de Quilombo tomado en la muerte, categorías explícitas de enemigos y saturación aritmética. Asigná categorías en datos/componentes, nunca por nombre de GameObject. Conectá muerte válida y POI completado con eventos explícitos y ciclo de suscripción correcto. Diferenciá una nueva vida de un objeto del pool: una baja puntúa una vez por aparición, no una vez por instancia para siempre.

Sumá supervivencia por segundo completo activo y congelá todo fuera del gameplay. No sumes por daño bruto, XP, nivel, devolución al pool ni abandono. Generá un resultado inmutable e idempotente al cierre con runId, IDs de build y versiones del plan. Una sesión que usó F9, cheats de QA o controles de aceleración queda marcada como práctica para siempre. Las pruebas deben poder inyectar reloj sin producir récords.

Mostrá puntaje durante juego y desglose al terminar. Probá el ejemplo de 3240 puntos, valores fraccionarios, muerte simultánea, muerte/despawn, pool reutilizado, cambios de Quilombo, pausa/level-up, cierre doble, abandono y reinicio. Congelá puntaje y build en el mismo momento de cierre. Verificá que muerte del jugador y victoria en el mismo frame tengan una prioridad explícita y consistente.
```

### Prompt 7 — Récords locales

```text
Aplicá la instrucción común. Implementá etapa 7 y persistencia/consulta local de sección 5.2.

Guardá top 20 por modo y rulesetVersion y último resultado mediante JSON versionado en Application.persistentDataPath, sin referencias de Unity. Escritura atómica, respaldo y recuperación segura de archivo ausente/corrupto. Incluir build, desglose, elegibilidad y motivo de cierre. runId hace idempotente el guardado. No guardar récords de práctica o abandono. Mantener historial de versiones sin mezclar clasificaciones.

Agregá Récords al menú y vista de build de una marca; en derrota mostrar récord comparable y acceso a la tabla. Orden: puntos descendente, tiempo descendente, runId ordinal ascendente. Si se agrega un alias, aclarar que es un perfil local, sin cuentas online. No presentar esta tabla como ranking global.

Probá reiniciar la aplicación, cambios de modo/reglas, empates, límite de 20, registro duplicado, cambios de nombres visibles con IDs estables, JSON viejo/corrupto y fallo de escritura sin bloquear gameplay. Revisá navegación teclado/mouse, foco, retorno y legibilidad en cuatro resoluciones. Mostrá una partida completa desde menú hasta marca guardada y reintento.
```

### Prompt 8 — Validación y balance de entrega

```text
Aplicá la instrucción común. Cerrá etapa 8 sobre la implementación real de todas las etapas previas.

Revisá arquitectura y flujo completo; corregí defectos observados sin expandir contenido. Ejecutá checks previos pertinentes y pruebas nuevas de builds, evoluciones, infinito, puntaje y persistencia. Documentá cualquier adaptación de expectativas por reglas nuevas; no borres aserciones para ocultar regresiones.

Creá Docs/BuildsEInfinito-Validacion.md con commit, comandos/entradas reales, entorno, logs, resultados y pendientes. Incluí una matriz con dos personajes, ambos modos y al menos tres builds: Termo/Yerba, Pelota/Zapatillas y Pelota/Petardo/Antena. Diferenciá simulación de prueba humana. Registrá tiempo hasta primera sinergia/evolución, opciones inútiles, daño por arma si hay instrumentación, causas de muerte y relación puntos/minuto. No declares balance terminado por pasar checks.

Verificá clásico a 240 s, infinito sobre 30 minutos simulados, pausas, múltiples elecciones, jefes, POIs, pool, cierre único, prácticas excluidas y persistencia. Medí en equipo declarado FPS/frame time, GC y picos/crecimiento de enemigos, gemas, proyectiles y memoria. Generá build de la plataforma disponible e indicá aparte el estado de Windows. Probá una ejecución fuera del Editor si se puede.

Actualizá contexto y adenda del GDD documentando clásico de 4 minutos, infinito, nuevos sistemas y límites de competencia local. Entregá una guía de prueba humana de 15-20 minutos, incidencias priorizadas y resultado exacto de build; no inventes evidencia ni publiques.
```

### Prompt 9 — Ranking online, opcional y posterior

```text
Aplicá la instrucción común. Diseñá la segunda entrega online sobre el resultado local ya verificado. En esta etapa entregá especificación y plan técnico; no crees servicios remotos ni despliegues.

Inspeccioná si ya hay backend/hosting y proponé la alternativa de menor complejidad para este proyecto. Compará ranking casual y ranking con resultados verificados, aclarando qué ataques sigue permitiendo cada uno. No afirmes que un hash, seed, secreto dentro del cliente o validaciones de plausibilidad prueban una partida.

Definí contrato de inicio/cierre de sesión, idempotencia, identidad, top/posición propia, tablas por modo/rulesetVersion, práctica/abandono, validación de valores/duración, límites de frecuencia, funcionamiento offline, moderación de alias y tratamiento/borrado de datos. Separá credenciales de servidor de datos públicos del cliente.

Entregá un ADR concreto y prompts posteriores separados para backend, integración Unity y verificación completa con dos instalaciones, incluyendo duplicados, reintentos y rechazo de sesiones inválidas. Indicá las decisiones de proveedor/costo/cuenta que deban resolverse antes de ejecutar esos prompts. El objetivo es una clasificación compartida explícitamente casual, salvo que se acuerde implementar autoridad de simulación adicional.
```

## 8. Criterio de finalización

El cambio está listo cuando una persona puede elegir personaje y modo, construir al menos tres estilos útiles, entender qué sinergias activó, conseguir una evolución, continuar más allá de cuatro minutos en infinito, perder y consultar un récord persistente comparable. El clásico sigue cerrando correctamente, el pool no contamina estadísticas ni puntos y una partida larga mantiene objetos y recursos bajo control.

La calidad de las builds se valida jugando: si todas las partidas terminan eligiendo Aerosol y el resto solo aumenta números, hay un problema de balance aunque el código esté correcto. Antes de agregar contenido, revisar cobertura, frecuencia, oportunidades de contraataque y tiempo necesario para que cada combinación resulte útil.

## 9. Registro de ejecución por etapas

### Etapa 0 — 2026-11-03

**Comprobado:** auditoría sobre `7014ba49f0a8058fe394bb5d2c6a5efb837e34a0`, rama `codex/demo-cuatro-minutos`, árbol inicialmente limpio. Raíz Unity verificada: `Conurbano Infinito/`. El commit `606639c4...` de la sección 1 corresponde a la inspección original del plan; el baseline vigente de esta sesión está en [BuildsEInfinito-Baseline.md](BuildsEInfinito-Baseline.md).

Sin cambios de gameplay. Confirmados clásico 240 s/Jefa 180 s, dos personajes, siete armas/cuatro espacios, 32 cartas únicas; cada arma tiene 8 o 9 rangos dirigidos compatibles, suficientes para el nivel 8 propuesto. No se encontraron AGENTS.md/CLAUDE.md locales ni el GDD original; el informe diferencia evidencia del código y referencias indirectas al GDD.

Unity 6000.3.23f1 recompiló ambas asambleas propias y ejecutó en copia aislada CharactersCheck (ShortRun de ambos personajes), CombatFeaturesCheck, BallCheck, PitchCheck (Quilombo/POIs) y UiRedesignCheck: **cinco procesos con salida 0**, cuatro resoluciones, sin errores C#/gameplay/asserts reportados. SearchDatabase no se reprodujo. Integridad: 602 archivos de Assets/Packages/ProjectSettings sin cambios. Logs/capturas/comandos conservados localmente en `TestResults/BuildsEInfinito/Etapa0-20261103/` (ignorado por git); detalles, mensajes ambientales, reproducción y límites en el baseline.

**Diseño pendiente:** todas las reglas nuevas de las secciones 3–5 y etapas 1–9. Etapa 1 preparada para implementar: IDs/estado runtime/cálculo/nivel máximo 8, preservación de fracciones y APIs, guard de selección y adaptación explícita de fixtures (BallCheck hoy exige nivel visual 10). El orden de trabajo y la puerta de verificación de cada etapa están en el baseline. No se implementaron otras etapas ni se ejecutaron builds, mediciones de rendimiento, soak largo o partidas humanas. No se hizo push, merge, publicación ni despliegue.

### Etapa 1 — Estado de build y estadísticas — 2026-11-03

**Implementación:** sólo etapa 1, secciones 3.2/3.7, sin activar etapas 2–9. Base `7014ba49f0a8058fe394bb5d2c6a5efb837e34a0`, rama `codex/demo-cuatro-minutos`. Al comenzar ya estaban modificados `Docs/UnityProjectContext.md` y sin seguimiento `Docs/BuildsEInfinito.md` / `Docs/BuildsEInfinito-Baseline.md`; se preservó su contenido previo. Raíz verificada por Assets/Packages/ProjectSettings: `Conurbano Infinito/`; no se usó Assets exterior. No se encontró AGENTS.md/CLAUDE.md en repo o ancestros. Guías locales: contexto, baseline, PartidaCorta, UI y dirección artística; ninguna herramienta histórica de generación fue ejecutada.

Archivos relativos a la raíz Unity:

| Archivo | Responsabilidad implementada |
| --- | --- |
| `Assets/_Game/Scripts/Player/BuildState.cs` | Estado C# por sesión: armas, nivel, bonus generales/dirigidos, próximos ataques y orbitales. Sin progreso en ScriptableObjects. |
| `Assets/_Game/Scripts/Player/BuildStatistics.cs` | Fórmulas y compatibilidad dirigida compartidas; Explosive para radio y Ricochet para rebotes. |
| `Assets/_Game/Scripts/Player/BuildRules.cs` y `Assets/_Game/Resources/BuildRules.asset` | Un perfil inmutable, ID `rules.builds-v1`: nivel 8, intervalo 0,12 s, cantidad 12 y velocidad del jugador 10. Se carga desde Resources, sin cablear Main. |
| `Assets/_Game/Scripts/Player/WeaponController.cs` | Dueño del estado; conserva APIs legacy, cuatro espacios, snapshots, refresco de orbitales y programación de ataques. Listas y bonus utilizables también antes de Awake en checks existentes. |
| `Assets/_Game/Scripts/Player/PlayerController.cs` | Multiplicador runtime de velocidad separado de la base, cálculo común con tope; conserva movimiento y controles. |
| `Assets/_Game/Scripts/{Combat/WeaponData,Player/CharacterData,Player/UpgradeData}.cs` | Identidad de definición: nuevo `stableId` serializado y getter. Las 41 definiciones existentes sólo reciben esa línea. |
| `Assets/_Game/Scripts/Player/{UpgradeData,UpgradeSelection,GameHud}.cs` | Nivel único 1–8, elegibilidad y revalidación, guard de una elección por frame, HUD y cartas con nivel de arma/rango diferenciados. |
| `Assets/_Game/Editor/BuildStateCheck.cs` | Comprobación nueva de reglas e integración; reutiliza Main, assets, Input System y estilo de asserts del proyecto. |
| `Assets/_Game/Editor/{BallCheck,ExpansionCheck,PetardoCheck,CombatFeaturesCheck,Hito6Check,UiRedesignCheck}.cs` | Fixtures compatibles con siete mejoras dirigidas y espera de frame entre elecciones; se mantienen las aserciones de física/combate/pool/UI. |

Identidades explícitas de siete armas, dos personajes y 32 cartas (incluidas cuatro pasivas): `weapon.<GUID>`, `character.<GUID>` o `upgrade.<GUID>`. Se escribieron una vez desde el GUID existente, no se calculan desde nombres ni mediante AssetDatabase en runtime. No hay guardados anteriores de builds que migrar. Los `.meta` existentes no cambian; tampoco fileIDs, referencias, enums, escenas, prefabs, arte, paquetes, Input Actions ni ajustes. Se agregan cinco meta para cuatro scripts nuevos y el perfil.

**Reglas resultantes:**

- Equipar deja el arma en nivel 1, cada carta dirigida válida suma 1 y al llegar a 8 todas sus mejoras normales dejan de ofrecerse/aplicarse. El máximo individual de cada carta se conserva. La adquisición no suma otro nivel. `GetWeaponLevel` devuelve 0 si no está equipada; el método de Selection delega y el HUD lee al controlador.
- Daño = `(base + bonus general plano + bonus dirigido plano) × (1 + porcentaje)`. Chimichurri conserva +1 plano y Parrilla +20% una sola vez, en cualquier orden. El daño real conserva fracciones hasta Health; no se usa el getter entero legacy para disparar. Redondeos de presentación: RoundToInt en ese getter y Ceil con tolerancia en Health, como antes.
- Intervalo = base × factores multiplicativos globales × factores dirigidos, mínimo 0,12 s al final. Ataque pendiente ajustado por la proporción de intervalos efectivos, sin acelerarse más al llegar al mínimo. Giro orbital y radio conservan los multiplicadores anteriores.
- Cantidad calculada al disparar/reconciliar orbitales con máximo 12; velocidad del jugador calculada con máximo 10. Los límites numéricos no cambian los siete valores base ni los máximos numéricos alcanzables por las cartas anteriores. El **nivel 8 sí cambia el balance**, dejando 1 o 2 rangos dirigidos sin consumir respecto del máximo anterior 9/10.
- Bonus generales alcanzan armas adquiridas después; proyectiles existentes conservan snapshot. Orbitales refrescan daño/cadencia/giro y reconcilian cantidad sin recrearse cada frame. Reiniciar reconstruye estado conservando personaje.
- Select revalida máximo de carta/arma, permite una sola elección por frame y bloquea pausa/muerte. La cola se mantiene pausada hasta consumirla. Los checks que antes hacían varias selecciones síncronas esperan un frame real, incluido el fixture de cuatro pasivas de UI.
- Ball/Expansion/Petardo usan daño 3, cadencia 1 y rebotes/cobertura/radio 3: siete mejoras, nivel 8. Conservan sus escenarios de seis blancos únicos, ocho nubes y área con 150 blancos, respectivamente. La nueva comprobación también usa otras distribuciones legales y verifica máximos individuales, incluidas cadencias de tres rangos.

**Verificación comprobada de esta etapa:** Unity **6000.3.23f1** recompiló `Assembly-CSharp` y `Assembly-CSharp-Editor` en copia aislada, macOS **26.5.2 / Apple M3 / 8 GiB / Metal**. El original no se abrió con el Editor. Resultados finales, cada uno en proceso nuevo:

| Entrada real | Salida | Evidencia principal |
| --- | --- | --- |
| `BuildStateCheck.RunBatch` | 0, 14,31 s | 42 IDs únicos/estables; siete armas base y nivel 1→8; máximos de cartas; fórmulas/orden; nuevas armas con bonus; clamps y ataque pendiente; daño 1,2 real; snapshots; orbitales sin recreación por frame; cola/teclado+Select en el mismo frame/pausa/revalidación; reinicio con ambos personajes; definiciones inmutables; incluye Hito6. |
| `CharactersCheck.RunBatch` | 0, 19,02 s | Dos perfiles, primera oferta, navegación de selector, animación/pausa/reinicio y ShortRun de ambos. |
| `CombatFeaturesCheck.RunBatch` | 0, 15,69 s | Enemigos/Health/XP/pool, Expansion/Aerosol, Petardo, VisualSlice5 y ShortRun. |
| `BallCheck.RunBatch` | 0, 12,21 s | Nivel 8 legal para ambos; seis impactos mejorados únicos, rebotes/capas/alcance/pool y física/pausa reales. |
| `PitchCheck.RunBatch` | 0, 30,44 s | Quilombo/POIs, curación, seis órdenes, dos resistencias físicas de seis segundos, reinicio, 100 semillas e indicadores en cuatro resoluciones. |
| `UiRedesignCheck.RunBatch` | 0, 49,24 s | 32 cartas en cuatro resoluciones: 1280×720, 1024×768, 1920×1080, 2560×1080; texto sin desbordes, teclado/foco/pausa/reinicio/menús/resultados. |

Logs y capturas locales, ignorados por git: `TestResults/BuildsEInfinito/Etapa1-20261103/`. `results.json` contiene comandos/argv/rutas/salidas; `run_checks.py` reproduce la secuencia en la copia registrada en `project-path.txt`, y `run_ui.py` conserva la repetición final. `tested-source-sha256.json` / `final-integrity.json` registran **612 archivos fuente**, con **586 de Assets/Packages idénticos en original y copia final**; los ajustes del original siguen intactos, los de la copia son descartables porque los checks cambian preferencias. Verificación estática: 41 assets existentes sólo añaden stableId; 306 GUIDs de meta únicos, meta presentes, enums sin cambios y rutas protegidas sin diff. Baseline previo intacto y plan original preservado. `git diff --check` sin errores.

Se inspeccionó visualmente `ball/mejoras-pelota-1280x720.png`: “Arma 2/8” y “Rango 1/3” legibles y separados, conservando GameUi/plaza. El check de layout final dejó `ui/text-layout.txt` vacío. No se informan fallos C#/gameplay/asserts en las seis comprobaciones finales; SearchDatabase=0. Se mantienen mensajes ambientales de licencia/Metal/UI Toolkit/cierre en los logs.

**Intentos fallidos conservados:** `attempt1/` contiene: regresión de inicialización de lista antes de Awake detectada por QuilomboSliceCheck (corregida con lista inicializada al construir el componente y agregado runtime creado al necesitarlo); fixture nuevo que retenía referencias SO invalidadas al entrar en Play Mode (ahora compara por ruta recargada); y fallo de resolución Game View 642×1156 al pedir 1920×1080. `attempt2/` conserva una corrida con cinco checks aprobados y un fallo UI “Enter did not resume”: el fixture elegía cuatro pasivas en un frame y dejaba selecciones pendientes tras introducir el guard. Se adaptó para esperar el frame y comprobar consumo de cada elección; la repetición UI final pasó sin debilitar aserciones. Los cinco checks de gameplay no se repitieron después de esa última modificación exclusivamente al fixture UI; las asambleas finales fueron recompiladas por su corrida.

**Límites de evidencia:** probado en Editor y con fixtures, sin partida humana de balance, mouse físico completo, build del jugador, Windows, FPS/frame time/GC/memoria o soak largo. El caso de simultaneidad usa teclado del Input System y el mismo Select que llama mouse, no un clic físico. No se afirma rendimiento, diversión ni preparación de entrega completa. El tope 8 requiere evaluación humana; no se compensó cambiando daño/oleadas.

**Cómo reproducir verificaciones:** usar Unity 6000.3.23f1 con gráficos en una copia aislada del proyecto. Copiar Assets, Packages y ProjectSettings a un directorio vacío; abrir una vez con esa versión si no hay Library. Ejecutar un proceso nuevo por comprobación, sin `-quit` (sus métodos cierran el Editor al terminar):

```sh
UNITY=/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity
PROJECT=/ruta/absoluta/copia-unity
OUTPUT=/ruta/absoluta/resultados
mkdir -p "$OUTPUT"
"$UNITY" -batchmode -projectPath "$PROJECT" -executeMethod BuildStateCheck.RunBatch -logFile "$OUTPUT/build-state.log"
CONURBANO_CHARACTERS_OUTPUT="$OUTPUT/characters" "$UNITY" -batchmode -projectPath "$PROJECT" -executeMethod CharactersCheck.RunBatch -logFile "$OUTPUT/characters.log"
CONURBANO_COMBAT_OUTPUT="$OUTPUT/combat" "$UNITY" -batchmode -projectPath "$PROJECT" -executeMethod CombatFeaturesCheck.RunBatch -logFile "$OUTPUT/combat.log"
CONURBANO_BALL_OUTPUT="$OUTPUT/ball" "$UNITY" -batchmode -projectPath "$PROJECT" -executeMethod BallCheck.RunBatch -logFile "$OUTPUT/ball.log"
CONURBANO_PITCH_OUTPUT="$OUTPUT/pitch" "$UNITY" -batchmode -projectPath "$PROJECT" -executeMethod PitchCheck.RunBatch -logFile "$OUTPUT/pitch.log"
CONURBANO_UI_OUTPUT="$OUTPUT/ui" "$UNITY" -batchmode -projectPath "$PROJECT" -executeMethod UiRedesignCheck.RunBatch -logFile "$OUTPUT/ui.log"
```

No ejecutar estos checks sobre una sesión con trabajo sin guardar: cambian escenas, resolución, preferencias de la copia y estado de juego. `CONURBANO_ART_SAMPLE` debe estar ausente para la comprobación UI completa. Las primeras corridas se conservan aunque fallen; no se borran aserciones para dar verde.

**Cómo probar jugando:** abrir `Conurbano Infinito/`, cargar Menu e iniciar Play Mode. Con cualquiera de los dos personajes, adquirir un arma y elegir sus mejoras: empieza en N1, las cartas indican el próximo nivel de arma y el rango individual, y el HUD sube hasta N8. Una vez en N8 no deberían volver a salir cartas dirigidas de esa arma. Obtener Chimichurri/Yerba antes de equipar otra arma y confirmar que los bonus también la alcanzan; probar Fernets y notar cambios de giro/cadencia/cantidad sin cortes. Elegir con 1–3, flechas/Enter y mouse; al tener varias subidas, pulsar teclado y clic simultáneamente no debe consumir dos cartas. Esc durante una elección debe conservarla al continuar. R después de perder/ganar conserva personaje y limpia niveles/bonus; M vuelve a Menu. Clásico conserva victoria 04:00 y Jefa 03:00. Alcanzar N8 en una partida natural depende de XP/azar; el check automatizado lo fuerza en sesiones independientes para las siete armas, sin afirmar balance humano.

**Diseño pendiente:** etapas 2–9. Se mantiene la política anterior al agotar todo el catálogo (descarta elecciones sin recompensa); fallback explícito de curación/continuar, cupo de pasivas y Antena/Bolsa van en etapa 2. No se agregaron afinidades, sinergias, evoluciones, modo infinito, puntaje, récords o servicios. No se hizo push, merge, publicación ni despliegue.

### Etapa 2 — Pasivas y elecciones — 2026-11-03

**Alcance implementado:** etapa 2 y reglas 3.3/3.6, sobre `7014ba49f0a8058fe394bb5d2c6a5efb837e34a0`, rama `codex/demo-cuatro-minutos`, con todos los cambios locales previos de etapa 1 preservados. El estado inicial y diff están en `TestResults/BuildsEInfinito/Etapa2-20261103/initial-status.txt` / `initial.diff`; `initial-sha256.json` permite distinguir esta etapa del trabajo anterior. Se verificó la raíz `Conurbano Infinito/` por Assets, Packages y ProjectSettings/ProjectVersion.txt. No se encontró AGENTS.md/CLAUDE.md en repo/ancestros; se siguieron contexto, contrato, PartidaCorta y guías vigentes de UI/arte. No se trabajó en Assets exterior ni se ejecutaron generadores históricos.

**Comportamiento integrado:**

- Seis pasivas disponibles y **cuatro espacios**. Adquirir una ocupa un espacio; mejorarla conserva el mismo. Chimichurri, Yerba, Zapatillas y Alfajor se reconocen por sus efectos serializados existentes, conservan sus cinco rangos y efectos previos, incluida la curación al aumentar vida. No requieren reescribir sus assets. Tanto elegibilidad como aplicación revalidan el cupo y máximo de pasivas.
- **Antena de Televisión:** cinco niveles, +10% de radio de Petardo por nivel; máximo +50%, suma porcentual, no `1,1^5`. Se combina multiplicativamente con los factores dirigidos de radio existentes: `radio base × factores dirigidos × (1 + 0,1 × nivel de Antena)`. No modifica otros tamaños o armas.
- **Bolsa del Supermercado:** tres niveles; niveles 1/2 son inversión sin proyectil extra, nivel 3 agrega exactamente +1 a Termo y Boleadora. No duplica Petardo, Chancleta, Pelota, Aerosol ni Fernets. Conserva el tope global de doce proyectiles. El bonus también alcanza un arma compatible adquirida después, sin volver a aplicar cartas.
- Compatibilidades y umbral están en datos (`compatibleWeapons`, `unlockAtLevel`); los nombres visibles sólo se usan para presentación. Las descripciones indican Petardo o Termo/Boleadora. Se ofrecen únicamente con al menos un arma compatible equipada.
- Armas mantienen cuatro espacios, N1 al adquirir, máximo N8 y máximos individuales por carta. El catálogo de Main pasa de 32 a **34 cartas**, añadiendo únicamente dos referencias, sin regenerar escena, objetos o catálogo.
- Una única regla de elegibilidad en Selection filtra nulos, duplicados, cupos, compatibilidad y máximos; se vuelve a consultar al elegir. Se muestran tres cartas si hay tres candidatas, una/dos si no alcanza; se centran sin rellenar con duplicados. La primera elección sigue incluyendo Aerosol para El Pibe o Termo para La Piba, también con XP en ráfaga. No se garantizan sinergias ni ofertas de etapas posteriores.
- Con cero candidatas aparece una opción explícita: **Recuperar vida** o **Continuar** con vida llena. Cada elección cura `max(1, floor(vida máxima × 0,1))`, limitada por la vida faltante. No aumenta estadísticas permanentes ni da puntos. `Health.IsFull` consulta vida real, incluida fracción; 4,5/5 no se trata como llena aunque la presentación entera diga 5/5. El fallback es estado de UI, no un ScriptableObject temporal ni otra carta serializada.
- Se conserva cada subida en cola hasta elegir; una sola elección por frame, también para fallback. Esc pausa sobre la elección y continuar mantiene tiempo congelado. Cartas que dejan de ser válidas regeneran opciones sin consumir la subida; el fallback también revalida si aparecen candidatas. Muerte bloquea selección/curación; reinicio reconstruye inventario y niveles conservando personaje.
- IMGUI/GameUi mantienen fuente, paleta y escala. Cartas distinguen adquirir/mejorar, nivel resultante y rango dirigido; cabecera indica cupos y HUD muestra cuatro espacios de pasivas. Los iconos nuevos reutilizan los ya existentes; no se agregaron sprites ni se alteró la plaza irregular.

**Archivos relevantes**, relativos a la raíz Unity:

| Archivo | Responsabilidad |
| --- | --- |
| `Assets/_Game/Scripts/Player/BuildState.cs` | Inventario y niveles de pasivas por sesión, separado de definiciones. |
| `Assets/_Game/Scripts/Player/{WeaponController,BuildStatistics}.cs` | Cupo y máximos, registro único de pasivas, cálculo de radio/cantidad desde la build. Proyectiles conservan snapshot al crearse. |
| `Assets/_Game/Scripts/Player/UpgradeData.cs` | Reconocimiento compatible de pasivas previas, compatibilidad y umbral nuevos; enum conserva 0–10 y añade 11/12 al final. |
| `Assets/_Game/Scripts/Player/UpgradeSelection.cs` | Elegibilidad compartida, cola/revalidación/fallback y presentación. Niveles de pasivas delegan al controlador; no hay segunda copia de ese progreso. |
| `Assets/_Game/Scripts/Combat/Health.cs` | Getter de vida llena para distinguir déficit fraccional. |
| `Assets/_Game/Scripts/Player/{GameHud,GameUi}.cs` | Cuatro slots e iconos reutilizados. |
| `Assets/_Game/Data/{Antena de Televisión,Bolsa del Supermercado}.asset` y meta | Dos definiciones nuevas con IDs explícitos; referencias a armas existentes. |
| `Assets/_Game/Scenes/Main.unity` | Sólo dos líneas añadidas a `UpgradeSelection.upgrades`. |
| `Assets/_Game/Editor/PassiveSelectionCheck.cs` y meta | Casos de etapa 2, integración real, snapshots, dos builds y capturas/layout en cuatro resoluciones. |
| `Assets/_Game/Editor/{BuildStateCheck,Hito6Check,ExpansionCheck,PetardoCheck,BallCheck}.cs` | Expectativas de agotamiento adaptadas a elegir curar/continuar; conservan aserciones de combate y física. Hito6 reserva cupo para comparar los cuatro efectos legacy. |

No cambian GUIDs/meta existentes, fileIDs, nombres de campos serializados ni valores de enums previos. Los nuevos campos tienen defaults compatibles: efectos 0–3 pasan a inventario sin migrar assets, `compatibleWeapons` vacío y umbral 1. No existen guardados de builds previos que migrar. Menu, prefabs, arte, paquetes y ProjectSettings permanecen idénticos al estado recibido.

**Evidencia comprobada:** Unity **6000.3.23f1**, macOS **26.5.2 / MacBook Air Apple M3 / 8 GiB / Metal**, batchmode con gráficos y copia aislada registrada en `project-path.txt`. El Editor no abrió el proyecto original. Compiló runtime y Editor. Tabla de resultados finales:

| Entrada real | Salida | Alcance comprobado |
| --- | --- | --- |
| `PassiveSelectionCheck.RunBatch` | 0, 31.46 s | Cupos, candidatas 0–3, compatibilidad, bonus/umbral, snapshot, cola/doble entrada, pausa, muerte, reinicio, dos builds y UI. |
| `BuildStateCheck.RunBatch` | 0, 13.22 s | 44 IDs; siete armas base/N1→N8, máximos, cálculo, clamps, nuevas armas, snapshots/orbitales y Hito6 adaptado. |
| `CharactersCheck.RunBatch` | 0, 18.18 s | Dos perfiles, primeras ofertas, selección/animación/reinicio y ShortRun de ambos. |
| `CombatFeaturesCheck.RunBatch` | 0, 14.47 s | Doce ciclos/seis enemigos: Health/XP/pool; Aerosol, Petardo, VisualSlice5 y ShortRun. |
| `BallCheck.RunBatch` | 0, 12.71 s | Adquisición/mejoras N8 para ambos, rebotes/alcance/capas, seis blancos únicos y física/pausa/pool. |
| `PitchCheck.RunBatch` | 0, 31.85 s | Quilombo/POIs, curación, seis órdenes, resistencia física/reinicio, semillas e indicadores. |
| `UiRedesignCheck.RunBatch` | 0, 62.09 s | 34 cartas en cuatro resoluciones, navegación/foco/pausa/reinicio/menú/resultados; texto sin desbordes. |

Integridad: **618 archivos fuente** registrados; **592 archivos Assets/Packages idénticos en original y copia final**, 309 GUIDs meta únicos y meta previos intactos. Main sólo añade dos referencias; no hay cambios en prefabs, arte, paquetes o ajustes del original. `git diff --check` sin errores.


El check nuevo verifica catálogo 0/1/2/3 con duplicados/nulos; cuatro armas/pasivas; N8 y máximos de cartas; compatibilidad y niveles Antena/Bolsa; bonus a Termo adquirido después de Bolsa con Boleadora; exclusiones; composición de radio y snapshot; tercera XP en cola; teclado más el mismo Select usado por mouse en un frame; pausa/continuar; carta obsoleta al llenar el cuarto espacio; mínimo/rounding/vida fraccional; muerte sin resurrección; y reinicio de ambos perfiles. Construye dos especializaciones mediante elecciones reales con catálogo/XP controlados: Termo/Bolsa/Yerba y Petardo/Antena, acompañada por Boleadora/Bolsa. **Esto prueba posibilidad e integración, no balance ni una partida humana natural.**

UI probada en **1280×720, 1024×768, 1920×1080 y 2560×1080**. `UiRedesignCheck` recorre las 34 cartas; el check nuevo captura las dos pasivas, ambos equipamientos, curación y continuar. Se inspeccionaron visualmente ofertas 1024×768, Bolsa 1920×1080, HUD 1280×720, curación 2560×1080 y continuar 1024×768. Los archivos `text-layout.txt` finales no registran desbordes. Se conserva la excepción conocida de `UnityEditor.Search.SearchDatabase`, contabilizada separadamente; no se atribuye a gameplay ni se la oculta.

**Intentos y correcciones conservados:** `attempt1/` registra un fixture que suponía un proyectil base de Boleadora; se corrigió a base 2 y bonus +1, sin tocar el arma. `attempt2/` conserva el primer check nuevo aprobado y el fallo del check anterior por comparar tiempos float con igualdad exacta. `attempt3/build-state-diagnostic.log` muestra intervalo 0,12 y tiempos representados idénticos; se usa `Mathf.Approximately` para el tiempo pendiente, manteniendo igualdad del mínimo y la aserción funcional. No se cambió la fórmula runtime para dar verde. La inspección visual detectó descripciones vacías por dos puntos sin comillas en el YAML nuevo: se citaron ambos textos y se añadió aserción de importación. `attempt3/` conserva además CombatFeaturesCheck fallido por esperar descarte automático de cartas agotadas; Expansion/Petardo/Ball ahora exigen fallback explícito y su resolución antes de proseguir. No se eliminaron aserciones de física, daños, rebotes o pool.

Evidencia local ignorada por git en `TestResults/BuildsEInfinito/Etapa2-20261103/`: logs/capturas, `results.json` con argv/salidas, `run_checks.py`, snapshot inicial, `stage2.diff`, `tested-source-sha256.json`, `static-integrity.json` y `final-integrity.json`. Integridad final y límites se registran junto a los resultados; las preferencias de la copia son descartables porque los checks cambian resolución y estado de Editor.

**Reproducción:** crear una copia vacía de Assets, Packages y ProjectSettings desde `Conurbano Infinito/`, mantener Unity 6000.3.23f1 y gráficos habilitados. Un proceso nuevo por check, sin `-quit` (las entradas cierran el Editor). No usar una sesión con trabajo sin guardar ni ejecutar menús de Setup/Apply/Generate. Para el check nuevo:

```sh
UNITY_BIN=/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity
QA_PROJECT=/ruta/absoluta/copia-unity
QA_OUTPUT=/ruta/absoluta/resultados
mkdir -p "$QA_OUTPUT/passives"
CONURBANO_PASSIVES_OUTPUT="$QA_OUTPUT/passives" "$UNITY_BIN" -batchmode -projectPath "$QA_PROJECT" -executeMethod PassiveSelectionCheck.RunBatch -logFile "$QA_OUTPUT/passives.log"
```

Las otras seis entradas y variables son las mismas de etapa 1; `run_checks.py` de esta etapa las ejecuta en orden después del check nuevo y se detiene al primer fallo. `CONURBANO_ART_SAMPLE` debe estar ausente para revisar todas las cartas en UI. `results.json` conserva las rutas reales usadas.

**Cómo probar jugando:** abrir Menu en la raíz Unity correcta y entrar con cualquiera de los dos personajes. Para tiros, conservar Termo (o adquirirlo con La Piba), elegir Bolsa hasta N3 y Yerba: debe haber un proyectil más, sin ocupar otro espacio de arma. Adquirir Boleadora después también recibe +1. Para área, adquirir Petardo y buscar Antena: el radio aumenta con cada nivel, hasta +50%; Bolsa no multiplica explosiones. Al reunir cuatro pasivas sólo vuelven a salir mejoras de las propias; observar cuatro slots y sus niveles. Confirmar primera oferta de cada perfil, elección por 1–3/flechas/Enter/mouse, Esc y reanudación sin perder elecciones, y R después de morir para limpiar inventario. Agotar la build para ver curación/continuar; como depende de XP/azar en clásico, el check automatizado lo reproduce con catálogos acotados. No cambiar assets para forzarlo. No se garantiza ninguna de estas builds en las primeras tres ofertas.

**Pendiente y fuera de esta etapa:** balance humano (incluido coste de Bolsa N1/N2 y frecuencia de Petardo/Antena), prueba completa con mouse físico, build del jugador, Windows, FPS/frame time/GC/memoria/soak. No se afirma evidencia de estos puntos. Afinidades, sinergias, evoluciones, infinito, puntaje y récords siguen como diseño de etapas 3–9; no están implementados ni garantizados por esta etapa. No se hizo push, merge, publicación ni despliegue.
