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

public static class ScoreCheck
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static string output;
    private static GameManager game;
    private static WeaponController weapons;
    private static SpawnManager spawner;
    private static QuilomboSystem quilombo;
    private static int errors, searchErrors;
    private static readonly HashSet<string> layoutIssues = new();

    public static async void RunBatch()
    {
        output = Environment.GetEnvironmentVariable("CONURBANO_SCORE_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new Exception("CONURBANO_SCORE_OUTPUT is required.");
        Directory.CreateDirectory(output);
        Application.logMessageReceived += OnLog;
        try
        {
            CheckArithmetic();
            var definitions = AssetDatabase.FindAssets("t:RunRules t:WeaponData t:UpgradeData t:CharacterData", new[] { "Assets/_Game" })
                .Select(AssetDatabase.GUIDToAssetPath).ToDictionary(p => p,
                    p => EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<ScriptableObject>(p)));
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Main.unity");
            EditorApplication.EnterPlaymode();
            await Until(() => EditorApplication.isPlaying);
            await Task.Delay(200);
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            await CheckDeathsAndPois();
            await CheckPauseAndClose();
            await CheckPracticeAndRestart();
            foreach (var entry in definitions)
                Require(EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<ScriptableObject>(entry.Key)) == entry.Value, "Definition mutated: " + entry.Key);
            Require(errors == 0 && layoutIssues.Count == 0, "Console/layout failures: " + errors + "/" + layoutIssues.Count);
            File.WriteAllLines(Path.Combine(output, "text-layout.txt"), layoutIssues);
            GameUi.CheckTextLayout = null;
            EditorApplication.ExitPlaymode();
            await Until(() => !EditorApplication.isPlaying);
            Debug.Log("SCORE_CHECK_PASS arithmetic=3240,fractions,saturation pool=6-prefabs,12-lives death-vs-despawn=True pois=3,once pause-queue=True immutable-close=True defeat-priority=True abandonment=True practice-latched=True modes=2 characters=2 layouts=4 knownEditorSearchErrors=" + searchErrors);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            File.WriteAllLines(Path.Combine(output, "text-layout.txt"), layoutIssues);
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static void CheckArithmetic()
    {
        var score = new ScoreSystem();
        for (int i = 0; i < 100; i++) score.EnemyKilled(EnemyScoreCategory.Common, 25);
        score.EnemyKilled(EnemyScoreCategory.Boss, 25);
        score.PoiCompleted(); score.PoiCompleted(); score.ObserveActiveTime(240);
        Require(score.Centipoints == 324000 && score.Points == 3240 && score.KillCount(EnemyScoreCategory.Boss) == 1 &&
            score.EnemyCentipoints(EnemyScoreCategory.Common) == 125000 && score.PoiCentipoints == 50000 &&
            score.SurvivalCentipoints == 24000, "3240 example / breakdown.");
        var fractional = new ScoreSystem();
        fractional.EnemyKilled(EnemyScoreCategory.Common, 10.49f);
        fractional.EnemyKilled(EnemyScoreCategory.Special, 10.5f);
        fractional.EnemyKilled(EnemyScoreCategory.Common, -1);
        fractional.EnemyKilled(EnemyScoreCategory.Common, 200);
        Require(fractional.Centipoints == 1100 + 3330 + 1000 + 2000 && fractional.Points == 74, "Fractional factor/clamps/floor.");
        fractional.ObserveActiveTime(0.99); Require(fractional.SurvivalSeconds == 0, "Partial second scored.");
        fractional.ObserveActiveTime(1); fractional.ObserveActiveTime(1); fractional.ObserveActiveTime(0);
        Require(fractional.SurvivalSeconds == 1 && fractional.SurvivalCentipoints == 100, "Seconds duplicated/rewound.");
        var saturated = new ScoreSystem(); saturated.ObserveActiveTime(double.MaxValue);
        saturated.EnemyKilled(EnemyScoreCategory.Boss, 100); saturated.PoiCompleted();
        Require(saturated.Centipoints == long.MaxValue && saturated.SurvivalCentipoints == long.MaxValue, "Saturating arithmetic.");
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity })
        {
            bool rejected = false;
            try { score.EnemyKilled(EnemyScoreCategory.Common, invalid); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Require(rejected, "Invalid Quilombo accepted.");
        }
        score.MarkPractice("QA"); score.MarkPractice("Otro motivo"); score.Freeze();
        score.EnemyKilled(EnemyScoreCategory.Boss, 100); score.PoiCompleted(); score.ObserveActiveTime(500);
        Require(score.Centipoints == 324000 && score.PracticeReason == "QA", "Freeze/practice latch.");
        Debug.Log("SCORE_ARITHMETIC_PASS example=3240 fractions=nearest-away floor-total=True saturation=long.MaxValue invalid-values=rejected");
    }

    private static async Task CheckDeathsAndPois()
    {
        await Fresh(GameMode.Endless, 0);
        string[] prefabs = { "MosquitoGigante", "PalomaDePlaza", "BolsaVoladora", "PerroCallejero", "VecinoGomero", "LaJefa" };
        foreach (string name in prefabs)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/" + name + ".prefab");
            EnemyScoreCategory category = name == "LaJefa" ? EnemyScoreCategory.Boss :
                name == "PerroCallejero" || name == "VecinoGomero" ? EnemyScoreCategory.Special : EnemyScoreCategory.Common;
            Require(prefab.GetComponent<ExperienceDrop>().ScoreCategory == category, "Explicit prefab category: " + name);
            Health previous = null;
            for (int life = 0; life < 12; life++)
            {
                Health enemy = spawner.Spawn(prefab);
                if (previous != null) Require(enemy == previous, "Pool did not reuse the test instance.");
                uint id = enemy.LifeId;
                enemy.name = "Nombre arbitrario";
                long kills = game.Score.KillCount(category), before = game.Score.Centipoints;
                quilombo.Remove(100); quilombo.Add(life % 2 == 0 ? 25 : 35);
                enemy.TakeDamage(enemy.MaximumHealth);
                enemy.TakeDamage(enemy.MaximumHealth);
                Call(enemy.GetComponent<ExperienceDrop>(), "DropGem");
                long basis = category == EnemyScoreCategory.Boss ? 1000 : category == EnemyScoreCategory.Special ? 30 : 10;
                Require(game.Score.KillCount(category) == kills + 1 && game.Score.Centipoints == before + basis * (life % 2 == 0 ? 125 : 135),
                    "Death scored twice or sampled stale Quilombo: " + name);
                ObjectPool.Release(enemy.gameObject);
                Health alive = spawner.Spawn(prefab);
                Require(alive == enemy && alive.LifeId != id, "New pooled life not distinguished.");
                before = game.Score.Centipoints; ObjectPool.Release(alive.gameObject);
                Require(game.Score.Centipoints == before, "Despawn awarded points.");
                previous = enemy;
            }
        }
        // Unsubscribing/re-enabling the game must not duplicate the listener.
        game.enabled = false;
        var mosquito = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/MosquitoGigante.prefab");
        long beforeDisabled = game.Score.Centipoints;
        Health ignored = spawner.Spawn(mosquito); ignored.TakeDamage(ignored.MaximumHealth); ObjectPool.Release(ignored.gameObject);
        Require(game.Score.Centipoints == beforeDisabled, "Disabled game kept score subscription.");
        game.enabled = true;
        Health observed = spawner.Spawn(mosquito); observed.TakeDamage(observed.MaximumHealth); ObjectPool.Release(observed.gameObject);
        Require(game.Score.KillCount(EnemyScoreCategory.Common) == 37, "Enable duplicated / lost score listener.");
        foreach (RiskZone zone in Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None))
        {
            Set(zone, "player", weapons.GetComponent<PlayerController>()); Set(zone, "playerHealth", weapons.GetComponent<Health>());
            long before = game.Score.PoiCount;
            Call(zone, "StartReward"); Call(zone, "StartReward"); Call(zone, "Complete"); Call(zone, "Complete");
            Require(game.Score.PoiCount == before + 1, "POI score must follow first earned reward only.");
        }
        Require(game.Score.PoiCount == 3 && game.Score.PoiCentipoints == 75000, "Three POIs.");
        Debug.Log("SCORE_EVENTS_PASS lives=72 despawns=72 renamed=True current-quilombo=True duplicate-death=True subscriptions-balanced=True pois=3,once");
    }

    private static async Task CheckPauseAndClose()
    {
        await Fresh(GameMode.Classic, 0);
        game.SetElapsedForPractice(2.75f); Call(game, "Update");
        Require(game.Score.SurvivalSeconds == 2, "Active complete seconds.");
        game.PauseGame(); long paused = game.Score.Centipoints; float time = game.ElapsedSeconds;
        Call(game, "Update"); await Task.Delay(100);
        Require(game.Score.Centipoints == paused && game.ElapsedSeconds == time && game.Result == null, "Pause altered score/closed run.");
        game.ContinueGame();
        ExperienceSystem xp = weapons.GetComponent<ExperienceSystem>();
        xp.AddExperience(xp.RequiredExperience * 3);
        UpgradeSelection selection = game.GetComponent<UpgradeSelection>();
        Require(selection.IsShowing && Time.timeScale == 0, "No queued choices.");
        paused = game.Score.Centipoints; time = game.ElapsedSeconds;
        Health enemy = spawner.Spawn(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/PerroCallejero.prefab"));
        enemy.TakeDamage(enemy.MaximumHealth); ObjectPool.Release(enemy.gameObject);
        Call(game, "Update"); await Task.Delay(100);
        Require(game.Score.Centipoints == paused && game.ElapsedSeconds == time, "Level choice awarded score/time.");
        while (selection.IsShowing) { selection.Select(0); await Frame(); }
        int closes = 0; RunResult eventResult = null;
        game.RunClosed += result => { closes++; eventResult = result; Call(game, "AbandonGame"); };
        game.SetElapsedForPractice(240); Call(game, "Update");
        Require(game.Result == null, "Victory committed before gameplay finished.");
        weapons.GetComponent<Health>().TakeDamage(weapons.GetComponent<Health>().MaximumHealth);
        Call(game, "LateUpdate");
        RunResult final = game.Result;
        Require(final != null && final.EndReason == GameManager.GameState.Defeat && closes == 1 && eventResult == final &&
            final.ActiveSeconds == 240 && final.SurvivalSeconds == 240 && final.IsPractice && !final.IsRecordEligible,
            "Deadline death priority / atomic result / practice eligibility.");
        Guid.ParseExact(final.RunId, "N");
        Require(final.StartedUtc.Kind == DateTimeKind.Utc && final.CharacterId == CharacterData.Selected.StableId &&
            final.ModeId == game.Configuration.StableId && final.RulesetVersion == "builds-infinito-1" && final.BuildVersion == Application.version,
            "Missing result identity / versions.");
        int weaponCount = final.Weapons.Count; long points = final.Centipoints;
        weapons.EquipWeapon(AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Pelota de Potrero.asset"));
        quilombo.Add(100); game.Score.PoiCompleted(); game.Score.EnemyKilled(EnemyScoreCategory.Boss, 100);
        game.SetElapsedForPractice(500); Call(game, "OnPlayerDied"); Call(game, "AbandonGame");
        Require(game.Result == final && final.Weapons.Count == weaponCount && final.Centipoints == points &&
            game.Score.Centipoints == points && game.ElapsedSeconds == 240 && closes == 1, "Closed score/build mutated / duplicate result.");
        GameUi.CheckTextLayout = CheckTextLayout;
        await CaptureSizes("derrota");
        await Fresh(GameMode.Classic, 1);
        PrepareFullBuild();
        game.SetElapsedForPractice(240); Call(game, "Update"); Call(game, "LateUpdate");
        Require(game.Result.EndReason == GameManager.GameState.Victory && game.Result.SurvivalSeconds == 240 &&
            game.Result.Weapons.Count == 4 && game.Result.Passives.Count == 4 && game.Result.Synergies.Count == 3 &&
            game.Result.Weapons.Count(w => w.EvolutionId != null) == 2 && game.Result.Passives.All(p => p.Level > 0),
            "Living deadline / full evolved build snapshot.");
        await CaptureSizes("victoria");
        var scrollKeyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            InputSystem.QueueStateEvent(scrollKeyboard, new KeyboardState(Key.PageDown)); InputSystem.Update(); scrollKeyboard.MakeCurrent();
            Call(game, "Update");
            InputSystem.QueueStateEvent(scrollKeyboard, new KeyboardState()); InputSystem.Update();
            Require((int)typeof(GameManager).GetField("firstResultLine", Flags).GetValue(game) == 5 &&
                game.State == GameManager.GameState.Victory, "Result keyboard scrolling changed the outcome.");
            await CaptureSizes("resumen-desplazado");
        }
        finally { InputSystem.RemoveDevice(scrollKeyboard); }
        await Fresh(GameMode.Classic, 0);
        game.Score.ObserveActiveTime(double.MaxValue);
        game.SetElapsedForPractice(240); Call(game, "Update"); Call(game, "LateUpdate");
        await CaptureSizes("puntaje-maximo");
        await Fresh(GameMode.Endless, 1);
        game.SetElapsedForPractice(300); Call(game, "Update"); Call(game, "LateUpdate");
        Require(game.Result == null && game.State == GameManager.GameState.Playing, "Endless result at classic deadline.");
        await CaptureSizes("hud-infinito");
        long before = game.Score.Centipoints;
        game.PauseGame(); Call(game, "AbandonGame");
        Require(game.Result.EndReason == GameManager.GameState.Abandoned && !game.Result.IsRecordEligible && game.Score.Centipoints == before,
            "Abandon awarded points / eligible record.");
    }

    private static async Task CheckPracticeAndRestart()
    {
        await Fresh(GameMode.Endless, 0);
        Require(game.Score.IsPractice, "Editor / automatic tests were eligible.");
        // Simulate a standalone session, bypassing only the automatic Editor practice mark.
        Set(game, "<Score>k__BackingField", new ScoreSystem());
        Require(!game.Score.IsPractice, "Standalone fixture not clean.");
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            for (int i = 0; i < 2; i++)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F9)); InputSystem.Update(); keyboard.MakeCurrent();
                Call(game, "Update");
                Require(game.IsFastForwarding == (i == 0), "F9 not delivered.");
                Require(game.Score.IsPractice && game.Score.PracticeReason.Contains("F9"), "F9 did not permanently mark practice.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            }
        }
        finally { InputSystem.RemoveDevice(keyboard); }
        Set(game, "<Score>k__BackingField", new ScoreSystem());
        Time.timeScale = 8; Call(game, "Update"); Time.timeScale = 1;
        Require(game.Score.IsPractice && game.Score.PracticeReason.Contains("simulación"), "Acceleration not marked.");
        Set(game, "<Score>k__BackingField", new ScoreSystem());
        Set(weapons.GetComponent<Health>(), "invulnerabilitySeconds", 10000f); Call(game, "Update");
        Set(weapons.GetComponent<Health>(), "invulnerabilitySeconds", 0.8f);
        Require(game.Score.IsPractice && game.Score.PracticeReason.Contains("invulnerabilidad"), "QA invulnerability not marked.");
        Set(game, "<Score>k__BackingField", new ScoreSystem());
        game.SetElapsedForPractice(10); Call(game, "Update");
        game.MarkPractice("QA health");
        Require(game.Score.IsPractice && game.Score.PracticeReason.Contains("inyectado"), "Injected clock / practice latch.");
        string oldId = null;
        game.RunClosed += result => oldId = result.RunId;
        game.RestartGame(); await Task.Delay(150); Bind();
        Require(oldId != null && game.Result == null && game.Score.Points < 2 && game.Score.PoiCount == 0 &&
            game.Score.KillCount(EnemyScoreCategory.Common) == 0 && game.Configuration.Mode == GameMode.Endless &&
            weapons.EquippedWeaponCount == 1, "Restart did not clear score/result/build or preserve mode.");
        weapons.GetComponent<Health>().TakeDamage(weapons.GetComponent<Health>().MaximumHealth);
        Require(game.Result.RunId != oldId, "Restart reused run ID.");
        RunResult final = game.Result;
        game.ReturnToMenu(); await Task.Delay(100);
        Require(SceneManager.GetActiveScene().name == "Menu" && final.EndReason == GameManager.GameState.Defeat, "Menu overwrote terminal result.");
        await Fresh(GameMode.Classic, 0);
        Set(game, "<Score>k__BackingField", new ScoreSystem());
        weapons.GetComponent<Health>().TakeDamage(weapons.GetComponent<Health>().MaximumHealth);
        Require(game.Result.IsRecordEligible && !game.Result.IsPractice, "Unassisted terminal run excluded.");
    }

    private static void PrepareFullBuild()
    {
        foreach (string name in new[] { "Termo Balístico", "Pelota de Potrero", "Petardo" })
            Require(weapons.EquipWeapon(AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/" + name + ".asset")), "Full build equip.");
        foreach (string name in new[] { "Yerba Premium", "Zapatillas Flecha", "Antena de Televisión", "Bolsa del Supermercado" })
        {
            UpgradeData data = AssetDatabase.LoadAssetAtPath<UpgradeData>("Assets/_Game/Data/" + name + ".asset");
            for (int i = 0; i < data.MaximumLevel; i++) data.Apply(weapons.GetComponent<PlayerController>(), weapons, weapons.GetComponent<Health>());
        }
        for (int i = 0; i < weapons.EquippedWeaponCount; i++)
            for (int level = 1; level < 8; level++)
                typeof(WeaponController).GetMethod("AdvanceWeaponLevel", Flags).Invoke(weapons, new object[] { weapons.GetEquippedWeapon(i) });
        Call(weapons, "RegisterBossDefeated");
        foreach (UpgradeData recipe in UpgradeData.EvolutionCards) Require(weapons.Evolve(recipe), "Full build evolution.");
    }

    private static async Task Fresh(GameMode mode, int character)
    {
        RunRules.Select(mode); CharacterData.Select(CharacterData.All[character]); SceneManager.LoadScene("Main");
        await Task.Delay(120); Bind();
        game.GetComponent<WaveDirector>().enabled = false; weapons.enabled = false;
    }
    private static void Bind()
    {
        game = Object.FindFirstObjectByType<GameManager>(); weapons = Object.FindFirstObjectByType<WeaponController>();
        spawner = Object.FindFirstObjectByType<SpawnManager>();
        quilombo = Object.FindFirstObjectByType<QuilomboSystem>();
    }
    private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Flags).SetValue(owner, value);
    private static void Call(object owner, string method) => owner.GetType().GetMethod(method, Flags).Invoke(owner, null);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task Frame() { int frame = Time.frameCount; await Until(() => Time.frameCount > frame); }
    private static async Task Until(Func<bool> ready)
    {
        double deadline = EditorApplication.timeSinceStartup + 30;
        while (!ready()) { if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Score check timed out."); await Task.Delay(10); }
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
