# Ranking casual — prompts posteriores a la etapa 9

Fecha: 2026-11-06. **Plan sin ejecutar.** Contrato: [ADR-001](BuildsEInfinito-Online-ADR.md). Ejecutar una tarea por vez después de registrar las decisiones de su sección 11. Estos textos no son autorización vigente para crear servicios, desplegar ni ejecutar todas las tareas. Mantener los pendientes humanos/Windows de [etapa 8](BuildsEInfinito-Validacion.md) visibles.

## 9A — Backend local y contrato comprobable

```text
Trabajamos en FranciscoLang2209/conurbano-infinito. Aplicá la instrucción común
de Docs/BuildsEInfinito.md. Leé AGENTS.md si existe, la guía local, contexto,
Docs/BuildsEInfinito-Online-ADR.md y las decisiones registradas por el dueño.
Implementá solamente 9A del plan online, después de confirmar en esos documentos
proveedor y alcance. No crees recursos remotos ni despliegues, no hagas push.

Inspeccioná git status y preservá trabajo ajeno. Confirmá raíz Unity
"Conurbano Infinito/"; el backend va fuera de Assets, en Backend/.
No modifiques gameplay, escenas, prefabs, enums, GUIDs, paquetes Unity o JSON local.
Si proveedor elegido difiere del ADR, primero adaptá el contrato explícitamente.

Implementá el contrato v1 completo con proveedor local de Auth, función HTTP y
Postgres: perfiles pendientes/admitidos, sesión antes de jugar, UTC servidor,
propiedad, versiones permitidas y expiración 72 h, cierre único, exclusiones,
validaciones exactas de long, mejor marca/top20/posición y detalle público.
Sin alias libre. Separá IDs Auth/públicos y no publiques snapshots privados.
Generá manifiesto desde IDs/datos reales del proyecto sin regenerar catálogo.
Application.version=1.0 no identifica artefacto: definí clientBuildId público.

Aplicá transacciones/bloqueos/índices únicos para inicio, finish y best.
Mismo ID/payload normalizado devuelve recibo original; otro payload da 409;
no renueves ventana ni reabras rejected/excluded/expired/superseded.
Conservá tombstones tras purgar detalle y fuera del top. Implementá límites
compartidos entre instancias, Retry-After, errores estables, suspensión/ocultación
administrativa y borrado reintentable con retiro inmediato de tabla.
No borres/recrees perfiles automáticamente a partir de un JWT después de DELETE.

Credenciales de servidor sólo en entorno; ningún service_role/DB password en
cliente/ejemplos/logs. Tablas privadas sin acceso REST de anon/authenticated;
rol SQL acotado, consultas parametrizadas y privilegios explícitos.
Revisá documentación actual, changelog y --help antes de usar CLI; fijá versiones.
No instales/actualices herramientas globales ni apuntes al proyecto remoto.

Dejá una suite local reproducible que pruebe:
- Dos identidades, sesión ajena y filtrado de datos; JWT inválido/vencido,
  perfil pendiente/suspendido/deleting y token emitido antes de borrar cuenta.
- Inicio repetido idéntico/distinto, dos inicios concurrentes, sustitución,
  finish duplicado concurrente, respuesta perdida y rollback de DB.
- Práctica/F9 declarada, QA, abandono, cambio de modo/reglas/personaje/build,
  ausencia de inicio, sesión vencida y replay tras purgar resultado.
- Long >2^53 y límites int64, NaN/Infinity/negativos, exceso de cuerpo/listas,
  campos duplicados, sumas, POIs, cotas de jefe, requisitos/IDs de build,
  clásico derrota a 240/victoria a 240, infinito sin victoria y pausas largas.
- Top/posición con >20 perfiles, empates exactos, reglas/modos separados,
  varias marcas por persona, moderación, borrado y cuenta eliminada.
- 429 en dos instancias (sin eximir duplicados), fallo de proveedor/SQL,
  acceso directo a tablas y rutas privadas denegado con claves públicas.

Medí consultas de top/posición con fixtures y EXPLAIN. No presentes cotas o
checks de payload como prueba de simulación honesta. Si falta runtime local
o proveedor, entregá comandos exactos y pendientes sin afirmar tests pasados.
Documentá setup local, fixtures separados de producción, variables públicas/
secretas, comandos/salidas, retención/limpieza/borrado y operación de moderación.
Actualizá contexto y registro; ningún servicio remoto ni deploy en esta tarea.
```

## 9B — Integración Unity sin alterar el récord local

```text
Aplicá la instrucción común en FranciscoLang2209/conurbano-infinito. Leé guías,
contexto, ADR-001, decisiones registradas e informe 9A. Implementá solamente 9B.
Confirmá raíz Unity y git status. Preservá cambios locales, GUIDs/fileIDs/enums/
campos, escenas, prefabs, SO, arte y paquetes. Sin generadores ni migración IMGUI.
No crees recursos remotos, no despliegues ni hagas push. Usá el entorno local/test
que el dueño haya identificado explícitamente; no adivines URL/credenciales.

Usá UnityWebRequest ya disponible y DTO de red separado; no agregues SDK ni
framework de servicios. Resolvé propietario de Auth/contexto/cola entre Menu
y Main con el mínimo código, sin estado de sesión en SO ni callbacks duplicados.
Persistí refresh token con almacén nativo seguro de cada plataforma admitida;
si no está disponible, seguí local. Nunca PlayerPrefs ni logs de tokens.
Un solo refresh en vuelo; verificá rotación y caída/reapertura.

Opt-in online desde menú con explicación casual/datos públicos; perfil pendiente
no puede publicar. Mantener Local y agregar Compartida con top20, posición propia,
detalle, fecha de cache y etiqueta "Tabla casual · resultados no verificados".
Usá GameUi, fuentes/tamaños y navegación existentes; no otro HUD grande.
OnGUI no inicia red por repaint. Descartá respuestas de vistas/tablas anteriores.

Prepará sesión antes del primer tick activo, también en R/reintento. Guardá runId
una vez y pasá contexto a GameManager sin romper APIs legacy de checks. Inicio
sin aceptación en 5 s reales: juego local con otro runId, sin promoción posterior.
Pausa y elecciones mantienen su cola y no dependen de respuestas HTTP.

Guardado local y cierre congelado antes de encolar envío desde RunClosed;
no modificar ScoreSystem/RunResult tras cierre ni consumir recompensas desde red.
Cola atómica separada de récords, máximo 20, contexto y payload inmutable;
cuando se llena avisar y continuar local sin descartar pendientes en silencio.
Reintentar mismo ID/payload, confirmar recibo tras timeout, sin nuevas sesiones
para reenviar resultados antiguos. Offline desde inicio nunca se sube.
Después de expiración queda local; 401 refresh único, 429 Retry-After y backoff
real independiente de timeScale; errores definitivos sin bucle.
DELETE propio debe solicitarse/confirmarse en servidor antes de limpiar token;
separá borrar identidad/cola/cache de borrar récords locales.

Agregá check acotado de integración con servidor de prueba/errores controlados:
payload long exacto, orden/valores/enums, falso eligibility, práctica permanente,
abandono, doble input/cierre, R/M y varias escenas sin doble suscripción,
pausa/niveles múltiples, timeout después del commit, reconexión/reinicio,
rotación Auth, cola llena/corrupta, sesión expirada, identidad distinta y borrado.
QA/fixtures sólo en entorno test y marcados práctica; no contaminar marcas reales.

Ejecutá checks pertinentes de Score/Records/Reload/Delivery y UI/personajes
en copia aislada, usando entradas reales. Revisá 1280x720, 1024x768, 1920x1080,
2560x1080, teclado/mouse/foco/volver/reintento y respuesta tardía.
Registrá plataforma/Unity, comandos/logs, test realmente ejecutado y pendientes.
Si no hay Unity, no afirmes compilación/Play Mode/build. Actualizá contexto y
registro; no presentes esta tarea como lanzamiento online ni balance aprobado.
```

## 9C — Verificación completa con dos instalaciones

```text
Aplicá instrucción común y ADR-001 sobre resultados de 9A/9B. Esta tarea verifica
el flujo completo; no crea servicios remotos, despliega, publica ni hace push.
Si no existe un entorno de test autorizado y accesible para ambos clientes,
dejá preparación/reproducción y registrá ese bloqueo; no uses producción.
Identificá backend/commit/política, artefactos/IDs de build, Unity, SO y hardware.
Usá dos instalaciones A/B con persistentDataPath/credenciales separados,
preferentemente dos máquinas y una Windows una vez disponible su módulo/equipo.
Dos ventanas con la misma carpeta/identidad no cuentan como dos instalaciones.

Registrá antes el estado de tablas test y cada identidad admitida. Prohibido
copiar tokens a logs. Separá HTTP de fixtures, Play Mode y partidas de player.
Podés aceptar builds QA sólo en entorno test con reglas de prueba explícitas;
no quitar la marca práctica de producción para obtener puntos en una prueba.

Recorrido humano A/B con teclado/mouse:
1. Activar Compartida, comprobar alias distinto y tabla sin marca propia.
2. A juega clásico con un personaje; pausa y elige varias mejoras en cola;
   cierra en derrota/victoria y guarda local primero. B refresca, ve su marca
   y detalle; A ve la misma posición/datos que B para esa tabla.
3. B juega con el otro personaje; probar infinito >240, muerte, jefes y POIs;
   A consulta esa tabla. No mezclar modos/versiones ni dos registros por persona.
4. Reabrir ambos players; misma identidad, récord local y resultado remoto.
   Superar una marca actualiza sólo su mejor fila y posición coherente.

Inyectá fallos de red/proveedor de forma controlada, con logs de servidor:
- Cortar red antes de inicio: local, nunca se importa después.
- Cortarla durante juego tras inicio aceptado: cierre local, pendiente y
  posterior envío dentro de ventana con mismo sessionId/runId/payload.
- Perder respuesta después de commit, reiniciar A, reenviar exactamente;
  un recibo/resultado, misma marca, sin duplicados incluso fuera del top.
  Repetir consulta después de expiresAt debe recuperar el commit previo, no
  convertir una respuesta perdida en expiración ni crear una sesión nueva.
- Duplicados concurrentes en API, payload cambiado con mismo ID (409), sesión
  ajena/sin inicio/expirada/superseded, tokens vencidos, 429 y 503/SQL rollback.
- Práctica/Editor/QA/F9/tiempo acelerado y abandono quedan excluidos; un cliente
  hostil que oculta flags sigue siendo amenaza conocida, no test anticheat pasado.
- Cambiar reglas/build/personaje respecto del inicio, números negativos/>int64,
  sumas imposibles, categorías/IDs inválidos, exceso de cuerpo y duración.
- Sembrar >20 perfiles sólo con fixtures test; empates, misma persona con varias
  marcas, separación de reglas históricas, top/posición atómicos y moderación.
- Suspender/ocultar un perfil: desaparece de tabla; reenvío no lo restaura.
  DELETE en A retira datos y acceso inmediatamente, B ya no ve la marca;
  token anterior no crea otra sesión. Completar borrado/retención verificables.

Controles de privacidad/operación: publishable key no puede leer/escribir tablas,
no secretos en artefacto/logs, alias generated-only, canal de reporte operativo,
retención de gateway/backups documentada, apagado de admisión no rompe partida
local ni solicitud de borrado. Confirmá propiedad real de IP para límites.

Entregá Docs/BuildsEInfinito-Online-Validacion.md: matriz casos/esperado/observado/
pasado-fallido-no ejecutado, comandos/entradas reales, códigos HTTP, IDs de test,
logs sanitizados, capturas UI, recibos y conteos DB antes/después. Diferenciá
partida humana, fixture y simulación; no afirmar build/FPS/Windows sin evidencia.
Repetí checks existentes sólo donde integración/fallos lo justifiquen.
Restaurá/limpiá únicamente datos de test autorizados y registrá qué quedó.
Actualizá contexto y registro con defectos/limitaciones. Sin publicación.
```

La aceptación futura exige estas pruebas observadas. La existencia de estos prompts, una prueba HTTP aislada o la build macOS previa no acredita conexión real entre dos instalaciones.
