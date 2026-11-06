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


### Etapa 3 — Afinidades y sinergias — 2026-11-03

Cierre de evidencia: 2026-11-04.

**Alcance implementado:** únicamente etapa 3, secciones 3.1/3.4. Base `7f45fa049507560a0e923fc44b8293810ff0deef`, rama `main`, árbol inicialmente limpio; etapas 1/2 ya integradas. Raíz Unity verificada con Assets/Packages/ProjectSettings: `Conurbano Infinito/`. No hay AGENTS.md/CLAUDE.md en repo/ancestros; se siguieron contexto, baseline, PartidaCorta y guías locales de UI/arte. El adjunto se usó como contrato de diseño para esta etapa, sin ejecutar sus otros prompts. No se modificaron escenas, prefabs, arte, paquetes, ajustes, Assets exterior ni meta existentes; no se ejecutaron herramientas históricas de regeneración.

**Comportamiento nuevo:**

- **El Pibe:** +10% de daño sólo para las definiciones etiquetadas explícitamente como proyectil directo (Termo y Boleadora). Se suma al porcentaje general: `(base + planos) × (1 + porcentaje general + 0,10)`. Con Parrilla es ×1,30, no ×1,32. Daño float hasta Health; el getter entero legacy conserva su redondeo y DamageMultiplier sigue representando únicamente el bonus general.
- **La Piba:** +1 rebote sólo si el patrón del arma es Ricochet. Pelota base pasa de tres a cuatro blancos; no cambia pierce de Boleadora, Straight, Returning ni otras armas. Ambos personajes conservan vida, velocidad, arma inicial, primera oferta, retrato y animaciones. El selector explica la afinidad.
- **Mate de Guardia:** Termo N4 + Yerba N3. Cada cuarto ataque del Termo agrega un proyectil. Un ataque es una llamada de Fire con blanco válido, sin contar por separado cada proyectil. Con varios proyectiles se incrementa una sola vez; rige el máximo global de doce.
- **Potrero Veloz:** Pelota N4 + Zapatillas N3. Velocidad de proyectil ×1,20, compuesta con mejoras dirigidas. Una activación, sin acumular multiplicadores al mejorar otra carta. Las Pelotas ya activas conservan su velocidad anterior.
- **Festejo del Barrio:** Pelota N4 + Petardo N4. Cada quinto impacto directo válido de Pelota prepara **una** carga. Cuentan rebotes e impactos letales sobre blancos previamente vivos. No cuentan duplicados del mismo proyectil/blanco, blancos ya muertos, capa ajena, daño rechazado ni explosiones/orbitales. Mientras está lista no se guardan impactos para otra carga. Se consume al comenzar el siguiente ataque de Petardo, que recibe radio ×1,25 en todos sus proyectiles; el ataque siguiente vuelve al radio normal. Compone con Antena y mejoras dirigidas de radio.
- Tres recetas editables con IDs/niveles explícitos; no se deduce ninguna regla desde nombres visibles. Se evalúan al adquirir/mejorar armas o pasivas, no por frame, y se activan una sola vez por sesión en cualquier orden de adquisición. Contadores a cero al activar y al reiniciar; no se otorgan puntos.
- GameHud encola avisos breves y los presenta al reanudar gameplay después de una elección. Una etiqueta pequeña señala **Festejo listo · Petardo +25%**. Esc abre afinidad y las tres recetas con requisitos, efecto y estado activo/pendiente; conserva los botones y navegación actuales. No hay otro panel grande durante combate.

**Archivos relevantes**, relativos a la raíz Unity:

| Archivo | Responsabilidad |
| --- | --- |
| `Assets/_Game/Scripts/Player/SynergyData.cs` y `Assets/_Game/Resources/Synergies/` | Tres definiciones SO inmutables de sesión: IDs de receta/requisitos, niveles, frecuencia, multiplicador y textos; carga/validación como las reglas existentes. |
| `Assets/_Game/Scripts/Player/BuildSynergies.cs` y `BuildState.cs` | Activaciones, contadores y carga runtime; sin scripting genérico ni estado de partida en assets. |
| `Assets/_Game/Scripts/Player/{WeaponController,BuildStatistics}.cs` | Personaje de sesión, evaluación de requisitos, fórmula de afinidad, velocidad, rebotes y snapshot por ataque. |
| `Assets/_Game/Scripts/Combat/Projectile.cs` | Origen de arma/controlador por disparo; notificación local sólo después de daño directo aceptado. OnDisable limpia ambos al volver al pool; explosiones y orbitales no notifican procs. No se agrega un bus de combate ni un callback por proyectil. |
| `Assets/_Game/Scripts/{Player/CharacterData,Combat/WeaponData}.cs` | Campos nuevos compatibles: affinity=None y directProjectile=false. Sólo los dos personajes y Termo/Boleadora reciben un campo adicional en sus assets. |
| `Assets/_Game/Scripts/Player/{GameHud,GameManager,MenuScreen}.cs` | Avisos, carga, consulta en pausa y afinidad en selector, reutilizando GameUi/IMGUI. Suscripción de avisos retirada en OnDisable. |
| `Assets/_Game/Editor/SynergyCheck.cs` y meta | Prueba nueva con elecciones reales, fixtures dirigidos, colisiones físicas, pausas, pool y capturas/layout. |
| `Assets/_Game/Editor/{BuildStateCheck,BallCheck}.cs` | Expectativas adaptadas al +10% del Pibe y al rebote adicional de La Piba, preservando pruebas de fracciones, blancos únicos, alcance, capas, física y pool. |

GUIDs/fileIDs/campos anteriores y valores de WeaponPattern/UpgradeEffect intactos. Nuevo CharacterAffinity con None=0 para definiciones previas no migradas; no se renombra ni convierte ningún campo. Sin guardados anteriores de builds que migrar. Nuevos meta: dos scripts runtime, un check, carpeta de sinergias y tres assets (siete); los existentes permanecen idénticos. No requiere cableado en Main/Inspector.

**Validación y alcance de la evidencia:** Unity **6000.3.23f1**, macOS **26.5.2 / MacBook Air Apple M3 / 8 GiB / Metal**. Batchmode con gráficos, copia aislada indicada en `TestResults/BuildsEInfinito/Etapa3-20261103/project-path.txt`; el proyecto original no se abrió en el Editor. Compilación de Assembly-CSharp/Assembly-CSharp-Editor y pruebas en sesiones nuevas. Antes de editar pasaron BuildStateCheck (71,15 s incluyendo importación inicial) y BallCheck (14,96 s) sobre el baseline, ambos salida 0; ambos reprodujeron la excepción conocida del Editor SearchDatabase.

| Entrada real | Salida final | Alcance principal |
| --- | --- | --- |
| `SynergyCheck.RunBatch` | 0, 25.54 s | Afinidades/órdenes/activación única, ataques/impactos/carga, composición, pool, impacto letal, física/pausa, reinicio e inmutabilidad; UI cuatro resoluciones. |
| `PassiveSelectionCheck.RunBatch` | 0, 32.73 s | Cupos, compatibilidad, Antena/Bolsa, cola, doble entrada, fallback, pausa, muerte/reinicio y cuatro resoluciones. |
| `BuildStateCheck.RunBatch` | 0, 12.12 s | IDs/niveles, fórmula con afinidad, fracciones, límites, snapshot/orbitales, selección y Hito6. |
| `CharactersCheck.RunBatch` | 0, 19.66 s | Ambos perfiles, primera oferta, selector/animaciones/reinicio; ShortRun de ambos personajes. |
| `CombatFeaturesCheck.RunBatch` | 0, 15.01 s | Doce ciclos/seis enemigos, Health/XP/pool, Expansion/Petardo/VisualSlice5/ShortRun. |
| `BallCheck.RunBatch` | 0, 14.83 s | Ambos personajes, 6/7 impactos mejorados únicos; base de La Piba con cuatro colisiones reales, pausa, alcance, capas y pool. |
| `PitchCheck.RunBatch` | 0, 32.63 s | POIs, resistencia física de seis segundos, órdenes, curación/Quilombo/reinicio e indicadores en cuatro resoluciones. |
| `UiRedesignCheck.RunBatch` | 0, 64.76 s | 34 cartas en cuatro resoluciones, foco/teclado, selector, menús, pausa, resultados y layout sin desbordes. |

**Ocho entradas aprobadas** en procesos nuevos; sin errores C#, de gameplay o asserts reportados en los resultados finales. Cada proceso registró una excepción conocida de SearchDatabase, también presente en los dos procesos de baseline. Las dos repeticiones de `final-targeted/` sólo cambian el fixture/registros de Editor, sin cambios runtime o de valores de assets. `final-import/` vuelve a aprobar SynergyCheck tras limpiar únicamente espacios finales en siete YAML/meta nuevos.


SynergyCheck cubre las siete armas con ambos personajes, etiquetas explícitas y exclusividad Ricochet; seis combinaciones de orden para las tres recetas; umbrales, aviso único y nuevas mejoras sin reactivación; ocho ataques de Termo con varios proyectiles y tope 12; velocidad/snapshot de Potrero; quinto impacto, duplicados, muertos, capa ajena, carga única sin acumular impactos, consumo del ataque completo, composición con Antena/radio dirigido, exclusión de explosiones, limpieza de origen y reuso de la misma instancia, impacto letal y bloqueo con dueño muerto. La física real comprueba cuatro blancos con La Piba, carga en el quinto impacto y pausa de proyectil/carga. Reinicia ambos personajes y compara serialización de definiciones antes/después para descartar mutación de SO.

**UI:** 1280×720, 1024×768, 1920×1080 y 2560×1080. El check nuevo captura recetas activas/pendientes, carga lista y aviso de Mate; el existente recorre las 34 cartas, selector y flujos de menú/pausa/resultados. Inspección visual de pausa activa a 1024×768, requisitos a 1920×1080 y carga a 1280×720; también selector 1024×768 y aviso de Mate 1280×720. Los informes finales `text-layout.txt` no registran desbordes. El uso de XP, catálogos y blancos controlados demuestra integración, no una build obtenida naturalmente, diversión o balance.

**Incidencias preservadas:** `attempt1/` contiene la primera corrida: reglas, combate y física aprobados, pero texto de Festejo listo de tres líneas en un área para dos a 1024×768. Se reemplazó el texto de carga en pausa por una frase corta, conservando el requisito y el efecto; no se agrandó el panel ni se redujo tipografía. La repetición aprobó las cuatro resoluciones. Después se precisó la prueba del cuarto/quinto impacto tras consumir carga, añadiendo un impacto letal; se repitieron los dos checks de Editor afectados. La excepción `UnityEditor.Search.SearchDatabase` también aparece en el baseline y se contabiliza separadamente en cada proceso; no se atribuye a gameplay ni se oculta. Se conservaron mensajes ambientales de licencia/Metal/dotnet/cierre.

**Evidencia local, ignorada por git:** `TestResults/BuildsEInfinito/Etapa3-20261103/`: snapshot y HEAD/status iniciales, baseline, intentos, logs, capturas, comandos/argv/salidas (`results.json`), scripts de reproducción, hashes de fuentes e integridad. `final-targeted/` contiene la repetición final de sinergias/Pelota después de precisar el fixture y su registro; las otras seis suites no requirieron repetición porque runtime/assets permanecieron idénticos. La comparación final confirma 597 archivos Unity de Assets/Packages idénticos en original y copia probada (se excluyen ocho `.gitkeep`, ignorados por Unity). La integridad estática verifica 316 GUIDs meta únicos, meta existentes intactos, cuatro assets previos con una sola línea añadida, enums serializados conservados y ninguna modificación en rutas protegidas. `git diff --check` sin errores. Las preferencias/ajustes de la copia son descartables porque los checks cambian resoluciones y estado del Editor.

**Reproducción:** copiar Assets, Packages y ProjectSettings de `Conurbano Infinito/` a un directorio vacío; no usar una sesión con trabajo sin guardar. Unity 6000.3.23f1, gráficos habilitados, sin `-nographics`, sin `-quit` (cada entrada cierra el proceso). No ejecutar menús Setup/Apply/Generate. Prueba nueva:

```sh
UNITY_BIN=/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity
QA_PROJECT=/ruta/absoluta/copia-unity
QA_OUTPUT=/ruta/absoluta/resultados
mkdir -p "$QA_OUTPUT/synergies"
CONURBANO_SYNERGIES_OUTPUT="$QA_OUTPUT/synergies" "$UNITY_BIN" -batchmode -projectPath "$QA_PROJECT" -executeMethod SynergyCheck.RunBatch -logFile "$QA_OUTPUT/synergies.log"
```

Las otras siete entradas/variables son las registradas en etapa 2. `run_checks.py` ejecuta las ocho en orden con salida por proceso y se detiene al primer fallo; `results.json` conserva las rutas reales. `CONURBANO_ART_SAMPLE` debe estar ausente para comprobar todas las cartas.

**Cómo probar jugando:** abrir Menu en la raíz correcta. El selector informa la afinidad; entrar con El Pibe para Termo/Boleadora +10%, o La Piba para Pelota con un rebote más. Reunir Termo N4/Yerba N3 y observar un proyectil extra cada cuatro ataques; reunir Pelota N4/Zapatillas N3 y notar la velocidad adicional; reunir Pelota N4/Petardo N4, golpear cinco veces con Pelota (cuentan rebotes) y observar carga lista y la siguiente explosión ampliada. Esc muestra las recetas antes y después de activarse. Elegir con teclado o mouse, pausar durante una elección y volver; no deben perderse elecciones. R al terminar limpia sinergias/carga y conserva personaje; M permite cambiarlo. Los requisitos no se garantizan en las primeras ofertas y obtenerlos en clásico depende de XP/azar. Para reproducir todos sin cambiar assets usar el check aislado, que fuerza progresión; no es una sesión de competencia ni una partida humana.

**Límites y diseño pendiente:** no se ejecutaron build del jugador, Windows, mouse físico completo, balance humano, FPS/frame time/GC/memoria ni soak; no se afirma evidencia de esos puntos. Evaluar frecuencia de ofertas y tiempo hasta sinergia jugando antes de ajustar balance. Etapas 4–9 siguen como diseño: no se implementan evoluciones, infinito, puntaje, récords ni servicios. No hubo push, merge, publicación ni despliegue.


### Etapa 4 — Evoluciones — 2026-11-04

**Alcance implementado y comprobado:** únicamente etapa 4, sección 3.5 y su integración con las etapas 1–3. HEAD `7f45fa049507560a0e923fc44b8293810ff0deef`, rama `main`; se recibió el trabajo local de etapa 3 sin commit (17 archivos modificados y sus scripts/datos nuevos). Se preservó ese trabajo, incluyendo sus registros históricos. Raíz Unity confirmada por Assets/Packages/ProjectSettings y ProjectVersion: `Conurbano Infinito/`, Unity 6000.3.23f1. No se encontró AGENTS.md/CLAUDE.md en repo/ancestros; se siguieron contexto, baseline, PartidaCorta y guías locales de UI/arte. No se ejecutaron los otros prompts del adjunto.

**Comportamiento:**

- Dos recetas/cartas `UpgradeData` en `Assets/_Game/Resources/Evolutions/`, editables en Inspector: familia objetivo, pasiva requerida, efecto y parámetros. Exigen arma N8 (máximo del perfil), su pasiva al máximo y una Jefa derrotada en esta sesión. El evento de muerte real de WaveDirector llega a GameManager/WeaponController; el logro persiste aunque falten arma o pasiva. Desactivar/despawnear al jefe no otorga el logro. Suscripciones equilibradas en enable/disable y retiradas al morir el jefe.
- Una receta lista ocupa la primera opción de la siguiente elección; las restantes opciones siguen siendo cartas distintas válidas, hasta tres. Si hay dos listas, orden ordinal de ID: `evolution.pelota-final` antes de `evolution.termo-acero`. Tras consumir la primera, la segunda aparece en la siguiente selección, también si ya estaba en cola. Cada receta se aplica una sola vez; pausa, revalidación, fallback y guardia de doble entrada se conservan.
- La evolución reemplaza **la identidad runtime del mismo espacio** mediante `BuildState.Weapon.Evolution`; conserva la definición de familia `Data`, nivel N8, bonus, cadencia pendiente y contadores. No se duplican WeaponData ni armas. `GetEquippedWeapon` conserva su contrato de definición de familia; `GetEvolution`, `GetWeaponIdentity` y `GetWeaponDisplayName` exponen evolución, ID y nombre actuales. Esto mantiene compatibles pasivas, afinidades, checks y las tres sinergias por familia. Arma base y mejoras normales dejan de ofrecerse; ni la evolución ni mejorar otras armas alteran orbitales ajenos.
- **Termo de Acero Inoxidable:** dos pulsos separados por 0,12 s activos; mismo intervalo entre inicios de ráfaga. El segundo usa 50% del daño del ataque, sin redondear. Conserva cantidad normal de proyectiles; el extra de cada cuarto ataque de Mate sólo aparece en el primero. Los dos pulsos comparten el tope global de doce proyectiles por ataque: se prioriza el primero y se limita el segundo al presupuesto restante. El catálogo actual no llega a saturarlo; cantidades forzadas por QA no permiten superar doce. Dirección, daño, velocidad, cantidad y pierce pendientes son snapshots al iniciar la ráfaga. El segundo pulso sale desde la posición actual del jugador en esa dirección; no retiene blanco ni callbacks de proyectiles. Si muere el blanco, el pulso puede salir sin buscarlo. Pausa y elección congelan el tiempo; muerte, desactivación, cierre y recarga cancelan el pulso pendiente. No se acumula otra ráfaga sobre un pulso pendiente.
- **Pelota de Final de Barrio:** suma dos rebotes a los comprados y a la afinidad exclusiva Ricochet de La Piba. Prioriza blancos vivos no visitados; cuando no encuentra otro a alcance, puede volver **una vez** al primer blanco, después de al menos otro impacto válido. Ese retorno consume el presupuesto finito de impactos, causa daño y cuenta para Festejo; no borra el conjunto de visitados ni inicia ciclos. El primer blanco debe seguir vivo, activo y en la misma vida (`Health.LifeId`, incrementado al reiniciar para pool). Morir o reutilizarse impide el retorno. Pool limpia origen, primer blanco, permisos y contadores del proyectil.
- Arte existente, ligero tinte dorado de proyectiles, borde dorado y `E 8` en HUD; tooltip con el nombre nuevo. Se conserva IMGUI/GameUi, tipografía, teclado/mouse y disposición irregular de la plaza. Los proyectiles anteriores a evolucionar conservan su snapshot.

**Archivos relevantes**, relativos a la raíz Unity:

| Archivos | Cambio de esta etapa |
| --- | --- |
| `Scripts/Player/UpgradeData.cs`, `Resources/Evolutions/{PelotaDeFinal,TermoDeAcero}.asset` dentro de `Assets/_Game/` | Recetas/cartas con IDs estables, validación al cargar y efecto EvolveWeapon=13 al final del enum. Nuevos campos con defaults compatibles; ninguno previo renombrado. |
| `Assets/_Game/Scripts/Player/{BuildState,BuildStatistics,WeaponController}.cs` | Identidad evolucionada y logro de sesión, snapshot/pulso pendiente, bonus de rebotes y APIs compatibles. |
| `Assets/_Game/Scripts/Player/{UpgradeSelection,GameHud,GameManager}.cs` | Oferta garantizada, presentación de carta/equipamiento y recepción del logro del jefe. |
| `Assets/_Game/Scripts/Enemy/WaveDirector.cs`, `Scripts/Combat/{Projectile,Health}.cs` dentro de `Assets/_Game/` | Evento de jefe derrotado, retorno único y generación de vida para no confundir una instancia reutilizada con el blanco original. |
| `Assets/_Game/Editor/EvolutionCheck.cs` y su meta | Check nuevo de reglas, integración Play Mode, física, cancelación, pool, entrada y layout. |
| `Assets/_Game/Editor/BuildStateCheck.cs` | Expectativa y registro de 46 IDs (antes 44): las dos identidades nuevas, conservando las aserciones anteriores. |
| `Assets/_Game/Editor/CombatFeaturesCheck.cs` | Sincroniza dos muestras de pausa con el límite de frame; conserva sprite/posición y añade estabilidad de normalizedTime. Corrección acotada de fixture, sin cambiar animación runtime. |

Sin cambios adicionales de esta etapa en escenas, prefabs, arte, datos existentes, paquetes, ajustes, Assets exterior ni meta previos. Se agregan dos assets, su carpeta y un check (cuatro meta nuevos, **320 GUIDs únicos** en total). Se conservan GUIDs, fileIDs, campos y todos los valores anteriores de enums. No se requiere migración de assets existentes ni cableado en Main: Resources incorpora las recetas al flujo actual. La comparación SHA-256 contra el árbol recibido valida la preservación de esas rutas, incluyendo el trabajo previo de etapa 3.

**Validación realmente ejecutada:** Unity **6000.3.23f1**, macOS **26.5.2**, **Apple M3 / 8 GiB / Metal**, batchmode con gráficos en copia aislada. Runtime y Editor recompilaron; original sin abrir en Unity. Un proceso nuevo por entrada:

| Entrada real | Salida / segundos | Alcance |
| --- | --- | --- |
| `EvolutionCheck.RunBatch` | 0 / 22,95 | Dos recetas; cuatro órdenes de requisitos/jefe y umbral de pasiva aislado, doble consumo, cuatro espacios, tres ofertas distintas/garantía/cola, teclado, orbitales, ráfaga/tope compartido, retorno, pool, reinicio vivo y UI. |
| `SynergyCheck.RunBatch` | 0 / 23,05 | Ambas afinidades, tres sinergias/órdenes, contadores, cargas, snapshot, física/pausa/pool y UI. |
| `PassiveSelectionCheck.RunBatch` | 0 / 23,15 | Cupos, compatibilidad, pasivas, selección, fallback, cola, pausa/muerte/reinicio y UI. |
| `BuildStateCheck.RunBatch` | 0 / 12,10 | 46 IDs, siete familias N1→N8, cálculos/clamps, snapshots/orbitales y Hito6. |
| `CharactersCheck.RunBatch` | 0 / 18,53 | Perfiles, selección/animaciones/reinicio y ShortRun de ambos. |
| `CombatFeaturesCheck.RunBatch` | 0 / 17,47 | Health/XP/pool de seis enemigos, Expansion/Petardo/VisualSlice5/ShortRun. |
| `BallCheck.RunBatch` | 0 / 14,00 | Adquisición/mejoras y rebotes, blancos únicos, capas, alcance, física, pausa y pool. |
| `PitchCheck.RunBatch` | 0 / 33,15 | POIs, Quilombo, curación, órdenes, resistencia física, reinicio e indicadores. |
| `UiRedesignCheck.RunBatch` | 0 / 42,47 | 34 cartas normales, cuatro resoluciones, foco/teclado, pausa/reinicio/menú y resultados. Las dos cartas de evolución las recorre el check nuevo. |

**Nueve suites aprobadas** con el tope de ataque compartido. Las primeras cinco corresponden a la última corrida completa; después de corregir el fixture de pausa se reejecutaron combate y las tres entradas restantes en `final-remainder/`, sin más cambios runtime/assets. Se conservan además las corridas completas y repeticiones previas. Resultados finales sin errores C#, de gameplay ni asserts reportados. En cada proceso se conserva una excepción conocida de `UnityEditor.Search.SearchDatabase`, también registrada en la evidencia previa de etapa 3; se contabiliza aparte, junto con mensajes ambientales de licencia/Metal/dotnet/cierre. No se ocultaron errores ni eliminaron aserciones.

El check nuevo aplica las cartas mediante XP/selección reales y verifica una evolución con cada personaje: Termo con El Pibe y Pelota con La Piba. Para los requisitos usa los dos órdenes arma/pasiva, jefe previo/posterior, arma N7 y pasiva N4 no elegibles. Comprueba recetas listas simultáneas, tres ofertas distintas, cola de dos, teclado 1 y Select coincidentes, bloqueo en pausa, rechazo de adquisición/mejoras base y aplicación doble. Conserva N8, estadísticas/intervalo y las tres sinergias, con orbitales ya activos. Cuatro ataques de Termo prueban Mate sólo en primer pulso, pausa y elección con pulso pendiente, snapshot tras bonus posterior, blanco destruido, muerte y reinicio vivo con pulso en vuelo. También se fuerza cantidad +100 y se comprueba que los dos pulsos juntos nunca exceden doce. Un caso aislado por receta comprueba pasiva N4 rechazada/N5 válida con arma N8 y jefe ya derrotado. Pelota prueba +2 y afinidad/bonus comprados, velocidad Potrero, duplicados, retorno único, muertos, cambio de vida y Festejo. Física real verifica A→B→A con pausa; reuso de la misma instancia de proyectil comprueba limpieza. Las definiciones SO se comparan antes/después y permanecen inmutables. **Los fixtures controlan XP, blancos y el Tick del jefe; esto no es evidencia de progresión natural, supervivencia humana ni balance.**

UI nueva comprobada en **1280×720, 1024×768, 1920×1080 y 2560×1080**; informes finales de layout vacíos. Se inspeccionaron visualmente ambas cartas a 1024×768, oferta de tres cartas a esa resolución y HUD evolucionado a 1280×720. Se conserva tipografía, no se reduce texto para hacerlo caber. Integridad final: **604 archivos de Assets/Packages idénticos entre original y copia probada**, excluidos ocho `.gitkeep` ignorados por Unity; meta existentes y rutas protegidas intactos. `git diff --check` aprobado.

**Intentos conservados:** `attempt1/` registra un error del check nuevo por acceder a BuildStatistics internal desde Assembly-CSharp-Editor; el fixture pasó a filtrar por el enum público. `attempt2/` conserva el primer recorrido de requisitos/cola/ráfaga aprobado y una expectativa que omitía los rebotes dirigidos comprados al subir Pelota a N8. `attempt3/` ya aprueba las dos mecánicas/física, pero falla por descripciones cortadas (ocho casos de layout en cuatro resoluciones). Se acortó la copia de ambas cartas manteniendo información esencial y sin agrandar panel ni bajar fuente. Las corridas completas y repeticiones ampliadas se conservan en `before-attack-cap/`. La revisión final precisó el tope de doce por ataque completo (no por pulso): se limitó el segundo al presupuesto restante y se agregó el caso de cantidad +100; se repitieron las nueve suites sobre esa fuente final.

**Incidencia de pausa del check anterior:** la segunda corrida completa se detuvo en `Dog animates during pause`; una sonda posterior se detuvo antes en `Pause advances player animation`. Se aisló un muestreo anterior a la evaluación del Animator del mismo frame: con timeScale=0 y reloj/posición estables, normalizedTime avanzaba exactamente el delta pendiente del clip. En el candidato: jugador 0,41009602→0,415124118 con delta 0,003519664 (clip 0,7 s); perro 0,32200563→0,32503587 con delta 0,0015151219 (clip 0,5 s). Una copia nueva reconstruida desde los hashes iniciales también mostró ese delta pendiente (perro 0,340776026→0,3445576 con delta 0,00189079507), previo a etapa 4. Según cruce o no una clave de sprite, la aserción fallaba o pasaba. Se corrigió únicamente CombatFeaturesCheck para muestrear tras un límite explícito de frame, manteniendo las aserciones de sprite/posición y agregando normalizedTime estable; no se agregó un sleep de asentamiento ni se tocaron clips/prefabs/runtime. La copia inicial con fixture corregido aprobó. Las sondas temporales se eliminaron; `pause-hypotheses.md` registra experimentos y logs. El primer intento de reutilizar la Library de QA de etapa 3 no compiló su caché de paquetes; se conserva como intento ambiental inutilizable, no como resultado de gameplay. La comparación válida usa copia nueva de fuentes iniciales, versiones intactas.

**Evidencia local ignorada:** `TestResults/BuildsEInfinito/Etapa4-20261104/`: HEAD/status/diff y SHA iniciales, `attempt1/2/3`, `before-attack-cap/` (incluye sus repeticiones dirigidas), `results-full-run.json` (resultados finales consolidados), `final-remainder/` (combat/Pelota/POIs/UI tras corregir el fixture), logs/capturas finales, sondas y baseline de pausa, scripts de reproducción, entorno, hashes y controles de integridad. `project-path.txt` conserva la copia usada. `stage4-runtime-and-checks.diff` permite distinguir las modificaciones sobre las fuentes recibidas de etapa 3; el diff inicial conserva el trabajo local ajeno.

**Reproducción segura:** copiar Assets, Packages y ProjectSettings de `Conurbano Infinito/` a un directorio vacío; usar exactamente Unity 6000.3.23f1, con gráficos habilitados, sin `-nographics` ni `-quit` (la entrada cierra el proceso). No ejecutar Setup/Apply/Generate ni usar una sesión con trabajo sin guardar. El check altera su sesión de prueba y preferencias de Editor; no guarda escenas ni debe ejecutarse sobre una futura partida de competencia.

```sh
UNITY_BIN=/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity
QA_PROJECT=/ruta/absoluta/copia-unity
QA_OUTPUT=/ruta/absoluta/resultados
mkdir -p "$QA_OUTPUT/evolutions"
CONURBANO_EVOLUTIONS_OUTPUT="$QA_OUTPUT/evolutions" "$UNITY_BIN" -batchmode -projectPath "$QA_PROJECT" -executeMethod EvolutionCheck.RunBatch -logFile "$QA_OUTPUT/evolutions.log"
```

El runner guardado ejecuta las nueve entradas en procesos nuevos y se detiene al primer fallo. `CONURBANO_ART_SAMPLE` debe estar ausente para revisar todas las cartas normales. La reproducción aislada no escribe marcas; todavía no existen puntaje/persistencia de etapas 6/7. No se agregó un cheat de evolución al juego. El marcado de práctica de futuras sesiones de QA sigue siendo contrato pendiente de etapa 6.

**Cómo probar jugando:** desde Menu elegir El Pibe; subir Termo a N8 mediante siete mejoras dirigidas, Yerba a N5, derrotar a La Jefa (aparece a 03:00) y recoger XP para otra elección. Elegir Termo de Acero Inoxidable; observar dos pulsos, segundo más débil, y cada cuarto ataque de Mate con extra sólo en el primero. Para La Piba, adquirir Pelota, llevarla a N8 y Zapatillas a N5, matar a La Jefa y elegir Pelota de Final de Barrio en la subida siguiente. Con pocos blancos cercanos puede verse el retorno tras otro impacto; con más enemigos prioriza los no visitados. El logro del jefe vale aunque se lo mate antes de completar los niveles. Mirar borde dorado/`E 8` y pasar mouse por el espacio para leer el nombre. Esc y una elección deben congelar los pulsos; al reiniciar con R se limpian evolución, logro y pendientes. Los niveles y otra subida posterior al jefe no están garantizados dentro de los cuatro minutos: la prueba aislada reproduce ambos casos sin alterar assets ni futuras partidas.

**Límites:** esta etapa no implementa infinito, puntaje, récords, ranking ni contenido adicional. No se generó build de jugador, ni se probó Windows, mouse físico completo, balance humano, FPS/frame time/GC/memoria o soak. Sigue pendiente medir cuánto tarda una evolución en una partida normal antes de cambiar balance. No hubo commit, push, merge, publicación ni despliegue.


### Etapa 5 — Modo infinito y estabilidad — 2026-11-04

**Alcance implementado:** únicamente etapa 5/sección 4, sobre HEAD `7f45fa049507560a0e923fc44b8293810ff0deef` y el árbol local recibido de etapas 3–4, preservado. El usuario confirmó esta etapa en la sesión. Remoto `FranciscoLang2209/conurbano-infinito`, raíz real `Conurbano Infinito/`, Unity 6000.3.23f1. No se encontraron AGENTS.md/CLAUDE.md en repo/ancestros. Se leyeron contexto, baseline, PartidaCorta y guías locales de UI/arte. El adjunto se usó como contrato de diseño; no se ejecutaron los otros prompts. Sin commit, push, merge, publicación ni despliegue.

**Comportamiento e integración:**

- Selector existente de dos personajes con botones **Clásico · 4 min / Infinito**, usando GameUi/IMGUI. Mouse, Tab/flechas/Enter y acceso rápido al botón de jugar; permanece el foco previo de Volver/A la plaza. Menú informa ambos modos. Reinicio conserva personaje y modo, y limpia sesión, sinergias, evoluciones, logro de jefe, POIs, pool y contadores. Una aplicación nueva comienza en Clásico.
- `RunRules` contiene dos definiciones SO en `Resources/RunRules/{Clasico,Infinito}.asset`, IDs `mode.classic-v1` y `mode.endless-v1`. `GameManager.Configuration` posee un **snapshot inmutable** de escalares por partida: ningún reloj, enemigo, contador o selección se escribe en los assets. No se duplica Main. El campo serializado legacy `durationSeconds` permanece intacto y sigue gobernando el plazo clásico; en infinito `HasTimeLimit=false` y `DurationSeconds=0` indica ausencia de plazo. Se revisaron sus consumidores runtime: GameManager y GameHud; los checks previos siguen probando el clásico.
- Clásico conserva victoria a 240 s, fases/mezcla/estadísticas originales y Jefa única a 180 s. Infinito mantiene la apertura y supera 240 s; el reloj es activo, congelado en pausa/elección. `GameState.Abandoned=4` se añade al final: reiniciar/volver al menú/cerrar una sesión activa emite abandono una vez; salir después de derrota/victoria conserva ese cierre. No hay puntaje ni récords todavía.
- En infinito, tramo 1 a 240 s, 2 a 360 s, etc.: vida nueva ×(1+0,20c), velocidad ×min(1+0,02c;1,30), intervalo final 0,57 / min(1+0,08c;2) / Quilombo, piso 0,20 s. **Decisión de balance de esta etapa:** el plan no cuantificaba el crecimiento del cupo; se usa ×(1+0,08c), editable mediante `pressurePerSegment`, luego Quilombo y techo 150 comunes. Quilombo sigue siendo `1 + Current/100 + bonusUmbral`, compuesto exactamente una vez. Antes de 240 s se conservan las fases vigentes. Son curvas iniciales, no balance humano aceptado.
- Jefa a 180/420/660/900… s. El director consume cada ventana incluso con la anterior viva: no acumula deuda, no duplica jefes y no crea élites. Si QA salta ventanas, sólo considera la última cruzada. Enemigos activos conservan vida/velocidad y posición al cambiar de tramo. Spawn calcula desde los campos base del prefab, sin modificarlos: bonus de vida y factores de movimiento son runtime y se limpian en reutilización. Vida máxima redondeada hacia arriba con tolerancia de 0,0001 para ruido float y saturación en int.MaxValue; daño de contacto/ataques y XP base no cambian. El perro también escala la longitud visible de su embestida. Configuración síncrona antes de asignar blancos y antes del próximo paso físico; no se cambia orden de ejecución del proyecto.
- HUD infinito muestra tiempo ascendente, Apertura/Tramo y el próximo evento entre tramo/Jefa; si la Jefa vive, lo informa. No muestra meta 04:00 ni rojo de últimos treinta segundos. Conserva barra de vida de jefe, equipo, pasivas y carga de Festejo. Se amplió cuatro píxeles la caja de texto de la Jefa al detectar recorte a 1024×768, conservando la fuente. Los POIs siguen de un uso por partida y advierten que Quilombo aumenta presión; comunicar puntos corresponderá a etapa 6, cuando existan.

**Presupuestos y política de objetos:** el único ObjectPool de Main comparte **256 gemas y 256 proyectiles activos**, editables en cada perfil y aplicados también al clásico. Los proyectiles de jugador, orbitales, piedras del Gomero, explosiones y sus visuales comparten el segundo cupo. Al saturarlo se omiten nuevas emisiones sin retirar las existentes; arma/ráfaga/consumo de sinergia conservan su reloj, y los orbitales faltantes pueden crearse cuando vuelve a haber sitio. Todos los llamadores runtime manejan el rechazo. Los impactos/avisos existentes pertenecen a actores y conservan un visual por actor/ataque y su duración; no se añade un emisor libre de VFX. Jefa mantiene un aviso, cancelado al morir/desactivar. No se despawnean comunes vivos para hacer espacio ni se emite muerte/XP al devolverlos.

Cuando hay 256 gemas, cada drop nuevo agrega **todo** su valor a la gema activa más cercana al lugar de muerte, incluso si queda lejos del jugador. No hay caducidad ni descarte de XP; recoger esa gema entrega el acumulado una vez. `ExperienceGem.experienceValue` pasa de int a long **no serializado** y `ExperienceSystem` conserva la API int añadiendo una entrada long. CombatFeaturesCheck consulta el valor público long, conservando su aserción de XP exacta. El pool registra activos/picos/instancias creadas/drops agrupados/tiros omitidos por enable/disable, incluso si se desactiva un objeto por fuera de Return. F3 amplía el diagnóstico existente, oculto inicialmente en Editor/Development Build; también muestra pico de frame y memoria asignada. El pool retiene el pico por prefab de un catálogo finito; estos cupos no son un límite de memoria en MiB ni una garantía de 60 FPS.

**Archivos relevantes**, relativos a la raíz Unity:

| Archivos | Responsabilidad |
| --- | --- |
| `Assets/_Game/Scripts/Player/RunRules.cs` y `Resources/RunRules/` | Definiciones editables, modo seleccionado y configuración inmutable/reglas de tramos. |
| `Scripts/Player/{GameManager,MenuScreen,GameHud}.cs` dentro de `Assets/_Game/` | Selección, reloj sin plazo, abandono, HUD y navegación. |
| `Assets/_Game/Scripts/Enemy/{WaveDirector,SpawnManager,EnemyMovement,MosquitoChase,EnemyCharge,EnemyRanged}.cs`, `Scripts/Combat/Health.cs` | Calendario/cupos y escala desde base por aparición, limpieza de reutilización. |
| `Assets/_Game/Scripts/Combat/{ObjectPool,PooledObject}.cs`, `Scripts/Enemy/ExperienceDrop.cs`, `Scripts/Player/{ExperienceGem,ExperienceSystem,WeaponController,PerformanceOverlay,RiskZone}.cs` | XP agrupada, presupuesto compartido, rechazo seguro, picos y advertencia de presión. |
| `Assets/_Game/Editor/EndlessCheck.cs` y meta; `CombatFeaturesCheck.cs` | Check nuevo y adaptación compatible de la lectura de XP. |

**Verificación realmente ejecutada:** baseline CombatFeaturesCheck sobre copia nueva del estado recibido, salida 0 (61,89 s incluyendo importación). Candidato en copia independiente, Unity 6000.3.23f1/macOS 26.5.2/Apple M3/8 GiB/Metal, con gráficos. Runtime y Editor compilaron. Diez procesos nuevos aprobados:

| Entrada real | Salida | Segundos |
| --- | ---: | ---: |
| `EndlessCheck.RunBatch` | 0 | 39.84 |
| `EvolutionCheck.RunBatch` | 0 | 20.67 |
| `SynergyCheck.RunBatch` | 0 | 22.17 |
| `PassiveSelectionCheck.RunBatch` | 0 | 23.39 |
| `BuildStateCheck.RunBatch` | 0 | 12.08 |
| `CharactersCheck.RunBatch` | 0 | 19.26 |
| `CombatFeaturesCheck.RunBatch` | 0 | 17.58 |
| `BallCheck.RunBatch` | 0 | 14.68 |
| `PitchCheck.RunBatch` | 0 | 34.02 |
| `UiRedesignCheck.RunBatch` | 0 | 68.19 |

EndlessCheck verifica 239,9/240/240,1; fronteras 240/360/1800; composición real de Quilombo; cupo de 150 comunes más una Jefa; omisión sin deuda y logro de jefe; enemigos activos intactos; doce ciclos por cada uno de los seis prefabs; despawn sin XP; 6.000 drops de 33.000 XP conservados y cobrados una vez; rechazo/reutilización/doble devolución de proyectiles; teclado/ambos modos, pausa con varias elecciones, muerte, reinicio, abandono y cuatro resoluciones. Capturas de selector/HUD/pausa en 1280×720, 1024×768, 1920×1080 y 2560×1080; informe final de layout vacío. Se inspeccionaron selector y HUD a 1024×768, además de la build nativa. Los checks anteriores mantienen física, niveles, sinergias/evoluciones y regresiones de ambos personajes; Pitch incluye seis órdenes de visita y dos resistencias físicas de seis segundos.

**Soak acelerado:** 1.800,726 segundos de simulación activa, 7.406 frames renderizados, 17,14 s de pared, 123 elecciones resueltas; tramo final 14. `Time.captureDeltaTime=1/30`, timeScale=8, física automática con su paso fijo habitual, sin F9. Jugador quieto con vida 1.000.000, cuatro armas y XP controlada cada 15 s activos; el resto del combate/spawn/pool/colisiones/ataques corre normalmente. Pico de 18 enemigos, 256 gemas y 19 proyectiles, 314 instancias creadas; no excedió cupos ni cerró por victoria. El cupo extremo 151 se prueba aparte: este soak no sostuvo esa densidad. CSV por minuto conserva objetos/memoria; pool final estable en 314. Memoria administrada antes/después de colección final sólo para diagnóstico: 769.966.080/745.095.168 bytes del proceso Editor (incluye fixtures y herramientas). No se usa esa cifra como memoria de player ni se afirma ausencia total de fugas. **No es supervivencia humana, 30 minutos reales ni medición de FPS.**

**Evidencia e integridad:** `TestResults/BuildsEInfinito/Etapa5-20261104/` (local, ignorado): HEAD/status/diff/hashes iniciales, baseline, cinco intentos conservados, runner, resultados de diez suites, logs, capturas/layout, soak/retención, scripts y copia separada para build, métricas de player y controles SHA-256. `stage5-existing-scripts.diff` separa esta etapa de cambios previos. Los 613 archivos Assets/Packages importados coinciden entre original y copia de tests (sin ocho .gitkeep); 325 GUIDs meta únicos. Ninguna escena, prefab, asset previo, meta previo, arte, paquete, ProjectSettings o Assets exterior cambió respecto del árbol recibido. Sólo se agregaron dos perfiles, carpeta, script y check con cinco metas. `git diff --check` aprobado. Unity nunca abrió el original.

Se conserva una excepción conocida de `UnityEditor.Search.SearchDatabase` por proceso, también presente en el baseline; sin errores C#/gameplay/asserts en resultados finales. Intentos 1–3 ajustaron sincronización física y entrega de teclado del fixture al patrón existente; intento 4 conserva un clic sintético IMGUI que no se entregó en batchmode (se retiró esa afirmación del check automatizado y se probó mouse nativo después). Intento 5 completó el primer soak y detectó layout: vida 10.000 usada por QA recortada en HUD normal y altura insuficiente de la Jefa. El fixture de capturas conserva ahora vida normal y usa invulnerabilidad temporal; la caja de la Jefa ganó altura sin achicar texto. No se quitaron aserciones para aprobar.

**Reproducción segura:** copiar Assets, Packages y ProjectSettings a un directorio vacío. Editor 6000.3.23f1, con gráficos, sin `-nographics` ni `-quit`; cada entrada cierra el proceso. No ejecutar Setup/Apply/Generate ni usar una sesión con trabajo sin guardar. El check cambia su propia sesión, preferencias de Editor y configuración temporal de QA; no escribe assets ni marcas.

```sh
UNITY_BIN=/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity
QA_PROJECT=/ruta/absoluta/copia-unity
QA_OUTPUT=/ruta/absoluta/resultados
mkdir -p "$QA_OUTPUT/endless"
CONURBANO_ENDLESS_OUTPUT="$QA_OUTPUT/endless" CONURBANO_ENDLESS_SOAK=1 "$UNITY_BIN" -batchmode -projectPath "$QA_PROJECT" -executeMethod EndlessCheck.RunBatch -logFile "$QA_OUTPUT/endless.log"
```

`run_checks.py` y `results.json` registran las diez entradas/variables/argv/rutas exactas; `candidate-project-path.txt` identifica la copia. CONURBANO_ART_SAMPLE debe estar ausente para revisar todas las cartas normales. F9 continúa siendo sólo aceleración del reloj de QA: no sustituye este soak. El marcado permanente de práctica pertenece a etapa 6; todavía no hay marcas persistidas.

**Cómo probar jugando:** abrir Menu, Jugar, elegir personaje y modo con mouse o Tab/flechas/Enter y pulsar A la plaza. Clásico: La Jefa a 03:00 y victoria a 04:00 aunque siga viva. Infinito: superar 04:00, observar Tramo 1; 06:00 inicia Tramo 2. Los encuentros siguientes de Jefa son a 07:00 y 11:00; si la anterior vive se omiten. Completar lugares para aumentar presión, sin recompensas repetidas al cruzar tramos. Esc y elecciones deben congelar reloj/ataques; volver con una cola pendiente debe mantenerla. Reiniciar con R desde pausa/fin mantiene modo/personaje y limpia build, POIs y logro del jefe; M vuelve al menú para cambiar modo. F3 muestra cupos/picos. Las gemas agrupadas pueden concentrar varias subidas al recogerlas. El check aislado fuerza fronteras y cupos sin editar assets; no representa progresión natural ni balance aceptado.

**Límites de alcance:** etapas 6–9 siguen como diseño: sin puntaje, resumen inmutable, récords ni servicios online. Balance humano, supervivencia natural prolongada y Windows siguen pendientes; sólo está instalado MacStandaloneSupport. La búsqueda de blancos conserva OverlapCircleAll: se midió GC agregado, pero no se identificó esa consulta como cuello de botella ni se hizo una optimización sin evidencia. No se actualizaron Unity/paquetes ni se migró IMGUI.


**Build y ejecución real fuera del Editor:** `Builds/Mac/Etapa5-20261104-QA/ConurbanoInfinitoQA.app`, macOS **universal x86_64+arm64 / Mono / Development**, Menu→Main. BuildPipeline.BuildPlayer devolvió **Succeeded**, cero errores y un warning; tamaño del reporte 333.406.026 bytes, duración interna 112,99 s / proceso 152,40 s. Se usó otra copia de fuentes, con dos scripts de QA temporales exclusivos de esa copia: una entrada de build y una sonda de ejecución, inactiva sin `-stage5-probe`. No se agregan al gameplay ni al repo. La build es de QA local, sin notarización ni validación Windows. El warning fue token vacío para símbolos de Unity Cloud Diagnostics; el postproceso automático informó que no podía subir símbolos por falta de credenciales. No se incorporaron credenciales ni se publicó nada.

Con la Mac desbloqueada, se comprobó por UI nativa **mouse** en Jugar, personaje, Infinito, A la plaza y Volver al menú; la selección amarilla y el HUD Infinito/Apertura fueron observados. Esc pausó y R conservó La Piba/modo al reiniciar. Se cerró la aplicación. Este recorrido breve no representa una partida humana completa. El intento de acceso previo al desbloqueo quedó registrado; la primera sonda de rendimiento tuvo visibilidad no confirmada, por eso se repitió con ventana visible y combate observado.

Sonda visible final (`standalone-visible/`), Apple M3 / 8 GiB / macOS 26.5.2 / Metal / ventana 1280×720 / VSync=1 / targetFrameRate=-1, **timeScale=1 y sin captureDeltaTime/F9**:

| Métrica | Resultado medido |
| --- | ---: |
| Ventana de medición | 60,014 s / 3.591 frames |
| FPS promedio | 59,84 |
| Frame p50 / p95 / p99 | 16,66 / 17,66 / 17,80 ms |
| Pico de frame | 99,00 ms |
| Pico desde preparación: enemigos / gemas / proyectiles | 151 / 256 / 28 |
| Enemigos en muestras de un segundo | 30–66 |
| Pool creado al final | 459, estable en muestras finales |
| GC Allocated In Frame, media / máximo | 7.754 / 77.030 bytes por frame |
| GC asignado total en ventana | 27.845.647 bytes |
| Memoria asignada Unity, inicio / fin | 103.937.063 / 104.544.035 bytes |
| Memoria administrada inicio / fin / tras colección diagnóstica final | 5.373.952 / 6.213.632 / 5.640.192 bytes |
| Reloj activo observado | 1804,991 → 1865,011 s |

**Límite del escenario:** se prepararon 150 comunes y un jefe, Quilombo 100, build de cuatro armas mejoradas, vida elevada, jugador quieto y elección de cartas desactivada sólo en la sonda. Tras preparar la densidad, spawn/daño/física/efectos corren a velocidad real. El combate reduce esa población: **no es un minuto con 150 comunes constantes**, ni una prueba humana o de 30 minutos reales. Los 59,84 FPS de promedio y el pico de 99 ms no certifican 60 FPS sostenidos. No se atribuyó el GC a OverlapCircleAll sin un perfil de llamadas; no se cambió la búsqueda a ciegas.

Al cerrar el player apareció `GarbageCollector disposing of ComputeBuffer`. Se lanzó y cerró la **demo Mac ya existente de 2026-10-16**, también Unity 6000.3.23f1: reproduce el mismo aviso (`old-demo-player.log`), por lo que el aviso antecede a esta etapa. Su causa no se resolvió aquí. Se conservan además los diagnósticos de memoria al cierre de Development Build, sin presentarlos como prueba de ausencia de fugas. No hubo excepciones de gameplay durante las dos sondas. La prueba antigua sólo establece el antecedente de ese aviso, no un baseline equivalente de rendimiento.

Para repetir la build instrumentada, copiar únicamente `Stage5BuildQa.cs` a Editor y `Stage5RuntimeProbe.cs` a Scripts **en una copia aislada**, desde la evidencia local; ejecutar `Stage5BuildQa.BuildMac` con `CONURBANO_STAGE5_APP` apuntando a un destino nuevo fuera de Assets. Los argv y salidas reales están en `build-mac-result.json` y `standalone-visible/result.json`; el ejecutable se invoca con `-stage5-probe /ruta/resultados`, se mantiene la ventana visible y sale solo tras la medición. Los scripts completos quedan conservados para revisar/reproducir la preparación. No distribuir esa sonda como build de entrega. Sin el flag, la app abre Menu y no aplica preparación de QA.


### Etapa 6 — Puntaje y cierre de partida — 2026-11-04

**Alcance implementado:** únicamente etapa 6, confirmada por el usuario, sobre HEAD `3e43deb1bed42e853fe635960f9f373414d92bcc`/main, árbol recibido limpio y etapas 1–5 integradas. Remoto y raíz Unity verificados: `FranciscoLang2209/conurbano-infinito`, `Conurbano Infinito/`. No se encontró AGENTS.md/CLAUDE.md en repo/ancestros; se consultaron contexto, baseline, PartidaCorta y las guías de UI/dirección artística. El adjunto y las secciones anteriores siguen siendo contrato de diseño: no se ejecutaron los demás prompts. No hubo commit, push, merge, publicación ni despliegue.

**Puntaje y eventos implementados:**

- `ScoreSystem`, C# sin MonoBehaviour ni referencias a assets, pertenece a cada GameManager. Conserva centipuntos `long`, bajas y centipuntos por categoría, cantidad/puntos de POIs y segundos/puntos de supervivencia. Las sumas y la conversión de segundos saturan en `long.MaxValue`. Mostrar total = división entera por 100; el desglose conserva centésimas, sin redondear cada baja.
- Categorías explícitas nuevas: `EnemyScoreCategory.Common=0`, `Special=1`, `Boss=2`. Los seis prefabs sólo agregan `scoreCategory` al ExperienceDrop existente: Mosquito/Paloma/Bolsa comunes, Perro/Gomero especiales, Jefa jefe. No hay élites identificadas ni categoría/recompensa ficticia para ellas. Nombres de GameObject, arte, XP y daño no determinan puntaje.
- Una baja común agrega `10 × factor`, especial `30 × factor`, jefe `1000 × factor`, en centipuntos. Factor entero = `100 + round(clamp(Quilombo,0,100))`, redondeo al entero más cercano con mitades alejándose de cero: Q=10,49 → 110; Q=10,5 → 111. Equivale a redondear el multiplicador a centésimas. Se toma el Quilombo vigente dentro del callback de muerte, antes de cualquier recompensa posterior.
- ExperienceDrop escucha Health.Died durante su aparición, notifica EnemyKilled una vez antes del drop y limpia sus oyentes al desactivarse. SpawnManager conecta ese evento por aparición y lo reenvía; GameManager suscribe/desuscribe con OnEnable/OnDisable. La reutilización del pool reinicia la aparición; el guard local y Health evitan muerte duplicada. Despawn/devolución no emiten puntaje. No se mantienen registros ilimitados de instancias muertas.
- `RiskZone.RewardEarned` se emite una vez cuando se gana la recompensa del desafío (StartReward), no al terminar la duración de un buff. Agrega 250 puntos sin multiplicador. Tiene guard de recompensa previa, jugador vivo y tiempo activo; GameManager conserva además un conjunto acotado de zonas premiadas. El preview avisa +250 y que Quilombo aumenta presión y puntos por bajas. No se reinician POIs ni se alteran sus recompensas anteriores.
- Supervivencia agrega un punto por segundo completo del mismo reloj activo de GameManager. Pausa, cola de elecciones y estados terminales no suman; tampoco daño bruto, XP, nivel, fallback o abandono. Abandonar conserva lo ya acumulado, sin premio adicional y sin elegibilidad para récord.

**Cierre y práctica implementados:**

`GameManager.CloseRun` es el único cierre de derrota/victoria/abandono: detecta QA, congela Score, copia la build y establece Result antes de notificar observadores. StopGame cancela combate y pulsos pendientes después de tomar el snapshot. StateChanged/RunClosed se emiten con el resultado ya disponible; un cierre reentrante o repetido no lo reemplaza ni notifica dos veces. Pausa no cierra. R/M/cerrar jugando generan abandono; salir después de derrota/victoria conserva el cierre. Reiniciar genera otra identidad y estado nuevo, manteniendo modo/personaje.

**Prioridad explícita:** la victoria clásica se confirma en LateUpdate, después del daño de los Update y pasos de física del frame. Una muerte antes de esa confirmación gana y cierra derrota, incluso si Update ya alcanzó 240 s. Las fuentes actuales de daño no trabajan en LateUpdate (el otro LateUpdate propio es CameraFollow). Después del cierre se ignoran muertes/puntos tardíos: el snapshot no se reabre. Los fixtures síncronos ShortRunCheck/Hito4Check/EndlessCheck ahora ejecutan también esa fase, sin retirar sus aserciones de victoria y detención.

`RunResult` y sus elementos son valores inmutables sin referencias Unity: schemaVersion=1, runId GUID, personaje/modo por ID, GameMode, rulesetVersion, Application.version como versión de build, inicio UTC, segundos activos, centipuntos/puntos, desglose por categoría/POIs/supervivencia, Quilombo final, familias de armas/niveles/IDs de evolución, pasivas/niveles, sinergias y motivo de cierre/práctica. Nombres visibles se copian sólo para la presentación. Las colecciones son de sólo lectura y no exponen las listas runtime. `RunRules.rulesetVersion` tiene default compatible **builds-infinito-1**, copiado a RunConfiguration; las definiciones SO no contienen estado de partida. No se escribió JSON ni se implementó su persistencia: la adaptación de este snapshot al esquema guardado pertenece a etapa 7.

Práctica es irreversible dentro de la sesión. **Toda sesión de Editor y todo batchmode** comienza como práctica, incluidos checks históricos/manuales y sus cheats F6/F7. En player se marca al usar F9, observar timeScale distinto de 0/1 o captureDeltaTime, alterar el reloj o modificar vida inicial/invulnerabilidad del jugador. `SetElapsedForPractice` valida e inyecta reloj marcando primero; `MarkPractice(motivo)` es la entrada obligatoria para herramientas nuevas de QA antes de modificar estado. Apagar F9/restaurar valores no recupera elegibilidad. Sólo derrota/victoria sin práctica son elegibles para la futura tabla; abandono nunca. Esto no es antitrampas ni detecta modificación arbitraria del binario/memoria; futuras herramientas deben usar el contrato explícito. F3 sólo consulta diagnóstico y no invalida.

**Presentación y archivos:** GameHud agrega una placa pequeña de puntos debajo de Quilombo, con etiqueta Práctica cuando corresponda. GameManager mantiene GameUi/Atkinson, botones R/M, flechas/Tab/Enter y mouse; el fin muestra total, desglose y tiempo/Quilombo congelados. La lista lateral muestra armas/evoluciones, pasivas, sinergias y bajas; rueda del mouse o RePág/AvPág permiten recorrerla. Se miden filas con la misma fuente/escala y sólo se dibujan filas completas dentro del panel, sin reducir texto por longitud. GameUi extrae su selección de estilo para compartir la medición, conservando rasterización y mínimo físico existentes. No requiere Inspector ni cambios de Main/Menu.

Archivos relevantes, relativos a la raíz Unity: `Scripts/Player/{ScoreSystem,RunResult,GameManager,GameHud,RunRules,GameUi,RiskZone}.cs`, `Scripts/Enemy/{ExperienceDrop,SpawnManager}.cs`, `Scripts/Combat/Health.cs` (dos getters de lectura para QA), todos dentro de `Assets/_Game/`; seis prefabs con una línea adicional cada uno; `Assets/_Game/Editor/ScoreCheck.cs` y los tres fixtures mencionados. Dos scripts runtime y un check nuevos, con sus tres meta. Ningún asset SO, escena, arte, meta previo, paquete, ajuste o Assets exterior fue modificado. GUIDs/fileIDs/campos previos y valores de enums existentes se conservan. No se ejecutaron generadores históricos.

**Verificación comprobada:** Unity **6000.3.23f1**, macOS **26.5.2 / Apple M3 / 8 GiB / Metal**, gráficos habilitados, proyectos independientes creados copiando Assets/Packages/ProjectSettings. Unity nunca abrió el original. Baseline CombatFeaturesCheck sobre el estado recibido: salida 0, **74,90 s** incluyendo importación. Runtime y Editor del candidato compilaron; once procesos nuevos finales aprobados:

| Entrada real | Salida | Segundos |
| --- | ---: | ---: |
| `ScoreCheck.RunBatch` | 0 | 22.96 |
| `EndlessCheck.RunBatch` | 0 | 18.18 |
| `EvolutionCheck.RunBatch` | 0 | 21.13 |
| `SynergyCheck.RunBatch` | 0 | 22.45 |
| `PassiveSelectionCheck.RunBatch` | 0 | 24.49 |
| `BuildStateCheck.RunBatch` | 0 | 12.96 |
| `CharactersCheck.RunBatch` | 0 | 22.02 |
| `CombatFeaturesCheck.RunBatch` | 0 | 13.93 |
| `BallCheck.RunBatch` | 0 | 12.99 |
| `PitchCheck.RunBatch` | 0 | 31.59 |
| `UiRedesignCheck.RunBatch` | 0 | 44.84 |

ScoreCheck cubre el ejemplo exacto **3240**, fracciones, límites de Quilombo, segundos incompletos/repetidos/retroceso, entradas inválidas y saturación. En Play Mode: seis prefabs × doce muertes y doce despawns, nombre cambiado, doble daño/callback, reutilización, Quilombo alternado, balance de suscripciones y tres POIs únicos. También pausa/cola, muerte en el frame límite, victoria vivo, infinito sin victoria, cierre reentrante/doble, snapshot ante cambios runtime posteriores, abandono desde pausa, reinicio y retorno al menú. Comprueba datos de cuatro armas/cuatro pasivas/tres sinergias/dos evoluciones en el snapshot, IDs/versiones/UTC, F9 encendido/apagado, aceleración, invulnerabilidad, reloj inyectado y elegibilidad. Para aislar la regla de elegibilidad de un player sin QA, el fixture sustituye únicamente el Score inicial marcado por Editor: es simulación controlada, no una partida humana ni un récord guardado.

UI de ScoreCheck: derrota, victoria con build completa, resumen desplazado por teclado, HUD infinito y total saturado, en **1280×720 / 1024×768 / 1920×1080 / 2560×1080**; informe de layout vacío. Se inspeccionaron capturas del resumen normal/desplazado y puntaje máximo; el check completo de UI conserva su navegación, cartas y cuatro resoluciones. Las suites previas conservan física, pool, evolución/sinergias, ambos personajes, elecciones y resistencias reales de POIs. No se retiraron aserciones para aprobar.

**Intentos conservados:** attempt1 pasó reglas, pero la inspección visual detectó que GUI.BeginScrollView combinado con la rasterización propia de GameUi dejaba texto fuera del panel; se reemplazó por filas completas medidas en coordenadas de GameUi. attempt2 amplió build/práctica/valores extremos y detectó un recorte de cuatro píxeles del máximo numérico: el título de resultados usa tamaño fijo 42, pausa conserva 44, sin ajuste por longitud. La corrida final incluye ambas correcciones, navegación por páginas y todas las regresiones. Una excepción conocida de Editor SearchDatabase por proceso, también en baseline, se registra aparte; sin errores C#/gameplay/asserts en resultados finales. No se presenta esa excepción como resuelta.

**Evidencia e integridad:** `TestResults/BuildsEInfinito/Etapa6-20261104/` (local, ignorado) contiene estado/HEAD/hashes iniciales, baseline, intentos, runner, argv/resultados, logs, capturas y layout. **627 archivos Assets/Packages** del original y copia probada idénticos, **328 GUIDs únicos**; ningún meta previo cambió. Los seis diffs de prefab se verificaron quitando exclusivamente la nueva línea de categoría y comparando byte por byte con HEAD. `integrity.json`, `tested-source-sha256.json`, `log-summary.json` y `results.json` conservan el detalle. `git diff --check` aprobado al cierre.

**Reproducción:** copiar Assets, Packages y ProjectSettings de la raíz Unity a un directorio nuevo. Ejecutar cada check en proceso nuevo con gráficos, sin -nographics/-quit ni Setup/Apply/Generate. El runner local `run_checks.py` conserva once entradas y sus variables de salida; no activa el soak de etapa 5.

```sh
UNITY_BIN=/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity
QA_PROJECT=/ruta/absoluta/copia-unity
QA_OUTPUT=/ruta/absoluta/resultados
mkdir -p "$QA_OUTPUT/score"
CONURBANO_SCORE_OUTPUT="$QA_OUTPUT/score" "$UNITY_BIN" -batchmode -projectPath "$QA_PROJECT" -executeMethod ScoreCheck.RunBatch -logFile "$QA_OUTPUT/score.log"
```

**Cómo probar jugando:** Menu → Jugar → personaje/modo → A la plaza. Mirar puntos debajo de Quilombo: un común sin Quilombo agrega 10; Perro/Gomero 30; Jefa 1000; cada segundo completo suma 1. Completar un lugar agrega 250 y aumenta los puntos de bajas posteriores según su nuevo Quilombo, sin aplicar el bonus de densidad a la fórmula de puntos. Esc y las elecciones congelan el puntaje. En clásico esperar 04:00 vivo; en infinito morir para ver el resumen. Recorrer la lista con rueda/RePág/AvPág, revisar build/desglose, R reinicia limpio y M permite cambiar modo/personaje. En Editor siempre se verá Práctica; en un player Development, usar y apagar F9 debe conservarla. No hay todavía tabla ni mejor marca comparable.

**Límites / diseño pendiente:** etapas **7–9** siguen como diseño: sin tabla/top20, guardado atómico/respaldo/recuperación, mejor marca, botón Récords ni ranking online. No se generó una nueva build del player ni se ejecutó Windows, mouse físico, medición de FPS/GC/memoria, soak largo o balance humano en esta etapa. Las builds y mediciones de etapa 5 son evidencia histórica de sus fuentes, no validan este candidato. La elegibilidad fuera de Editor se ejercitó con un fixture controlado; queda su comprobación en una nueva build real. Los checks no demuestran diversión, dificultad aceptada ni protección contra manipulación.


### Etapa 7 — Récords locales — 2026-11-06

**Alcance implementado:** únicamente etapa 7 / sección 5.2, confirmada explícitamente por el usuario. HEAD recibido `3e43deb1bed42e853fe635960f9f373414d92bcc`, `main`; el árbol ya contenía la implementación y documentación local de etapa 6 sin commit. Se preservó ese trabajo. Raíz comprobada `Conurbano Infinito/`, remoto FranciscoLang2209/conurbano-infinito. No hay AGENTS.md/CLAUDE.md en repositorio ni ancestros; se consultaron el contexto, este contrato y la guía de UI local. Los registros anteriores conservan su alcance histórico. Etapas 8–9 no se implementan aquí.

**Persistencia:** `LocalRecords.cs` adapta el snapshot inmutable RunResult a DTOs JSON sin referencias a objetos Unity (`StoredRun`, `StoredBuildItem`, `StoredEnemyResult`). Esquema de archivo y resultado **1**. Incluye identidad, personaje/modo, versiones de reglas/build, UTC, tiempo, centipuntos long exactos, desglose/bajas/jefes, Quilombo, armas/familias/niveles/evoluciones, pasivas, sinergias, cierre, práctica y elegibilidad. Los nombres son snapshots de presentación; IDs definen identidad. El resumen y la tabla usan esos valores congelados, sin modificar SO ni el estado de la partida.

Ubicación normal: `Application.persistentDataPath/local-records.json`; Editor/batchmode usan **local-records-qa.json** para separar las pruebas de las marcas del jugador. El archivo conserva `lastResult` para cualquier cierre, incluido práctica/abandono, y hasta veinte récords elegibles **por modeId + rulesetVersion**. Se mantienen tablas de versiones anteriores. Orden: centipuntos exactos descendentes, segundos activos descendentes, runId ordinal ascendente; puntos visibles usan división entera por 100. La elegibilidad exige victoria/derrota sin práctica. El cierre sigue siendo único: reabrir resultado/tabla, volver al menú después de morir y reintentar no vuelven a registrar ese run.

`registeredRunIds` conserva también identidades que salieron del top: repetir un runId no cambia puntuación ni último resultado, aunque se intente registrar con valores distintos. Es un índice de cadenas, sin guardar partidas completas fuera del top/última; crece una identidad por cierre. Este límite se declara con `shortcut:`: compactarlo requiere acordar otro contrato de idempotencia. No se agrega alias, cuenta, servicio de red ni interfaz para un backend hipotético.

**Escritura y recuperación:** se escribe `.tmp`, se fuerza Flush(true), se relee/valida y se promueve mediante File.Replace (archivo existente) o File.Move (primera creación), dentro del mismo directorio. Hay `.bak` desde el primer guardado; después conserva el estado válido anterior. Un temporal incompleto nunca se promueve al cargar. JSON ausente empieza vacío o recupera backup; JSON inválido intenta respaldo. Si ambos están dañados, se puede comenzar un archivo válido conservando copias `.corrupt-<guid>` del principal y del respaldo antes de reemplazarlos. Una recuperación desde backup conserva ese backup durante la siguiente promoción. El estado en memoria se confirma sólo tras la promoción. Fallos de acceso/escritura/plataforma se convierten en aviso y no bloquean gameplay ni reemplazan el resultado anterior.

No existía un formato de récords desplegado en etapas previas. JSON sin versión o con versión desconocida se abre en modo de sólo lectura y se informa incompatibilidad: no se inventa una migración ni se sobrescribe información desconocida. Para una migración futura hace falta su esquema real. Se puede preservar/renombrar manualmente ese archivo fuera del juego y volver a abrir para empezar uno nuevo; no se borra automáticamente.

**UI e integración:** `RecordsView.cs` es una vista IMGUI concreta, propiedad de MenuScreen o GameManager. Menú agrega Récords (L); fin de partida agrega mejor marca del mismo modo/reglas y acceso a la tabla (L), junto a R/M. La tabla local permite alternar modo y versiones, recorrer veinte marcas en páginas de cinco, abrir build/desglose y consultar el último resultado incluso si no es elegible. Conserva foco, flechas/Tab/Enter/Espacio, mouse, Esc para volver y rueda/RePág/AvPág para detalle. Abrir/cerrar tabla no reanuda el combate ni vuelve a cerrar la partida. Se reutilizan GameUi, Atkinson, colores y medición de filas completas; se comparte ese dibujo con el resumen final existente. No requiere conexión en Inspector.

**Archivos propios de esta etapa:** `Assets/_Game/Scripts/Player/{LocalRecords,RecordsView}.cs`, integración acotada en `{GameManager,MenuScreen}.cs`, `Assets/_Game/Editor/RecordsCheck.cs`, tres meta nuevos y ambos documentos de contexto/etapas. El resto del árbol recibido, incluidos cambios de etapa 6 en ScoreCheck/RunResult/ScoreSystem, prefabs y GameUi, se preserva. No se modifican escenas, prefabs, catálogo, metas previos, enums serializados, campos serializados, arte, paquetes, ajustes, herramientas históricas ni el Assets exterior. Sin commit/push/merge/publicación/despliegue.

**Comprobado hasta aquí:** Unity **6000.3.23f1**, macOS **26.5.2 / Apple M3 / 8 GiB / Metal**, con gráficos, copia independiente de Assets/Packages/ProjectSettings; el Editor no abrió el proyecto original. Baseline recibido: ScoreCheck.RunBatch aprobado. RecordsCheck.RunBatch y RecordsCheck.ReloadBatch aprobaron en procesos nuevos: JSON/top20/modos/versiones/empates/ID duplicado incluso expulsado, precisión fraccionaria y long.MaxValue, último resultado, práctica/abandono, nombres visibles modificados sin cambiar IDs, backup ausente/dañado/doble corrupción, archivos sin versión/futuros, temporal incompleto y fallo de escritura que deja intacto el guardado previo.

El check integrado recorre **Menu → personaje → Main → baja → derrota → resultado guardado → Récords → build → volver → R/reintento → abandono → Menu**. La marca elegible de ese recorrido usa el mismo fixture explícito de etapa 6 que aísla la marca inicial de Editor; es QA controlada, no una partida humana ni un récord competitivo. La recarga se comprueba cerrando Unity y ejecutando ReloadBatch en un segundo proceso, incluidos IDs/nombres y el último abandono integrado. No se presenta como reinicio de una build del player.

Se capturan tabla, páginas, ambas modalidades, historial, último resultado de práctica, derrota, detalle/desplazamiento y build con cuatro armas/cuatro pasivas/tres sinergias/dos evoluciones en **1280×720 / 1024×768 / 1920×1080 / 2560×1080**. Informe de texto vacío y revisión visual de tabla/build/derrota a 1024×768. Teclado usa eventos Input System despachados por los handlers reales, como los checks previos. Mouse usa eventos IMGUI sobre los controles reales de RecordsView en un host Editor aislado con corrección Retina; no es mouse físico en un player.

**Intentos conservados:** el primer check detectó que JsonUtility aplica inicializadores al cargar `{}`; se retiraron los defaults de los campos deserializados y ahora sólo la creación explícita asigna esquema 1 y colecciones vacías. También se verifica que un JSON con versión pero sin las colecciones obligatorias recupere el respaldo. Otros intentos corrigieron el despacho sintético de teclado y la prueba de mouse: GameView batch no reenviaba los eventos sintéticos, y el host de UI necesita convertir puntos Retina. Se conserva toda evidencia; no se quitaron aserciones para aprobar. El baseline y los checks registran aparte la excepción conocida de SearchDatabase; no se atribuye a esta etapa ni se declara resuelta.

**Reproducción:** usar siempre una copia vacía del proyecto y un directorio nuevo de resultados. Gráficos habilitados; no usar -nographics, -quit ni Setup/Apply/Generate. Cada entrada cierra su propio proceso; la segunda relee los archivos de la primera. Las pruebas escriben sólo fixtures del directorio de salida y el archivo QA separado de los récords del player.

```sh
UNITY_BIN=/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity
QA_PROJECT=/ruta/absoluta/copia-unity
QA_OUTPUT=/ruta/absoluta/resultados-nuevos
mkdir -p "$QA_OUTPUT"
CONURBANO_RECORDS_OUTPUT="$QA_OUTPUT" "$UNITY_BIN" -batchmode -projectPath "$QA_PROJECT" -executeMethod RecordsCheck.RunBatch -logFile "$QA_OUTPUT/records.log"
CONURBANO_RECORDS_OUTPUT="$QA_OUTPUT" "$UNITY_BIN" -batchmode -projectPath "$QA_PROJECT" -executeMethod RecordsCheck.ReloadBatch -logFile "$QA_OUTPUT/reload.log"
```

**Cómo probar jugando:** abrir Menu y entrar a Récords con L/mouse o flechas/Tab/Enter; alternar Clásico/Infinito y versión. Volver con Esc, Jugar, elegir personaje/modo y A la plaza. Jugar hasta morir o ganar a 04:00; comprobar puntos, mejor local y resumen. L abre tabla sin reanudar; abrir una marca muestra su build, rueda/RePág/AvPág recorre detalle y Esc vuelve primero a tabla y luego al resultado. R inicia otra partida conservando modo/personaje; M vuelve al menú. Cerrar y reabrir la aplicación debe conservar las marcas del mismo modo/reglas. Abandonar desde pausa y usar F9 deben quedar sólo en Último resultado, sin aumentar el top. En Editor toda sesión es práctica; para una marca natural elegible se necesita un player actualizado y jugar sin herramientas QA. La demo/build histórica de etapa 5 no contiene estos cambios.

**Límites:** no se generó build nueva, no se ejecutó Windows, reinicio de player independiente, mouse físico, partida humana, balance, FPS/GC/memoria ni soak en esta etapa. No hay ranking global ni protección contra manipulación local. Sólo se preservan y reconocen los esquemas descritos; una versión desconocida requiere migración explícita. Validación de entrega y diseño online siguen pendientes en etapas 8–9.

**Regresiones ejecutadas:** trece entradas aprobadas en procesos nuevos (la recarga usa el mismo directorio de fixtures del primer proceso).

| Entrada real | Salida | Segundos |
| --- | ---: | ---: |
| `RecordsCheck.RunBatch` | 0 | 39.05 |
| `RecordsCheck.ReloadBatch` | 0 | 7.36 |
| `ScoreCheck.RunBatch` | 0 | 20.29 |
| `EndlessCheck.RunBatch` | 0 | 17.97 |
| `EvolutionCheck.RunBatch` | 0 | 21.33 |
| `SynergyCheck.RunBatch` | 0 | 24.43 |
| `PassiveSelectionCheck.RunBatch` | 0 | 23.41 |
| `BuildStateCheck.RunBatch` | 0 | 12.43 |
| `CharactersCheck.RunBatch` | 0 | 18.43 |
| `CombatFeaturesCheck.RunBatch` | 0 | 13.65 |
| `BallCheck.RunBatch` | 0 | 12.86 |
| `PitchCheck.RunBatch` | 0 | 31.30 |
| `UiRedesignCheck.RunBatch` | 0 | 44.40 |

Tras la revisión final del límite JSON se ampliaron las aserciones de colecciones obligatorias y se repitieron las pruebas específicas de persistencia/recarga/cierre; sus resultados se registran debajo. No se amplió contenido ni se cambiaron reglas de juego.

**Repetición específica final aprobada** (fuentes finales, colecciones JSON obligatorias):

| Entrada real | Salida | Segundos |
| --- | ---: | ---: |
| `RecordsCheck.RunBatch` | 0 | 42.47 |
| `RecordsCheck.ReloadBatch` | 0 | 8.69 |
| `ScoreCheck.RunBatch` | 0 | 22.04 |

**Evidencia final:** `TestResults/BuildsEInfinito/Etapa7-20261106/` contiene HEAD/estado/hashes recibidos, baseline, siete intentos de diagnóstico, dos corridas completas de RecordsCheck aprobadas, la corrida final ampliada, runners/argv/resultados, logs/capturas/layout e integridad. `run_checks.py`/`results.json` registran las trece entradas; `verify_persistence.py`/`persistence-results.json` registran las tres repeticiones finales. `persistence-final/` contiene fixtures y capturas finales; `score-final/` conserva las regresiones del cierre. Todos los informes finales de texto están vacíos. La excepción SearchDatabase conocida se separa; no hay errores C#, gameplay o aserciones en las corridas aprobadas. Integridad: **633 archivos Assets/Packages idénticos a la copia probada, 331 GUIDs únicos, todos los metas previos y rutas protegidas intactos**; sobre archivos recibidos sólo cambiaron GameManager/MenuScreen y estos dos documentos. `integrity.json`/`tested-source-sha256.json` y git diff --check conservan la comprobación.


### Etapa 8 — Validación integral y balance de entrega — 2026-11-06

**Alcance autorizado:** etapa 8, confirmada explícitamente por el usuario. HEAD recibido `3e43deb1bed42e853fe635960f9f373414d92bcc`/main y etapas 6–7 locales sin commit, preservadas. Raíz verificada `Conurbano Infinito/`; sin AGENTS.md/CLAUDE.md en repo/ancestros. El adjunto se usa como contrato de diseño, sin ejecutar otras etapas. No se modifica gameplay, balance, escenas/prefabs/catálogo, arte, paquetes, ajustes originales, enums/campos ni metas previos. No se usa Assets exterior ni generadores históricos. Sin commit/push/merge/publicación/despliegue.

**Nuevo entregable técnico:** `Assets/_Game/Editor/DeliveryCheck.cs` y meta. Reutiliza escenas/datos/reglas existentes: doce integraciones (dos personajes × ambos modos × Termo/Yerba, Pelota/Zapatillas, Pelota/Petardo/Antena), doce recorridos con WASD/catálogo/XP/salud normales y entrada de build macOS. Comprueba selección real, sinergia, evolución cuando corresponde, frontera de modo, cierre único, guardado/recarga, práctica excluida, SO inmutables, scripts faltantes y presupuestos. La ruta automática mide distancia para no confundir un teclado sin foco con progresión. No se agregan sistemas runtime o instrumentación permanente.

**Comprobado en esta sesión:** Unity 6000.3.23f1/macOS 26.5.2/MacBook Air M3/8 GiB/Metal, gráficos y copia aislada; original nunca abierto con Unity. Trece entradas de regresión aprobadas: Records/Reload, Score, Endless, Evolution, Synergy, PassiveSelection, BuildState, Characters/ShortRun ambos, CombatFeatures, Ball, Pitch y UiRedesign. DeliveryCheck final aprobó 12/12 integraciones y 12/12 recorridos (sin ofertas inválidas, movimiento verificado); en los recorridos hubo derrotas tempranas, ninguna sinergia/evolución natural, por lo que **no prueban utilidad ni balance de las builds objetivo**. La configuración de foco del fixture nuevo se corrigió tras una aserción de movimiento fallida; intentos conservados, sin cambiar PlayerController ni quitar aserciones.

Soak aprobado: **1.800,726 s simulados**, 125 elecciones, tramo 14, picos **37 enemigos/256 gemas/19 proyectiles**, 335 instancias finales. captureDeltaTime=1/30/timeScale=8, salud/XP controladas, combate y física completos; no es treinta minutos reales. Regresiones cubren pausa/cola, jefes/POIs/pool, cierre/puntaje/práctica y persistencia/corrupción. UI en cuatro resoluciones, informes de layout finales vacíos; inspección visual de detalle evolucionado a 1024×768 y player a 1280×720.

**Build limpia:** `Builds/Mac/Etapa8-20261106/ConurbanoInfinito.app`, Release/Mono/universal x86_64+arm64, **Succeeded, cero errores/un warning**, 127.109.916 bytes de reporte. Firma ad hoc, sin notarización ni publicación. No incluye sonda. Player comprobado con teclado/mouse nativos: Menu/Récords/modos, La Piba/Infinito, pausa/reinicio/menú/abandono excluido, cerrar/reabrir y detalle persistido. No se afirma rueda nativa (falló esa llamada CUA; AvPág sí observado) ni partida humana completa.

**Medición separada:** app Development con nombre de producto/guardados QA y sonda temporal sólo en la copia de build; también Succeeded. Ventana visible 1280×720/VSync=1/timeScale=1, **120,022 s reales / 7.190 frames / 59,91 FPS promedio**, p95 **17,663 ms**, pico **166,660 ms**. Se prepararon 150 comunes + jefe/Q100/cuatro armas/vida elevada; durante medición hubo 20–94 enemigos. Picos de preparación: 151/256/33, pool 246→459 y sin nuevas instancias en últimos 30 s muestreados. GC medio 8.274,79 bytes/frame; memoria Unity 103.979.787→104.652.711 bytes. No certifica 60 FPS sostenidos, memoria estable indefinidamente ni mejora frente a otra carga. Segundo proceso del player relee práctica excluida y un fixture elegible explícito separado; no se introduce una marca artificial en el guardado del jugador. Sondas retiradas de la copia y ausentes del repo/runtime limpio.

**Incidencias/límites:** SearchDatabase preexistente se registra aparte; ComputeBuffer al cerrar player vuelve a reproducirse y ya está en el log de la demo de octubre. Causas no resueltas. Windows **no generado/ejecutado**: falta WindowsStandaloneSupport/equipo Windows; IsBuildTargetSupported=True no prueba módulo instalado. No hay daño acumulado por arma ni atribución del golpe mortal. Balance humano, tiempos naturales hasta sinergia/evolución, Aerosol/Bolsa, 30 minutos reales y rendimiento sostenido pendientes. No se cambian estadísticas a partir de muertes del bot. Etapa 9 continúa sólo como diseño opcional.

**Informe y reproducción:** [BuildsEInfinito-Validacion.md](BuildsEInfinito-Validacion.md) contiene comandos/entradas/entorno/logs, matriz completa, métricas, incidencias priorizadas y guía humana de **15–20 minutos**. [BuildsEInfinito-GDD-Adenda.md](BuildsEInfinito-GDD-Adenda.md) documenta clásico 4 min/infinito/sistemas/competencia local; el GDD original no está disponible y no se afirma cotejo de primera mano. Evidencia local ignorada: `TestResults/BuildsEInfinito/Etapa8-20261106/`, incluyendo argv/resultados, intentos, logs/CSV/capturas/manifiestos e integridad. La validación técnica fue ejecutada; **aceptación de balance y entrega multiplataforma no terminadas**.

**Integridad final de etapa 8:** 635 archivos Assets/Packages idénticos a copia probada, 332 GUIDs únicos; 831 de 833 archivos recibidos intactos (las únicas modificaciones recibidas son los dos documentos solicitados). Cuatro archivos nuevos propios: DeliveryCheck/meta, informe y adenda. Ocho informes de layout finales vacíos y git diff --check aprobado. Fuentes finales de DeliveryCheck aprobadas en delivery-verified (17,18 s); las diferencias frente a la entrada usada para la build limpia son sólo rótulos de log de Editor, sin efecto en el player. Manifiestos de integridad/build conservados en la evidencia.


### Etapa 9 — Especificación de ranking casual online — 2026-11-06

**Alcance autorizado y entregado: diseño/plan, sin implementar ni desplegar servicios.** Selección explícita del usuario de Prompt 9. HEAD `3e43deb1bed42e853fe635960f9f373414d92bcc`/main, trabajo local de etapas 6–8 preservado; raíz Unity `Conurbano Infinito/` comprobada. Sin AGENTS.md/CLAUDE.md en árbol fuente/ancestros. Guía local/contexto/plan/evidencia de etapa 8 consultados. Sólo dos documentos nuevos y apéndices a este registro/contexto; sin runtime, migraciones, assets, metas, cambios de enums/campos, escenas/prefabs, regeneración o actualización de paquetes. Sin commit/push/merge/publicación/despliegue.

**Inspección comprobada:** existe vínculo declarado a Unity Cloud en ProjectSettings; no se encontraron integración UGS de ranking, SDK Supabase, servidor, migraciones o hosting/API de ranking en código/configuración del repo. No se consultaron dashboards ni se deduce ausencia de infraestructura externa a partir del árbol local. Se cotejaron GameManager.Awake/CloseRun/RunClosed, RunResult/StoredRun, LocalRecords/RecordsView/MenuScreen/GameUi, ScoreSystem y RunRules, además de entradas/aserciones de ScoreCheck/RecordsCheck/DeliveryCheck y reporte previo. Versiones actuales: modos `mode.classic-v1`/`mode.endless-v1`, ruleset por default de código `builds-infinito-1`, Application.version 1.0; no confundir con identificación o atestación de artefacto.

**Entregable de diseño:** [BuildsEInfinito-Online-ADR.md](BuildsEInfinito-Online-ADR.md), ADR-001, propone un proyecto Supabase administrado con Auth, una función HTTP y Postgres transaccional; compara UGS y servidor propio. Propuesta condicionada a confirmar infraestructura existente, proveedor/cuenta/región/coste/operación; tarifas oficiales consultadas, sin contratación. Piloto cerrado, identidad por instalación y alias generado; recuperación/multiequipo/alta pública requieren otra decisión. Compara ranking casual con autoridad de simulación adicional y ataques residuales; hash, seed, secreto en cliente, JWT y plausibilidad no demuestran partida honesta.

Define inicio/cierre/estado, propiedad e idempotencia también concurrente, DTO exacto de long sin cambiar JSON local, validación/coherencia/cotas/duración, versiones y top20 de una mejor marca por identidad con posición propia consistente. Tabla por modo/ruleset; práctica y abandono excluidos. Guardado local primero; partida offline desde inicio nunca sube, desconexión posterior reenvía snapshot idéntico antes de 72 h de pared. Incluye límites de frecuencia/coste, errores/backoff, cola acotada, seguridad de credenciales, moderación, privacidad, retención/tombstones y borrado con JWT previo. Cotas, presupuesto y 72 h son parámetros propuestos para validar, no garantías ni límites nuevos del juego local.

**Plan posterior:** [BuildsEInfinito-Online-Prompts.md](BuildsEInfinito-Online-Prompts.md), tareas 9A backend local, 9B integración Unity y 9C verificación con dos instalaciones/identidades. Exigen evidencia de duplicados/reintentos/respuesta perdida/rechazos, ventanas, práctica/abandono, orden/top/posición, offline, UI y borrado; separan prueba humana/fixture/Play Mode/player. No se ejecutaron y no autorizan creación de servicios remotos o despliegues. Decisiones necesarias antes de implementarlos enumeradas en ADR §11.

**Verificaciones realmente ejecutadas:** Git/remoto/raíz/configuración y búsqueda de guías/backend; revisión estática de flujo/contrato y fuentes oficiales/changelog; enlaces locales, fences e integridad SHA-256 de archivos recibidos; `git diff --check`. Ningún archivo recibido salvo los dos documentos de registro/contexto cambia y su contenido previo se conserva íntegro como prefijo. No se ejecutan SQL, Auth, rutas HTTP, dos clientes, compilación Unity, Play Mode, mediciones o build en esta etapa documental; Unity instalado, no requerido para estos cambios. La evidencia de etapa 8 es histórica y no prueba el protocolo online.

**Resultado y cómo probarlo:** etapa 9 documental entregada; implementación online pendiente. Hoy se juega igual y se consultan récords locales. La guía humana local sigue en [validación de etapa 8](BuildsEInfinito-Validacion.md); para futura prueba compartida usar 9C después de 9A/9B y entorno test autorizado. Balance humano, Windows y estabilidad prolongada de etapa 8 continúan pendientes. El ranking propuesto seguirá aceptando falsificaciones plausibles del cliente, por lo que debe anunciarse casual/no verificado.
