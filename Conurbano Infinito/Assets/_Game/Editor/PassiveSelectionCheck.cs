using System;
using System.Collections;
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

public static class PassiveSelectionCheck
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const string Root = "Assets/_Game/";
    private static readonly HashSet<string> layoutIssues = new();
    private static int errors, searchErrors;
    private static string output;
    private static Keyboard keyboard;
    private static WeaponController weapons;
    private static UpgradeSelection selection;
    private static Health health;
    private static ExperienceSystem xp;
    private static GameManager game;

    public static async void RunBatch()
    {
        output = Environment.GetEnvironmentVariable("CONURBANO_PASSIVES_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new Exception("CONURBANO_PASSIVES_OUTPUT is required.");
        Directory.CreateDirectory(output);
        Application.logMessageReceived += OnLog;
        try
        {
            var snapshots = AssetDatabase.FindAssets("t:UpgradeData t:WeaponData", new[] { Root })
                .Select(AssetDatabase.GUIDToAssetPath).ToDictionary(p => p,
                    p => EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<ScriptableObject>(p)));
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene(Root + "Scenes/Main.unity");
            EditorApplication.EnterPlaymode();
            await Until(() => EditorApplication.isPlaying);
            await Task.Delay(200);
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            keyboard = InputSystem.AddDevice<Keyboard>();
            Require(Card("Antena de Televisión").Description.Contains("Petardo") &&
                Card("Bolsa del Supermercado").Description.Contains("Termo y Boleadora"), "Missing serialized compatibility copy.");
            await CheckCandidatesAndSlots();
            await CheckCompatibleBonuses();
            await CheckQueueAndFallback();
            await CheckLayoutsAndBuilds();
            foreach (CharacterData character in CharacterData.All)
            {
                await Fresh(character);
                Require(selection.SelectedPassives.Count == 0 && selection.GetLevel(Card("Bolsa del Supermercado")) == 0 &&
                    weapons.EquippedWeaponCount == 1 && weapons.GetWeaponLevel(character.StartingWeapon) == 1 &&
                    !selection.IsShowing && Time.timeScale == 1f, "Restart retained build state.");
                Grant();
                Require(selection.Options.Contains(character.FirstWeaponUpgrade), "First character offer lost.");
            }
            foreach (var entry in snapshots)
                Require(EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<ScriptableObject>(entry.Key)) == entry.Value,
                    "Mutated definition: " + entry.Key);
            Require(errors == 0, "Gameplay Console errors: " + errors);
            File.WriteAllLines(Path.Combine(output, "text-layout.txt"), layoutIssues);
            Require(layoutIssues.Count == 0, "Text overflow: " + string.Join("; ", layoutIssues));
            GameUi.CheckTextLayout = null;
            InputSystem.RemoveDevice(keyboard);
            EditorApplication.ExitPlaymode();
            await Until(() => !EditorApplication.isPlaying);
            Debug.Log("PASSIVE_SELECTION_PASS slots=4 weapons=4 weapon-cap=8 card-caps=True candidates=0,1,2,3 compatibility=True future-weapons=True antenna=5 bag=3 snapshot=True queue=True double-input=True pause=True stale=True death=True reset=2 layouts=4 builds=2 immutable=True knownEditorSearchErrors=" + searchErrors);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            File.WriteAllLines(Path.Combine(output, "text-layout.txt"), layoutIssues);
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static async Task CheckCandidatesAndSlots()
    {
        var basic = new[] { Card("Chimichurri Casero"), Card("Yerba Premium"), Card("Zapatillas Flecha"), Card("Alfajor Triple") };
        for (int count = 0; count <= 3; count++)
        {
            await Fresh(CharacterData.All[0]);
            SetDeck(basic.Take(count).Concat(basic.Take(count)).Concat(new UpgradeData[] { null }).ToArray());
            Grant();
            Require(selection.Options.Count == count && selection.OptionCount == Math.Max(1, count) &&
                selection.Options.Distinct().Count() == count && Time.timeScale == 0f &&
                selection.IsFallback == (count == 0), "Candidate cardinality " + count);
            selection.Select(0);
            await Frame();
            Require(!selection.IsShowing && Time.timeScale == 1f, "Choice did not resume.");
        }
        await Fresh(CharacterData.All[0]);
        foreach (var card in basic) await Choose(card);
        Require(selection.SelectedPassives.Count == 4, "Passive capacity.");
        weapons.EquipWeapon(Weapon("Petardo"));
        Require(!Card("Antena de Televisión").CanOffer(weapons) && !Card("Bolsa del Supermercado").CanOffer(weapons), "Fifth passive offered.");
        foreach (var card in basic)
        {
            while (selection.GetLevel(card) < card.MaximumLevel) await Choose(card);
            Require(!card.CanOffer(weapons) && selection.SelectedPassives.Count == 4, "Owned passive cap or duplicate slot.");
        }
        var movement = weapons.GetComponent<PlayerController>();
        int damage = weapons.Damage, maximumHealth = health.MaximumHealth;
        foreach (var card in basic) card.Apply(movement, weapons, health);
        Require(damage == weapons.Damage && maximumHealth == health.MaximumHealth, "Max passive applied again.");
        weapons.EquipWeapon(Weapon("Boleadora"));
        weapons.EquipWeapon(Weapon("Pelota de Potrero"));
        Require(weapons.EquippedWeaponCount == 4 && !Card("Equipar Aerosol").CanOffer(weapons), "Fifth weapon offered.");
        var directed = AllCards().Where(c => c.TargetWeapon == Weapon("Termo Balístico") && c.Effect != UpgradeEffect.EquipWeapon).ToArray();
        foreach (var card in directed)
            while (card.CanOffer(weapons) && selection.GetLevel(card) < card.MaximumLevel) await Choose(card);
        Require(weapons.GetWeaponLevel(Weapon("Termo Balístico")) == 8 && directed.All(c => !c.CanOffer(weapons)), "Weapon cap 8.");
        SetDeck(directed.Concat(basic).Concat(new[] { Card("Equipar Aerosol"), Card("Antena de Televisión") }).ToArray());
        Grant();
        Require(selection.IsFallback, "Exhausted/full inventory offered an invalid card.");

        await Fresh(CharacterData.All[0]);
        foreach (var card in basic.Take(3)) await Choose(card);
        SetDeck(new[] { Card("Bolsa del Supermercado") });
        Grant();
        Card("Alfajor Triple").Apply(weapons.GetComponent<PlayerController>(), weapons, health);
        selection.Select(0);
        Require(selection.IsFallback && selection.GetLevel(Card("Bolsa del Supermercado")) == 0 &&
            (int)Field(selection, "pendingSelections") == 1, "Stale fifth passive consumed choice.");
        await Frame();
        selection.Select(0);
        Require(!selection.IsShowing, "Stale choice did not recover.");
        Debug.Log("PASSIVE_SLOTS_PASS candidates=0,1,2,3 duplicates/null filtered caps=4,4,8 individual=True stale=True");
    }

    private static async Task CheckCompatibleBonuses()
    {
        await Fresh(CharacterData.All[0]);
        Require(!Card("Antena de Televisión").CanOffer(weapons), "Antena offered without Petardo.");
        SetDeck(new[] { Card("Antena de Televisión") });
        Grant();
        Require(selection.IsFallback, "Incompatible passive offered.");
        selection.Select(0);
        await Frame();
        await Fresh(CharacterData.All[1]);
        var bag = Card("Bolsa del Supermercado");
        for (int level = 1; level <= 3; level++)
        {
            await Choose(bag);
            Require(Shots(Weapon("Boleadora")).Length == Weapon("Boleadora").ProjectileCount + (level < 3 ? 0 : 1), "Bag threshold " + level);
        }
        Require(!bag.CanOffer(weapons) && selection.SelectedPassives.Count == 1, "Bag max/slots.");
        weapons.EquipWeapon(Weapon("Termo Balístico"));
        Require(Shots(Weapon("Termo Balístico")).Length == Weapon("Termo Balístico").ProjectileCount + 1, "Future Termo lost bag bonus.");
        weapons.EquipWeapon(Weapon("Petardo"));
        Require(Shots(Weapon("Petardo")).Length == 1, "Bag duplicated Petardo.");
        weapons.EquipWeapon(Weapon("Chancleta Teledirigida"));
        Require(Shots(Weapon("Chancleta Teledirigida")).Length == Weapon("Chancleta Teledirigida").ProjectileCount, "Bag affected incompatible weapon.");
        var antenna = Card("Antena de Televisión");
        await Choose(antenna);
        var first = Shots(Weapon("Petardo")).Single();
        float snapshot = (float)Field(first, "blastRadius");
        for (int level = 2; level <= 5; level++) await Choose(antenna);
        Require(Mathf.Approximately((float)Field(first, "blastRadius"), snapshot) &&
            Mathf.Approximately(snapshot, Weapon("Petardo").ExplosionRadius * 1.1f), "Changed active projectile snapshot.");
        var maxShot = Shots(Weapon("Petardo")).Single();
        Require(Mathf.Approximately((float)Field(maxShot, "blastRadius"), Weapon("Petardo").ExplosionRadius * 1.5f) &&
            !antenna.CanOffer(weapons), "Antenna additive levels/cap.");
        weapons.IncreaseWeaponExplosionRadius(Weapon("Petardo"), 1.2f);
        Require(Mathf.Approximately((float)Field(Shots(Weapon("Petardo")).Single(), "blastRadius"),
            Weapon("Petardo").ExplosionRadius * 1.2f * 1.5f), "Directed/passive radius composition.");
        Debug.Log("PASSIVE_BONUS_PASS bag=0,0,1 future=Termo excluded=Petardo,Chancleta antenna=10%-50% snapshot=True");
    }

    private static async Task CheckQueueAndFallback()
    {
        await Fresh(CharacterData.All[0]);
        SetDeck(Array.Empty<UpgradeData>());
        health.ConfigureStartingHealth(25);
        health.TakeDamage(10);
        xp.AddExperience(26);
        Require(selection.IsFallback && (int)Field(selection, "pendingSelections") == 3, "Fallback lost XP burst.");
        await Press(Key.Digit1);
        selection.Select(0);
        Require(health.CurrentHealth == 17 && (int)Field(selection, "pendingSelections") == 2, "Fallback healed twice in one frame.");
        await Frame();
        game.PauseGame();
        selection.Select(0);
        Require(health.CurrentHealth == 17 && Time.timeScale == 0f, "Fallback selected while paused.");
        game.ContinueGame();
        Require(Time.timeScale == 0f && selection.IsFallback, "Pause lost fallback.");
        selection.Select(0);
        await Frame();
        selection.Select(0);
        await Frame();
        Require(health.CurrentHealth == 21 && health.MaximumHealth == 25 && !selection.IsShowing && Time.timeScale == 1f,
            "Fallback queue changed permanent stats or blocked play.");
        health.Heal(100);
        Grant();
        Require(selection.IsFallback && health.IsFull, "Full-health continue unavailable.");
        selection.Select(0);
        await Frame();
        Require(health.CurrentHealth == 25 && !selection.IsShowing, "Continue gave permanent reward.");
        health.ConfigureStartingHealth(5);
        health.TakeDamage(0.5f);
        Require(!health.IsFull && health.CurrentHealth == 5, "Fractional health considered full.");
        Grant();
        selection.Select(0);
        await Frame();
        Require(health.IsFull, "Minimum one healing failed.");
        Grant();
        health.ConfigureStartingHealth(5);
        health.TakeDamage(5);
        selection.Select(0);
        Require(health.IsDead && game.State == GameManager.GameState.Defeat && Time.timeScale == 0f,
            "Choice resurrected dead player.");
        Debug.Log("PASSIVE_FALLBACK_PASS queue=3 heal=floor10%-min1 full=continue fractional=True pause=True death=True");
    }

    private static async Task CheckLayoutsAndBuilds()
    {
        GameUi.CheckTextLayout = CheckTextLayout;
        await Fresh(CharacterData.All[0]);
        await Choose(Card("Yerba Premium"));
        SetDeck(new[] { Card("Bolsa del Supermercado"), Card("Yerba Premium"), Card("Chimichurri Casero") });
        Grant();
        await CaptureSizes("bolsa");
        selection.Select(selection.Options.ToList().IndexOf(Card("Bolsa del Supermercado")));
        await Frame();
        for (int i = 0; i < 2; i++) await Choose(Card("Bolsa del Supermercado"));
        weapons.EquipWeapon(Weapon("Petardo"));
        SetDeck(new[] { Card("Antena de Televisión"), Card("Chimichurri Casero"), Card("Yerba Premium") });
        Grant();
        await CaptureSizes("ofertas");
        int chosen = selection.Options.ToList().IndexOf(Card("Antena de Televisión"));
        selection.Select(chosen);
        await Frame();
        await CaptureSizes("build-termo-bolsa-yerba");
        await Fresh(CharacterData.All[1]);
        weapons.EquipWeapon(Weapon("Petardo"));
        for (int i = 0; i < 5; i++) await Choose(Card("Antena de Televisión"));
        await Choose(Card("Chimichurri Casero"));
        for (int i = 0; i < 3; i++) await Choose(Card("Bolsa del Supermercado"));
        await Choose(Card("Alfajor Triple"));
        await CaptureSizes("build-petardo-antena");
        SetDeck(Array.Empty<UpgradeData>());
        health.ConfigureStartingHealth(25);
        health.TakeDamage(10);
        Grant();
        await CaptureSizes("curar");
        selection.Select(0);
        await Frame();
        health.Heal(100);
        Grant();
        await CaptureSizes("continuar");
        GameUi.CheckTextLayout = null;
    }

    private static async Task Fresh(CharacterData character)
    {
        CharacterData.Select(character);
        SceneManager.LoadScene("Main");
        await Task.Delay(150);
        weapons = Object.FindFirstObjectByType<WeaponController>();
        selection = Object.FindFirstObjectByType<UpgradeSelection>();
        game = Object.FindFirstObjectByType<GameManager>();
        health = weapons.GetComponent<Health>();
        xp = weapons.GetComponent<ExperienceSystem>();
        weapons.enabled = false;
        weapons.GetComponent<PlayerController>().enabled = false;
        Object.FindFirstObjectByType<WaveDirector>().enabled = false;
        Object.FindFirstObjectByType<SpawnManager>().enabled = false;
    }

    private static async Task Choose(UpgradeData card)
    {
        Require(card.CanOffer(weapons), "Cannot choose " + card.name);
        SetDeck(new[] { card });
        Grant();
        Require(selection.Options.Count == 1 && selection.Options[0] == card, "Card not offered " + card.name);
        int before = selection.GetLevel(card);
        selection.Select(0);
        Require(selection.GetLevel(card) == before + 1 && !selection.IsShowing, "Choice not applied " + card.name);
        await Frame();
    }
    private static Projectile[] Shots(WeaponData data)
    {
        foreach (var shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) ObjectPool.Release(shot.gameObject);
        object state = ((IEnumerable)Field(weapons, "equipped")).Cast<object>().Single(s =>
            (WeaponData)s.GetType().GetField("Data").GetValue(s) == data);
        // Fire only needs a position; using the player avoids adding a fake enemy to the live pool.
        typeof(WeaponController).GetMethod("Fire", Flags).Invoke(weapons, new[] { state, health });
        return Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
    }
    private static void Grant() => xp.AddExperience(xp.RequiredExperience - xp.CurrentExperience);
    private static void SetDeck(UpgradeData[] cards) => typeof(UpgradeSelection).GetField("upgrades", Flags).SetValue(selection, cards);
    private static UpgradeData Card(string name) => AssetDatabase.LoadAssetAtPath<UpgradeData>(Root + "Data/" + name + ".asset");
    private static WeaponData Weapon(string name) => AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Data/" + name + ".asset");
    private static IEnumerable<UpgradeData> AllCards() => AssetDatabase.FindAssets("t:UpgradeData", new[] { Root + "Data" })
        .Select(g => AssetDatabase.LoadAssetAtPath<UpgradeData>(AssetDatabase.GUIDToAssetPath(g)));
    private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static async Task Press(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        InputSystem.Update();
        keyboard.MakeCurrent();
        typeof(UpgradeSelection).GetMethod("Update", Flags).Invoke(selection, null);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();
        await Task.CompletedTask;
    }
    private static async Task CaptureSizes(string name)
    {
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1024, 768), new Vector2Int(1920, 1080), new Vector2Int(2560, 1080) })
        {
            await (Task)typeof(UiRedesignCheck).GetMethod("SetSize", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { size.x, size.y });
            string path = Path.Combine(output, name + "-" + size.x + "x" + size.y + ".png");
            ScreenCapture.CaptureScreenshot(path);
            await Until(() => File.Exists(path) && new FileInfo(path).Length > 1000);
        }
    }
    private static void CheckTextLayout(Rect rect, string value, GUIStyle style)
    {
        if (string.IsNullOrEmpty(value) || Event.current.type != EventType.Repaint) return;
        var content = new GUIContent(value);
        Vector2 needed = style.CalcSize(content);
        float height = style.wordWrap ? style.CalcHeight(content, rect.width) : needed.y;
        if (height > rect.height + 2 || !style.wordWrap && needed.x > rect.width + 2)
            layoutIssues.Add($"{Screen.width}x{Screen.height}: '{value}' needs {needed.x:0}x{height:0}, has {rect.width:0}x{rect.height:0}");
    }
    private static async Task Frame() { int frame = Time.frameCount; await Until(() => Time.frameCount > frame); }
    private static async Task Until(Func<bool> condition)
    {
        double deadline = EditorApplication.timeSinceStartup + 30;
        while (!condition())
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Passive check timed out.");
            await Task.Delay(10);
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Exception && trace.Contains("UnityEditor.Search.SearchDatabase")) { searchErrors++; return; }
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
    }
}
