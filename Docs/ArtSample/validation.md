# Validación de la muestra

Unity 6000.3.23f1 / Metal / macOS. Proyecto de prueba: /tmp/conurbano-art-20261030, copia aislada de Assets, Packages y ProjectSettings. Se clonó una Library existente para ahorrar importaciones. No se instaló MCP ni ningún paquete. Las pruebas alteran sólo la sesión y preferencias de la copia.

## Evidencia confirmada

| Comprobación | Resultado |
| --- | --- |
| Compilación por Unity | Sin errores C# en las ejecuciones registradas. |
| Muestra antes y después | ART_SAMPLE_PASS / ART_CHECK_PASS, ambos con cero errores de juego. |
| Game View / ScreenCapture | Main y Menu en 1024×768, 1280×720, 1920×1080 y 2560×1080. Se revisaron visualmente PNG del HUD/plaza y selector, incluido 4:3 y ultrawide. |
| Recursos UI | UI_ASSETS_PASS: 27 texturas, 18 iconos a tamaño nativo, siete armas con iconos distintos del proyectil; Point, sin mipmaps/compresión, bordes de nueve secciones y caracteres españoles. |
| Muestra del selector | Flechas, Tab hasta Volver y A la plaza, arriba/abajo y Enter; La Piba entra con vida máxima 4 y velocidad 5,5; cuadros de caminata distintos y retorno a PlayerIdle. |
| Ambos personajes | CHARACTERS_CHECK_PASS: selección, cancelación y recuerdo, vida/velocidad/arma, desplazamiento, walk/idle/pausa, 20 ofertas de mejoras y reinicio limpio. ShortRunCheck pasó para ambos: primera mejora, pausa, fases, Jefa única, victoria, derrota y retorno al menú. |
| UI completa | UI_RUNTIME_PASS / UI_REDESIGN_PASS: vida/XP/Quilombo, arma N1/N2, cuatro slots, mejoras, F3, pausa, flechas/Enter, R/M, menú, créditos, derrota y victoria. |
| Reproducibilidad del suelo | python3 Tools/generate_plaza.py pasa aserciones de opacidad, costuras y bytes idénticos en dos generaciones. |
| Revisión de imports en código | 15 texturas activas de protagonistas: metadatos a 48 PPU, Point, sin mipmaps ni compresión predeterminada. Registro import-audit.json. Esta comprobación es lectura de .meta, no una prueba visual en movimiento. |
| Conservación de gameplay | 110 rutas de escenas, prefabs y datos idénticas a la copia previa /tmp/conurbano-club-20261030. Sin cambios propios en esos archivos. |
| Correspondencia con la copia validada | 555 archivos de Assets y Packages comparados byte a byte, sin diferencias. |
| Editor original | Actualización de assets y Play Mode con el nuevo terreno/HUD; Esc abre pausa, M vuelve al menú y Enter abre el nuevo selector. Se salió de Play Mode y se restauró Main sin guardar modificaciones de escena. |

## Mouse: límite explícito

Las tarjetas y acciones siguen usando GUI.Button; el hover se dibuja con la posición del evento y el foco de teclado tiene indicador independiente. La revisión de las capturas detectó texto blanco sobre crema al hacer hover: GameUi.Text ahora fija el color también en hover, active y focused; se verificó visualmente la nueva captura ultrawide con el texto legible.

**No se verificó el clic de mouse de extremo a extremo.** La inyección de eventos en batchmode no seleccionó la tarjeta, y los intentos de clic nativo mediante la herramienta de escritorio fallaron con noWindowsAvailable, aunque las teclas sí funcionaron. No se atribuye ese resultado a un defecto del juego ni se cuenta como prueba aprobada. Se retiró la captura provisional de ese intento y el inyector fallido; las pruebas restantes no simulan una aprobación de mouse. Antes de una entrega pública, hacer clic en ambas tarjetas, Volver y A la plaza en el Editor.

## Incidencias preexistentes

Las sesiones Before y After registraron una excepción de UnityEditor.Search.SearchDatabase al iniciar el índice del Editor. Ya estaba documentada y el clasificador existente la registra separadamente: knownEditorSearchErrors=1; no se borró ni se contó como error de juego. Los logs originales se conservan.

## Repetir

En una copia aislada del proyecto, con el ejecutable de Unity 6000.3.23f1 y gráficos habilitados (sin -nographics):

```sh
CONURBANO_ART_SAMPLE=1 CONURBANO_UI_OUTPUT=/ruta/absoluta/capturas Unity -batchmode -projectPath /ruta/copia -executeMethod UiRedesignCheck.RunBatch -logFile /ruta/sample.log
CONURBANO_UI_OUTPUT=/ruta/absoluta/ui Unity -batchmode -projectPath /ruta/copia -executeMethod UiRedesignCheck.RunBatch -logFile /ruta/ui.log
CONURBANO_CHARACTERS_OUTPUT=/ruta/absoluta/personajes Unity -batchmode -projectPath /ruta/copia -executeMethod CharactersCheck.RunBatch -logFile /ruta/characters.log
```

Evidencia local completa, ignorada por Git: TestResults/ArtSample/before.log, sample.log, ui.log, characters.log, import-audit.json y carpetas Ui/Characters. Capturas comparables versionables: Docs/ArtSample/Before y After.

No se generó build distribuible, no se ejecutó en Windows y no se midieron rendimiento ni balance humano. No se introdujo Pixel Perfect Camera: el movimiento suave y escalado fraccional originales siguen siendo un límite conocido. La repetición de la textura sigue existiendo; esta muestra mejora su lectura, no construye un sistema nuevo de mundo.
