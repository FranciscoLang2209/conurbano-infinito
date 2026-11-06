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

public static class RecordsCheck
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static string output, savePath;
    private static WeaponController weapons;
    private static int errors, searchErrors;
    private static readonly HashSet<string> layout = new();
    private static Keyboard keyboard;

    public static async void RunBatch()
    {
        output = Environment.GetEnvironmentVariable("CONURBANO_RECORDS_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new Exception("CONURBANO_RECORDS_OUTPUT is required.");
        Directory.CreateDirectory(output);
        Application.logMessageReceived += OnLog;
        try
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Main.unity");
            EditorApplication.EnterPlaymode(); await Until(() => EditorApplication.isPlaying); await Task.Delay(200);
            weapons = Object.FindFirstObjectByType<WeaponController>();
            Object.FindFirstObjectByType<GameManager>().PauseGame();
            CheckFiles();
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            GameUi.CheckTextLayout = CheckLayout;
            await CheckFlow();
            Require(errors == 0 && layout.Count == 0, $"Console/layout: {errors}/{layout.Count}");
            File.WriteAllLines(Path.Combine(output, "text-layout.txt"), layout);
            InputSystem.RemoveDevice(keyboard); GameUi.CheckTextLayout = null;
            EditorApplication.ExitPlaymode(); await Until(() => !EditorApplication.isPlaying);
            Debug.Log("RECORDS_CHECK_PASS top20=True modes-rules=True ties=True idempotent-evicted=True last-practice-abandon=True atomic-backup-recovery=True incompatible-preserved=True write-failure=True stable-ids=True menu-close-record-retry=True keyboard-mouse=True layouts=4 knownEditorSearchErrors=" + searchErrors);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            File.WriteAllLines(Path.Combine(output, "text-layout.txt"), layout);
            Debug.LogException(error); EditorApplication.Exit(1);
        }
    }

    private static RunResult Result(string id, int kills = 1, float seconds = 10, GameMode mode = GameMode.Classic,
        string version = "builds-infinito-1", string practice = null, GameManager.GameState reason = GameManager.GameState.Defeat)
    {
        var score = new ScoreSystem();
        for (int i = 0; i < kills; i++) score.EnemyKilled(EnemyScoreCategory.Common, 0);
        score.ObserveActiveTime(seconds);
        if (practice != null) score.MarkPractice(practice);
        score.Freeze();
        RunConfiguration config = RunRules.All[(int)mode].CreateConfiguration();
        config = new RunConfiguration(config.StableId, mode, 240, 120, .2f, .02f, 1.3f, .08f, 2, .2f, 150, 240, 256, 256, version);
        return new RunResult(id, new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc), "records-qa", config,
            CharacterData.Selected.StableId, seconds, 0, reason, score, weapons);
    }

    private static void CheckFiles()
    {
        savePath = Path.Combine(output, "fixtures", "records.json");
        var records = new LocalRecords(savePath);
        string classic = RunRules.All[0].CreateConfiguration().StableId;
        string endless = RunRules.All[1].CreateConfiguration().StableId;
        const string version = "builds-infinito-1";
        Require(records.LastResult == null && records.Table(classic, version).Length == 0, "Missing file.");
        RunResult first = Result("000-first");
        Require(records.Save(first) && File.Exists(savePath + ".bak"), "First atomic save / backup.");
        string before = File.ReadAllText(savePath);
        Require(records.Save(first) && File.ReadAllText(savePath) == before, "Duplicate rewrote save.");
        for (int i = 0; i < 25; i++) Require(records.Save(Result("run-" + i.ToString("D2"), i + 1)), "Top20 save.");
        StoredRun[] table = new LocalRecords(savePath).Table(classic, version);
        Require(table.Length == 20 && table[0].runId == "run-24" && table[19].runId == "run-05", "Order / top20 / reload.");
        Require(records.Save(Result("run-23", 1000)) && records.LastResult.runId == "run-24", "Duplicate changed identity/score.");
        Require(records.Save(Result("000-first", 1000)) && records.Table(classic, version)[0].runId == "run-24", "Evicted ID registered again.");
        Require(records.Save(Result("endless", 2, 10, GameMode.Endless)) && records.Table(endless, version).Length == 1, "Mode table.");
        Require(records.Save(Result("old-version", 2, 10, GameMode.Classic, "previous-rules")) &&
            records.Table(classic, "previous-rules").Length == 1 && records.Table(classic, version).Length == 20, "Version history.");
        Require(records.Save(Result("practice", practice: "F9")) && records.LastResult.practiceReason == "F9" && records.Table(classic, version).Length == 20, "Practice record leaked.");
        Require(records.Save(Result("abandon", reason: GameManager.GameState.Abandoned)) && records.LastResult.endReason == GameManager.GameState.Abandoned && !records.LastResult.isRecordEligible, "Abandon record leaked.");
        string atomicPath = Path.Combine(output, "fixtures", "atomic.json");
        var atomic = new LocalRecords(atomicPath); Require(atomic.Save(first), "Atomic fixture.");
        string intact = File.ReadAllText(atomicPath);
        Directory.CreateDirectory(atomicPath + ".tmp");
        Require(!atomic.Save(Result("failed-replace")) && File.ReadAllText(atomicPath) == intact && atomic.LastResult.runId == first.RunId, "Failed write changed previous result.");
        Directory.Delete(atomicPath + ".tmp");
        File.WriteAllText(atomicPath + ".tmp", "interrupted write");
        Require(new LocalRecords(atomicPath).LastResult.runId == first.RunId, "Incomplete temp promoted.");
        foreach (bool saturated in new[] { false, true })
        {
            var score = new ScoreSystem();
            if (saturated) score.ObserveActiveTime(double.MaxValue);
            else score.EnemyKilled(EnemyScoreCategory.Common, 10.5f);
            score.Freeze();
            var exact = new RunResult("exact-" + saturated, DateTime.UtcNow, "records-qa", RunRules.All[0].CreateConfiguration(),
                CharacterData.Selected.StableId, saturated ? float.MaxValue : 0, 10.5f, GameManager.GameState.Defeat, score, weapons);
            string exactPath = Path.Combine(output, "fixtures", "exact-" + saturated + ".json");
            Require(new LocalRecords(exactPath).Save(exact) && new LocalRecords(exactPath).LastResult.centipoints == (saturated ? long.MaxValue : 1110), "Exact long/centipoint roundtrip.");
        }
        string tiesPath = Path.Combine(output, "fixtures", "ties.json");
        var ties = new LocalRecords(tiesPath);
        ties.Save(Result("z", 1, 20)); ties.Save(Result("b", 2, 10)); ties.Save(Result("a", 1, 20));
        Require(ties.Table(classic, version).Select(r => r.runId).SequenceEqual(new[] { "a", "z", "b" }), "Time/ordinal tiebreak.");
        string id = ties.LastResult.weapons[0].id;
        string named = File.ReadAllText(tiesPath).Replace(ties.LastResult.weapons[0].displayName, "Nombre nuevo");
        File.WriteAllText(tiesPath, named);
        Require(new LocalRecords(tiesPath).LastResult.weapons[0].id == id && new LocalRecords(tiesPath).LastResult.weapons[0].displayName == "Nombre nuevo", "Stable ID depends on name.");
        File.WriteAllText(savePath, "{ broken");
        var recovered = new LocalRecords(savePath);
        Require(recovered.LastResult.runId == "practice" && recovered.Notice != null, "Backup recovery.");
        Require(recovered.Save(Result("after-recovery")) && Directory.GetFiles(Path.GetDirectoryName(savePath), "records.json.corrupt-*").Length == 1, "Corrupt evidence / recovery write.");
        File.Delete(savePath);
        Require(new LocalRecords(savePath).LastResult != null, "Missing primary did not recover backup.");
        File.WriteAllText(savePath, "{\"schemaVersion\":1}");
        Require(new LocalRecords(savePath).LastResult != null, "Missing required collections discarded backup.");
        File.WriteAllText(savePath, "{}");
        var old = new LocalRecords(savePath);
        Require(!old.Save(Result("old")) && File.ReadAllText(savePath) == "{}", "Unversioned file overwritten.");
        File.WriteAllText(savePath, "{\"schemaVersion\":99}");
        Require(!new LocalRecords(savePath).Save(Result("future")) && File.ReadAllText(savePath).Contains("99"), "Future schema overwritten.");
        File.WriteAllText(savePath, "{\"schemaVersion\":1,\"records\":[{}],\"registeredRunIds\":[]}");
        Require(new LocalRecords(savePath).LastResult != null, "Invalid data accepted instead of backup.");
        File.WriteAllText(savePath, "broken"); File.WriteAllText(savePath + ".bak", "broken too");
        var both = new LocalRecords(savePath);
        Require(both.LastResult == null && both.Notice != null && both.Save(Result("fresh-after-damage")), "Both damaged recovery.");
        File.WriteAllText(savePath, "broken again");
        Require(new LocalRecords(savePath).LastResult.runId == "fresh-after-damage", "Recovered save missing good backup.");
        string blocker = Path.Combine(output, "fixtures", "blocker"); File.WriteAllText(blocker, "not a directory");
        var failed = new LocalRecords(Path.Combine(blocker, "records.json"));
        Require(!failed.Save(first) && failed.Notice != null, "Write failure escaped / reported success.");
        File.WriteAllText(Path.Combine(output, "files-pass.txt"), "top20, tables, exact ties, duplicate and evicted IDs, last result, practice, abandonment, reload, names/IDs, backup, missing/corrupt/old/future JSON, write failure\n");
    }

    private static async Task CheckFlow()
    {
        SceneManager.LoadScene("Menu"); await Task.Delay(150);
        var menu = Object.FindFirstObjectByType<MenuScreen>();
        await Press(Key.L);
        var view = (RecordsView)Field(menu, "recordsView");
        Require(view != null && view.IsOpen, "Menu records shortcut.");
        await CaptureSizes("records-menu"); await Press(Key.Escape);
        Require(!view.IsOpen, "Records return.");
        await Press(Key.Enter); await Press(Key.RightArrow); await Press(Key.Enter); await Task.Delay(150);
        var game = Object.FindFirstObjectByType<GameManager>(); weapons = Object.FindFirstObjectByType<WeaponController>();
        Require(game != null && game.State == GameManager.GameState.Playing, "Menu to run.");
        game.GetComponent<WaveDirector>().enabled = false;
        // Only isolate Editor's initial practice mark; this is controlled QA, not a human record.
        Set(game, "<Score>k__BackingField", new ScoreSystem());
        Health enemy = Object.FindFirstObjectByType<SpawnManager>().Spawn(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/MosquitoGigante.prefab"));
        enemy.TakeDamage(enemy.MaximumHealth);
        weapons.GetComponent<Health>().TakeDamage(weapons.GetComponent<Health>().MaximumHealth);
        Require(game.Result.IsRecordEligible, "Controlled terminal result.");
        var records = (LocalRecords)Field(game, "records");
        Require(records.LastResult.runId == game.Result.RunId && records.Table(game.Result.ModeId, game.Result.RulesetVersion).Any(r => r.runId == game.Result.RunId), "Close did not persist record.");
        int count = records.Table(game.Result.ModeId, game.Result.RulesetVersion).Length;
        await CaptureSizes("defeat-record");
        await Press(Key.L); view = (RecordsView)Field(game, "recordsView");
        Require(view.IsOpen && Time.timeScale == 0, "Terminal records unpaused.");
        await CaptureSizes("records-table");
        // Full build is runtime-only; definitions and scene assets remain untouched.
        PrepareBuild();
        var full = new LocalRecords(Path.Combine(output, "fixtures", "full.json"));
        Require(full.Save(Result("full-build", 5, 240)), "Full build save.");
        StoredRun restored = new LocalRecords(Path.Combine(output, "fixtures", "full.json")).LastResult;
        Require(restored.weapons.Length == 4 && restored.passives.Length == 4 && restored.synergies.Length == 3 && restored.weapons.Count(w => !string.IsNullOrEmpty(w.evolutionId)) == 2, "Full build roundtrip.");
        var pages = new LocalRecords(Path.Combine(output, "fixtures", "pages.json"));
        for (int i = 0; i < 25; i++) pages.Save(Result("page-" + i.ToString("D2"), i + 1));
        pages.Save(Result("historic", 10, 120, GameMode.Classic, "previous-rules"));
        pages.Save(Result("infinite", 5, 300, GameMode.Endless));
        pages.Save(Result("last-practice", practice: "F9"));
        view = new RecordsView(pages, game.Result.ModeId, game.Result.RulesetVersion); Set(game, "recordsView", view);
        await CaptureSizes("records-top20");
        for (int i = 0; i < 8; i++) await Press(Key.DownArrow);
        await Press(Key.Enter); Require((int)Field(view, "page") == 1, "Next page navigation.");
        await CaptureSizes("records-page2");
        await Press(Key.DownArrow); await Press(Key.Enter);
        Require(((string[])Field(view, "versions"))[(int)Field(view, "versionIndex")] == "previous-rules", "Historical version navigation.");
        await CaptureSizes("records-history");
        await Press(Key.Enter); Require((int)Field(view, "modeIndex") == 1, "Mode navigation.");
        await CaptureSizes("records-endless");
        Set(view, "focus", 5); await Press(Key.Enter);
        Require(((StoredRun)Field(view, "detail")).practiceReason == "F9", "Last result navigation / practice.");
        await CaptureSizes("records-last-practice"); await Press(Key.Escape);
        view = new RecordsView(full, game.Result.ModeId, game.Result.RulesetVersion); Set(game, "recordsView", view);
        await Press(Key.DownArrow); await Press(Key.DownArrow); await Press(Key.Enter);
        await CaptureSizes("records-evolved-build");
        await Press(Key.PageDown); await Press(Key.PageDown); await CaptureSizes("records-evolved-scroll");
        await Press(Key.Escape);
        // Explicit score/time ties are presented as separate selectable runs.
        var fixture = new LocalRecords(Path.Combine(output, "fixtures", "ties.json"));
        view = new RecordsView(fixture, game.Result.ModeId, game.Result.RulesetVersion);
        Set(game, "recordsView", view);
        await CaptureSizes("records-ties");
        await Press(Key.DownArrow); await Press(Key.DownArrow); await Press(Key.Enter);
        Require(Field(view, "detail") != null, "Keyboard detail.");
        await CaptureSizes("records-build");
        await Press(Key.PageDown); Require((int)Field(view, "firstLine") > 0, "Detail keyboard scroll.");
        await CaptureSizes("records-build-scroll"); await Press(Key.Escape);
        // IMGUI mouse click on first row, at reference coordinates.
        await (Task)typeof(UiRedesignCheck).GetMethod("SetSize", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { 1280, 720 });
        await Click(view, new Vector2(500, 44 + 220));
        Require(Field(view, "detail") != null, "Mouse detail.");
        await Press(Key.Escape); await Press(Key.Escape);
        Require(!view.IsOpen && game.Result != null && records.Table(game.Result.ModeId, game.Result.RulesetVersion).Length == count, "Reopen changed terminal / duplicate save.");
        string runId = game.Result.RunId;
        await Press(Key.R); await Task.Delay(150);
        game = Object.FindFirstObjectByType<GameManager>();
        Require(game.Result == null && game.State == GameManager.GameState.Playing, "Retry flow.");
        Require(LocalRecords.OpenDefault().LastResult.runId == runId, "Retry replaced last terminal result.");
        game.ReturnToMenu(); await Task.Delay(150);
        Require(LocalRecords.OpenDefault().LastResult.endReason == GameManager.GameState.Abandoned && !LocalRecords.OpenDefault().LastResult.isRecordEligible, "Abandon last result.");
        await Press(Key.L); await Press(Key.Escape);
        Debug.Log("RECORDS_FLOW_PASS Menu->Main->kill->Defeat->Saved->Records->Build->Return->Retry->Abandon->Menu; controlled-editor-fixture=True");
    }


    private static void PrepareBuild()
    {
        foreach (string name in new[] { "Termo Balístico", "Pelota de Potrero", "Petardo" })
            Require(weapons.EquipWeapon(AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/" + name + ".asset")), "Equip build.");
        foreach (string name in new[] { "Yerba Premium", "Zapatillas Flecha", "Antena de Televisión", "Bolsa del Supermercado" })
        {
            UpgradeData data = AssetDatabase.LoadAssetAtPath<UpgradeData>("Assets/_Game/Data/" + name + ".asset");
            for (int i = 0; i < data.MaximumLevel; i++) data.Apply(weapons.GetComponent<PlayerController>(), weapons, weapons.GetComponent<Health>());
        }
        for (int i = 0; i < weapons.EquippedWeaponCount; i++)
            for (int level = 1; level < 8; level++)
                typeof(WeaponController).GetMethod("AdvanceWeaponLevel", Flags).Invoke(weapons, new object[] { weapons.GetEquippedWeapon(i) });
        typeof(WeaponController).GetMethod("RegisterBossDefeated", Flags).Invoke(weapons, null);
        foreach (UpgradeData recipe in UpgradeData.EvolutionCards) Require(weapons.Evolve(recipe), "Evolve build.");
    }

    public static void ReloadBatch()
    {
        output = Environment.GetEnvironmentVariable("CONURBANO_RECORDS_OUTPUT");
        var records = new LocalRecords(Path.Combine(output, "fixtures", "ties.json"));
        Require(records.LastResult.runId == "a" && records.LastResult.weapons[0].displayName == "Nombre nuevo", "Fresh application reload.");
        Require(LocalRecords.OpenDefault().LastResult.endReason == GameManager.GameState.Abandoned, "Integration reload.");
        Debug.Log("RECORDS_RELOAD_PASS fresh-editor-process=True names-and-ids=True integrated-last-result=True");
        EditorApplication.Exit(0);
    }

    private static async Task Press(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update(); keyboard.MakeCurrent();
        Require(keyboard[key].wasPressedThisFrame, "Key not delivered: " + key);
        // Same dispatch as existing UI checks: batchmode has no OS keyboard events.
        foreach (Type type in new[] { typeof(GameManager), typeof(MenuScreen) })
        {
            Object target = Object.FindFirstObjectByType(type);
            if (target != null) type.GetMethod("Update", Flags).Invoke(target, null);
        }
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); await Task.Delay(80);
    }
    private sealed class MouseWindow : EditorWindow
    {
        public RecordsView View;
        private void OnGUI()
        {
            if (View == null) return;
            Matrix4x4 previous = GameUi.Begin(); View.Draw(); GameUi.End(previous);
        }
    }

    private static async Task Click(RecordsView view, Vector2 point)
    {
        // Exercise the actual RecordsView IMGUI controls in an isolated GUI host.
        // Batchmode GameView does not forward synthetic OS mouse input.
        var window = ScriptableObject.CreateInstance<MouseWindow>();
        window.View = view; window.position = new Rect(0, 0, 1280, 720); window.ShowPopup();
        await Frame();
        point *= EditorGUIUtility.pixelsPerPoint;
        window.SendEvent(new Event { type = EventType.MouseDown, mousePosition = point, button = 0 });
        window.SendEvent(new Event { type = EventType.MouseUp, mousePosition = point, button = 0 });
        await Frame();
        window.Close();
    }
    private static object Field(object owner, string name) => owner.GetType().GetField(name, Flags).GetValue(owner);
    private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Flags).SetValue(owner, value);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task Frame() { int frame = Time.frameCount; await Until(() => Time.frameCount > frame); }
    private static async Task Until(Func<bool> ready)
    {
        double deadline = EditorApplication.timeSinceStartup + 30;
        while (!ready()) { if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Records check timed out."); await Task.Delay(10); }
    }
    private static async Task CaptureSizes(string name)
    {
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1024, 768), new Vector2Int(1920, 1080), new Vector2Int(2560, 1080) })
        {
            await (Task)typeof(UiRedesignCheck).GetMethod("SetSize", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { size.x, size.y });
            string path = Path.Combine(output, name + "-" + size.x + "x" + size.y + ".png");
            ScreenCapture.CaptureScreenshot(path); await Until(() => File.Exists(path) && new FileInfo(path).Length > 1000);
        }
    }
    private static void CheckLayout(Rect rect, string value, GUIStyle style)
    {
        if (string.IsNullOrEmpty(value) || Event.current.type != EventType.Repaint) return;
        var content = new GUIContent(value); Vector2 needed = style.CalcSize(content);
        float height = style.wordWrap ? style.CalcHeight(content, rect.width) : needed.y;
        if (height > rect.height + 2 || !style.wordWrap && needed.x > rect.width + 2)
            layout.Add($"{Screen.width}x{Screen.height}: '{value}' needs {needed.x:0}x{height:0}, has {rect.width:0}x{rect.height:0}");
    }
    private static void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Exception && trace.Contains("UnityEditor.Search.SearchDatabase")) { searchErrors++; return; }
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
    }
}
