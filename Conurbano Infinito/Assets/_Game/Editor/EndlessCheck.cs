using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class EndlessCheck
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static string output;
    private static GameManager game;
    private static WaveDirector director;
    private static WeaponController weapons;
    private static ObjectPool pool;
    private static Keyboard keyboard;
    private static int errors, searchErrors;
    private static readonly HashSet<string> layoutIssues = new();

    public static async void RunBatch()
    {
        output = Environment.GetEnvironmentVariable("CONURBANO_ENDLESS_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new Exception("CONURBANO_ENDLESS_OUTPUT is required.");
        Directory.CreateDirectory(output);
        Application.logMessageReceived += OnLog;
        try
        {
            var snapshots = AssetDatabase.FindAssets("t:RunRules t:WaveData t:WeaponData t:CharacterData", new[] { "Assets/_Game" })
                .Select(AssetDatabase.GUIDToAssetPath).ToDictionary(p => p,
                    p => EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<ScriptableObject>(p)));
            CheckRules();
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Main.unity");
            EditorApplication.EnterPlaymode();
            await Until(() => EditorApplication.isPlaying);
            await Task.Delay(200);
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            await CheckClockAndBosses();
            await CheckDirectorLimits();
            await CheckPoolAndExperience();
            await CheckMenuAndPause();
            if (Environment.GetEnvironmentVariable("CONURBANO_ENDLESS_SOAK") == "1") await Soak();
            foreach (var entry in snapshots)
                Require(EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<ScriptableObject>(entry.Key)) == entry.Value,
                    "Definition mutated: " + entry.Key);
            Require(errors == 0 && layoutIssues.Count == 0, "Console/layout failures: " + errors + "/" + layoutIssues.Count);
            File.WriteAllLines(Path.Combine(output, "text-layout.txt"), layoutIssues);
            GameUi.CheckTextLayout = null;
            Time.captureDeltaTime = 0f;
            EditorApplication.ExitPlaymode();
            await Until(() => !EditorApplication.isPlaying);
            Debug.Log("ENDLESS_CHECK_PASS modes=2 boundaries=True bosses=True pool=True budgets=True xp-conserved=True pause-queue=True menu-keyboard=True layouts=4 immutable=True knownEditorSearchErrors=" + searchErrors);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            File.WriteAllLines(Path.Combine(output, "text-layout.txt"), layoutIssues);
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static void CheckRules()
    {
        RunConfiguration classic = RunRules.All[0].CreateConfiguration(), endless = RunRules.All[1].CreateConfiguration();
        Require(!classic.IsEndless && endless.IsEndless && classic.StableId != endless.StableId, "Explicit profiles.");
        foreach (float t in new[] { 0f, 180f, 239.9f, 240f, 240.1f, 359.9f, 360f, 1800f, 100000f })
        {
            Require(classic.SegmentAt(t) == 0 && classic.HealthFactorAt(t) == 1f && classic.SpeedFactorAt(t) == 1f,
                "Classic scaling changed.");
            Require(Mathf.Approximately(classic.SpawnIntervalAt(t, 0.57f, 1.7f), 0.57f / 1.7f) &&
                classic.CommonLimitAt(t, 55, 1.7f) == 94, "Classic pressure changed.");
        }
        Require(endless.SegmentAt(239.9f) == 0 && endless.SegmentAt(240f) == 1 && endless.SegmentAt(240.1f) == 1 &&
            endless.SegmentAt(359.9f) == 1 && endless.SegmentAt(360f) == 2 && endless.SegmentAt(1800f) == 14, "Segment boundaries.");
        Require(Mathf.Approximately(endless.HealthFactorAt(240f), 1.2f) &&
            Mathf.Approximately(endless.SpeedFactorAt(240f), 1.02f) &&
            Mathf.Approximately(endless.SpeedFactorAt(100000f), 1.3f), "Enemy curves.");
        Require(Mathf.Approximately(endless.SpawnIntervalAt(240f, 0.57f, 1.7f), 0.57f / 1.08f / 1.7f) &&
            endless.SpawnIntervalAt(100000f, 0.57f, 2.35f) == 0.2f &&
            endless.CommonLimitAt(240f, 55, 1.7f) == 101 && endless.CommonLimitAt(100000f, 55, 2.35f) == 150,
            "Pressure must compose Quilombo exactly once and respect caps.");
        Require(endless.BossSlotAt(179.9f, 180f) == -1 && endless.BossSlotAt(180f, 180f) == 0 &&
            endless.BossSlotAt(419.9f, 180f) == 0 && endless.BossSlotAt(420f, 180f) == 1 &&
            endless.BossSlotAt(660f, 180f) == 2 && classic.BossSlotAt(660f, 180f) == 0, "Boss calendar.");
        Debug.Log("ENDLESS_RULES_PASS segments=0/1/2/14 speed-cap=1.3 interval-floor=0.2 common-cap=150 pressure=once");
    }

    private static async Task CheckClockAndBosses()
    {
        await Fresh(GameMode.Classic);
        SetClock(239.9f); Call(game, "Update");
        Require(game.State == GameManager.GameState.Playing, "Classic wins early.");
        SetClock(240f); Call(game, "Update"); Call(game, "LateUpdate");
        Require(game.State == GameManager.GameState.Victory && game.ElapsedSeconds == 240f, "Classic deadline.");
        await Fresh(GameMode.Endless);
        foreach (float time in new[] { 239.9f, 240f, 240.1f })
        {
            SetClock(time); Call(game, "Update");
            Require(game.State == GameManager.GameState.Playing && game.ElapsedSeconds >= time && !game.HasTimeLimit &&
                game.DurationSeconds == 0f, "Infinite clipped/won at deadline.");
        }

        await Fresh(GameMode.Endless);
        director.Tick(179.9f); Require(director.ActiveBoss == null, "Early boss.");
        director.Tick(180f); Health first = director.ActiveBoss;
        Require(first != null && first.MaximumHealth == 100 && director.NextBossSeconds == 420f, "Opening boss.");
        director.Tick(419.9f); director.Tick(420f); director.Tick(420f);
        Require(director.ActiveBoss == first && director.NextBossSeconds == 660f &&
            Object.FindObjectsByType<BossAttack>(FindObjectsSortMode.None).Count(a => !a.GetComponent<Health>().IsDead) == 1,
            "Live boss duplicated or debt retained.");
        first.TakeDamage(first.MaximumHealth);
        Require(weapons.HasDefeatedBoss, "Boss achievement disconnected.");
        director.Tick(421f); Require(director.ActiveBoss == first, "Skipped boss queued as debt.");
        director.Tick(660f); Health second = director.ActiveBoss;
        Require(second != null && !second.IsDead && second.MaximumHealth == 180 && director.NextBossSeconds == 900f,
            "Recurring scaled boss.");
        int previousHealth = second.MaximumHealth;
        director.Tick(900f);
        Require(director.ActiveBoss == second && second.MaximumHealth == previousHealth && director.NextBossSeconds == 1140f,
            "Existing boss rescaled at boundary.");
        Debug.Log("ENDLESS_BOSSES_PASS 180/420/660/900 one-live=True skip-no-debt=True immutable-active=True achievement=True");
    }

    private static async Task CheckPoolAndExperience()
    {
        await Fresh(GameMode.Endless);
        SpawnManager spawner = Object.FindFirstObjectByType<SpawnManager>();
        WaveData waves = (WaveData)Field(director, "waves");
        foreach (GameObject prefab in waves.Phases.SelectMany(p => p.enemies).Select(o => o.prefab).Append(waves.BossPrefab).Distinct())
        {
            int basis = prefab.GetComponent<Health>().MaximumHealth;
            Health previous = null;
            for (int cycle = 0; cycle < 12; cycle++)
            {
                float scale = cycle % 2 == 0 ? 1.2f : 1f;
                Health spawned = spawner.Spawn(prefab, scale, cycle % 2 == 0 ? 1.3f : 1f);
                Require(spawned.MaximumHealth == (int)Math.Ceiling(basis * (double)scale - 0.0001d) && spawned.IsFull,
                    "Pool accumulated health multiplier: " + prefab.name);
                if (previous != null) Require(previous == spawned && spawned.LifeId > 0, "Expected pooled life.");
                foreach (string type in new[] { "EnemyMovement", "MosquitoChase", "EnemyCharge", "EnemyRanged" })
                {
                    Component movement = spawned.GetComponent(type);
                    if (movement != null) Require(Mathf.Approximately((float)movement.GetType().GetProperty("SpawnSpeedFactor", Flags).GetValue(movement),
                        cycle % 2 == 0 ? 1.3f : 1f), "Pool speed did not reset.");
                }
                int gems = pool.ActiveGemCount;
                ObjectPool.Release(spawned.gameObject);
                Require(pool.ActiveGemCount == gems, "Despawn granted XP.");
                previous = spawned;
            }
        }
        ExperienceGem gemPrefab = AssetDatabase.LoadAssetAtPath<ExperienceGem>("Assets/_Game/Prefabs/ExperienceGem.prefab");
        Require(gemPrefab != null, "Gem prefab.");
        long total = 0;
        for (int i = 0; i < 6000; i++)
        {
            int value = 1 + i % 10; total += value;
            pool.DropExperience(gemPrefab, new Vector3(100 + i % 300, i / 300, 0), value);
        }
        ExperienceGem[] gemsNow = Object.FindObjectsByType<ExperienceGem>(FindObjectsSortMode.None);
        Require(gemsNow.Length == game.Configuration.MaximumGems && pool.ActiveGemCount == gemsNow.Length &&
            gemsNow.Sum(g => g.Value) == total && pool.MergedDrops == 6000 - gemsNow.Length, "XP grouping lost value or cap.");
        ExperienceSystem xp = weapons.GetComponent<ExperienceSystem>();
        foreach (ExperienceGem gem in gemsNow) { gem.TryCollect(xp); gem.TryCollect(xp); }
        long spent = (long)(xp.Level - 1) * 5 + (long)(xp.Level - 1) * (xp.Level - 2) * 3 / 2;
        Require(spent + xp.CurrentExperience == total && pool.ActiveGemCount == 0, "Grouped XP collection duplicated/lost value.");

        await Fresh(GameMode.Endless);
        Projectile shotPrefab = (Projectile)Field(weapons, "projectilePrefab");
        var shots = new List<GameObject>();
        for (int i = 0; i < game.Configuration.MaximumProjectiles; i++)
            shots.Add(pool.Spawn(shotPrefab.gameObject, new Vector3(300, 300, 0), Quaternion.identity));
        Require(shots.All(s => s != null) && pool.ActiveProjectileCount == shots.Count &&
            pool.Spawn(shotPrefab.gameObject, Vector3.zero, Quaternion.identity) == null, "Projectile cap.");
        Health target = Object.FindFirstObjectByType<SpawnManager>().Spawn(waves.Phases[0].enemies[0].prefab);
        target.transform.position = weapons.transform.position + Vector3.right;
        Physics2D.SyncTransforms();
        Call(weapons, "Update");
        Require(pool.ActiveProjectileCount == shots.Count && pool.SkippedProjectiles >= 2, "Budget overflow broke weapon firing.");
        foreach (GameObject shot in shots) ObjectPool.Release(shot);
        Require(pool.ActiveProjectileCount == 0 && pool.PeakProjectileCount == game.Configuration.MaximumProjectiles, "Projectile release counters.");
        GameObject reused = pool.Spawn(shotPrefab.gameObject, Vector3.zero, Quaternion.identity);
        Require(shots.Contains(reused) && pool.ActiveProjectileCount == 1, "Projectile pool reuse.");
        ObjectPool.Release(reused); ObjectPool.Release(reused);
        Require(pool.ActiveProjectileCount == 0, "Double release underflow.");
        Debug.Log("ENDLESS_BUDGETS_PASS enemies=6 cycles=12 drops=6000 xp=" + total + " gems=256 projectiles=256 despawn-no-xp=True");
    }

    private static async Task CheckDirectorLimits()
    {
        await Fresh(GameMode.Endless);
        QuilomboSystem quilombo = Object.FindFirstObjectByType<QuilomboSystem>();
        quilombo.Add(35f);
        director.Tick(240f);
        Require(director.CurrentCommonLimit == 101 &&
            Mathf.Approximately(director.CurrentSpawnInterval, 0.57f / 1.08f / 1.7f), "Director pressure composed twice.");
        director.Tick(241f);
        var tracked = (List<Health>)Field(director, "activeEnemies");
        Require(tracked.Count == 1, "Expected one common.");
        Health survivor = tracked[0]; int health = survivor.MaximumHealth; uint life = survivor.LifeId;
        director.Tick(360f);
        Require(survivor.MaximumHealth == health && survivor.LifeId == life && !survivor.IsDead, "Segment replaced/rescaled active enemy.");
        quilombo.Add(65f);
        for (int i = 0; i < 200; i++) director.Tick(100000f + i);
        Require(director.CurrentCommonLimit == 150 && director.CurrentSpawnInterval == 0.2f &&
            tracked.Count == 150 && director.ActiveEnemyCount == 151 && director.PeakEnemyCount == 151, "Live common/boss caps.");
        long xp = Object.FindObjectsByType<ExperienceGem>(FindObjectsSortMode.None).Sum(g => g.Value);
        ObjectPool.Release(tracked[10].gameObject);
        director.Tick(100300f);
        Require(tracked.Count == 150 && director.ActiveEnemyCount == 151 &&
            Object.FindObjectsByType<ExperienceGem>(FindObjectsSortMode.None).Sum(g => g.Value) == xp,
            "Inactive tracking or despawn rewards.");
        Debug.Log("ENDLESS_DIRECTOR_PASS pressure=once live-common=150 live-boss=1 active-not-rescaled=True inactive-pruned=True");
    }

    private static async Task CheckMenuAndPause()
    {
        await Fresh(GameMode.Endless);
        int abandoned = 0;
        game.StateChanged += state => { if (state == GameManager.GameState.Abandoned) abandoned++; };
        game.PauseGame(); game.ReturnToMenu();
        await Task.Delay(150); Require(abandoned == 1, "Abandon did not close once.");
        GameUi.CheckTextLayout = CheckTextLayout;
        await Press(Key.Enter);
        MenuScreen menu = Object.FindFirstObjectByType<MenuScreen>();
        Require((bool)Field(menu, "showingCharacters"), "Character/mode screen.");
        Set(menu, "characterFocus", 5); await Press(Key.Enter);
        Require(RunRules.SelectedMode == GameMode.Endless, "Keyboard infinite selection.");
        await CaptureSizes("selector-infinito");
        Set(menu, "characterFocus", 4); await Press(Key.Enter);
        Require(RunRules.SelectedMode == GameMode.Classic, "Keyboard classic selection.");
        Set(menu, "characterFocus", 5); await Press(Key.Enter);
        Set(menu, "characterFocus", 3); await Press(Key.Enter); await Task.Delay(150);
        Bind();
        Require(!game.HasTimeLimit && game.State == GameManager.GameState.Playing, "Menu did not launch infinite.");
        Set(weapons.GetComponent<Health>(), "invulnerabilitySeconds", 10000f);
        SetClock(661f); director.Tick(661f);
        await CaptureSizes("hud-infinito");
        ExperienceSystem xp = weapons.GetComponent<ExperienceSystem>();
        UpgradeSelection selection = game.GetComponent<UpgradeSelection>();
        xp.AddExperience(xp.RequiredExperience * 3);
        float time = game.ElapsedSeconds;
        int segment = director.EndlessSegment;
        game.PauseGame(); await CaptureSizes("pausa-infinito");
        selection.Select(0); Call(game, "Update");
        Require(selection.IsShowing && Time.timeScale == 0f && game.ElapsedSeconds == time, "Pause lost queued choices/clock.");
        game.ContinueGame();
        Require(Time.timeScale == 0f, "Resume bypassed pending level choice.");
        int choices = 0;
        while (selection.IsShowing) { selection.Select(0); choices++; await Frame(); }
        Require(choices >= 2 && Time.timeScale == 1f && director.EndlessSegment == segment, "Queue did not drain.");
        CharacterData character = CharacterData.Selected;
        game.RestartGame(); await Task.Delay(150); Bind();
        Require(!game.HasTimeLimit && game.ElapsedSeconds < 1f && CharacterData.Selected == character &&
            director.EndlessSegment == 0 && !weapons.HasDefeatedBoss && pool.MergedDrops == 0, "Restart state/mode/character.");
        weapons.GetComponent<Health>().TakeDamage(100000);
        Require(game.State == GameManager.GameState.Defeat, "Infinite does not end on death.");
        int changes = 0; game.StateChanged += _ => changes++;
        game.ReturnToMenu(); await Task.Delay(150);
        Require(changes == 0, "Returning after defeat changed terminal reason.");
        RunRules.Select(GameMode.Classic);
        await Fresh(GameMode.Classic);
        Require(game.HasTimeLimit && game.DurationSeconds == 240f, "Mode switch retained infinite.");
        GameUi.CheckTextLayout = null;
        Debug.Log("ENDLESS_FLOW_PASS menu-keyboard=True restart-mode-character=True abandon!=defeat=True pause-queue=True layouts=4");
    }

    private static async Task Soak()
    {
        await Fresh(GameMode.Endless);
        game.enabled = true; weapons.enabled = true;
        weapons.GetComponent<Health>().ConfigureStartingHealth(1000000);
        weapons.EquipWeapon(AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Pelota de Potrero.asset"));
        weapons.EquipWeapon(AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Petardo.asset"));
        weapons.EquipWeapon(AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Fernets Orbitales.asset"));
        ExperienceSystem xp = weapons.GetComponent<ExperienceSystem>();
        UpgradeSelection selection = game.GetComponent<UpgradeSelection>();
        var log = new List<string> { "simSeconds,enemies,peakEnemies,gems,projectiles,activePool,createdPool,mergedDrops,skippedShots,allocatedBytes,managedBytes" };
        float nextSample = 0f, nextXp = 15f;
        int choices = 0, startFrame = Time.frameCount;
        Time.captureDeltaTime = 1f / 30f;
        Time.timeScale = 8f;
        double start = EditorApplication.timeSinceStartup;
        while (game.ElapsedSeconds < 1800f)
        {
            Require(game.State == GameManager.GameState.Playing, "Soak ended early.");
            if (selection.IsShowing) { selection.Select(0); choices++; if (!selection.IsShowing) Time.timeScale = 8f; }
            else if (game.ElapsedSeconds >= nextXp)
            {
                nextXp += 15f; xp.AddExperience(xp.RequiredExperience);
            }
            if (game.ElapsedSeconds >= nextSample)
            {
                nextSample += 60f;
                Require(director.ActiveEnemyCount <= 151 && pool.ActiveGemCount <= 256 && pool.ActiveProjectileCount <= 256,
                    "Soak object budget exceeded.");
                log.Add($"{game.ElapsedSeconds:0.00},{director.ActiveEnemyCount},{director.PeakEnemyCount},{pool.ActiveGemCount},{pool.ActiveProjectileCount},{pool.ActiveCount},{pool.CreatedCount},{pool.MergedDrops},{pool.SkippedProjectiles},{UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()},{GC.GetTotalMemory(false)}");
                File.WriteAllLines(Path.Combine(output, "soak.csv"), log);
                if ((int)nextSample % 300 == 0) Debug.Log("ENDLESS_SOAK_PROGRESS simulation=" + game.ElapsedSeconds);
            }
            Require(EditorApplication.timeSinceStartup - start < 900, "Soak wall-clock timeout.");
            await Frame();
        }
        Time.captureDeltaTime = 0f; Time.timeScale = 1f;
        Require(director.EndlessSegment == 14 && game.State == GameManager.GameState.Playing && errors == 0,
            "Thirty-minute soak failed.");
        long beforeGc = GC.GetTotalMemory(false);
        long afterGc = GC.GetTotalMemory(true);
        File.WriteAllText(Path.Combine(output, "soak-retention.txt"), $"managedBeforeCollection={beforeGc}\nmanagedAfterCollection={afterGc}\ncreatedPool={pool.CreatedCount}\n");
        Debug.Log($"ENDLESS_SOAK_PASS simulation={game.ElapsedSeconds} wallSeconds={EditorApplication.timeSinceStartup - start:0.00} renderedFrames={Time.frameCount - startFrame} choices={choices} peakEnemies={director.PeakEnemyCount} peakGems={pool.PeakGemCount} peakProjectiles={pool.PeakProjectileCount} created={pool.CreatedCount} captureDelta=1/30 timeScale=8 boostedHealth=1000000 controlledXp=True stationary=True NOT_REALTIME_FPS");
        await CaptureSizes("soak-final");
    }

    private static async Task Fresh(GameMode mode)
    {
        RunRules.Select(mode); SceneManager.LoadScene("Main"); await Task.Delay(100); Bind();
        weapons.enabled = false; Time.timeScale = 1f;
    }
    private static void Bind()
    {
        game = Object.FindFirstObjectByType<GameManager>(); director = Object.FindFirstObjectByType<WaveDirector>();
        weapons = Object.FindFirstObjectByType<WeaponController>(); pool = Object.FindFirstObjectByType<ObjectPool>();
    }
    private static object Field(object owner, string field) => owner.GetType().GetField(field, Flags).GetValue(owner);
    private static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Flags).SetValue(owner, value);
    private static void SetClock(float time) => Set(game, "<ElapsedSeconds>k__BackingField", time);
    private static void Call(object owner, string method) => owner.GetType().GetMethod(method, Flags).Invoke(owner, null);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task Frame() { int frame = Time.frameCount; await Until(() => Time.frameCount > frame); }
    private static async Task Until(Func<bool> ready)
    {
        double deadline = EditorApplication.timeSinceStartup + 30;
        while (!ready()) { if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Endless check timed out."); await Task.Delay(10); }
    }
    private static async Task Press(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update(); keyboard.MakeCurrent();
        Require(keyboard[key].wasPressedThisFrame, "Synthetic key not delivered: " + key);
        foreach (Type type in new[] { typeof(MenuScreen), typeof(GameManager) })
        {
            Object target = Object.FindFirstObjectByType(type);
            if (target != null) type.GetMethod("Update", Flags).Invoke(target, null);
        }
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); await Task.Delay(80);
    }
    private static async Task CaptureSizes(string name)
    {
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1024, 768), new Vector2Int(1920, 1080), new Vector2Int(2560, 1080) })
        {
            await (Task)typeof(UiRedesignCheck).GetMethod("SetSize", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { size.x, size.y });
            string path = Path.Combine(output, name + "-" + size.x + "x" + size.y + ".png");
            ScreenCapture.CaptureScreenshot(path); await Until(() => File.Exists(path) && new FileInfo(path).Length > 1000);
        }
    }
    private static void CheckTextLayout(Rect rect, string value, GUIStyle style)
    {
        if (string.IsNullOrEmpty(value) || Event.current.type != EventType.Repaint) return;
        var content = new GUIContent(value); Vector2 needed = style.CalcSize(content);
        float height = style.wordWrap ? style.CalcHeight(content, rect.width) : needed.y;
        if (height > rect.height + 2 || !style.wordWrap && needed.x > rect.width + 2)
            layoutIssues.Add($"{Screen.width}x{Screen.height}: '{value}' needs {needed.x:0}x{height:0}, has {rect.width:0}x{rect.height:0}");
    }
    private static void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Exception && trace.Contains("UnityEditor.Search.SearchDatabase")) { searchErrors++; return; }
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
    }
}
