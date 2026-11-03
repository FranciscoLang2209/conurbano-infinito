# Dirección de arte — Conurbano Infinito

Estado: Pibe, Mosquito, Paloma, las cuatro armas, Bolsa, Gema y Jefa tienen sprites originales. `Main` tiene piso de plaza compuesto por baldosas, pasto y props, más sprites de Kiosco y Parrilla en las zonas de riesgo. Los cinco actores tienen animaciones básicas; el decorado es estático. Ver `Docs/ArtIntegrationLog.md`.

## Estado comprobado del proyecto

- Unity 6000.3.23f1; cámara ortográfica de `Main` con tamaño 5 y seguimiento suavizado (`CameraFollow`, `smoothTime = 0.15`). La escena mantiene un plano de piso repetido, ahora compuesto con tiles y props de 48 px por unidad, sin collider ni tilemap. No hay Pixel Perfect Camera configurada.
- Los siete PNG de `Assets/_Game/Art/Placeholders/` siguen disponibles como referencia, pero ninguno está conectado a los siete prefabs principales. Son siluetas blancas originales de **16×16 px**.
- Los placeholders conservan sus imports: Sprite, Point, sin mipmaps y compresión predeterminada sin comprimir; 16 PPU salvo `Gema`, que usa 64 PPU. La nueva Gema conserva 64 PPU para mantener su tamaño visible de 0,25 unidades.
- Los prefabs también modifican el tamaño con su escala: jugador 1; mosquito 0,7; paloma 1,25; bolsa 0,9×0,75; jefa 2. `WeaponController` reutiliza `TermoProjectile` para las cuatro armas implementadas (`Termo Balístico`, `Boleadora`, `Chancleta Teledirigida` y `Fernets Orbitales`) y les aplica sprite, color y escala desde cada `WeaponData`.
- `GameHud`, menú y pantallas de pausa/mejoras se dibujan con IMGUI y colores propios. Destellos de daño ya existen; el aviso de ataque de la jefa es un cuadrado generado en tiempo de ejecución. No hay sombras circulares bajo los actores.

## Regla de escala y lectura

Usar **48 PPU para actores y armas nuevos** como densidad de píxeles del mundo. La Gema es una excepción de 16×16 a 64 PPU para conservar su escala visual. Con 48 PPU, un lienzo de 48×48 ocupa 1×1 unidad y uno de 32×32 ocupa 0,67×0,67 unidades *antes* de escala del prefab. Diseñar el contenido visible dentro del lienzo, con pivote inferior central para actores y centro para proyectiles; comparar en la vista Game con el tamaño ortográfico actual. La sustitución de un sprite requiere revisar visualmente escala, pivote, color multiplicado por `SpriteRenderer`, collider y orden de dibujo del prefab. No cambiar PPU, transformaciones ni colliders en bloque.

| Elemento de la primera slice | Lienzo | Lectura buscada | Animación inicial |
| --- | --- | --- | --- |
| El Pibe del Barrio | 48×48 px | Gorrita azul, buzo amarillo, mochila roja y termo claro; volumen principal de 30–40 px de ancho, sin cara detallada. | 2 cuadros de reposo, 4 de caminata. |
| Mosquito Gigante | 32×32 px | Cuerpo violeta, alas claras y trompa larga; reconocible en negro como silueta. | 2 cuadros de alas. |
| Paloma de Plaza | 32×32 px | Cuerpo gris claro, cabeza pequeña, pico amarillo y alas anchas; silueta distinta de la del mosquito. | 2 cuadros de aleteo o paso. |
| Termo Balístico | 24×24 px | Cilindro alto con tapa y franja contrastada, distinguible de una bala simple. | 1 cuadro; rotación solo si luego mejora su lectura. |

Los actores deben tener contorno exterior de 1 px en `#18242C`, con quiebres deliberados en zonas muy oscuras. Usar 3–5 colores principales por sprite y grupos de píxeles grandes; una sombra interior de 1–2 tonos basta. Sin degradados, blur ni ruido de píxeles sueltos. La sombra circular suave y pequeña bajo jugador y enemigos será una capa visual separada, sin collider, cuando se integre arte. Impactos: destello breve ya existente, más una marca simple de 2–3 cuadros solo si mejora la lectura. Experiencia: cian; daño: rojo. Mantener esos efectos escasos cuando haya muchos enemigos.

## Paleta propuesta: «Plaza del Conurbano»

Paleta original equivalente de **22 colores**, definida aquí para no depender de un pack externo. Usar los hexadecimales como fuente de verdad para futuros archivos de paleta en `Art/Palettes/`; los tintes y la transparencia son variantes de estos colores.

| Grupo | Colores |
| --- | --- |
| Contornos y sombras | `#18242C` tinta, `#2B3A3B` sombra |
| Pasto y follaje | `#2F5843` oscuro, `#4E8451` medio, `#84B96A` claro, `#B9D879` luz |
| Calle y vereda | `#514E4B` asfalto oscuro, `#746C62` asfalto, `#A89C86` hormigón |
| Luces y neutrales | `#E8D7B2` crema, `#F9EED7` blanco cálido |
| Azules | `#283F65` azul noche, `#4079BA` azul, `#61BBD3` celeste, `#B5E6DF` cian claro |
| Amarillos | `#C7932E` ocre, `#F2C447` amarillo |
| Rojos y piel | `#9D3C39` rojo oscuro, `#E25E4C` rojo, `#C98A62` piel |
| Acento enemigo | `#694668` ciruela, `#A15CAC` violeta |

El pasto ocupa la mayor superficie y debe mantenerse menos brillante que jugador, enemigos, experiencia y ataques. Reservar celeste para experiencia y señales positivas, rojo para daño y peligro, y amarillo para el jugador y señales importantes. No depender solo del color: distinguir también por silueta y movimiento.

## Estructura y procedencia

```text
Assets/_Game/Art/
  Placeholders/                 # mantener durante la transición
  Sprites/
    Player/
    Enemies/
    Weapons/
    Environment/
  Tiles/
  VFX/
  Palettes/
Assets/ThirdParty/<Proveedor>/
```

El Pibe, Mosquito, Paloma, las cuatro armas, Bolsa, Gema, Jefa, piso, bancos, árboles y decoración secundaria se crearon desde cero en el proyecto. Recursos futuros de terceros requieren comprobar que su licencia permite uso, modificación y distribución en el juego. Antes de importar, registrar en `Docs/Licencias.md` nombre, autor, URL, licencia y archivos utilizados, y añadir un README junto al recurso en `Assets/ThirdParty/<Proveedor>/`. No copiar arte de otros juegos ni usar packs genéricos para los protagonistas.

## Importación y validación de cada sprite nuevo

1. Buscar referencias de objetos reales y del entorno, sin calcar sprites ajenos. Dibujar primero una silueta con 3–5 colores; probarla en la vista Game a tamaño real y con varios enemigos a la vez. Solo entonces añadir detalles y cuadros de animación.
2. Importar como `Sprite (2D and UI)`, `Point`, sin mipmaps y sin compresión que difumine bordes. Usar 48 PPU para actores y armas; la Gema conserva 64 PPU por escala visible. Mantener pivote consistente por categoría y revisar la configuración de plataforma efectiva antes de exportar.
3. Reemplazar una referencia de prefab a la vez. La tinta de la nueva imagen se multiplicará por el `SpriteRenderer.color` actual; el arma también recibe `projectileColor` y `projectileScale` en ejecución. El `HitFlash` actual destella a blanco: si el color normal también es blanco, el destello se pierde. Para enemigos a color, usar temporalmente un tinte gris neutro que preserve contraste de brillo, hasta revisar ese efecto en otra tarea. Verificar tamaño, visibilidad, destello de daño y espacio respecto del collider. Comparar también con varios enemigos en pantalla.

## Riesgos visibles

- La Gema usa 64 PPU para conservar su tamaño visible; revisar su legibilidad en la vista Game si se cambia su dibujo.
- El seguimiento suavizado de cámara y las escalas fraccionarias de prefabs pueden mover bordes fuera de la cuadrícula de píxeles. Evaluar el resultado en Game antes de considerar una Pixel Perfect Camera o cambiar movimiento.
- El color multiplicado de prefabs y `WeaponData` puede ensuciar la paleta del arte final. Revisar cada integración; no neutralizar los tintes globalmente todavía.
- El piso repetido sigue siendo una sola superficie visual, no un mapa con obstáculos. El HUD IMGUI ya usa marcos compactos; la tipografía de Unity y los iconos de mejoras sin sprite siguen pendientes de arte final.

## Actualización de interfaz — 2026-10-16

HUD y pantallas ahora usan recursos PNG propios, VT323 y una ilustración original de plaza, integrados a través de GameUi sin reemplazar IMGUI. Dirección, controles, importación, procedencia, capturas y validaciones en `Docs/UiRedesign/README.md`. Este cambio reemplaza los pendientes de tipografía e iconos de interfaz mencionados arriba.

Segunda iteración: 17 iconos de UI originales a tamaño nativo, contorno tinta de 1 px, luz superior izquierda y paleta existente. Las seis armas tienen un dibujo del objeto separado del proyectil; el termo incluye tapa, cuello, cuerpo alto y manija abierta. Paneles, fuente y distribución se conservan. Comparativa y capturas reales en `Docs/UiRedesign/IconIteration/`.

## Animaciones y Vecino Gomero — 2026-10-23

El Pibe ahora usa seis poses de caminata; el perro distingue carrera, preparación, embestida y descanso. El nuevo Vecino Gomero lleva campera verde, pantalón azul y una gomera con banda roja; su preparación marca una trayectoria roja estrecha, distinta de la franja amarilla del perro. Todos usan el Canvas y la paleta propios, con impacto de chispas y muerte breve por compresión, inclinación y desvanecimiento. Recursos, parámetros y validación en `Docs/CombateYVecinoGomero.md`.

## Muestra de plaza y selección — 2026-10-30

Terreno con tres verdes cercanos (#54735A, #506E56, #58785E), detalle #648264 y camino #9B947F / juntas #898574, definidos junto a la paleta compartida en Tools/generate_visual_slice.py. Se eliminaron parches cuadrados y decoración con siluetas de enemigos/pickups; bancos, árboles y carteles se agrupan alrededor del cruce libre. Se conserva la textura repetida a 48 PPU efectivos. Ambos protagonistas reciben una sombra de apoyo en sus cuadros activos, sin cambios de clips ni mecánicas.

HUD con XP explícita, niveles NV y borde de slots; selector con retratos de 176 px, estadísticas alineadas y selección/hover/foco diferenciados. Se conservan IMGUI, fuente, nueve secciones y referencias. Documentación, comparativas reales, evaluación de packs y límites en [Docs/ArtSample/README.md](ArtSample/README.md). Esta sección actualiza las observaciones antiguas sobre el piso ruidoso y la UI pendiente.

## Corrección de lectura y desorden — 2026-10-30

La dirección de interfaz vigente pasa a Atkinson Hyperlegible Regular/Bold, con texto crema sobre carbón o tinta sobre crema y superficies sin textura detrás de las letras. Azul gastado para bordes y botones; amarillo para selección, recompensa y acentos. La plaza recupera basura decorativa apagada y composición desigual sin tocar el bloque del pasto/caminos ni añadir colisiones. Esto sustituye la decisión previa de quitar bolsas y botellas del piso. Evidencia y archivos de la intervención en `Docs/VisualCorrection/README.md`.
