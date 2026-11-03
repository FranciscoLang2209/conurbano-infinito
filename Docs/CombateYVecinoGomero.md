# Animaciones y Vecino Gomero — 2026-10-23

Francisco informó que siguió probando la demo, que se entiende y funciona bien, y autorizó avanzar con el pulido y el enemigo propuestos. Se conserva la partida de cuatro minutos, sus seis fases, armas, estadísticas existentes y los dos lugares de Quilombo.

## Cambios

- El Pibe camina con seis poses a 10 fps: piernas alternadas y rebote de torso/mochila. Conserva Idle, el parámetro `Walking`, velocidad, input y collider.
- El perro tiene cuatro estados visuales: carrera, preparación agachado, embestida y descanso con lengua afuera. `EnemyCharge` gobierna `ChargePhase`; el Animator no decide daño ni tiempos. Se conservan los 0,65 s de aviso, 0,5 s de embestida y 1,1 s de descanso. La franja amarilla se dibuja por encima del piso: antes su orden de dibujo podía ocultarla detrás de la plaza.
- El anillo de impacto suma chispas. Al morir, el enemigo se comprime, inclina y desvanece durante los mismos 0,22 s. El cuerpo deja de simular inmediatamente y la XP cae al morir. El Animator queda detenido hasta volver al pool; color, escala, rotación, simulación y animación se restablecen al reutilizarlo. Los efectos de enemigos ahora usan tiempo de juego y se congelan al pausar.
- El **Vecino Gomero** entra en las opciones de aparición desde 02:00, con peso 1; no se garantiza una aparición exacta a ese segundo. El aviso de fase explica la línea roja. Los intervalos, límites y pesos de los otros enemigos se conservan.
- Tiene 10 de vida, entrega 3 XP y hace 1 de daño de contacto. Se acerca a 1,3 unidades/s hasta quedar a 5,5 unidades. Luego se detiene, fija la dirección y marca durante 0,9 s una línea roja de 8,5 unidades. Reutiliza el sonido de aviso y su límite de repetición. La piedra sale a 0,45 unidades del centro, viaja a 6 unidades/s, recorre hasta 8 y hace 1 de daño. El siguiente ataque espera 3,5 s. Moverse de costado evita el disparo; acercarse para atacar también es posible.
- Las piedras usan el pool existente. Su barrido circular de radio 0,12 consulta sólo la capa Player, evitando atravesar al jugador por un paso grande de física. Cada piedra impacta una vez; se retira al agotar el alcance o perder al tirador/blanco. La preparación se cancela al morir o desactivarse el enemigo o el blanco. No se añadieron capas ni se modificó la matriz de colisiones.

## Recursos e integración

`Tools/generate_combat_features.py` crea 18 cuadros originales y la piedra con el Canvas y la paleta existentes. Incluye autocomprobaciones de dimensiones, opacidad y poses distintas en los ciclos de caminata/carrera. No requiere dependencias nuevas. Los cuadros se guardan en `Assets/_Game/Art/Animations/Frames/`, a 48 PPU, Point, sin mipmaps ni compresión. El Pibe conserva pivote inferior central; perro, vecino y piedra usan centro.

Los assets ya están integrados. Para reproducirlos: ejecutar el generador y, fuera de Play Mode, **Tools > Conurbano Infinito > Integrar animaciones y Vecino Gomero**. Este menú actualiza PlayerWalk, conecta el Animator del perro y crea los prefabs/controladores del vecino y la piedra; no regenera escenas ni ejecuta los menús antiguos de preparación. No se debe volver a ejecutar la integración de la cuarta slice para aplicar estos cambios: utiliza la caminata anterior.

## Validación

La prueba `CombatFeaturesCheck.RunBatch` se ejecuta en una copia aislada del proyecto, con gráficos habilitados:

```sh
CONURBANO_COMBAT_OUTPUT=/ruta/capturas Unity -batchmode -projectPath /ruta/copia -executeMethod CombatFeaturesCheck.RunBatch -logFile /ruta/validacion.log
```

La comprobación modifica sólo la sesión/copia: entrada simulada, cuerpos y relojes controlados, blancos de prueba y configuración del Editor para entrar a Play Mode. Verifica referencias de assets, inclusión en oleadas, movimiento/animación del jugador, estados del perro, dirección fija de la gomera, esquiva, alcance, barrido rápido, daño único y capas, pausa, cancelación por muerte y reutilización. Ejecuta además ExpansionCheck, PetardoCheck, VisualSlice5Check y ShortRunCheck en sesiones nuevas de Main; termina fuera de Play Mode. Las capturas son fixtures de validación, no una partida humana.

Resultado del 2026-10-23, Unity 6000.3.23f1: compilación del proyecto original con código de salida 0, sin errores ni warnings de C#. La copia aislada terminó con código 0 y `COMBAT_FEATURES_CHECK_PASS`, incluidos Hito 4/9/11, las comprobaciones nuevas y ExpansionCheck, PetardoCheck, VisualSlice5Check y ShortRunCheck. Se verificaron seis poses distintas del Pibe, cuatro estados del perro, daño/capas/barrido/alcance de la gomera, pausa, cancelación, pool, fases, jefe único, victoria, reinicio, derrota y menú. No hubo errores nuevos de gameplay. El arranque de la copia repitió una excepción interna ya documentada de `UnityEditor.Search.SearchDatabase`; se registró aparte (`knownEditorSearchErrors=1`) y no se ocultó del log.

Capturas 1280×720 inspeccionadas: `TestResults/CombatFeatures/avisos-perro-gomero.png`, `descanso-perro.png` y `muerte-enemigo.png`. La inspección detectó y permitió corregir la franja del perro tapada por el piso. Los logs de compilación y validación están en esa misma carpeta, ignorada por Git. No se modificaron escenas, paquetes, entradas ni ProjectSettings en el proyecto original; se conservaron los cambios de UI que ya estaban en el workspace.

La nueva presión desde el minuto dos requiere una partida humana; la validación automática no demuestra balance ni diversión. No se agregó otra arma ni se generó una build de entrega en esta iteración.

## Candidato de dificultad posterior — 2026-10-23

La etapa 1 de `PlanDificultad.md` redujo los intervalos y aumentó los límites de vivos; los valores actuales están en `PartidaCorta.md`. Tras informar Francisco victorias sin mucho esfuerzo con el candidato actual y rutas variadas, la etapa 2 sube la vida del Gomero a **12** y la del perro a **10**, además de Paloma 15 y Bolsa 6. Se conservan el peso 1 y entrada del Gomero desde 120 s, daño, avisos, velocidades, pool y la densidad de etapa 1. Las cifras de 10 de vida del Gomero y la conservación de intervalos/límites anteriores describen la integración original.

CombatFeaturesSetup quedó sincronizado con los 12 de vida del Gomero; no se ejecutó la integración para balancear. CombatFeaturesCheck ahora comprueba daño, muerte, XP única y vida completa al reutilizar los seis prefabs, en dos ciclos cada uno. La validación técnica de resistencia se registra en `PlanDificultad.md` y `TestResults/Difficulty/Resistance/`; la devolución humana posterior se recoge a continuación.

Francisco encontró mejor la resistencia pero escasa la presión al adquirir Aerosol. La ronda posterior conserva esas vidas y reduce sólo los intervalos de aparición desde 30 s; el tramo del Gomero pasa de 0,85 a 0,75 s, con los mismos pesos, tope y comportamiento. Valores actuales y evidencia en la sección 12 de `PlanDificultad.md`. La evaluación del nuevo candidato de frecuencia sigue pendiente.
