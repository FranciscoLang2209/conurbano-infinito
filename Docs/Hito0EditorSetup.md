# Hito 0 — configuración aplicada

El proyecto Unity está en `Conurbano Infinito/` y usa **Unity 6.3 LTS 6000.3.23f1**.

Configuración confirmada:

1. **Version Control Mode: Visible Meta Files** y **Asset Serialization Mode: Force Text**.
2. Estructura `Assets/_Game/{Scenes,Scripts,Data,Prefabs,Art,Audio,UI}`.
3. Escena vacía `Assets/_Game/Scenes/Main.unity` como única escena habilitada de arranque.
4. Perfil activo **Windows x86_64**.
5. Capas `Player`, `Enemy`, `Projectile` y `Pickup`, con la matriz de [CollisionLayers.md](CollisionLayers.md).

Para comprobarlo en otro equipo:

1. Abrí `Conurbano Infinito/` con Unity Hub usando exactamente `6000.3.23f1`.
2. Verificá que `Main` esté abierta y que su jerarquía no tenga GameObjects.
3. Abrí **File > Build Profiles**: Windows debe estar activo y `Main` debe ser la única escena habilitada.
4. Generá una build Windows fuera de `Assets` y ejecutala en Windows.
