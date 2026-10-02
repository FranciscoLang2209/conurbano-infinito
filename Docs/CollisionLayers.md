# Capas y colisiones 2D iniciales

Capas de juego requeridas: `Player`, `Enemy`, `Projectile` y `Pickup`.

| Capa A | Capa B | Colisiona | Motivo |
| --- | --- | --- | --- |
| Player | Player | No | El MVP tiene un solo jugador. |
| Player | Enemy | Sí | Contacto de enemigo con jugador. |
| Player | Projectile | No | Sin fuego amigo. |
| Player | Pickup | Sí | Recogida de objetos. |
| Enemy | Enemy | No | Sin bloqueo físico entre enemigos en el MVP. |
| Enemy | Projectile | Sí | Impactos de proyectiles contra enemigos. |
| Enemy | Pickup | No | Los pickups no interfieren con enemigos. |
| Projectile | Projectile | No | Proyectiles independientes. |
| Projectile | Pickup | No | Los pickups no bloquean proyectiles. |
| Pickup | Pickup | No | Los pickups no se empujan entre sí. |

La matriz es simétrica. El resto de capas conserva la configuración predeterminada de Unity hasta que el juego use objetos en ellas.
