# Interfaz de barrio — Conurbano Infinito

Dirección: placa de club de barrio y señalética de kiosco. Tinta, crema, celeste, amarillo y rojo de la paleta existente. Escudo de club para nivel, corazón para vida, estrella celeste para XP, parlante con ondas de ruido para Quilombo y cronómetro de supervivencia. La ilustración de plaza tiene un lugar propio en pausa y menú, sin tapar el combate.

## Integración

Se conserva IMGUI y sus propietarios: `GameManager` para estados/reloj/acciones, `GameHud` para indicadores, `UpgradeSelection` para elecciones y `RiskZone` para lugares. No hay nuevas dependencias, modificaciones de mecánicas, datos, escenas ni prefabs. No hace falta reconectar botones en el Inspector.

- HUD: vida y XP juntas, nivel en escudo, reloj central, Quilombo independiente con valor y estado textual. Los avisos de oleada duran tres segundos cerca del reloj, fuera del centro de combate.
- Cuatro slots de armas con iconos propios del objeto, nivel y nombre al pasar el mouse. Las siete armas usan los mismos iconos en HUD y cartas de mejora; Aerosol muestra la lata. Las pasivas usan iconos con cantidad y descripción al pasar el mouse.
- Nivel de arma: N1 al equipar; +1 por cada mejora específica elegida para esa arma, excluida la adquisición. Las mejoras globales siguen como pasivas. Es una presentación del progreso existente, no una nueva mecánica ni un máximo inventado.
- Pausa y resultados: ilustración lateral, tiempo alcanzado, título y acción principal. Menú y mejoras comparten tipografía, paleta y superficies.
- Botones: PNG separados para normal, hover, seleccionado, presionado y deshabilitado. IMGUI mantiene el estado presionado durante el clic. La flecha seleccionada se anima con `Time.unscaledTime`, también en pausa.
- Esc, R, M, C y 1/2/3 se mantienen; flechas/Tab y Enter/Espacio agregan navegación. En mejoras se usan izquierda/derecha. En volumen, izquierda/derecha ajustan 5% cuando está seleccionado.
- Diagnóstico: oculto inicialmente. F3 alterna FPS/enemigos sólo con `UNITY_EDITOR` o `DEVELOPMENT_BUILD`; F9 conserva su función de desarrollo.
- Lugares: etiquetas discretas, sin marco de menú; ceden las franjas del HUD y del equipamiento. Desde la Canchita del 2026-10-30, los tres indicadores buscan espacio libre a lo largo del borde, conservando margen entre sí. El desafío activo y la recompensa más reciente tienen prioridad sobre el aviso temporal del Kiosco, sin cancelar su bonus. Los desafíos muestran de antemano recompensa y coste de Quilombo.

## Recursos

Carpeta de ejecución: `Conurbano Infinito/Assets/_Game/Resources/BarrioUi/`.

- 18 iconos originales transparentes: corazón, estrella XP, parlante de Quilombo, cronómetro, escudo, zapatilla, puño, cadencia, flecha, ubicación, mate y siete armas. Diseñados a tamaño nativo: XP/ubicación 16 px; corazón/pasivas/flecha 24; Quilombo 30; reloj/mate 32; armas 40; escudo 48. Contorno tinta de 1 px y luz desde arriba a la izquierda. Pelota de Potrero se incorporó el 2026-10-30; su fuente compartida con el sprite de combate está en Tools/generate_club_art.py.
- 8 superficies originales de 32×32: papel, esmalte, slot y cinco estados de botón. Bordes de 6 px para nueve secciones. Fuente reproducible: `Tools/generate_ui_art.py`, reutilizando `Canvas` y la paleta del proyecto.
- `plaza.png`: ilustración original generada con la herramienta integrada `image_gen`, 1536×1024, incorporada al proyecto; no depende de una ruta de caché.
- `VT323-Regular.ttf`: fuente de Google Fonts, con OFL 1.1 adjunta. Ver `Docs/Licencias.md`.

`BarrioUiImport` limita sus cambios a esta carpeta: Sprite/Single/FullRect, 48 PPU, Point, Clamp, sin mipmaps ni compresión, transparencia y bordes declarados. Los PNG conservan sus colores originales; el importador prepara los píxeles para IMGUI en el espacio lineal del proyecto. `GameUi` aplica la misma conversión a texto y tintes. Los recursos se cargan una vez y se reinician al entrar en Play Mode con domain reload deshabilitado.

Escala: referencia de 1280×720, ajuste uniforme por el lado limitante y márgenes relativos a `Screen.safeArea`; las pantallas se centran en esa área. Los iconos comparten densidad de un píxel dibujado por píxel de referencia. `GameUi.Icon` centra sin agrandar por encima del tamaño nativo, conserva proporciones y alinea el origen a píxeles de pantalla. La ilustración conserva su ajuste proporcional. El juego sigue siendo de escritorio; no se añadió interacción táctil.

## Reproducir la validación

`UiRedesignCheck.RunBatch` se ejecuta en una **copia aislada** con Unity 6000.3.23f1 y gráficos habilitados:

```sh
CONURBANO_UI_OUTPUT=/ruta/absoluta/capturas Unity -batchmode -projectPath /ruta/copia -executeMethod UiRedesignCheck.RunBatch -logFile /ruta/validacion.log
```

La comprobación cambia escenas, resolución de Game View y preferencias de entrada sólo en esa copia. No usarla como herramienta de edición sobre una sesión con trabajo sin guardar. Usa eventos del Input System a través de los manejadores reales; las capturas son de Play Mode, con fixtures de daño, XP y equipo completo. No representan una partida ganada a mano.

Segunda iteración, comparativa y validación: ver [IconIteration/README.md](IconIteration/README.md). Las capturas de la primera dirección visual siguen en `Captures/`.

## Prompt de ilustración (image_gen integrado)

Use case: illustration-story. Asset type: original pixel-art illustration for a Unity 2D game's pause and main menu. Generate a single finished landscape illustration, no UI mockup, no text.

Primary request: Argentine conurbano neighborhood plaza at dusk, a tiny corner kiosk with corrugated awning, club de barrio wall, overhead wires and string lights, a barbecue grill, plastic chair, thermos and mate sitting on a low wall, jacaranda tree silhouette and worn paving. Quiet affectionate neighborhood atmosphere, slightly eccentric survival-game world, no people.

Style/medium: authentic clean low-resolution pixel art, deliberately large pixel clusters, strict grid, flat 3-tone shading per material, strong illustrative silhouette, crisp stepped edges, no antialiasing, no gradients, no pseudo-pixel blur. Compose as a charming small game illustration, not a realistic photo.

Composition: landscape 3:2 framing, kiosk on left and tree on right, thermos and mate in foreground; all elements inside a cohesive scene.

Color palette: restricted to dark ink #18242C, petrol #1E4C4E, dark green #2F5843, cream #F9EED7, muted cream #E8D7B2, gold #F2C447, red #E25E4C, cyan #61BBD3, night blue #283F65 and subtle intermediate shades.

Constraints: no writing, no letters, no logos, no watermark, no borders or panels, no interface elements. Original art for Conurbano Infinito.
