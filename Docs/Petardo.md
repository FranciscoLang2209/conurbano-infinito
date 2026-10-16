# Petardo

Arma automática de área: se lanza hacia la posición del enemigo más cercano dentro de seis unidades. Explota al tocar un enemigo vivo o al llegar al destino, aunque el blanco se haya movido o muerto. Cada enemigo alcanzado recibe daño una sola vez; jugador y otras capas quedan excluidos. El anillo dorado muestra el radio durante 0,22 segundos y reutiliza el mismo proyectil del pool.

| Parámetro | Inicial | Con tres mejoras de esa estadística |
| --- | --- | --- |
| Daño por enemigo | 2 | 5 |
| Intervalo | 3 s | 2,187 s |
| Radio | 1,5 unidades | 2,592 unidades |
| Velocidad | 6 unidades/s | Sin mejora propia |

`Main` incluye adquisición de Petardo y mejoras de daño (+1), radio (×1,2) y cadencia (×0,9). La adquisición aparece mientras haya un espacio libre entre los cuatro permitidos; las mejoras aparecen después de equiparlo, con tres rangos cada una. Las cartas y el HUD usan el sprite del arma.

## Arte

`Assets/_Game/Art/Sprites/Weapons/Petardo.png` se generó con la herramienta integrada `image_gen`, con fondo transparente. La fuente mide 1254×1254; Unity la reduce a 64×64, sin compresión ni mipmaps, con filtro Point. Se normaliza el lienzo a una unidad de mundo usando PPU igual al ancho original; la escala del proyectil es 0,6. Esta importación conserva el tamaño físico aun al reducir la textura y no cambia otras armas.

Prompt usado:

> Use case: stylized-concept. Asset type: one small weapon sprite for a top-down 2D pixel-art Unity game called Conurbano Infinito. Create ONE original red cylindrical firecracker ('Petardo') with a short bent cream fuse and one tiny yellow spark. Single isolated upright firecracker, centered in a square canvas, transparent background, generous transparent margin. Style: extremely simple true pixel art representing a logical 24x24 pixel canvas enlarged with nearest-neighbor square blocks, 1 logical pixel dark outline #18242C, solid flat colors #9D3C39 dark red, #E25E4C red, #E8D7B2 cream, #F2C447 yellow; 3-5 colors total, large contiguous pixel clusters. Body about 8 logical pixels wide and 13 logical pixels tall, fuse 4 logical pixels long. No label, letters, numbers, logos, watermark, no extra objects, no smoke, no background, no gradients, no smoothing, no drop shadow. Must remain recognizable at a tiny on-screen size. Output a clean game-ready transparent PNG.

## Comprobación

En una partida nueva de `Main`, ejecutar **Tools > Conurbano Infinito > Comprobar Petardo en Play Mode**. Comprueba adquisición por la selección real, filtrado de cartas, mejoras hasta rango máximo, detonación por impacto y llegada al destino, daño único con colliders duplicados, exclusión del jugador, alcance del radio, 150 enemigos juntos, devolución al pool y reutilización como Termo. Termina con las mejoras y armas equipadas en esa sesión; salir y volver a entrar en Play Mode restablece la partida.

El balance inicial es provisional. La comprobación funcional no mide diversión, dificultad de una partida completa ni rendimiento en Windows.

La validación funcional se ejecutó en Unity 6000.3.23f1, en una copia aislada del proyecto con `-batchmode -nographics`. Pasaron `Comprobar Petardo en Play Mode`, Hito 3, Hito 8, Hito 10, quinta slice visual, Quilombo y geometría de indicadores. El código y los assets del proyecto se compararon con los de esa copia y sus GUIDs y referencias se verificaron por separado.

Al iniciar el Editor en modo batch se registró `ArgumentOutOfRangeException` en `UnityEditor.Search.SearchDatabase.GetDefaultSearchDatabase`, antes de las comprobaciones de gameplay. Una corrida inicial que salió de Play Mode inmediatamente después de Hito 8 reprodujo la aserción interna `t.GetParent() == nullptr` ya registrada en `ArtIntegrationLog.md`. La corrida final dejó procesar un segundo de frames antes de salir de cada sesión y completó todos los checks sin esa aserción. La incidencia del índice se mantuvo en el log y se contabilizó aparte; no se cambiaron paquetes ni preferencias del Editor.

La prueba prolongada completó 600 segundos acumulados de gameplay a velocidad normal con Termo y Petardo, movimiento por teclado simulado y reinicio después de tres derrotas. Terminó con código 0 y `PETARDO_SOAK_PASS gameplaySeconds=600.0 deaths=3 choices=0 editorIndexErrors=1`. No hubo errores de gameplay ni aserciones; la única excepción fue la del índice al iniciar el Editor. El recorrido automático no recogió experiencia y permaneció en nivel 1: acredita estabilidad durante ese tiempo, no supervivencia de diez minutos en una sola partida ni balance de progresión. Las mejoras sí quedaron cubiertas por la comprobación funcional independiente.

La inspección del sprite y la explosión en Game View quedó pendiente porque la Mac estaba bloqueada. No se generó una build Windows.
