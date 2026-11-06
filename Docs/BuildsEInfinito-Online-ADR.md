# ADR-001 — Clasificación compartida casual

Fecha: 2026-11-06. Etapa 9. **Decisión de diseño propuesta; backend e integración todavía no implementados ni probados.** El usuario autorizó especificación y plan, sin crear servicios ni desplegar. Las cifras operativas de este ADR son propuestas para un piloto, no métricas ni garantías del proveedor.

## 1. Base inspeccionada y alcance

Repositorio `FranciscoLang2209/conurbano-infinito`, `main`, HEAD `3e43deb1bed42e853fe635960f9f373414d92bcc`, con etapas 6–8 locales sin commit. Raíz Unity comprobada: `Conurbano Infinito/`, Unity 6000.3.23f1, URP 17.3.0, Input System 1.20.0. No se encontraron AGENTS.md/CLAUDE.md en el árbol fuente ni en ancestros. Guía local: [contexto](UnityProjectContext.md), [plan](BuildsEInfinito.md), [PartidaCorta](PartidaCorta.md), [UI](UiRedesign/README.md) y [ArtDirection](ArtDirection.md).

La [etapa 8](BuildsEInfinito-Validacion.md) documenta validación técnica macOS; balance humano, Windows y estabilidad prolongada siguen pendientes. Este diseño puede prepararse con esa evidencia, pero no convierte esos pendientes en aprobaciones ni habilita un lanzamiento público.

| Evidencia local | Consecuencia para el diseño |
| --- | --- |
| `ProjectSettings/ProjectSettings.asset` declara `cloudProjectId`, organización y nombre de proyecto Unity Cloud | Hay vinculación declarada a Unity Cloud. No prueba que existan servicios de ranking o credenciales utilizables. No se inspeccionaron dashboards/cuentas remotas. |
| `Packages/manifest.json` incluye Multiplayer Center y módulos UnityWebRequest; no Authentication, Leaderboards, Cloud Code ni SDK Supabase | No hay integración de ranking instalada. No actualizar Unity/paquetes para diseñarla; proponer HTTP con el módulo existente. |
| Búsqueda en código/configuración versionada y archivos locales no ignorados: sin servidor, funciones, migraciones, URL de API propia, Docker, manifiestos de hosting o CI de backend | No hay backend/hosting de ranking encontrado **en el repositorio**. No afirmar ausencia en toda la cuenta del propietario. |
| `GameManager.Awake` genera `runId` GUID N, captura configuración, personaje, UTC local y `Application.version` | Reutilizar runId; el servidor agrega su sessionId/UTC. `Application.version` vale actualmente 1.0 y no identifica por sí sola fuentes ni honestidad. |
| `GameManager.CloseRun` congela ScoreSystem, crea RunResult, guarda LocalRecords y emite RunClosed | Mantener cierre y guardado local primero; envío posterior del snapshot. No puntuar desde una respuesta HTTP. |
| `RunResult.cs` / `LocalRecords.cs`: esquema 1, long, IDs, build/desglose; top20 local por modo/reglas | DTO de red separado; no migrar el JSON local ni reinterpretar enums serializados. |
| `ScoreSystem.cs`: Common/Special/Boss, 10/30/1000, multiplicador a centésimas, supervivencia por floor | No inventar categoría Elite ni reconstruir puntos con el Quilombo final. |
| `RunRules.cs` y assets Clasico/Infinito | IDs `mode.classic-v1` y `mode.endless-v1`; `rulesetVersion` usa el default de código `builds-infinito-1` (no está escrito en esos YAML). |
| `RecordsView`, `MenuScreen`, `GameUi` | Vista Compartida separada de Local, con la navegación, fuente y tamaños existentes. No migrar IMGUI ni tocar la plaza. |

Rutas de código de esta tabla relativas a `Conurbano Infinito/Assets/_Game/Scripts/Player/`, salvo configuración y assets expresamente indicados. Se revisaron también ScoreCheck, RecordsCheck y DeliveryCheck como puntos de verificación futura. Ningún runtime, asset, meta, escena, prefab, enum o paquete cambia en etapa 9.

## 2. Decisión y alternativas

Proponer **un proyecto Supabase administrado: Auth + una Edge Function HTTP con rutas v1 + Postgres**. Transacciones y restricciones únicas resuelven sesiones, recibos, mejor marca e idempotencia en un solo almacén. Unity usa UnityWebRequest y DTOs propios pequeños; no necesita un SDK C# ni un sitio web para consultar la tabla del piloto cerrado.

Esta elección es una inferencia sobre el trabajo necesario para el contrato de este juego, no un benchmark de proveedores. Antes de implementarla, el propietario debe confirmar si el proyecto Unity Cloud ya dispone de una solución equivalente aprovechable. Si existe, revisar este ADR antes de sumar otro proveedor.

| Alternativa | Trabajo para este contrato | Decisión |
| --- | --- | --- |
| Supabase administrado | Escribir API/validaciones/transacción; Auth y DB administradas. Control exacto de long, desempates y duplicados | Recomendada para el piloto; cuenta y presupuesto pendientes. |
| Unity Gaming Services | Aprovecha vinculación declarada y ofrece Leaderboards; sesiones/idempotencia/validación requieren Cloud Code y almacenamiento persistente, además de Authentication y permisos | Alternativa válida si ya está operativa. No asumir que habilitar Leaderboards basta. Comprobar compatibilidad con Unity 6000.3.23f1 antes de elegir SDKs. |
| Servidor propio o Functions con DB y Auth de otros proveedores | Administrar proceso o unir proveedores sin código existente que reutilizar | Más operación para esta entrega; diferir. |
| Subir el JSON local directamente | Menos código, sin control de pertenencia, sesiones o cierre único | No satisface el contrato. |

UGS permite [acceder a Leaderboards desde Cloud Code](https://docs.unity.com/en-us/leaderboards/access-cloud-code) y [restringir escrituras directas de jugadores](https://docs.unity.com/en-us/cloud-code/3.0.0/server-access-control). Son referencias conceptuales; la segunda corresponde a una versión que anuncia Unity 6.7+, no una orden de instalarla en este proyecto.

Supabase documenta [autenticación de funciones](https://supabase.com/docs/guides/functions/auth) y [conexión a Postgres](https://supabase.com/docs/guides/database/connecting-to-postgres). La implementación deberá fijar versiones y comprobar compatibilidad real. Se propone conexión agrupada desde la función con un rol limitado al esquema del juego, SQL parametrizado y transacciones; no exponer tablas mediante Data API al cliente. No agregar Redis, colas remotas, Realtime, servidor de combate, telemetría por frame ni un panel administrativo nuevo.

## 3. Qué significa «casual»

| Modelo | Qué puede garantizar | Qué sigue permitiendo |
| --- | --- | --- |
| Casual propuesto | Identidad autenticada, pertenencia de sesión, escrituras únicas, coherencia de formato/sumas, cotas, separación de tablas y moderación | Cliente modificado puede inventar bajas/build/puntos dentro de cotas, ocultar práctica, automatizar partidas, esperar tiempo real para justificar duración, copiar credenciales y crear identidades nuevas. |
| Simulación autoritativa adicional | Servidor ejecuta reglas y concede puntos desde su propio estado; inputs validados, versión fija y recursos de cómputo medidos | Bots, automatización de inputs, cuentas múltiples, colusión, fallos del servidor y robo de cuenta. No garantiza que jugó una persona. |
| Replay validado | Sólo sería verificación si el servidor reproduce y verifica todo el estado relevante con reglas deterministas demostradas | Un registro incompleto o física divergente permite falsos positivos/negativos; necesitaría otra arquitectura y evaluación. No existe aquí. |

Hash del payload: sólo comparación de duplicados. Hash público de build: sólo identificación de una versión declarada. Seed: sólo dato de reproducción cuando proceda. Secreto dentro del ejecutable: extraíble. JWT: autentica una identidad, no la simulación. Cotas y duración del servidor: controles de plausibilidad, **ninguno prueba una partida**. No anunciar «anticheat», «resultado verificado» ni ofrecer premios basados en estas marcas.

## 4. Identidad, participación y alias

Participación online opt-in desde menú; por defecto se mantiene juego local. Al activarla se explica qué se publica y que son resultados no verificados. Auth anónimo asigna un sujeto privado por instalación; `playerId` público es otro identificador aleatorio generado por el servidor. No se aceptan ownerId/playerId suministrados por un resultado como autoridad.

Primera versión: alias generado por el servidor, por ejemplo `Vecino 8F2A91C4`, con sufijo único y sin texto libre ni nombres obtenidos del equipo. Esto evita una pantalla y una cola de moderación de nombres. No hay correo, contraseña, login social, recuperación de cuenta o vínculo entre instalaciones en el piloto. Reinstalar/perder credencial crea otra identidad; copiarla permite suplantarla. Confirmar estas limitaciones antes de adoptar el proveedor.

Persistir refresh token mediante almacenamiento de credenciales nativo de macOS/Windows; access token en memoria. No PlayerPrefs ni JSON de récords, logs, argumentos de proceso o repositorio. Renovación de token serializada: una sola operación en vuelo, persistir la nueva credencial antes de dar por terminada la rotación; probar caída de proceso y reconexión. Si el almacén no está disponible, continuar local y explicar que no hay identidad persistente; no degradar silenciosamente a texto plano. Validar esta integración en ambos SO antes de distribuir online allí.

El piloto será cerrado: un trigger confiable al crear usuario Auth crea un perfil pendiente; después requiere admisión manual del operador. GET /me no crea perfiles desde un JWT. Consultas y escrituras del juego requieren perfil admitido; no construir códigos de invitación ni su propio sistema de contraseñas. [Auth anónimo](https://supabase.com/docs/guides/auth/auth-anonymous) necesita protección de altas y limpieza explícita; Supabase recomienda CAPTCHA y documenta límites por IP. La admisión de perfiles no elimina abuso del endpoint de Auth. Lanzamiento abierto exige resolver el flujo CAPTCHA compatible con escritorio o adoptar otra identidad en una revisión del ADR; no se declara resuelto por limitar la API propia.

Moderación inicial: operador con acceso administrativo puede ocultar una marca, suspender perfil y sustituir un alias generado si resulta ofensivo accidentalmente. Canal de reporte/contacto debe definirse antes del piloto; sin formulario/chat nuevo. Dejar motivo/código y actor en auditoría privada. Una marca oculta se excluye de top y posición; recalcular mejor marca visible desde las retenidas. No recuperar automáticamente una marca borrada por moderación al reenviar el cierre. No prometer bloquear reincidentes por dispositivo o IP.

## 5. Contrato HTTP v1

Base futura `https://<proyecto>/functions/v1/leaderboard`; el placeholder no es un servicio creado. HTTPS obligatorio, `Authorization: Bearer <JWT usuario>`, clave publishable en `apikey` según documentación vigente. La publishable key y URL son públicas; no autorizan escrituras por sí solas. Comprobar JWT y sesión Auth vigente, sujeto, perfil admitido/no suspendido y propiedad en cada operación. La capa DB usa únicamente identidad verificada por la función, nunca metadata editable del usuario.

Toda respuesta incluye `schemaVersion: 1`, `serverNowUtc` y requestId para diagnóstico sin secretos. Errores JSON: `code`, `retryable` y, para 429, `retryAfterSeconds` más cabecera Retry-After. UTC de servidor es autoridad para ventanas; UTC local es informativo y privado. Me y borrado propio admiten también perfiles pendientes/suspendidos: suspender publicación o alcanzar una cuota no bloquea solicitar eliminación.

| Método / ruta | Entrada y resultado |
| --- | --- |
| `GET /v1/me` | Perfil propio: playerId, alias, estado, activeSessionId o null. No devuelve JWT ni identificadores privados de Auth. Perfil pendiente: respuesta propia limitada, resto de rutas 403. |
| `POST /v1/runs/start` | schemaVersion=1, clientRunId GUID N minúsculo, modeId, rulesetVersion, characterId, buildVersion, clientBuildId público. Devuelve sessionId UUID del servidor, los valores aceptados, startedAtUtc, expiresAtUtc y estado. |
| `POST /v1/runs/{sessionId}/finish` | Snapshot descrito debajo. Devuelve recibo inmutable: sessionId/clientRunId, estado accepted/excluded/rejected, receivedAtUtc, reasonCode e improvedBestAtClose. No guarda una posición como permanente. |
| `GET /v1/runs/{sessionId}` | Estado/recibo propio para resolver timeout después de envío. Una sesión ajena o desconocida da el mismo 404. No es una consulta pública de partidas. |
| `GET /v1/leaderboards?modeId=…&rulesetVersion=…` | Top20 de mejores marcas por identidad y posición propia en la misma instantánea, totalPlayers y asOfUtc; sin paginación en v1. |
| `GET /v1/entries/{runId}` | Detalle público de una mejor marca actual visible: build validada, desglose y cierre; sin UTC local, datos Auth, prácticas ni datos de red. 404 si dejó de ser mejor, está oculta o ausente. |
| `DELETE /v1/me` | Elimina identidad/partidas propias, invalida acceso del juego y devuelve recibo de solicitud de borrado; ver sección 10. No equivale a borrar récords locales. |

### Inicio y versiones

Generar clientRunId una sola vez antes del primer tick activo. El servidor fija sessionId/UTC y liga modo, versión, personaje y build declarada; el cierre no puede cambiarlos. Mantener un manifiesto permitido en servidor con IDs/catálogos/niveles, modos, reglas, políticas de admisión y builds compatibles. Cada sesión guarda policyVersion y snapshot de la política operativa aceptada; devolver policyVersion al iniciar y conservarla en el recibo. Retirar una build impide nuevos inicios; honrar sesiones previas hasta vencer salvo revocación extraordinaria explícita con motivo visible. `clientBuildId` distingue artefactos aunque ambos tengan `Application.version=1.0`; es público y falsificable, no una atestación. Cambiar reglas jugables requiere otro rulesetVersion y otra tabla, no editar silenciosamente la anterior.

Un solo inicio activo por identidad. Un inicio nuevo con distinto clientRunId sustituye atómicamente el anterior (`superseded`, sin récord). Repetir el mismo inicio y contenido devuelve la sesión original, sin sustituir una sesión posterior ni renovar expiresAt. Repetir ID con contenido distinto da 409. Índice único global de clientRunId; colisión con otra identidad da error genérico sin revelar al dueño.

Preparar la sesión desde el flujo de iniciar/reintentar, antes de cargar Main o liberar la simulación. Tiempo de espera máximo inicial 5 s reales; si no hay aceptación, empezar local con otro runId y no promocionar esa partida al reconectar. El intento desconocido puede existir en servidor: vence/sustituye sin marca. No frenar la plaza durante la partida para mantener una sesión de red.

### Payload de cierre

DTO de red v1 separado de `StoredRun`. Campos obligatorios, sin valores desconocidos ni objetos Unity:

| Grupo | Campos / representación |
| --- | --- |
| Identificación | schemaVersion=1, clientRunId, modeId, rulesetVersion, characterId, buildVersion, clientBuildId. Deben coincidir con el inicio; sessionId viene en ruta. |
| Tiempo y cierre | startedUtc del cliente (ISO UTC informativo), activeSeconds número finito float32, endReason texto `defeat`, `victory` o `abandoned`; practice boolean y practiceReason código o null. |
| Puntos | centipoints, poiCount, poiCentipoints, survivalSeconds, survivalCentipoints: **cadenas decimales canónicas no negativas de int64**. No enviar points como segunda fuente. |
| Enemigos | Exactamente common/special/boss una vez cada uno; kills y centipoints en cadenas int64. No enviar ordinales del enum ni categoría elite. |
| Build | weapons (1–4), passives (0–4), synergies (0–3): IDs y niveles; evolutionId nullable en armas. Sin nombres suministrados por cliente; nombres públicos desde catálogo del servidor. |
| Quilombo | finalQuilombo número finito 0–100. No sustituye al multiplicador de cada muerte. |

Cada ítem de build contiene `id` y `level`; armas además `evolutionId`. Cada enemigo contiene `category`, `kills` y `centipoints`. practiceReason usa códigos de red explícitos (`editor_qa`, `batch_qa`, `accelerated_time`, `modified_clock`, `modified_health`, `other_practice`), sin enviar texto de diagnóstico local; practice=true exige un código y false exige null. El adaptador mapea motivos existentes sin renombrar PracticeReason ni alterar su latch.

`activeSeconds` conserva el valor float32 de RunResult, incluyendo fracción; normalizar al mismo float32 y almacenar `real` en DB. Las cadenas int64 se parsean con aritmética exacta (BigInt/SQL bigint), nunca Number de JavaScript; serializarlas de vuelta como cadenas. El parser rechaza signos, espacios, exponente, ceros iniciales salvo `0`, valores >9223372036854775807, campos duplicados y no finitos. Límite de cuerpo 16 KiB, strings de ID <=128 bytes, listas acotadas; rechazo antes de trabajo costoso. Probar round-trip cerca de 1/240 s y números >2^53.

### Cierre único y reintentos

Transacción: bloquear sesión y perfil, comprobar propietario/estado/ventana, validar snapshot, guardar recibo y resultado, actualizar mejor marca si corresponde y finalizar sesión; commit único. Restricciones únicas sobre sessionId y clientRunId respaldan la lógica, también con dos funciones concurrentes. El digest de payload canónico se calcula **en servidor** después del parseo: mismos valores con otro orden de claves equivalen; ordenar colecciones sin semántica de orden, conservar slots de armas.

Mismo cierre normalizado: 200 con el recibo original, incluso si la marca dejó de ser mejor o la sesión venció después. Otro contenido para un cierre ya recibido: 409, no sobrescribir. Práctica o abandono: recibo excluded, cero clasificación; no devolver como error transitorio. Snapshot bien formado pero inválido: 422/rejected terminal, no permitir «corregir» una puntuación hasta que pase. JSON inparseable/413/auth inválida no consume la sesión. Una sesión vencida/superseded sin recibo da 410 y no se reabre. No revelar si la sesión pertenece a otra persona.

401 permite un refresh y repetir la misma operación; si falla, conservar pendiente y detener reintentos. 408/timeout/conexión/5xx: persistir y reintentar con mismo sessionId, clientRunId y payload. 429: respetar Retry-After. 400/403/404/409/410/413/422: sin bucle automático; estado visible y local intacto. Los errores de persistencia DB producen 503, nunca un falso accepted. Consulta de estado tras respuesta perdida no genera otra partida.

## 6. Validación de admisión, no de simulación

Estas reglas corresponden al primer manifiesto online propuesto; se deben verificar con fixtures del cliente real antes de abrir escrituras. Si rechazan una partida honesta, corregir el contrato/manifiesto con nueva versión de política, sin relajar a ciegas.

1. Verificar esquema, propiedad, perfil, build/mode/ruleset/character permitidos, IDs existentes y coincidencia con el inicio. No confiar en `isRecordEligible` del JSON local ni aceptar un backup histórico como sesión nueva.
2. Práctica declarada, Editor/QA declarados por build admitida sólo para pruebas o abandono: excluded. Una sesión conocida como práctica jamás recupera elegibilidad. Un cliente modificado puede mentir sobre esas señales: permanece dentro de la amenaza casual.
3. `activeSeconds >= 0` y `activeSeconds <= receivedAtUtc - startedAtUtc + 2 s`. Tolerancia de reloj/acumulación propuesta; no usar reloj del usuario ni restar pausas declaradas. Sesión expira a las **72 h de pared desde inicio**, incluidas pausas y retraso de envío. Infinito no gana por tiempo. Más de 72 h sigue jugándose/local, pero ese cierre no entra online; no agregar un límite al modo local.
4. Clásico: victoria exactamente en el float32 240; derrota entre 0 y 240 inclusive, porque muerte tiene prioridad en ese frame. Infinito: sólo derrota para marca. SurvivalSeconds = floor(activeSeconds) y survivalCentipoints = segundos ×100, con aritmética exacta. No sumar pausas/elecciones ni recompensar abandono.
5. POIs: 0–3 y poiCentipoints = count ×25000. No exigir matar a la Jefa para ganar clásico. Cota de bajas de jefe: 0 antes de 180; clásico como máximo 1 desde 180; infinito agrega las ventanas 420 + 240n hasta el tiempo activo. Es una cota, no obligación de matar cada jefe.
6. Por categoría, con base b=10/30/1000: `kills*b*100 <= categoryCentipoints <= kills*b*200`, múltiplo de b; cero bajas exige cero puntos. La suma de enemigos + POIs + supervivencia debe igualar total. No recalcular todas las bajas usando finalQuilombo. La información agregada no permite reconstruir cada multiplicador.
7. Cota operativa amplia inicial: totalKills <= floor(activeSeconds ×5000)+151 y totalCentipoints <=10^15. Rechazar int64 saturado en online; el guardado local saturado sigue válido. Son cotas contra payloads absurdos, **no límites físicos demostrados ni anticheat**. Registrar razón y contrastar con las partidas humanas pendientes antes del piloto.
8. Armas N1–8 y pasivas con máximos reales, IDs únicos/cupos/compatibilidad dirigida; recetas y sinergias por IDs. Evolución exige arma N8, pasiva máxima y al menos un jefe declarado. Validar que las sinergias declaradas coincidan con requisitos alcanzados en el snapshot. No probar orden de adquisición, XP ni daño a partir de esa build final.

## 7. Tablas, posición y almacenamiento

Clasificación **Casual / modo / rulesetVersion**, sin mezclar pruebas con producción. Una mejor marca visible por playerId en cada tabla. Diferencia intencional con top20 local de partidas: evita que una instalación ocupe veinte posiciones. Conservar resultados recientes para detalle/moderación y mejorar una marca; no subir todo el historial local.

Comparador: centipoints exactos descendente, activeSeconds descendente, clientRunId ordinal ASCII ascendente (32 hex minúsculas; collation C). Posición 1 + cantidad de mejores marcas visibles anteriores según el mismo comparador; sin empates compartidos. Tanto top20 como `me` se calculan en una misma instantánea DB; una respuesta posterior puede mostrar otra posición. Sin marca propia: `me: null`; sólo contar perfiles admitidos/no suspendidos y marcas visibles. `points` mostrado = floor(centipoints/100), sin ordenar por el entero mostrado. No separar tablas por personaje/buildVersion si comparten reglas aprobadas.

Modelo lógico propuesto; todavía no es una migración:

| Tabla privada | Claves y responsabilidad |
| --- | --- |
| profiles | authSubject único, playerId público único, alias único, estado pending/admitted/suspended/deleting; nunca publicar authSubject. |
| rulesets | (modeId,rulesetVersion), catálogo/manifest/política de admisión versionada y builds permitidas; histórico de sólo lectura al retirar nuevas sesiones. |
| run_sessions | sessionId PK, clientRunId único, owner FK, configuración fija, UTC servidor, vencimiento/estado, digest y recibo terminal. Índice parcial único de sesión activa por owner. |
| run_results | sessionId único/FK; snapshot validado privado, visibilidad y motivo. Catálogo valida todo JSONB recibido antes de guardarlo. |
| leaderboard_best | PK (owner,modeId,rulesetVersion); referencia a resultado visible, puntos bigint y tiempo real para consultar sin recorrer cada sesión. |
| request_limits / moderation_audit | Contadores transaccionales por ventana; auditoría administrativa limitada. No logs de cuerpos/tokens. |

Índice de orden en leaderboard_best: `(modeId,rulesetVersion,centipoints DESC,activeSeconds DESC,clientRunId COLLATE C ASC)`, e índices en FKs/propietarios consultados. Actualizar best bloqueando el perfil para serializar cierres y moderación; al ocultar o borrar recalcular en la misma transacción. Si no queda resultado retenido, perfil sin marca hasta otra partida. Medir consulta de posición con EXPLAIN/fixtures antes de optimizar; no cache distribuida/materializaciones adicionales ahora.

Esquema `game_private` no expuesto por Data API, sin permisos a anon/authenticated/PUBLIC; no funciones SQL públicas privilegiadas. RLS como defensa adicional, roles de función limitados y pruebas de acceso directo denegado, tanto lectura como escritura. Para verificar sesión Auth, autorizar al rol backend sólo la lectura necesaria de id/user_id en auth.sessions y comprobar session_id del JWT y su sujeto; nunca leer refresh tokens ni abrir ese acceso al juego. Administrador Auth sólo para operaciones que lo requieren; separación del rol DB de la API y credenciales de migración. No dar una clave service_role al juego ni usar una view pública para saltarse RLS. Ver [seguridad Data API](https://supabase.com/docs/guides/api/securing-your-api) y [claves públicas/secretas](https://supabase.com/docs/guides/getting-started/api-keys).

## 8. Frecuencia, coste y operación

Propuestas para el piloto, aplicadas en servidor y compartidas entre instancias (ventanas fijas UTC, incremento atómico; no diccionario en memoria de una función):

| Operación | Por identidad | Por IP de origen confiable |
| --- | --- | --- |
| Inicio | 6/minuto y 60/día | 60/minuto |
| Cierre, incluidos duplicados y estado de sesión | 30/minuto | 120/minuto |
| Tabla, detalle y me | 30/minuto | 300/minuto |
| Borrado | 3/hora | 30/hora |

No aceptar X-Forwarded-For arbitrario; usar únicamente la IP provista/validada por el gateway y probar su contrato. Si el entorno no la garantiza, no fingir protección por IP: resolverlo antes de habilitar el piloto. Compartir Wi-Fi/NAT no equivale a identidad; un bloqueo afecta red, no determina culpabilidad. Alta/refresh Auth tienen límites del proveedor separados; no quedan protegidos por estos contadores. Intentos inválidos también consumen presupuesto. Botnets/cuentas múltiples pueden eludirlos.

Cliente: tabla sólo al abrir/refrescar, no cada frame; cache local con asOf, sin sondeo durante combate. Tiempo HTTP máximo 10 s reales (inicio 5 s), cierre con backoff 2/4/8/16/32/60 s, jitter y máximo 5 intentos por apertura; después botón Reintentar o próxima apertura. 429 siempre respeta espera mayor. Usar tiempo real aunque Time.timeScale=0. Un solo envío por sesión en vuelo; doble input de UI no duplica acciones.

Métricas mínimas agregadas: peticiones por ruta/estado, latencia, duplicados/conflictos, rechazos por código, filas/tamaño DB y errores de escritura. Interruptor de admisión online operado en servidor devuelve 503/retryable mientras el juego local sigue. Presupuesto global del piloto: máximo 10.000 solicitudes/día y 1.000 perfiles admitidos; al alcanzarlo cerrar nuevas operaciones del juego con estado explícito, manteniendo disponible la solicitud de borrado. Este freno de aplicación no garantiza un techo de facturación de Auth/gateway ni evita tráfico abusivo antes de la función.

Según [precios oficiales consultados el 2026-11-06](https://supabase.com/pricing), Free figura a USD 0 con 500 MB de base y pausa tras una semana inactiva; Pro parte de USD 25/mes y puede sumar consumo/proyectos. No se cotiza UGS ni se garantiza coste cero. Estimar con MAU anónimos, invocaciones/reintentos, DB/recibos, egreso, backups y logs. Propuesta de presupuesto inicial: USD 30/mes, sujeto a aprobación; alertas a 50/80/100% y responsable que puede desactivar admisión. Revalidar tarifas antes de contratar; sin compras en esta etapa.

## 9. Offline e integración Unity posterior

La plaza y el récord local no dependen de red. Partida iniciada offline/sin sesión aceptada queda local para siempre; no subir marcas antiguas, importar el top20 ni crear una sesión retroactiva. Si pierde conexión **después** de inicio aceptado, puede terminar y publicar el snapshot dentro de las 72 h originales. No autenticar una cola con una identidad distinta ni renovar sesiones para hacerlas elegibles.

Una cola local separada de `local-records.json` conserva contexto de sesión y cierre inmutable, estado e intento siguiente; escritura atómica con respaldo, máximo 20 pendientes y límite de 16 KiB por payload. No desalojar un pendiente silenciosamente: si se llena, nuevas partidas siguen local con aviso. Al superar expiresAt, consultar estado antes de declarar expiración: un commit anterior con respuesta perdida conserva su recibo accepted aunque la consulta llegue tarde. Sólo expiración/rechazo confirmado por servidor sale como terminal de la cola; sin red, mostrar «ventana vencida, confirmación pendiente». Recibo perdido se recupera por GET/reenvío exacto; reiniciar la app conserva sessionId, runId y bytes. Una caída antes de guardar el cierre no reconstruye una partida usando XP o estado vivo; sólo podrá recuperarse un snapshot local completo con su contexto de sesión. Crash durante juego no crea derrota ficticia.

| Punto existente | Cambio futuro mínimo |
| --- | --- |
| MenuScreen / GameManager.RestartGame | Preparación de sesión antes de Main; reintento conserva modo/personaje. Guardar contexto runtime fuera de SO y pasar runId acordado a Awake sin renombrar campos. |
| GameManager.CloseRun / RunClosed | Conservar prioridad muerte/victoria, Freeze y Save local; capturar envío después. Al abandonar marcar sesión excluida; si no hay red, no bloquear salida. |
| RunResult / StoredRun | Adaptador de red de valores, enums a strings explícitas y long a decimal; conservar esquema/local/campos actuales. |
| C# nuevo acotado | Cliente HTTP/Auth, contexto/cola online y vista compartida; sólo responsabilidades reales, sin ILeaderboardService si no hay un segundo consumidor que lo necesite. Metas nuevos al implementar. |
| RecordsView / GameUi | Conservar Local y ofrecer Compartida; top20, posición, detalle y fecha de cache. Estados Cargando/Sin conexión/Pendiente/Enviado/No elegible. Etiqueta visible «Tabla casual · resultados no verificados». |
| Configuración | URL, publishable key y clientBuildId públicos; credenciales de usuario fuera de SO. Secretos de servidor sólo entorno del backend. No tocar Main/Menu/prefabs con regeneradores. |
| Checks | Nuevo check online en entorno local/test; extender Score/Records/Delivery sólo por puntos reales de integración. Preservar cola de elecciones/pausa/teclado/mouse. |

OnGUI sólo dibuja/recibe acciones; no dispara HTTP por repaint. Cancelar solicitudes de lectura al cerrar una vista y descartar respuestas viejas por generación; respuesta de otra tabla/identidad no reemplaza la actual. Cierre y cola sobreviven al cambio de escena mediante un propietario explícito; desuscribir RunClosed al terminar cada run, sin duplicar callbacks tras R/M.

## 10. Datos, retención y borrado

Público: playerId aleatorio, alias generado, mejor puntaje/tiempo, modo/reglas, personaje/build de juego y detalle validado de la marca visible. Privado: sujeto Auth, sesiones, recibos, UTC, razones de rechazo, contadores y auditoría. Nunca publicar tokens, IP, fingerprints de hardware, correo ni historial de prácticas. No recopilar inputs, chats, ubicación o telemetría por frame para este ranking.

Propuesta de retención: resultados no mejores hasta 90 días; mejores visibles mientras exista perfil/tabla; payload de práctica/abandono se descarta al emitir recibo (sólo motivo codificado). Conservar tombstone mínimo de sesión terminal (owner, IDs, digest, estado/recibo) hasta borrar identidad: asegura idempotencia incluso tras salir del top o purgar detalle. No borrar ese índice a los 90 días y luego admitir el mismo runId como nuevo. Es crecimiento explícito por cierre; medir tamaño y revisar si el uso requiere otro contrato antes de compactarlo.

Sesiones vencidas se convierten en tombstones; rate buckets expiran en 48 h, logs técnicos sin cuerpos/tokens en 7 días y auditoría administrativa sin texto libre en 90 días. Si se necesita correlacionar IP, HMAC con secreto diario sólo servidor y borrar al terminar su ventana; nunca IP cruda en tablas propias. Confirmar retención del gateway/proveedor por separado: no afirmar que la API puede eliminar logs de terceros.

DELETE /v1/me debe bloquear perfil y retirar su marca inmediatamente; la comprobación de perfil impide nuevas escrituras incluso con un JWT aún vigente. Registrar operación de borrado y eliminar sesiones/recibos/resultados/best/datos de perfil y cuenta Auth mediante trabajo reintentable hasta completar, objetivo <=7 días. Mantener sólo el identificador aleatorio de solicitud/estado hasta 7 días después de terminar; no conservar runIds ligados al borrado. Respuesta repetida durante el proceso: misma solicitud; después de eliminación 401/404 sin datos, no recrear perfil automáticamente con ese token.

La eliminación Auth no invalida instantáneamente JWT emitidos según [documentación de usuarios](https://supabase.com/docs/guides/auth/managing-user-data); verificar perfil y sesión vigente es necesario. No autoprovisionar perfiles desde cualquier JWT válido. Antes de borrar tokens locales, registrar el recibo de solicitud; no decir «borrado completado» sólo por limpiar el dispositivo. El usuario puede elegir borrar cola/identidad/cache locales y, por separado, sus récords locales. Si perdió su credencial anónima, no hay recuperación/autoborrado demostrable: soporte puede ocultar o investigar, sin entregar control por conocer alias/runId.

Backups del proveedor pueden conservar datos hasta vencer su retención contratada: informar plazo real antes del piloto y verificar que restauraciones reapliquen solicitudes de borrado pendientes. Responsable del juego debe definir contacto de privacidad, condiciones de participación y tratamiento aplicable con asesoramiento pertinente; este ADR fija comportamiento técnico propuesto, no certifica cumplimiento legal.

## 11. Decisiones previas y ejecución posterior

| Decisión que debe registrar el propietario | Propuesta para avanzar |
| --- | --- |
| ¿Existe ranking/backend útil en Unity Cloud fuera del repo? | Confirmarlo; si existe, comparar su contrato antes de adoptar Supabase. |
| Proveedor/cuenta/organización/región, personas con acceso y recuperación administrativa | Supabase administrado, proyecto de test aislado; elegir región por jugadores y tratamiento de datos, sin asumir residencia. |
| Coste, método de pago si corresponde, alertas, backups y quién responde | Piloto Free sujeto a pausas o Pro desde tarifa vigente; presupuesto propuesto USD 30/mes, no contratado. |
| Identidad y alcance público | Piloto cerrado, Auth anónimo + admisión manual; sin recuperación/multiequipo. Lanzamiento abierto requiere resolver altas abusivas/CAPTCHA. |
| Competencia | Casual explícita; una mejor marca por identidad; no premios/anticheat ni autoridad simulada. |
| Política operativa | Aceptar/revisar 72 h online, cotas, límites, retención/borrado, canal de soporte y moderador; documentar backups. |
| Versiones de juego y plataformas a admitir | Manifiesto de build real aprobado; resolver pendientes humanos de etapa 8 y validación de credenciales/cliente Windows antes de habilitar esa plataforma. |

No hace falta resolverlas para leer/revisar este ADR. Deben quedar registradas antes de ejecutar implementación que dependa de ellas. [Prompts siguientes](BuildsEInfinito-Online-Prompts.md): 9A backend local, 9B integración Unity contra entorno de test autorizado, 9C verificación con dos instalaciones. No incluyen publicar, crear recursos remotos, push ni despliegue; el eventual entorno remoto se habilita en una tarea separada y explícita.

## 12. Verificación de esta etapa

Ejecutado: inspección de Git/remoto/raíz/configuración, búsqueda de guías/backend, lectura de propietarios del flujo local y evidencia documental de etapa 8; consulta de fuentes oficiales y changelog; revisión estática de contrato, enlaces locales e integridad de archivos recibidos; `git diff --check`.

Resultado documental: 10 enlaces locales resueltos, fences balanceados, 828 de 830 archivos recibidos idénticos; los dos restantes son contexto/registro con contenido anterior íntegro como prefijo. Dos archivos nuevos, este ADR y los prompts. Manifiesto SHA-256 inicial y resultado de verificación conservados fuera del repo en la carpeta temporal `conurbano-etapa9-20261106-govg01l5` (ruta exacta en la salida de herramientas); no contienen credenciales. Para repetir la revisión: `git diff --check`, comprobar destinos relativos de enlaces de ambos documentos y contrastar únicamente los apéndices nuevos con el contenido recibido, no atribuir a etapa 9 todo el diff acumulado contra HEAD.

Changelog revisado: [CLI v3 del 4 de noviembre](https://supabase.com/changelog/supabase-cli-v3) cambia destinos/confirmaciones y [exposición de tablas](https://supabase.com/changelog/45329-breaking-change-tables-not-exposed-to-data-and-graphql-api-automatically) exige permisos explícitos. Reconsultar --help y documentación al implementar; no copiar comandos antiguos ni actualizar el proyecto ahora.

No ejecutado aquí: SQL, Auth, HTTP del contrato, migraciones, dos clientes, compilación Unity, Play Mode, FPS, build o despliegue. Unity local existe, pero este cambio es sólo documentación y no necesita ejecutar el motor. Los checks de etapa 8 son evidencia histórica, no pruebas nuevas de red. El protocolo sigue siendo un diseño hasta implementar y superar los tres prompts de verificación.

Para jugar hoy: usar menú/personaje/modo y Récords locales según la guía humana de [etapa 8](BuildsEInfinito-Validacion.md); no hay botón online añadido. La prueba jugable futura con dos instalaciones se describe en 9C, sin confundirla con una prueba ya realizada.
