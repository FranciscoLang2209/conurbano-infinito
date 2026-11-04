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

public static class EvolutionCheck
{
    private const string Root = "Assets/_Game/";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly List<GameObject> targets = new();
    private static readonly HashSet<string> layoutIssues = new();
    private static WeaponController weapons;
    private static GameManager game;
    private static UpgradeSelection selection;
    private static string output;
    private static int errors, searchErrors;

    public static async void RunBatch()
    {
        output = Environment.GetEnvironmentVariable("CONURBANO_EVOLUTIONS_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new Exception("CONURBANO_EVOLUTIONS_OUTPUT is required.");
        Directory.CreateDirectory(output);
        Application.logMessageReceived += OnLog;
        try
        {
            var snapshots = AssetDatabase.FindAssets("t:UpgradeData t:WeaponData t:CharacterData t:SynergyData", new[] { Root })
                .Select(AssetDatabase.GUIDToAssetPath).ToDictionary(p => p,
                    p => EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<ScriptableObject>(p)));
            Require(UpgradeData.EvolutionCards.Length == 2 && UpgradeData.EvolutionCards.Select(c => c.StableId).Distinct().Count() == 2,
                "Two unique editable evolution recipes required.");
            Require(UpgradeData.EvolutionCards.All(c => c.Description.Length > 20 && c.RequiredPassive.MaximumLevel == 5), "Imported recipe data.");
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene(Root + "Scenes/Main.unity");
            EditorApplication.EnterPlaymode();
            await Until(() => EditorApplication.isPlaying);
            await Task.Delay(200);
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            await CheckRequirementsAndQueue();
            await CheckBurst();
            await CheckBall();
            foreach (CharacterData character in CharacterData.All)
            {
                await Fresh(character);
                Require(!weapons.HasDefeatedBoss && weapons.GetEvolution(character.StartingWeapon) == null,
                    "Restart retained evolution or boss achievement.");
            }
            foreach (var entry in snapshots)
                Require(EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<ScriptableObject>(entry.Key)) == entry.Value,
                    "Definition mutated: " + entry.Key);
            Require(errors == 0 && layoutIssues.Count == 0, "Console/layout failures: " + errors + "/" + layoutIssues.Count);
            File.WriteAllLines(Path.Combine(output, "text-layout.txt"), layoutIssues);
            GameUi.CheckTextLayout = null;
            EditorApplication.ExitPlaymode();
            await Until(() => !EditorApplication.isPlaying);
            Debug.Log("EVOLUTION_CHECK_PASS recipes=2 orders=4 boss-before-after=True full-slots=True guaranteed-stable-order=True queued-double-input=True burst-pause-death-reset=True return-once=True pool-life=True physics=True layouts=4 immutable=True knownEditorSearchErrors=" + searchErrors);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            File.WriteAllLines(Path.Combine(output, "text-layout.txt"), layoutIssues);
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static async Task CheckRequirementsAndQueue()
    {
        foreach (UpgradeData recipe in UpgradeData.EvolutionCards)
        foreach (bool bossFirst in new[] { false, true })
        {
            await Fresh(CharacterData.All[recipe.Evolution == EvolutionEffect.ThermosBurst ? 0 : 1]);
            if (bossFirst) DefeatBoss();
            if (bossFirst)
            {
                await Raise(recipe.TargetWeapon, 7);
                await MaxPassive(recipe.RequiredPassive);
                Require(!recipe.CanOffer(weapons), "Evolution offered below N8.");
                await Raise(recipe.TargetWeapon, 8);
            }
            else
            {
                await Raise(recipe.TargetWeapon, 8);
                for (int i = 1; i < recipe.RequiredPassive.MaximumLevel; i++) await Choose(recipe.RequiredPassive);
                Require(!recipe.CanOffer(weapons), "Evolution offered below maximum passive.");
                await Choose(recipe.RequiredPassive);
            }
            Require(recipe.CanOffer(weapons) == bossFirst, "Boss achievement/order lost.");
            if (!bossFirst) DefeatBoss();
            Require(recipe.CanOffer(weapons), "All requirements did not unlock recipe.");
            Require(weapons.GetEvolution(recipe.TargetWeapon) == null, "Evolution applied without choice.");
            await ChooseEvolution(recipe);
            Require(!recipe.CanOffer(weapons) && !weapons.Evolve(recipe), "Evolution consumed twice.");
            Require(!Card("Equipar " + recipe.TargetWeapon.name).CanOffer(weapons) &&
                AllCards().Where(c => c.TargetWeapon == recipe.TargetWeapon).All(c => !c.CanOffer(weapons)), "Base/normal cards offered after evolution.");
            Debug.Log("EVOLUTION_REQUIREMENTS_PASS id=" + recipe.StableId + " bossFirst=" + bossFirst);
        }

        foreach (UpgradeData recipe in UpgradeData.EvolutionCards)
        {
            await Fresh(CharacterData.All[recipe.Evolution == EvolutionEffect.ThermosBurst ? 0 : 1]);
            DefeatBoss(); await Raise(recipe.TargetWeapon, 8);
            for (int i = 1; i < recipe.RequiredPassive.MaximumLevel; i++) await Choose(recipe.RequiredPassive);
            Require(!recipe.CanOffer(weapons), "N8 and a defeated boss must still require maximum passive.");
            await Choose(recipe.RequiredPassive);
            Require(recipe.CanOffer(weapons), "Maximum passive failed to complete recipe.");
        }
        Debug.Log("EVOLUTION_PASSIVE_THRESHOLD_PASS recipes=2 boss-already-defeated=True weapon=8 passive4=False passive5=True");

        await Fresh(CharacterData.All[0]);
        UpgradeData thermo = Recipe(EvolutionEffect.ThermosBurst), ball = Recipe(EvolutionEffect.BallReturn);
        await Raise(thermo.TargetWeapon, 8); await Raise(ball.TargetWeapon, 8);
        await MaxPassive(thermo.RequiredPassive); await MaxPassive(ball.RequiredPassive);
        weapons.EquipWeapon(Weapon("Fernets Orbitales")); weapons.EquipWeapon(Weapon("Petardo"));
        await Raise(Weapon("Petardo"), 4);
        Require(weapons.EquippedWeaponCount == 4, "Four weapon fixture.");
        Call(weapons, "EnsureOrbitals", State(Weapon("Fernets Orbitales")));
        var orbitals = ((IEnumerable)State(Weapon("Fernets Orbitales")).GetType().GetField("Orbitals")
            .GetValue(State(Weapon("Fernets Orbitales")))).Cast<Projectile>().ToArray();
        DefeatBoss();
        var order = UpgradeData.EvolutionCards;
        typeof(UpgradeSelection).GetField("upgrades", Flags).SetValue(selection,
            new[] { Card("Chimichurri Casero"), Card("Alfajor Triple"), Card("Antena de Televisión") });
        GainLevel(); GainLevel();
        Require(selection.Options.Count == 3 && selection.Options.Distinct().Count() == 3 &&
            selection.Options[0] == order[0] && !selection.Options.Contains(order[1]), "Stable guaranteed recipe with two distinct normal cards.");
        GameUi.CheckTextLayout = CheckTextLayout;
        await CaptureSizes("evolucion-primera");
        game.PauseGame(); selection.Select(0);
        Require(weapons.GetEvolution(order[0].TargetWeapon) == null, "Selected during pause.");
        game.ContinueGame();
        selection.Select(0); selection.Select(0);
        Require(weapons.GetEvolution(order[0].TargetWeapon) == order[0] && weapons.GetEvolution(order[1].TargetWeapon) == null &&
            selection.IsShowing && selection.Options[0] == order[1] && Time.timeScale == 0f, "Queue or double input consumed two evolutions.");
        await Frame(); await CaptureSizes("evolucion-segunda");
        selection.Select(0);
        Require(!selection.IsShowing && weapons.EquippedWeaponCount == 4 &&
            weapons.GetWeaponLevel(thermo.TargetWeapon) == 8 && weapons.GetWeaponLevel(ball.TargetWeapon) == 8 &&
            orbitals.All(p => p.gameObject.activeSelf) && orbitals.SequenceEqual(((IEnumerable)State(Weapon("Fernets Orbitales"))
                .GetType().GetField("Orbitals").GetValue(State(Weapon("Fernets Orbitales")))).Cast<Projectile>()), "Slots/levels/orbitals changed.");
        Require(weapons.ActiveSynergies.Count == 3, "Evolution lost family synergies.");
        await CaptureSizes("hud-evoluciones");
        GameUi.CheckTextLayout = null;
        Debug.Log("EVOLUTION_QUEUE_PASS full=4 sorted=True guarantee=1 queued=2 simultaneous-input=once orbitals-preserved=True families=3 layouts=4");
    }

    private static async Task CheckBurst()
    {
        await Fresh(CharacterData.All[0]);
        UpgradeData recipe = Recipe(EvolutionEffect.ThermosBurst); WeaponData data = recipe.TargetWeapon;
        await Raise(data, 8); await MaxPassive(recipe.RequiredPassive); DefeatBoss();
        float interval = weapons.FireInterval; int damage = weapons.Damage;
        await ChooseEvolution(recipe);
        Require(weapons.Damage == damage && weapons.FireInterval == interval &&
            weapons.GetWeaponIdentity(data) == recipe.StableId && weapons.GetWeaponDisplayName(data) == recipe.DisplayName, "Stats/identity changed incorrectly.");
        Health target = Target(new Vector2(200, 200));
        int baseCount = data.ProjectileCount + (int)State(data).GetType().GetField("ProjectileCountBonus").GetValue(State(data));
        // Mate's counter is untouched by evolution or by the delayed second pulse.
        for (int attack = 1; attack <= 4; attack++)
        {
            Projectile[] first = Fire(data, target);
            Require(first.Length == baseCount + (attack == 4 ? 1 : 0), "Mate counted a pulse as another attack.");
            float firstDamage = (float)Field(first[0], "damage");
            float firedAt = Time.time;
            if (attack == 1)
            {
                game.PauseGame(); await Task.Delay(200);
                Require(Mathf.Approximately(Time.time, firedAt) && (int)State(data).GetType().GetField("PulseCount").GetValue(State(data)) > 0 &&
                    Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length == first.Length, "Pause advanced second pulse.");
                game.ContinueGame();
            }
            if (attack == 2)
            {
                // The delayed pulse holds direction/stats, never a callback to this target or a pooled shot.
                target.TakeDamage(2000); Object.DestroyImmediate(target.gameObject);
                weapons.IncreaseDamage(2);
            }
            if (attack == 3)
            {
                GainLevel(); await Task.Delay(180);
                Require(selection.IsShowing && Time.timeScale == 0f &&
                    (int)State(data).GetType().GetField("PulseCount").GetValue(State(data)) > 0,
                    "Level-up advanced pending pulse.");
                selection.Select(0);
            }
            await Until(() => (int)State(data).GetType().GetField("PulseCount").GetValue(State(data)) == 0);
            Projectile[] second = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Except(first).ToArray();
            Require(second.Length == baseCount && second.All(p => Mathf.Approximately((float)Field(p, "damage"), firstDamage * 0.5f)),
                "Second pulse count/damage/snapshot.");
            Require(Time.time - firedAt >= 0.12f && Time.time - firedAt < 0.3f, "Pulse delay differs from active-time schedule.");
            ReleaseShots();
            if (attack == 2) target = Target(new Vector2(200, 200));
        }
        var countField = State(data).GetType().GetField("ProjectileCountBonus");
        int previousCountBonus = (int)countField.GetValue(State(data));
        weapons.IncreaseWeaponProjectileCount(data, 100);
        Require(Fire(data, target).Length == 12, "Burst first pulse bypassed global attack budget.");
        await Task.Delay(180);
        Require(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length == 12 &&
            (int)State(data).GetType().GetField("PulseCount").GetValue(State(data)) == 0,
            "Two pulses exceeded twelve projectiles per complete attack.");
        ReleaseShots(); countField.SetValue(State(data), previousCountBonus);
        Fire(data, target); weapons.GetComponent<Health>().TakeDamage(2000);
        Require((int)State(data).GetType().GetField("PulseCount").GetValue(State(data)) == 0 && !weapons.enabled,
            "Death did not cancel pending pulse.");
        await Task.Delay(180);
        Require(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length == baseCount, "Pulse fired after death.");
        await Fresh(CharacterData.All[0]);
        Require(weapons.GetEvolution(data) == null && (int)State(data).GetType().GetField("PulseCount").GetValue(State(data)) == 0,
            "Restart retained delayed pulse.");
        await Raise(data, 8); await MaxPassive(recipe.RequiredPassive); DefeatBoss(); await ChooseEvolution(recipe);
        target = Target(new Vector2(200, 200)); Fire(data, target);
        Require((int)State(data).GetType().GetField("PulseCount").GetValue(State(data)) > 0, "Live pending pulse fixture.");
        WeaponController previous = weapons;
        game.RestartGame(); await Task.Delay(150);
        weapons = Object.FindFirstObjectByType<WeaponController>(); game = Object.FindFirstObjectByType<GameManager>();
        selection = Object.FindFirstObjectByType<UpgradeSelection>(); StopAutomaticAttacks();
        Require(previous == null && weapons.GetEvolution(data) == null && !weapons.HasDefeatedBoss &&
            Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length == 0, "Live restart retained a pulse/shot/evolution.");
        Debug.Log("EVOLUTION_BURST_PASS character=Pibe attacks=4 delay=0.12 second-damage=0.5 mate-first-only=True pause=True dead-target=True snapshot=True death-cancel=True restart=True");
    }

    private static async Task CheckBall()
    {
        await Fresh(CharacterData.All[1]);
        UpgradeData recipe = Recipe(EvolutionEffect.BallReturn); WeaponData data = recipe.TargetWeapon;
        await Raise(data, 8); await MaxPassive(recipe.RequiredPassive); DefeatBoss(); await ChooseEvolution(recipe);
        weapons.EquipWeapon(Weapon("Petardo")); await Raise(Weapon("Petardo"), 4);
        Health first = Target(new Vector2(201, 200)), second = Target(new Vector2(202, 200));
        Physics2D.SyncTransforms(); Projectile shot = Fire(data, first)[0];
        int directedBounces = (int)State(data).GetType().GetField("RicochetBonus").GetValue(State(data));
        Require((int)Field(shot, "remainingHits") == data.Pierce + directedBounces + 1 + 2 + 1, "Two evolution bounces plus Piba affinity.");
        Require(Mathf.Approximately((float)Field(shot, "speed"), data.ProjectileSpeed * 1.2f), "Evolved family lost Potrero.");
        Contact(shot, first); Contact(shot, first);
        Require(first.CurrentHealth == 1000 - Mathf.RoundToInt((float)Field(shot, "damage")), "Immediate duplicate/return accepted.");
        Contact(shot, second);
        Require(shot.gameObject.activeSelf && (bool)Field(shot, "aimingAtFirst"), "Return not scheduled after another target.");
        Contact(shot, first);
        Require(!shot.gameObject.activeSelf && first.CurrentHealth < second.CurrentHealth && !weapons.FestejoReady, "Return loop or duplicate charge.");
        shot = Fire(data, first)[0]; Contact(shot, first); Contact(shot, second);
        Require(weapons.FestejoReady, "Evolved Pelota/return not counted by Festejo.");
        ReleaseShots();
        // A dead first target and the same Health reused for a new life must never be returned to.
        shot = Fire(data, first)[0]; Contact(shot, first); first.TakeDamage(2000); Contact(shot, second);
        Require(!shot.gameObject.activeSelf, "Returned to a dead first target.");
        first.ResetForReuse();
        shot = Fire(data, first)[0]; Contact(shot, first);
        uint life = first.LifeId; first.gameObject.SetActive(false); first.ResetForReuse(); first.gameObject.SetActive(true);
        Contact(shot, second);
        Require(first.LifeId != life && !shot.gameObject.activeSelf && first.CurrentHealth == 1000, "Returned to recycled first-target life.");
        ReleaseShots(); ClearTargets();
        // Real physics chain A -> B -> A, then no live unvisited target remains: ends without a cycle.
        first = Target((Vector2)weapons.transform.position + new Vector2(2, 0));
        second = Target((Vector2)weapons.transform.position + new Vector2(3.5f, 1.5f));
        first.GetComponent<CircleCollider2D>().radius = second.GetComponent<CircleCollider2D>().radius = 0.3f;
        Physics2D.SyncTransforms(); shot = Fire(data, first)[0];
        await Until(() => second.CurrentHealth < 1000);
        game.PauseGame(); Vector3 frozen = shot.transform.position; await Task.Delay(160);
        Require(frozen == shot.transform.position, "Evolved return moved during pause."); game.ContinueGame();
        await Until(() => !shot.gameObject.activeSelf);
        Require(first.CurrentHealth < second.CurrentHealth && second.CurrentHealth < 1000, "Physical return failed.");
        ReleaseShots();
        var pool = Object.FindFirstObjectByType<ObjectPool>(); var prefab = (Projectile)Field(weapons, "projectilePrefab");
        Projectile reused = pool.Spawn(prefab.gameObject, weapons.transform.position, Quaternion.identity).GetComponent<Projectile>();
        Require(reused == shot && Field(reused, "firstRicochetTarget") == null && !(bool)Field(reused, "allowRicochetReturn") &&
            !(bool)Field(reused, "aimingAtFirst") && Field(reused, "sourceController") == null, "Pooled projectile retained return/source.");
        reused.SetAppearance(data.ProjectileSprite, data.ProjectileColor, data.ProjectileScale, data.Pattern);
        reused.Initialize(Vector2.right, 1, 8, 10, 5); Contact(reused, first); Contact(reused, second);
        Require(!reused.gameObject.activeSelf, "Anonymous reused projectile retained evolution.");
        Debug.Log("EVOLUTION_BALL_PASS character=Piba extra-bounces=2 affinity=1 potrero=True return-once=True dead-first=True recycled-life=True festejo=True real-physics=A-B-A pause=True pool-reset=True");
    }

    private static void DefeatBoss()
    {
        WaveDirector director = Object.FindFirstObjectByType<WaveDirector>();
        SpawnManager spawner = Object.FindFirstObjectByType<SpawnManager>();
        spawner.enabled = director.enabled = true; director.Tick(180f);
        Require(director.ActiveBoss != null && !weapons.HasDefeatedBoss, "Boss fixture/state.");
        director.ActiveBoss.TakeDamage(10000);
        Require(weapons.HasDefeatedBoss, "Real boss death event not integrated.");
        director.enabled = spawner.enabled = false;
    }
    private static UpgradeData Recipe(EvolutionEffect effect) => UpgradeData.EvolutionCards.Single(c => c.Evolution == effect);
    private static UpgradeData[] AllCards() => AssetDatabase.FindAssets("t:UpgradeData", new[] { Root + "Data" })
        .Select(g => AssetDatabase.LoadAssetAtPath<UpgradeData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
    private static async Task Raise(WeaponData data, int level)
    {
        if (!weapons.IsEquipped(data)) await Choose(Card("Equipar " + data.name));
        while (weapons.GetWeaponLevel(data) < level)
        {
            UpgradeData card = AllCards().FirstOrDefault(c => c.TargetWeapon == data && c.Effect >= UpgradeEffect.WeaponDamage && c.Effect <= UpgradeEffect.WeaponRicochets &&
                c.CanOffer(weapons) && selection.GetLevel(c) < c.MaximumLevel);
            Require(card != null, "No compatible rank to reach N8: " + data.name); await Choose(card);
        }
    }
    private static async Task MaxPassive(UpgradeData data)
    {
        while (weapons.GetPassiveLevel(data) < data.MaximumLevel) await Choose(data);
    }
    private static async Task Choose(UpgradeData data)
    {
        typeof(UpgradeSelection).GetField("upgrades", Flags).SetValue(selection, new[] { data }); GainLevel();
        Require(selection.Options.Contains(data), "Card unavailable: " + data.name);
        selection.Select(selection.Options.ToList().IndexOf(data)); StopAutomaticAttacks(); await Frame();
    }
    private static async Task ChooseEvolution(UpgradeData data)
    {
        typeof(UpgradeSelection).GetField("upgrades", Flags).SetValue(selection, Array.Empty<UpgradeData>()); GainLevel();
        Require(selection.Options.Count == 1 && selection.Options[0] == data, "Evolution not guaranteed.");
        var keyboard = InputSystem.AddDevice<Keyboard>();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit1));
        InputSystem.Update(); keyboard.MakeCurrent();
        Call(selection, "Update"); selection.Select(0);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
        InputSystem.RemoveDevice(keyboard);
        StopAutomaticAttacks(); await Frame();
        Require(weapons.GetEvolution(data.TargetWeapon) == data && weapons.GetWeaponLevel(data.TargetWeapon) == 8, "Evolution did not replace slot identity.");
    }
    private static void GainLevel()
    {
        var xp = weapons.GetComponent<ExperienceSystem>(); xp.AddExperience(xp.RequiredExperience - xp.CurrentExperience);
    }
    private static async Task Frame() { int frame = Time.frameCount; await Until(() => Time.frameCount > frame); }

    private static async Task Fresh(CharacterData character)
    {
        ClearTargets(); CharacterData.Select(character); SceneManager.LoadScene("Main");
        await Task.Delay(150);
        weapons = Object.FindFirstObjectByType<WeaponController>();
        game = Object.FindFirstObjectByType<GameManager>(); selection = Object.FindFirstObjectByType<UpgradeSelection>();
        weapons.GetComponent<PlayerController>().enabled = false;
        Object.FindFirstObjectByType<WaveDirector>().enabled = false;
        Object.FindFirstObjectByType<SpawnManager>().enabled = false;
        StopAutomaticAttacks();
        Require(weapons.ActiveSynergies.Count == 0 && !weapons.FestejoReady, "Fresh build has old procs.");
    }

    private static void StopAutomaticAttacks()
    {
        foreach (object item in (IEnumerable)Field(weapons, "equipped"))
            item.GetType().GetField("NextAttackTime").SetValue(item, float.PositiveInfinity);
    }
    private static T[] Definitions<T>() where T : ScriptableObject => AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { Root })
        .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
    private static WeaponData Weapon(string name) => AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Data/" + name + ".asset");
    private static UpgradeData Card(string name) => AssetDatabase.LoadAssetAtPath<UpgradeData>(Root + "Data/" + name + ".asset");
    private static object State(WeaponData data) => ((IEnumerable)Field(weapons, "equipped")).Cast<object>()
        .Single(s => (WeaponData)s.GetType().GetField("Data").GetValue(s) == data);
    private static Projectile[] Fire(WeaponData data, Health target)
    {
        var before = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
        Call(weapons, "Fire", State(data), target);
        return Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Except(before).ToArray();
    }
    private static Health Target(Vector2 position)
    {
        var obj = new GameObject("Evolution check target"); targets.Add(obj);
        obj.layer = LayerMask.NameToLayer("Enemy"); obj.transform.position = position;
        obj.AddComponent<CircleCollider2D>().radius = 0.05f;
        var health = obj.AddComponent<Health>(); health.ConfigureStartingHealth(1000); return health;
    }
    private static void Contact(Projectile shot, Health target)
    {
        shot.GetComponent<Rigidbody2D>().position = target.transform.position;
        Physics2D.SyncTransforms(); Call(shot, "OnTriggerEnter2D", target.GetComponent<Collider2D>());
    }
    private static void ReleaseShots()
    {
        foreach (Projectile shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) ObjectPool.Release(shot.gameObject);
    }
    private static void ClearTargets() { foreach (GameObject obj in targets) if (obj != null) Object.DestroyImmediate(obj); targets.Clear(); }
    private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static void Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task Until(Func<bool> condition)
    {
        double deadline = EditorApplication.timeSinceStartup + 30;
        while (!condition())
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Evolution check timed out.");
            await Task.Delay(10);
        }
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
