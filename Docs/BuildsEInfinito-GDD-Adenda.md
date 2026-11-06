# Adenda de alcance — builds, infinito y competencia local

Fecha: 2026-11-06. Complementa el GDD v0.1 mencionado en el plan. El original no está en este repositorio: esta adenda no afirma haberlo cotejado ni reemplaza sus reglas de identidad, humor o dirección artística. Contrato detallado: [BuildsEInfinito.md](BuildsEInfinito.md). Evidencia y pendientes: [validación de etapa 8](BuildsEInfinito-Validacion.md).

## Experiencia implementada

- Juego local 2D en la plaza existente, ataques automáticos, WASD/flechas y mouse/teclado para menús. IMGUI/GameUi, tipografía legible y disposición irregular de objetos conservadas.
- **Clásico:** sobrevivir cuatro minutos activos. La Jefa aparece a 03:00; no hace falta matarla para ganar a 04:00. Una muerte antes de la confirmación de victoria del frame tiene prioridad.
- **Infinito:** misma apertura y plaza, sin plazo de victoria. Desde 04:00, tramos cada dos minutos escalan nuevas apariciones desde su base. Jefa a 03:00/07:00/11:00…; una ventana se omite si sigue viva la anterior. Termina al morir o abandonar. Pausa y elecciones congelan el tiempo activo; reintento conserva personaje y modo.
- Dos personajes actuales: El Pibe con +10% de daño en Termo/Boleadora y La Piba con un rebote adicional sólo para Ricochet. Se mantienen sus vidas, velocidades, armas y primeras ofertas.
- Siete familias de armas, cuatro espacios y nivel runtime 1–8; adquirir empieza en 1, cada carta dirigida válida suma uno. Se conservan máximos individuales. Las definiciones ScriptableObject no guardan progreso de sesión.
- Seis pasivas y cuatro espacios. Mejorar una propia no consume otro. Antena aumenta radio de Petardo; Bolsa agrega un proyectil a Termo/Boleadora recién al nivel 3. Las compatibilidades y cupos filtran ofertas.
- Tres sinergias: Mate (Termo N4/Yerba N3), Potrero (Pelota N4/Zapatillas N3), Festejo (Pelota N4/Petardo N4). Aviso breve, consulta en pausa y carga visible sin panel permanente grande.
- Dos evoluciones por carta explícita: Termo N8/Yerba N5/jefe derrotado y Pelota N8/Zapatillas N5/jefe derrotado. Conservan familia, espacio y mejoras. Dos recetas listas se priorizan por ID; matar al jefe no evoluciona silenciosamente.
- Hasta tres ofertas válidas distintas y cola de subidas. Si se agotan, curación de 10% de vida máxima, mínimo uno, o continuar con vida llena; sin puntos extra por fallback.
- Tres POIs de un uso; Quilombo aumenta presión y puntos por bajas. Presupuestos de 150 comunes + un jefe, 256 gemas y 256 proyectiles/visuales compartidos. XP excedente se agrupa completa en una gema existente, incluso lejana; tiros nuevos pueden omitirse al llenar su cupo.

## Competencia implementada y sus límites

Puntaje en centipuntos long exactos, desglose y resultado congelado al cierre. Se premian bajas válidas, POIs y segundos completos activos. No daño bruto, XP, nivel, despawn ni mejoras sobrantes. Las categorías de enemigos son explícitas y el pool reinicia cada vida.

Top20 local por modo y versión de reglas, desempate por tiempo activo y runId, último resultado, build y consulta desde menú/fin. JSON esquema 1, promoción atómica/respaldo/recuperación; formatos desconocidos se conservan sin sobrescribir. Práctica y abandono no entran al top; F9/controles QA marcan práctica de forma permanente, incluso si se apagan. Editor y batchmode son práctica. El último resultado puede mostrar esas sesiones excluidas.

La tabla compite sólo en una misma instalación/equipo. Capturas permiten comparación manual sin garantía de honestidad. No hay cuenta, alias online, clasificación compartida, servidor ni verificación autoritativa. Un hash/seed/secreto del cliente no probaría una partida. Etapa 9 sigue siendo una especificación opcional posterior, sin servicios ni despliegues autorizados aquí.

## Diseño pendiente de aceptación

Las reglas anteriores están integradas y tienen comprobaciones técnicas. Las curvas y topes son parámetros iniciales; no se presenta el balance como terminado. Falta prueba humana de progresión y utilidad de tres especializaciones, tiempos naturales de sinergia/evolución, evaluación de Aerosol y coste de Bolsa N1/N2. La evidencia acelerada usa fixtures o una ruta automática y se distingue de una partida humana.

La medición corta en hardware declarado no certifica 60 FPS sostenidos ni memoria estable indefinidamente. Windows requiere su módulo y ejecución en equipo correspondiente. No se agregan personajes, mapas, fusiones, metaprogresión, desafíos diarios o ranking online a esta entrega.
