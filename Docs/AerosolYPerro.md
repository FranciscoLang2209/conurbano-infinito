# Aerosol y Perro Callejero

La partida actual dura cuatro minutos; su progresión, primera oferta de Aerosol y avisos adicionales se describen en [PartidaCorta.md](PartidaCorta.md).

## Aerosol

Arma automática de corto alcance: apunta al enemigo más cercano y lanza cinco nubes en un abanico de 70 grados. Cada nube atraviesa hasta cinco enemigos y alcanza 2,8 unidades desde el punto de disparo. No aplica veneno ni daño continuo: cada nube daña una vez a cada enemigo alcanzado. Varias nubes pueden alcanzar al mismo enemigo.

| Parámetro | Inicial | Con tres mejoras de esa estadística |
| --- | --- | --- |
| Daño por nube | 1 | 4 |
| Intervalo entre ráfagas | 0,8 s | 0,5832 s |
| Nubes por ráfaga | 5 | 8 |
| Velocidad | 5 unidades/s | Sin mejora propia |

`Main` incluye adquisición y tres mejoras propias: daño (+1), cadencia (intervalo ×0,9) y cobertura (+1 nube dentro del mismo abanico). Cada mejora tiene tres rangos y aparece después de equipar el arma. Conserva el límite de cuatro armas y las mejoras globales existentes. Las cartas usan un icono de lata; los proyectiles y el HUD usan el sprite de nube.

## Perro Callejero

Enemigo de 8 HP, daño de contacto de 1 y recompensa de 3 XP. Aparece con peso 1 desde la fase de 01:00, compartiendo los límites e intervalos de las oleadas existentes.

Persigue a 1,7 unidades/s. A 4,5 unidades o menos, si terminó su cooldown, se detiene y anuncia su trayectoria con una franja amarilla durante 0,65 s. Después recorre esa dirección fija a 8 unidades/s durante 0,5 s; permite esquivarlo lateralmente. Descansa 1,1 s, vuelve a perseguir y espera al menos 2 s antes de preparar otra embestida. Al aparecer también espera esos 2 s. La pausa detiene el movimiento y los tiempos. La pérdida o muerte del blanco cancela la embestida; la reutilización del pool restablece vida, blanco, aviso y cooldown.

## Arte

`Tools/generate_expansion_art.py` reutiliza `Canvas` y la paleta de `generate_visual_slice.py`. Produce tres PNG originales: lata y nube de 24×24, perro de 48×48. Unity los importa a 48 PPU, Point, sin mipmaps ni compresión. El perro usa escala 1, collider circular de radio 0,45 y feedback de impacto/muerte existente. Su sprite es estático.

## Comprobación

En una partida nueva de `Main`, ejecutar **Tools > Conurbano Infinito > Comprobar Aerosol y Perro en Play Mode**. Verifica adquisición por cartas, filtrado y agotamiento de mejoras, cinco/ocho nubes, daño, perforación, borde del abanico, alcance, exclusión del jugador, colliders duplicados y reuso como Termo. Comprueba las fases del perro, aviso inmóvil, dirección fija, descanso, pausa, cancelación al morir el blanco, reuso y drop de XP. La prueba modifica la progresión de esa sesión: salir y volver a entrar en Play Mode para jugar desde cero.

El alcance se mide desde el nacimiento de la nube; el radio del collider agrega un margen físico en el impacto. Los valores son un balance inicial: falta evaluar esta combinación jugando una partida completa y en la build Windows.

Validación del 2026-10-16: Unity 6000.3.23f1 compiló y pasaron siete comprobaciones en batchmode: Aerosol/Perro, Petardo, Hito 3, Hito 8, Hito 10, Quilombo y quinta slice visual. Las pruebas de Play Mode se ejecutaron en sesiones nuevas; la geometría y los impactos del Aerosol se verificaron con la simulación de Physics2D. No hubo excepciones ni aserciones en la corrida final. La inspección de los PNG se hizo por separado; no se comprobó su legibilidad en Game View ni se generó build Windows.
