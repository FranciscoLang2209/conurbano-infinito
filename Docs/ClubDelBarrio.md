# El Club del Barrio

## Primer avance: personajes seleccionables — 2026-10-30

Implementado el primer paso acordado de la expansión: selector y segundo personaje completo. Pelota de Potrero y Canchita se incorporaron en los avances descritos más abajo. El Patovica sigue pendiente.

| Personaje | Vida inicial | Velocidad | Arma inicial | Adquisición ofrecida en el primer nivel |
| --- | ---: | ---: | --- | --- |
| El Pibe del Barrio | 5 | 5 | Termo Balístico | Aerosol |
| La Piba del Club | 4 | 5,5 | Boleadora | Termo Balístico |

Los atributos de La Piba son una primera configuración para probar. Tiene sprite propio y seis poses de caminata, chaqueta del club, coleta y boleadoras. Reutiliza las transiciones `Walking` del jugador con un AnimatorOverrideController. El arma sigue siendo la Boleadora existente.

Desde Menu, Jugar abre el selector. Flechas izquierda/derecha o Tab cambian la tarjeta; Enter/Espacio inicia y Esc cancela. Con mouse se elige una tarjeta y luego A la plaza. Cancelar no cambia el personaje seleccionado. Al volver al menú, el selector recuerda el último personaje jugado; R conserva ese personaje y reinicia vida, velocidad, armas, mejoras y XP. La selección dura durante la sesión de juego y se reinicia al volver a entrar en Play Mode o abrir otra vez la aplicación.

Los perfiles son ScriptableObjects en `Assets/_Game/Resources/Characters/`. PlayerController aplica vida, movimiento y animación; WeaponController toma el arma inicial en Awake, sin depender del orden de Awake del jugador. La configuración de vida también es válida antes o después del Awake de Health. UpgradeSelection toma la primera adquisición del perfil y conserva el filtrado de armas ya equipadas y el límite de cuatro espacios.

Se agregó Equipar Termo Balístico al catálogo de mejoras de Main. El Pibe ya lo tiene equipado y no recibe esa adquisición; La Piba puede conseguirlo. El resto de armas y mejoras conserva sus reglas. No se modificaron los intervalos, vidas de enemigos, POIs, duración ni recompensas de Quilombo.

## Recursos y reproducción

`python3 Tools/generate_club_art.py` genera el idle y seis poses de La Piba, más dos retratos para el selector, con Canvas y la paleta existentes. Incluye aserciones de opacidad y poses distintas. Arte original, sin dependencias nuevas.

Los assets están integrados: no hace falta ejecutar preparación ni conectar referencias. Para reconstruirlos, fuera de Play Mode y con las escenas guardadas, ejecutar **Tools > Conurbano Infinito > Integrar personajes del Club**. Ese menú actualiza únicamente los nuevos recursos y la adquisición del Termo. Las caras del selector usan la misma conversión de color de BarrioUiImport, en una carpeta separada de los 26 recursos originales de interfaz.

Punto de recuperación anterior: `/Users/franciscolang/.codex/backups/videojuegos/club-2026-10-30-inicio/`, con 589 archivos y hashes verificados. Incluye todos los cambios locales previos; no usar git restore sobre el trabajo anterior de UI, combate o dificultad.

## Validación

Unity 6000.3.23f1, Metal/macOS. Compilación e integración inicial en el proyecto; pruebas de Play Mode con gráficos en una copia aislada. Assets y Packages originales se compararon byte a byte con la copia final: 533 archivos sin diferencias. ProjectSettings del proyecto original se preservaron; sólo la copia cambia las preferencias de pruebas y el tamaño de Game View.

| Comprobación | Resultado |
| --- | --- |
| CharactersCheck.RunBatch | Pasó: catálogo, selector con teclado, cancelación, cambio y recuerdo del personaje |
| Perfiles de ambos personajes | Pasó: vida actual/máxima, velocidad, arma inicial, desplazamiento, animación, idle y pausa |
| Primeras mejoras de La Piba | Pasaron 20 ofertas de tres opciones únicas y válidas, sin readquirir Boleadora; adquisición real del Termo |
| Reinicio | Pasó: se conserva personaje y se eliminan daño, velocidad mejorada, armas adquiridas y XP |
| ShortRunCheck | Pasó para ambos personajes: primera adquisición, pausa, fases, Jefa única, victoria, reinicio, derrota y menú |
| CombatFeaturesCheck | Pasó: perro, Gomero, daño, XP única, seis prefabs y doce ciclos de pool; Expansion, Petardo y efectos previos |
| UiRedesignCheck | Pasó: HUD, equipo y mejoras, navegación, pausa, créditos, resultados y nuevo paso del selector |
| Inspección visual | Selector completo sin recortes a 1024×768, 1280×720 y 2560×1080; también capturado a 1920×1080. La Piba visible con 4/4 de vida y Boleadora |

Los tres logs finales terminan con código 0 y sus marcadores PASS. Cada arranque de Play Mode repitió la excepción interna preexistente de `UnityEditor.Search.SearchDatabase`; se cuenta aparte como `knownEditorSearchErrors=1`, conservando su traza. No hubo errores nuevos de gameplay. Las primeras corridas de personajes/UI terminaron con código 1 al contabilizar esa excepción como error de juego; se conservaron y se ajustó únicamente su clasificación, como ya hacía CombatFeaturesCheck. También se corrigió la espera del fixture para aguardar el marcador final de ShortRunCheck en vez del nombre de escena.

Evidencia local, ignorada por Git: `TestResults/Club/Characters/validation.log`, `TestResults/Club/Regression/combat.log`, `TestResults/Club/Ui/validation.log` y capturas en esas carpetas.

Para repetir personajes en una copia aislada:

```sh
CONURBANO_CHARACTERS_OUTPUT=/ruta/absoluta/capturas Unity -batchmode -projectPath /ruta/copia -executeMethod CharactersCheck.RunBatch -logFile /ruta/validacion.log
```

No se generaron nuevas builds de distribución ni se midieron rendimiento o dificultad humana. Las entradas con mouse están implementadas mediante los botones IMGUI existentes; la automatización ejercitó teclado. El selector tiene dos tarjetas; al incorporar un tercero habrá que ampliar su distribución.

## Segundo avance: Pelota de Potrero — 2026-10-30

La séptima arma puede aparecer entre las adquisiciones al subir de nivel, para ambos personajes, mientras haya espacio entre las cuatro armas. Se conservan las primeras ofertas propias de cada personaje; no se garantiza que la pelota aparezca en una elección concreta.

| Parámetro inicial | Valor |
| --- | --- |
| Daño por impacto | 2, antes de mejoras y bonus globales |
| Espera entre disparos | 1,8 s |
| Velocidad | 8 unidades/s |
| Alcance inicial y búsqueda de siguiente blanco | 5 unidades |
| Rebotes | 2: hasta tres enemigos distintos por pelota |
| Mejoras | Daño +1, cadencia ×0,9 y rebotes +1; tres rangos cada una |

Tras cada impacto, la pelota cambia de dirección hacia el enemigo vivo no golpeado más cercano dentro de alcance. Vuela físicamente hacia esa posición; no teletransporta el daño ni persigue al blanco después de fijar la dirección. Si se queda sin blancos o sin impactos disponibles, vuelve al pool. Cada tramo también termina al recorrer su alcance. Con los tres rangos de rebotes alcanza hasta seis blancos distintos. No hace daño al jugador ni repite daño sobre un enemigo por tener varios colliders o seguir en contacto.

Reutiliza Projectile, el prefab y el pool existentes. Ricochet y WeaponRicochets se agregaron al final de sus enums, conservando los valores serializados anteriores. El campo `pierce` representa los impactos adicionales y, en esta arma, los rebotes. WeaponController conserva los bonus por arma; UpgradeSelection y el HUD ya cuentan la mejora nueva sin añadir otra pantalla.

Recursos originales: `Art/Sprites/Weapons/PelotaDePotrero.png` de 48×48 a 48 PPU, Point, pivote central y escala 0,35; `Resources/BarrioUi/weapon_ball.png` de 40×40 para HUD y cartas. Tools/generate_club_art.py genera ambos; generate_ui_art.py reproduce también el icono desde la misma función. Se regeneró la UI y se verificó que sus 26 texturas anteriores conservaran sus bytes. Se mantuvieron también las poses y retratos anteriores de los personajes.

Los recursos y las cuatro cartas ya están integrados en Main; no hace falta configurar el Inspector. Para reconstruir únicamente esta parte, ejecutar **Tools > Conurbano Infinito > Integrar Pelota de Potrero**, fuera de Play Mode y con las escenas guardadas. No ejecutar la preparación de personajes para incorporar la pelota. La integración de esta ronda se hizo en una copia aislada, preservando la sesión abierta del Editor; Main recibió sólo cuatro referencias nuevas al catálogo.

Checkpoint previo: `/Users/franciscolang/.codex/backups/videojuegos/club-2026-10-30-pre-pelota/`, con 629 archivos y hashes verificados. Incluye los personajes y todos los cambios locales anteriores.

### Validación de la pelota

Unity 6000.3.23f1, Metal/macOS, copia aislada con gráficos. BallCheck.RunBatch compiló y pasó con el tamaño final del proyectil. Verificó adquisición y filtrado para ambos personajes, las tres mejoras hasta su rango máximo, desaparición de cartas agotadas, límite de cuatro armas, daño y seis impactos al mejorar rebotes, vecino más cercano, capas, enemigos muertos, colliders duplicados, falta de vecinos, alcance y reutilización pelota → Termo → pelota.

Una prueba adicional con cuerpos y colisiones reales confirmó tres impactos de la versión base y un cuarto enemigo intacto. Comprobó que pausa congela desplazamiento y daño. Las capturas de gameplay, las tres cartas y el HUD N2 se inspeccionaron a 1280×720. Los enemigos de esa captura son fixtures de física; no representan una partida humana.

Pasaron también CombatFeaturesCheck (incluye Expansion, Petardo, efectos, pool y ShortRun) y UiRedesignCheck, actualizado para siete armas y 27 texturas. La prueba de UI conservó su recorrido y capturas de cuatro resoluciones. Cada arranque repitió la excepción interna ya conocida de SearchDatabase, registrada aparte; no hubo errores nuevos de gameplay ni errores de C#.

Evidencia: `TestResults/Club/Pelota/validation.log`, `Regression/combat.log`, `Ui/validation.log` y las capturas de esa carpeta. La primera validación con escala 0,7 también pasó; se redujo a 0,35 tras inspeccionar el tamaño y se repitió la prueba de física. Los logs se conservan. No se generó build de distribución ni se midieron balance humano o rendimiento.

Para repetir en una copia aislada:

```sh
CONURBANO_BALL_OUTPUT=/ruta/absoluta/capturas Unity -batchmode -projectPath /ruta/copia -executeMethod BallCheck.RunBatch -logFile /ruta/validacion.log
```

## Tercer avance: Canchita del Club — 2026-10-30

Tercer POI en Main, disponible para ambos personajes y ubicado al azar por QuilomboManager junto al Kiosco y la Parrilla. Reutiliza los ocho puntos existentes, con los mismos mínimos de 15 unidades al jugador y 10 entre lugares.

- Resistir **seis segundos seguidos** dentro del círculo amarillo recupera **hasta dos puntos de vida** y añade **10 de Quilombo**, una vez por partida.
- Salir antes reinicia el progreso. La pausa congela el desafío. El aviso muestra de antemano la recuperación y el coste de Quilombo.
- La curación respeta la vida máxima, incluidas mejoras, y no revive ni reinicia la invulnerabilidad. Completar con vida llena consume igualmente la recompensa y añade Quilombo; no se guarda una curación para otra visita.
- La muerte durante el desafío impide completarlo. Reiniciar restaura progreso, recompensa y Quilombo para los tres lugares.

Los umbrales 10/25 y sus bonus 0,15/0,35 se conservan, sin élites. Las nuevas combinaciones son:

| Lugares completados | Quilombo | Multiplicador de oleadas |
| --- | ---: | ---: |
| Canchita sola | 10 | 1,25 |
| Kiosco + Canchita | 20 | 1,35 |
| Parrilla + Canchita | 25 | 1,60 |
| Los tres | 35 | 1,70 |

Kiosco + Parrilla sigue dando 25 y multiplicador 1,6. No se cambiaron intervalos, topes base, enemigos ni duración. El tope teórico del cierre con los tres lugares es `ceil(55 × 1,7) = 94` comunes, más La Jefa si vive; esto no es una medición de rendimiento.

Health añade Heal y RiskZoneData añade Healing al final del enum, preservando los valores de las recompensas anteriores. RiskZone conserva el temporizador y el bloqueo de una sola recompensa; consulta la vida del jugador para impedir completaciones tras morir. Los avisos dan prioridad al desafío activo y luego a la recompensa más reciente: el bonus del Kiosco continúa aunque su panel ceda espacio. GameHud busca posiciones libres en el mismo borde para los tres indicadores, sin crear un sistema de UI nuevo.

Arte original: `Art/Sprites/Environment/CanchitaDelClub.png`, 192×192, 48 PPU, Point, sin compresión/mipmaps, pivote central y escala 1. Cancha, arcos, bandera del club, banco y pelota usan Canvas y la paleta existente en Tools/generate_club_art.py. El círculo amarillo de 53 píxeles coincide con el trigger de radio 1,1 y su desplazamiento vertical de −2/48. El renderer usa orden 0 y z=1 como los otros POIs, por encima del piso y detrás de los actores. La primera inspección detectó el sprite tapado por el piso con orden −1; se corrigió sólo la cancha y se repitieron las capturas.

Ya está integrado. Para reconstruir únicamente esta parte, fuera de Play Mode y con las escenas guardadas, usar **Tools > Conurbano Infinito > Integrar Canchita del Club**. No hace falta ejecutar otras preparaciones. La autoría se hizo en una copia aislada para preservar el Editor abierto; se incorporaron a Main sólo los cinco bloques del objeto nuevo, su raíz y una referencia en QuilomboManager. Los datos de Kiosco y Parrilla y los recursos de personajes/armas anteriores se conservaron.

Checkpoint: `/Users/franciscolang/.codex/backups/videojuegos/club-2026-10-30-pre-canchita/`, con 645 archivos y hashes verificados.

### Validación de la Canchita

PitchCheck.RunBatch usa Unity 6000.3.23f1, Metal/macOS, en una copia aislada. Comprueba 100 semillas de colocación, las tres zonas registradas y sus referencias, importación, curación con límites y vida fraccional, vida máxima mejorada, invulnerabilidad y ausencia de resurrección. Para cada personaje, usa el cuerpo y los contactos reales para entrar/salir, pausa, reinicio de progreso y una resistencia completa de seis segundos, sin acelerar ese reloj. Verifica recompensa única, vida llena y muerte durante el desafío.

Las seis permutaciones de visita usan contactos reales y fixtures de progreso para no esperar todos sus temporizadores. Comprueban cada suma de Quilombo, los bonus 10/25 sin élites, el intervalo efectivo con Q=35 y reinicio limpio. QuilomboSliceCheck conserva los casos originales de Kiosco/Parrilla; su fixture de Edit Mode inicializa ahora Health antes de simular entradas. No se inyectan puntos de Quilombo para alcanzar etapas.

PoiIndicatorsCheck cubre las 1.536 combinaciones de ocho direcciones para tres indicadores en tres formatos. Se capturaron además tres indicadores juntos en un borde a 1024×768, 1280×720, 1920×1080 y 2560×1080. Las capturas de resistencia, recuperación y desafío con bonus del Kiosco activo se inspeccionan en `TestResults/Club/Canchita/`; son fixtures, no partidas humanas ganadas.

El fixture espera el primer Update de la escena y un paso de física después de mover al jugador; una espera fija de 100 ms produjo una entrada no procesada y se conservó ese fallo. Las pruebas temporales y sus logs permanecen en TestResults, fuera de los assets del proyecto.

La ejecución final de PitchCheck terminó con código 0 y `PITCH_CHECK_PASS`. CombatFeaturesCheck también pasó sin modificar sus aserciones: incluye animaciones, perro, Gomero, daño, pool, Expansion, Petardo, efectos y ShortRun. Sus dos primeros intentos fallaron en comprobaciones temporales antiguas del destello y de la animación al pausar. Una corrida instrumentada y la repetición final sin instrumentación pasaron; las sondas registraron el reloj detenido durante pausa y el destello terminado tras avanzar el reloj de juego. No se confirmó la causa de los fallos intermitentes y no se modificaron PlayerController, HitFlash ni esas pruebas para obtener el resultado. Se conservan `Regression/combat-initial-flash-timing.log`, `combat-second-pause-timing.log`, `combat-diagnostic.log` y las sondas externas a Assets.

Assets y Packages del proyecto se compararon byte a byte con la copia probada: 555 archivos sin diferencias. Los arranques vuelven a registrar la excepción interna preexistente de SearchDatabase, contada por separado; las corridas finales no registran errores nuevos de gameplay ni de C#.

UiRedesignCheck también terminó con código 0 y `UI_REDESIGN_PASS`, conservando el recorrido de HUD, mejoras, pausa, menú, créditos, victoria y derrota en cuatro resoluciones. Logs finales: `TestResults/Club/Canchita/validation.log`, `Regression/combat.log` y `Ui/validation.log`. La auditoría final confirmó los tres assets nuevos con sus `.meta`, los cinco bloques nuevos de Main y únicamente dos bloques anteriores modificados para registrar la raíz y el tercer elemento.

Para repetir en una copia aislada:

```sh
CONURBANO_PITCH_OUTPUT=/ruta/absoluta/capturas Unity -batchmode -projectPath /ruta/copia -executeMethod PitchCheck.RunBatch -logFile /ruta/validacion.log
```

No se generó build de distribución ni se midieron balance humano o rendimiento con los tres lugares. El procedimiento de carga se actualizó para usar la vida inicial del perfil de personaje en la copia de prueba; editar sólo Health ya no evita la configuración de Awake del personaje.

## Siguiente incorporación prevista

Patovica: resistencia frontal y ventana de vulnerabilidad al preparar un golpe, con aviso legible e integración en oleadas y pool.
