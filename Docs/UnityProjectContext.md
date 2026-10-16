# Contexto de Conurbano Infinito

<!-- unity-onboarding:generated:start -->
Revisado el 2026-10-16 sobre `ad51a31`, rama `codex/demo-cuatro-minutos`; prototipo previo preservado en `0a2d39f`.

- Raíz: `Conurbano Infinito/`; Unity **6000.3.23f1**. Render URP, Input System (activeInputHandler=1), juego local 2D. No se detectaron paquetes ni herramientas Unity MCP; hay Editor local y ejecutable para batchmode.
- Código propio: `Assets/_Game/Scripts/{Player,Enemy,Combat,Camera}`; herramientas y comprobaciones en `Assets/_Game/Editor`. No hay asmdefs propios: runtime en Assembly-CSharp y editor en Assembly-CSharp-Editor.
- Composición por MonoBehaviours, referencias `[SerializeField] private`, clases sealed sin namespace. Datos de armas, mejoras y oleadas en ScriptableObjects de `Assets/_Game/Data`. IMGUI compartido mediante GameUi.
- Flujo habilitado de build: Menu → Main. MenuScreen carga Main; GameManager gobierna Playing/Paused/Defeat/Victory, pausa con Esc y recarga escena con R. La victoria requiere sobrevivir 240 s; no exige matar a La Jefa. SampleScene está deshabilitada.
- GameManager avanza el reloj; WaveDirector elige fases de Oleadas.asset, pondera enemigos con Quilombo y genera a La Jefa a 180 s. SpawnManager asigna blancos. ObjectPool/PooledObject reinician cuerpos, Health y visuales.
- Demo: Kiosco (+10) y Parrilla (+15) activan los umbrales de Quilombo 10/25 en cualquier orden; bonuses de presión 0,15/0,35, sin élites. Se conservan la escala 0–100 y las recompensas. Resultados de validación en Docs/DemoValidacion.md; registro de partidas humanas pendientes en Docs/PruebaDemo.md.
- WeaponController equipa hasta cuatro armas; UpgradeSelection ofrece tres cartas al subir de nivel. Main prioriza la adquisición de Aerosol en la primera elección. Projectile implementa proyectiles rectos, de regreso, orbitales y explosivos.
- Health emite Damaged/Died. HitFlash y PlayerSurvival muestran impactos; AudioDirector escucha proyectiles, experiencia, jugador, estados y avisos de ataque. EnemyCharge prepara una dirección fija y muestra una barra celeste durante su recuperación.
- Tests: Unity Test Framework 1.6.0 instalado; se usan comprobaciones MenuItem y asserts/excepciones en Editor, sin suite NUnit propia. ShortRunCheck cubre elección, pausa, fases, jefe, victoria, reinicio, derrota y menú; ExpansionCheck/PetardoCheck cubren física, mejoras y pool. QuilomboSliceCheck cubre visitas en ambos órdenes y umbrales 10/25. Ejecutarlos en sesiones nuevas; alteran la sesión. Hay una aserción intermitente al salir de Play Mode, sin causa confirmada; no atribuirla automáticamente al pool.
- Validación disponible: batchmode en copia aislada, Editor/Game View con automatización UI, logs de Unity. Builds de entrega Windows x86_64/Mono y Mac universal generadas en Builds/*/Demo-2026-10-16. El Editor corre en macOS; ejecución en otra PC Windows y balance humano siguen pendientes.
- Restricciones: preservar GUIDs, fileIDs, nombres serializados y cambios locales existentes; no regenerar escenas/prefabs ni cambiar paquetes para validar. Ante audio nuevo, mantener importación y referencias de Main.

Fuentes: ProjectVersion.txt, manifest.json/packages-lock.json, ProjectSettings.asset, EditorBuildSettings.asset; GameManager, MenuScreen, WaveData/WaveDirector, SpawnManager, WeaponController, UpgradeSelection, Projectile, Health, EnemyCharge, BossAttack, AudioDirector, GameHud y checks de Editor; Docs/AerosolYPerro.md, Petardo.md, ArtIntegrationLog.md.
<!-- unity-onboarding:generated:end -->
