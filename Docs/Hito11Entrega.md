# Hito 11 — preparación de entrega del MVP

## Estado del proyecto

- Proyecto Unity: `Conurbano Infinito/`, versión **6000.3.23f1**.
- Escenas habilitadas: `Assets/_Game/Scenes/Menu.unity` primero y `Assets/_Game/Scenes/Main.unity` después. `SampleScene` sigue deshabilitada.
- Menú: **Jugar**, **Créditos**, **Salir** y volumen general persistente. Enter inicia y C abre créditos; Esc vuelve desde créditos.
- Partida: Esc pausa y reanuda. En pausa, derrota y victoria se puede reiniciar con R o volver al menú con M.
- El HUD muestra valores y barras de vida y experiencia. El jugador ya tenía destello al recibir daño; los enemigos destellan al recibir proyectiles. Audio original provisional: dos músicas y siete efectos.

## Generar build Windows

La demo actual tiene una build de entrega en `Builds/Windows/Demo-2026-10-16/`, generada con Unity 6000.3.23f1 para Windows x86_64/Mono, sin Development Build. Los controles y el registro de tres partidas están en `PruebaDemo.md`. Sigue pendiente ejecutarla en otra PC Windows.

1. En una computadora con **Unity 6.3 LTS 6000.3.23f1** y el módulo **Windows Build Support** instalado, abrir la carpeta `Conurbano Infinito/` desde Unity Hub. Esperar que termine la importación y confirmar que la Console no tenga errores.
2. Abrir **File > Build Profiles**. Seleccionar el perfil **Windows**, arquitectura **x86_64**, y dejar *Development Build* desactivado. Confirmar que `Menu` sea la primera escena habilitada y `Main` la segunda. `SampleScene` debe seguir deshabilitada.
3. Pulsar **Build** y elegir una carpeta vacía fuera de `Assets`, por ejemplo `Builds/Windows/Hito11/`. Guardar el ejecutable como `ConurbanoInfinito.exe`.
4. Copiar la carpeta `Docs/` del repositorio al lado del `.exe` para que el archivo `Docs/Licencias.md` citado en Créditos acompañe la entrega. Distribuir la **carpeta completa** producida por Unity, incluidos `ConurbanoInfinito_Data/`, `UnityPlayer.dll` y cualquier otra dependencia que Unity genere; no enviar solo el `.exe`.
5. Comprimir esa carpeta completa en ZIP para transferirla. En la otra computadora Windows x86_64, descomprimir y ejecutar `ConurbanoInfinito.exe` desde la carpeta extraída. No hace falta instalar Unity Editor en esa máquina.

## Prueba en otra computadora

1. Confirmar que aparece el menú, que se leen los nombres de ambos integrantes en **Créditos**, que se puede volver con Esc y que el control de volumen afecta la música. Cerrar y abrir de nuevo: el volumen elegido debe conservarse.
2. Elegir **Jugar**. Mover con WASD y flechas. Verificar que los ataques salen solos, que los enemigos y proyectiles tienen tamaño y color legibles, que un impacto hace destellar al enemigo y suena, y que al recibir daño destella el jugador y baja la barra de vida.
3. Recoger experiencia y comprobar barra, valor numérico, sonido de recogida y pantalla de mejora al subir de nivel. Elegir una mejora y comprobar que la partida continúa.
4. Pulsar Esc: el juego debe quedar inmóvil; reanudar con Esc. Volver al menú con M, iniciar otra partida y comprobar que el tiempo, vida y experiencia comienzan de cero.
5. Dejar morir al jugador: debe aparecer **DERROTA**, sonar su efecto, y permitir **R** para reiniciar o **M** para ir al menú.
6. En una partida completa sin aceleración, comprobar la aparición del miniboss a **03:00** y **VICTORIA** a **04:00**, con efecto de audio y opciones R/M. El atajo F9 solo existe en Editor o Development Build y sirve para una comprobación rápida del reloj; no reemplaza esta partida completa.
7. Probar **Salir** en el menú y revisar que el proceso termine. Reabrir y verificar que `Docs/Licencias.md` está junto a la build y que la Console del Editor no registra errores nuevos al repetir el recorrido allí.

## Criterios de aceptación y riesgos pendientes

El hito se acepta si los pasos anteriores pasan en una PC Windows distinta de la de desarrollo, sin referencias faltantes, errores de ejecución ni cortes de audio perceptibles. Los sprites y sonidos son provisionales y originales; la plaza usa piso y decoraciones originales, con Kiosco y Parrilla separados. Las pruebas de rendimiento del hito 10 deben repetirse si se reemplazan estos recursos por assets finales. Las builds de septiembre son anteriores a la demo actual: generar una nueva y probarla en otra PC.
