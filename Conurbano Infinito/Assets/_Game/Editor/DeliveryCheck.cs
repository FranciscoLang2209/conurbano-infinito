using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Runs only in an isolated project: controlled integration and scripted survival are separate evidence.
public static class DeliveryCheck
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static string output;
    private static GameManager game;
    private static WeaponController weapons;
    private static UpgradeSelection selection;
    private static UpgradeData[] cards;
    private static Keyboard keyboard;
    private static int errors, searchErrors;
    [Serializable] private sealed class Matrix { public List<Row> runs = new(); }
    [Serializable] private sealed class Row
    {
        public string character, mode, build, scenario, endReason, deathAttribution;
        public float activeSeconds, firstSynergy = -1, firstEvolution = -1, pointsPerMinute, routeDistance;
        public int seed, choices, invalidOffers, offPlanChoices, peakEnemies, peakGems, peakProjectiles, createdPool;
        public long points;
        public string[] weapons, passives, synergies;
    }

    public static async void RunBatch()
    {
        output = Environment.GetEnvironmentVariable("CONURBANO_DELIVERY_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new Exception("CONURBANO_DELIVERY_OUTPUT required.");
        Directory.CreateDirectory(output);
        Application.logMessageReceived += OnLog;
        var matrix = new Matrix();
        try
        {
            var snapshots = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/_Game" })
                .Select(AssetDatabase.GUIDToAssetPath).ToDictionary(p => p,
                    p => EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<ScriptableObject>(p)));
            foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Game" }).Select(AssetDatabase.GUIDToAssetPath))
                Require(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Transform>(true)
                    .All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), "Missing prefab script: " + path);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Main.unity");
            EditorApplication.EnterPlaymode();
            await Until(() => EditorApplication.isPlaying);
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            for (int character = 0; character < 2; character++)
            foreach (GameMode mode in new[] { GameMode.Classic, GameMode.Endless })
            for (int build = 0; build < 3; build++)
            {
                await Controlled(character, mode, build);
                matrix.runs.Add(await Simulated(character, mode, build));
                File.WriteAllText(Path.Combine(output, "matrix.json"), JsonUtility.ToJson(matrix, true));
            }
            foreach (var entry in snapshots)
                Require(EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<ScriptableObject>(entry.Key)) == entry.Value,
                    "Definition mutated: " + entry.Key);
            Require(matrix.runs.Count == 12 && errors == 0, "Matrix incomplete or runtime errors.");
            Time.captureDeltaTime = 0; Time.timeScale = 1;
            EditorApplication.ExitPlaymode();
            await Until(() => !EditorApplication.isPlaying);
            Debug.Log("DELIVERY_CHECK_PASS controlled=12 scripted=12 immutable=True missing-scripts=0 knownEditorSearchErrors=" + searchErrors);
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    private static async Task Fresh(int character, GameMode mode, int seed)
    {
        Time.captureDeltaTime = 0; Time.timeScale = 1;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
        CharacterData.Select(CharacterData.All[character]); RunRules.Select(mode);
        SceneManager.LoadScene("Main"); await Frame(); await Frame();
        game = Object.FindFirstObjectByType<GameManager>();
        weapons = Object.FindFirstObjectByType<WeaponController>();
        selection = Object.FindFirstObjectByType<UpgradeSelection>();
        cards = (UpgradeData[])Get(selection, "upgrades");
        UnityEngine.Random.InitState(seed);
        game.MarkPractice("Etapa 8: simulación automatizada");
        Require(SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true))
            .All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), "Missing scene script.");
    }

    private static WeaponData Weapon(string name) => AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/" + name + ".asset");
    private static string BuildName(int build) => new[] { "Termo/Yerba", "Pelota/Zapatillas", "Pelota/Petardo/Antena" }[build];
    private static bool OnPlan(UpgradeData card, int build) => card.Effect == UpgradeEffect.EvolveWeapon ||
        (card.TargetWeapon != null && (card.TargetWeapon == Weapon(build == 0 ? "Termo Balístico" : "Pelota de Potrero") || build == 2 && card.TargetWeapon == Weapon("Petardo"))) ||
        card.Effect == (build == 0 ? UpgradeEffect.FireInterval : build == 1 ? UpgradeEffect.MovementSpeed : UpgradeEffect.PassiveExplosionRadius);

    private static async Task Choose(UpgradeData card)
    {
        await Frame();
        Require(card.CanOffer(weapons) && selection.GetLevel(card) < card.MaximumLevel, "Invalid controlled card: " + card.name);
        Set(selection, "upgrades", new[] { card });
        weapons.GetComponent<ExperienceSystem>().AddExperience(weapons.GetComponent<ExperienceSystem>().RequiredExperience);
        int index = selection.Options.ToList().IndexOf(card);
        Require(index >= 0, "Controlled card absent."); selection.Select(index);
        Require(!selection.IsShowing, "Controlled choice not consumed.");
    }

    private static async Task Raise(WeaponData data, int level)
    {
        if (!weapons.IsEquipped(data)) await Choose(cards.First(c => c.TargetWeapon == data && c.Effect == UpgradeEffect.EquipWeapon));
        while (weapons.GetWeaponLevel(data) < level)
            await Choose(cards.First(c => c.TargetWeapon == data && c.Effect != UpgradeEffect.EquipWeapon &&
                c.CanOffer(weapons) && selection.GetLevel(c) < c.MaximumLevel));
    }

    private static async Task Controlled(int character, GameMode mode, int build)
    {
        await Fresh(character, mode, 8000 + character * 100 + (int)mode * 10 + build);
        weapons.enabled = false;
        WeaponData primary = Weapon(build == 0 ? "Termo Balístico" : "Pelota de Potrero");
        await Raise(primary, build == 2 ? 4 : 8);
        UpgradeData passive = cards.First(c => c.Effect == (build == 0 ? UpgradeEffect.FireInterval :
            build == 1 ? UpgradeEffect.MovementSpeed : UpgradeEffect.PassiveExplosionRadius));
        if (build == 2) await Raise(Weapon("Petardo"), 4);
        for (int i = 0; i < passive.MaximumLevel; i++) await Choose(passive);
        Require(weapons.ActiveSynergies.Count == 1 && weapons.GetPassiveLevel(passive) == passive.MaximumLevel,
            "Controlled specialization missing.");
        if (build != 2)
        {
            WaveDirector director = Object.FindFirstObjectByType<WaveDirector>();
            director.Tick(180); director.ActiveBoss.TakeDamage(director.ActiveBoss.MaximumHealth);
            Require(weapons.HasDefeatedBoss, "Boss achievement disconnected.");
            UpgradeData recipe = UpgradeData.EvolutionCards.First(c => c.TargetWeapon == primary);
            await Choose(recipe);
            Require(weapons.GetEvolution(primary) == recipe && weapons.GetWeaponLevel(primary) == 8, "Evolution not integrated.");
        }
        Set(selection, "upgrades", cards);
        game.SetElapsedForPractice(240); await Frame(); await Frame();
        Require(game.State == (mode == GameMode.Classic ? GameManager.GameState.Victory : GameManager.GameState.Playing), "Mode boundary.");
        if (mode == GameMode.Endless) weapons.GetComponent<Health>().TakeDamage(100000);
        RunResult result = game.Result;
        Require(result != null && result.IsPractice && !result.IsRecordEligible && result.Synergies.Count == 1, "Closing snapshot.");
        var records = new LocalRecords(Path.Combine(output, $"controlled-{character}-{mode}-{build}.json"));
        Require(records.Save(result) && records.Save(result) && records.Table(result.ModeId, result.RulesetVersion).Length == 0,
            "Practice leaked into top or persistence failed.");
        Require(new LocalRecords(Path.Combine(output, $"controlled-{character}-{mode}-{build}.json")).LastResult.runId == result.RunId,
            "Closing reload failed.");
        game.PauseGame(); weapons.GetComponent<Health>().TakeDamage(100000);
        Require(ReferenceEquals(result, game.Result), "Closing repeated.");
        Debug.Log($"DELIVERY_CONTROLLED_PASS character={character} mode={mode} build={BuildName(build)} forcedXp=True forcedBoss={build != 2}");
    }

    private static async Task<Row> Simulated(int character, GameMode mode, int build)
    {
        int seed = 8100 + character * 100 + (int)mode * 10 + build;
        await Fresh(character, mode, seed);
        var row = new Row { character = CharacterData.Selected.DisplayName, mode = mode.ToString(), build = BuildName(build), seed = seed,
            scenario = "scripted square route; normal health/XP/catalog; captureDelta=1/30 timeScale=8; not human or realtime FPS" };
        var waypoints = new[] { new Vector2(4, 3), new Vector2(-4, 3), new Vector2(-4, -3), new Vector2(4, -3) };
        int waypoint = 0;
        bool paused = false;
        Vector2 previousPosition = weapons.transform.position;
        Time.captureDeltaTime = 1f / 30; Time.timeScale = 8;
        double start = EditorApplication.timeSinceStartup;
        while (game.State == GameManager.GameState.Playing && game.ElapsedSeconds < (mode == GameMode.Classic ? 240 : 480))
        {
            if (selection.IsShowing)
            {
                row.invalidOffers += selection.Options.Count(c => !c.CanOffer(weapons) || selection.GetLevel(c) >= c.MaximumLevel);
                int index = selection.Options.ToList().FindIndex(c => OnPlan(c, build));
                if (index < 0) { index = 0; row.offPlanChoices++; }
                selection.Select(index); row.choices++;
                if (!selection.IsShowing) Time.timeScale = 8;
            }
            if (!paused && !selection.IsShowing && game.ElapsedSeconds >= 5)
            {
                game.PauseGame(); float clock = game.ElapsedSeconds; long points = game.Score.Centipoints;
                await Frame(); await Frame();
                Require(game.ElapsedSeconds == clock && game.Score.Centipoints == points, "Pause changed clock/score.");
                game.ContinueGame(); Time.timeScale = 8; paused = true;
            }
            Vector2 delta = waypoints[waypoint] - (Vector2)weapons.transform.position;
            if (delta.sqrMagnitude < 0.7f) waypoint = (waypoint + 1) % waypoints.Length;
            var keys = new List<Key>();
            if (Mathf.Abs(delta.x) > .3f) keys.Add(delta.x > 0 ? Key.D : Key.A);
            if (Mathf.Abs(delta.y) > .3f) keys.Add(delta.y > 0 ? Key.W : Key.S);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys.ToArray())); InputSystem.Update();
            if (row.firstSynergy < 0 && weapons.ActiveSynergies.Count > 0) row.firstSynergy = game.ElapsedSeconds;
            if (row.firstEvolution < 0 && Enumerable.Range(0, weapons.EquippedWeaponCount).Any(i => weapons.GetEvolution(weapons.GetEquippedWeapon(i)) != null))
                row.firstEvolution = game.ElapsedSeconds;
            Require(EditorApplication.timeSinceStartup - start < 180, "Scripted matrix timeout.");
            await Frame();
            row.routeDistance += Vector2.Distance(previousPosition, weapons.transform.position);
            previousPosition = weapons.transform.position;
        }
        row.activeSeconds = game.ElapsedSeconds; row.points = game.Score.Points;
        row.pointsPerMinute = row.points * 60f / Mathf.Max(1, row.activeSeconds);
        row.endReason = game.State.ToString();
        row.deathAttribution = game.State == GameManager.GameState.Defeat ? "Health.Died; attacker not instrumented" : "No death observed";
        row.weapons = Enumerable.Range(0, weapons.EquippedWeaponCount).Select(i => weapons.GetWeaponDisplayName(weapons.GetEquippedWeapon(i)) + " N" + weapons.GetWeaponLevel(weapons.GetEquippedWeapon(i))).ToArray();
        row.passives = weapons.SelectedPassives.Select(p => p.DisplayName + " N" + weapons.GetPassiveLevel(p)).ToArray();
        row.synergies = weapons.ActiveSynergies.Select(s => s.DisplayName).ToArray();
        ObjectPool pool = Object.FindFirstObjectByType<ObjectPool>();
        row.peakEnemies = Object.FindFirstObjectByType<WaveDirector>().PeakEnemyCount;
        row.peakGems = pool.PeakGemCount; row.peakProjectiles = pool.PeakProjectileCount; row.createdPool = pool.CreatedCount;
        Require(row.routeDistance > 2 && row.invalidOffers == 0 && row.peakEnemies <= 151 && row.peakGems <= 256 && row.peakProjectiles <= 256,
            $"Movement/offers/budgets: distance={row.routeDistance} invalid={row.invalidOffers} peaks={row.peakEnemies}/{row.peakGems}/{row.peakProjectiles}.");
        Debug.Log($"DELIVERY_SCRIPTED_RESULT character={character} mode={mode} build={BuildName(build)} seconds={row.activeSeconds} reason={row.endReason} points={row.points}");
        return row;
    }

    public static void BuildMac()
    {
        string app = Environment.GetEnvironmentVariable("CONURBANO_DELIVERY_APP");
        if (string.IsNullOrEmpty(app)) throw new Exception("CONURBANO_DELIVERY_APP required.");
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Require(scenes.SequenceEqual(new[] { "Assets/_Game/Scenes/Menu.unity", "Assets/_Game/Scenes/Main.unity" }), "Unexpected build scenes.");
        Debug.Log("DELIVERY_WINDOWS_SUPPORT targetSupported=" + BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64) +
            " moduleDirectoryPresent=" + Directory.Exists(Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines/WindowsStandaloneSupport")));
        Debug.Log("DELIVERY_BUILD_CONFIG backend=" + PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone) + " architecture=" + PlayerSettings.GetArchitecture(NamedBuildTarget.Standalone));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = app,
            target = BuildTarget.StandaloneOSX, options = Environment.GetEnvironmentVariable("CONURBANO_DELIVERY_QA") == "1" ? BuildOptions.Development : BuildOptions.None });
        Debug.Log($"DELIVERY_BUILD_RESULT result={report.summary.result} errors={report.summary.totalErrors} warnings={report.summary.totalWarnings} bytes={report.summary.totalSize} duration={report.summary.totalTime}");
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    private static object Get(object owner, string name) => owner.GetType().GetField(name, Flags).GetValue(owner);
    private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Flags).SetValue(owner, value);
    private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    private static async Task Frame() { int frame = Time.frameCount; await Until(() => Time.frameCount > frame); }
    private static async Task Until(Func<bool> ready)
    {
        double deadline = EditorApplication.timeSinceStartup + 30;
        while (!ready()) { Require(EditorApplication.timeSinceStartup < deadline, "Delivery check timed out."); await Task.Delay(1); }
    }
    private static void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Exception && trace.Contains("UnityEditor.Search.SearchDatabase")) { searchErrors++; return; }
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
    }
}
