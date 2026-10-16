# Contexto de Conurbano Infinito

<!-- unity-onboarding:generated:start -->
Revisado el 2026-10-16 sobre `6ac7d2b` y cambios locales de Aerosol, Perro y Petardo.

- Raíz: `Conurbano Infinito/`; Unity **6000.3.23f1**. Render URP, Input System (activeInputHandler=1), juego local 2D. No se detectaron paquetes ni herramientas Unity MCP; hay Editor local y ejecutable para batchmode.
- Código propio: `Assets/_Game/Scripts/{Player,Enemy,Combat,Camera}`; herramientas y comprobaciones en `Assets/_Game/Editor`. No hay asmdefs propios: runtime en Assembly-CSharp y editor en Assembly-CSharp-Editor.
- Composición por MonoBehaviours, referencias `[SerializeField] private`, clases sealed sin namespace. Datos de armas, mejoras y oleadas en ScriptableObjects de `Assets/_Game/Data`. IMGUI compartido mediante GameUi.
- Flujo habilitado de build: Menu → Main. MenuScreen carga Main; GameManager gobierna Playing/Paused/Defeat/Victory, pausa con Esc y recarga escena con R. La victoria requiere sobrevivir 240 s; no exige matar a La Jefa. SampleScene está deshabilitada.
- GameManager avanza el reloj; WaveDirector elige fases de Oleadas.asset, pondera enemigos con Quilombo y genera a La Jefa a 180 s. SpawnManager asigna blancos. ObjectPool/PooledObject reinician cuerpos, Health y visuales.
- WeaponController equipa hasta cuatro armas; UpgradeSelection ofrece tres cartas al subir de nivel. Main prioriza la adquisición de Aerosol en la primera elección. Projectile implementa proyectiles rectos, de regreso, orbitales y explosivos.
- Health emite Damaged/Died. HitFlash y PlayerSurvival muestran impactos; AudioDirector escucha proyectiles, experiencia, jugador, estados y avisos de ataque. EnemyCharge prepara una dirección fija y muestra una barra celeste durante su recuperación.
- Tests: Unity Test Framework 1.6.0 instalado; se usan comprobaciones MenuItem y asserts/excepciones en Editor, sin suite NUnit propia. ShortRunCheck cubre elección, pausa, fases, jefe, victoria, reinicio, derrota y menú; ExpansionCheck/PetardoCheck cubren física, mejoras y pool. Ejecutarlos en sesiones nuevas; alteran la sesión.
- Validación disponible: batchmode en copia aislada, Editor/Game View con automatización UI, logs de Unity. Objetivo histórico Windows; el Editor corre en macOS. Una build Windows y el balance humano requieren validación separada.
- Restricciones: preservar GUIDs, fileIDs, nombres serializados y cambios locales existentes; no regenerar escenas/prefabs ni cambiar paquetes para validar. Ante audio nuevo, mantener importación y referencias de Main.

Fuentes: ProjectVersion.txt, manifest.json/packages-lock.json, ProjectSettings.asset, EditorBuildSettings.asset; GameManager, MenuScreen, WaveData/WaveDirector, SpawnManager, WeaponController, UpgradeSelection, Projectile, Health, EnemyCharge, BossAttack, AudioDirector, GameHud y checks de Editor; Docs/AerosolYPerro.md, Petardo.md, ArtIntegrationLog.md.
<!-- unity-onboarding:generated:end -->
