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
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class SynergyCheck
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
        output = Environment.GetEnvironmentVariable("CONURBANO_SYNERGIES_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new Exception("CONURBANO_SYNERGIES_OUTPUT is required.");
        Directory.CreateDirectory(output);
        Application.logMessageReceived += OnLog;
        try
        {
            var snapshots = AssetDatabase.FindAssets("t:SynergyData t:WeaponData t:UpgradeData t:CharacterData", new[] { Root })
                .Select(AssetDatabase.GUIDToAssetPath).ToDictionary(p => p,
                    p => EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<ScriptableObject>(p)));
            Require(SynergyData.All.Select(s => s.StableId).Distinct().Count() == 3, "Three stable synergy IDs.");
            foreach (SynergyData data in SynergyData.All)
            {
                Require(Definitions<WeaponData>().Any(w => w.StableId == data.WeaponId), "Unknown weapon ID.");
                Require(data.Effect == SynergyEffect.FestejoDelBarrio ?
                    Definitions<WeaponData>().Any(w => w.StableId == data.SecondWeaponId) :
                    Definitions<UpgradeData>().Any(p => p.StableId == data.PassiveId), "Unknown requirement ID.");
            }
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene(Root + "Scenes/Main.unity");
            EditorApplication.EnterPlaymode();
            await Until(() => EditorApplication.isPlaying);
            await Task.Delay(200);
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            await CheckAffinities();
            await CheckOrders();
            await CheckCombat();
            await CheckPhysicsAndUi();
            foreach (CharacterData character in CharacterData.All)
            {
                await Fresh(character);
                Require(weapons.ActiveSynergies.Count == 0 && !weapons.FestejoReady, "Restart retained synergy state.");
            }
            foreach (var entry in snapshots)
                Require(EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<ScriptableObject>(entry.Key)) == entry.Value,
                    "Definition mutated: " + entry.Key);
            Require(errors == 0 && layoutIssues.Count == 0, "Console/layout failures: " + errors + "/" + layoutIssues.Count);
            File.WriteAllLines(Path.Combine(output, "text-layout.txt"), layoutIssues);
            GameUi.CheckTextLayout = null;
            EditorApplication.ExitPlaymode();
            await Until(() => !EditorApplication.isPlaying);
            Debug.Log("SYNERGY_CHECK_PASS affinities=2 orders=6 thresholds=True activation-once=True attacks-not-projectiles=True direct-hits=True dead-duplicate-secondary=False charge-cap=1 whole-attack=True snapshot=True pool=True physics=True pause=True reset=2 layouts=4 immutable=True knownEditorSearchErrors=" + searchErrors);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            File.WriteAllLines(Path.Combine(output, "text-layout.txt"), layoutIssues);
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static async Task CheckAffinities()
    {
        var all = Definitions<WeaponData>();
        Require(CharacterData.All[0].DirectProjectileDamageBonus == 0.1f && CharacterData.All[0].RicochetBonus == 0 &&
            CharacterData.All[1].DirectProjectileDamageBonus == 0f && CharacterData.All[1].RicochetBonus == 1,
            "Expected character affinities: Pibe +10%, Piba +1 Ricochet.");
        Require(all.Count(w => w.DirectProjectile) == 2 && Weapon("Termo Balístico").DirectProjectile &&
            Weapon("Boleadora").DirectProjectile, "Direct-projectile labels must be explicit on only Termo/Boleadora.");
        foreach (CharacterData character in CharacterData.All)
        {
            await Fresh(character);
            Require(weapons.GetComponent<Health>().MaximumHealth == character.StartingHealth &&
                Mathf.Approximately(weapons.GetComponent<PlayerController>().MovementSpeed, character.MovementSpeed), "Character base stats changed.");
            foreach (WeaponData data in all)
            {
                if (!weapons.IsEquipped(data) && weapons.EquippedWeaponCount == 4) await Fresh(character);
                if (!weapons.IsEquipped(data)) weapons.EquipWeapon(data);
                StopAutomaticAttacks();
                Health target = Target(new Vector2(200, 200));
                Projectile shot;
                if (data.Pattern == WeaponPattern.Orbiting)
                {
                    Call(weapons, "EnsureOrbitals", State(data));
                    shot = ((IEnumerable)State(data).GetType().GetField("Orbitals").GetValue(State(data))).Cast<Projectile>().First();
                }
                else shot = Fire(data, target)[0];
                float expectedDamage = data.Damage * (1f + (data.DirectProjectile ? character.DirectProjectileDamageBonus : 0f));
                Require(Mathf.Approximately((float)Field(shot, "damage"), expectedDamage), "Affinity damage on " + data.name);
                if (data.Pattern != WeaponPattern.Returning && data.Pattern != WeaponPattern.Orbiting && data.Pattern != WeaponPattern.Explosive)
                    Require((int)Field(shot, "remainingHits") == data.Pierce + 1 +
                        (data.Pattern == WeaponPattern.Ricochet ? character.RicochetBonus : 0), "Affinity changed pierce outside Ricochet.");
                ReleaseShots();
                ClearTargets();
            }
        }
        Debug.Log("SYNERGY_AFFINITIES_PASS all-seven-weapons both-characters base-health-speed-first-offer-preserved=True");
    }

    private static async Task CheckOrders()
    {
        foreach (SynergyEffect effect in Enum.GetValues(typeof(SynergyEffect)))
        foreach (bool reverse in new[] { false, true })
        {
            await Fresh(CharacterData.All[reverse ? 1 : 0]);
            SynergyData data = SynergyData.All.Single(s => s.Effect == effect);
            WeaponData primary = Definitions<WeaponData>().Single(w => w.StableId == data.WeaponId);
            int notices = 0;
            weapons.SynergyActivated += activated => { if (activated == data) notices++; };
            if (effect == SynergyEffect.FestejoDelBarrio)
            {
                WeaponData second = Definitions<WeaponData>().Single(w => w.StableId == data.SecondWeaponId);
                await Raise(reverse ? second : primary, 4);
                Require(!weapons.ActiveSynergies.Contains(data), "Activated with one weapon.");
                await Raise(reverse ? primary : second, 3);
                Require(!weapons.ActiveSynergies.Contains(data), "Activated below N4.");
                await Raise(reverse ? primary : second, 4);
            }
            else
            {
                UpgradeData passive = Definitions<UpgradeData>().Single(p => p.StableId == data.PassiveId);
                if (reverse) for (int i = 0; i < 3; i++) await Choose(passive);
                await Raise(primary, reverse ? 3 : 4);
                Require(!weapons.ActiveSynergies.Contains(data), "Activated below both requirements.");
                if (reverse) await Raise(primary, 4);
                else
                {
                    await Choose(passive); await Choose(passive);
                    Require(!weapons.ActiveSynergies.Contains(data), "Activated below passive N3.");
                    await Choose(passive);
                }
                await Choose(passive);
            }
            Require(weapons.ActiveSynergies.Contains(data) && notices == 1, "Activation/order/idempotence.");
            await Choose(Card(primary == Weapon("Pelota de Potrero") ? "Pelota - Cadencia" : primary.name + " - Cadencia"));
            Require(notices == 1 && weapons.ActiveSynergies.Count(s => s == data) == 1, "Repeated activation.");
            Debug.Log("SYNERGY_ORDER_PASS effect=" + effect + " reverse=" + reverse);
        }
    }

    private static async Task CheckCombat()
    {
        await Fresh(CharacterData.All[0]);
        await BuildAll();
        WeaponData termo = Weapon("Termo Balístico"), ball = Weapon("Pelota de Potrero"), petardo = Weapon("Petardo");
        await Choose(Card("Antena de Televisión")); await Choose(Card("Antena de Televisión"));
        weapons.IncreaseWeaponExplosionRadius(petardo, 1.1f);
        Health target = Target(new Vector2(200, 200));
        weapons.IncreaseWeaponProjectileCount(termo, 2);
        for (int attack = 1; attack <= 8; attack++)
        {
            Require(Fire(termo, target).Length == termo.ProjectileCount + 2 + (attack % 4 == 0 ? 1 : 0), "Mate counted projectiles, not attacks.");
            ReleaseShots();
        }
        weapons.IncreaseWeaponProjectileCount(termo, 100);
        for (int attack = 0; attack < 4; attack++) { Require(Fire(termo, target).Length == 12, "Mate bypassed cap."); ReleaseShots(); }
        Projectile speedShot = Fire(ball, target)[0];
        Require(Mathf.Approximately((float)Field(speedShot, "speed"), ball.ProjectileSpeed * 1.2f), "Potrero multiplier.");
        weapons.IncreaseWeaponProjectileSpeed(ball, 1.1f);
        Require(Mathf.Approximately((float)Field(speedShot, "speed"), ball.ProjectileSpeed * 1.2f), "Active shot speed changed.");
        ReleaseShots();
        ClearTargets();
        // Two attacks, including ricochets, reach five valid direct hits.
        var crowd = Enumerable.Range(0, 8).Select(i => Target(new Vector2(201 + i, 200))).ToArray();
        Physics2D.SyncTransforms();
        Projectile shot = Fire(ball, crowd[0])[0];
        for (int i = 0; i < 3; i++) Contact(shot, crowd[i]);
        Require(!weapons.FestejoReady, "Charged before five hits.");
        shot = Fire(ball, crowd[0])[0];
        Contact(shot, crowd[0]);
        Contact(shot, crowd[0]);
        Health dead = Target(new Vector2(200, 202)); dead.TakeDamage(1000);
        Contact(shot, dead);
        Health friendly = Target(new Vector2(200, 203)); friendly.gameObject.layer = LayerMask.NameToLayer("Player");
        Contact(shot, friendly);
        Require(!weapons.FestejoReady, "Dead/duplicate/friendly counted.");
        game.PauseGame();
        Vector3 position = shot.transform.position;
        await Task.Delay(100);
        Require(!weapons.FestejoReady && position == shot.transform.position, "Pause changed proc or projectile.");
        game.ContinueGame();
        Contact(shot, crowd[1]);
        Require(weapons.FestejoReady, "Fifth direct hit did not charge.");
        Contact(shot, crowd[2]);
        ReleaseShots();
        // Hits while ready are discarded; Petardo snapshots one charge for every projectile.
        weapons.IncreaseWeaponProjectileCount(petardo, 2);
        float radius = petardo.ExplosionRadius * 1.1f * 1.2f;
        Projectile[] blasts = Fire(petardo, crowd[0]);
        Require(blasts.Length == 3 && !weapons.FestejoReady &&
            blasts.All(p => Mathf.Approximately((float)Field(p, "blastRadius"), radius * 1.25f)), "Whole attack charge consumption.");
        foreach (Projectile blast in blasts) { Contact(blast, crowd[0]); Contact(blast, crowd[0]); }
        Require(!weapons.FestejoReady, "Explosion generated a recursive charge.");
        ReleaseShots();
        Require(Fire(petardo, crowd[0]).All(p => Mathf.Approximately((float)Field(p, "blastRadius"), radius)), "Charge applied twice.");
        ReleaseShots();
        shot = Fire(ball, crowd[0])[0];
        for (int i = 0; i < 3; i++) Contact(shot, crowd[i]);
        Require(!weapons.FestejoReady, "Stored hits while charge was already ready.");
        ReleaseShots();
        // Reuse that very instance without a source: no stale callback survives the pool.
        var pool = Object.FindFirstObjectByType<ObjectPool>();
        var prefab = (Projectile)Field(weapons, "projectilePrefab");
        Projectile reused = pool.Spawn(prefab.gameObject, weapons.transform.position, Quaternion.identity).GetComponent<Projectile>();
        Require(reused == shot && Field(reused, "sourceController") == null && Field(reused, "sourceWeapon") == null, "Pool retained source.");
        reused.SetAppearance(ball.ProjectileSprite, Color.white, ball.ProjectileScale, ball.Pattern);
        reused.Initialize(Vector2.right, 1f, 8f, 10, 20f);
        for (int i = 0; i < 5; i++) Contact(reused, crowd[i]);
        Require(!weapons.FestejoReady, "Pooled anonymous projectile counted for old owner.");
        ReleaseShots();
        // A new sourced projectile completes the two remaining hits, then death blocks further procs.
        shot = Fire(ball, crowd[0])[0]; Contact(shot, crowd[0]);
        Require(!weapons.FestejoReady, "Charge kept a hit banked while ready.");
        crowd[1].ConfigureStartingHealth(1);
        Contact(shot, crowd[1]);
        Require(weapons.FestejoReady, "Valid hits lost after pool reuse.");
        Require(crowd[1].IsDead, "Lethal direct hit was not counted.");
        ReleaseShots(); Fire(petardo, crowd[0]); ReleaseShots();
        weapons.GetComponent<Health>().TakeDamage(1000);
        shot = Fire(ball, crowd[0])[0];
        for (int i = 0; i < 3; i++) Contact(shot, crowd[i]);
        ReleaseShots();
        shot = Fire(ball, crowd[0])[0]; Contact(shot, crowd[0]); Contact(shot, crowd[1]);
        Require(!weapons.FestejoReady, "Dead owner generated a charge.");
        ReleaseShots(); ClearTargets();
        Debug.Log("SYNERGY_COMBAT_PASS mate=8-attacks potrero=1.2 snapshot=True festejo=5-valid-hits capacity=1 secondary=False pool=True death=True");
    }

    private static async Task CheckPhysicsAndUi()
    {
        await Fresh(CharacterData.All[1]);
        await BuildAll();
        var crowd = new[] { new Vector2(2, 0), new Vector2(3, 1.5f), new Vector2(4.5f, 1.5f), new Vector2(6, 1.5f), new Vector2(7, 3) }
            .Select(p => Target((Vector2)weapons.transform.position + p)).ToArray();
        foreach (Health target in crowd) target.GetComponent<CircleCollider2D>().radius = 0.3f;
        Physics2D.SyncTransforms();
        Projectile shot = Fire(Weapon("Pelota de Potrero"), crowd[0])[0];
        await Until(() => !shot.gameObject.activeSelf);
        Require(crowd.Take(4).All(t => t.CurrentHealth < 1000) && crowd[4].CurrentHealth == 1000 && !weapons.FestejoReady,
            "La Piba must hit four enemies with real Ricochet physics.");
        shot = Fire(Weapon("Pelota de Potrero"), crowd[0])[0];
        await Until(() => weapons.FestejoReady);
        game.PauseGame();
        Vector3 frozen = shot.transform.position;
        await Task.Delay(150);
        Require(frozen == shot.transform.position && weapons.FestejoReady, "Pause failed with ready charge.");
        GameUi.CheckTextLayout = CheckTextLayout;
        await CaptureSizes("pausa-sinergias");
        game.ContinueGame();
        ReleaseShots(); ClearTargets();
        await CaptureSizes("festejo-listo");
        await Fresh(CharacterData.All[0]);
        game.PauseGame();
        await CaptureSizes("pausa-requisitos");
        game.ContinueGame();
        // A real choice schedules the activation notice until play resumes.
        await Raise(Weapon("Termo Balístico"), 4);
        await Choose(Card("Yerba Premium")); await Choose(Card("Yerba Premium")); await Choose(Card("Yerba Premium"));
        await Until(() => ((string)Field(Object.FindFirstObjectByType<GameHud>(), "feedbackMessage"))?.Contains("Mate de Guardia") == true);
        await CaptureSizes("aviso-mate");
        GameUi.CheckTextLayout = null;
        Debug.Log("SYNERGY_PHYSICS_UI_PASS real-collisions=4 first-fifth-charge=True paused=True active-pending-ready-notice layouts=4");
    }

    private static async Task BuildAll()
    {
        await Raise(Weapon("Termo Balístico"), 4);
        await Raise(Weapon("Pelota de Potrero"), 4);
        await Raise(Weapon("Petardo"), 4);
        for (int i = 0; i < 3; i++) { await Choose(Card("Yerba Premium")); await Choose(Card("Zapatillas Flecha")); }
        Require(weapons.ActiveSynergies.Count == 3, "Build cannot activate all three recipes.");
    }

    private static async Task Raise(WeaponData data, int level)
    {
        if (!weapons.IsEquipped(data)) await Choose(Card("Equipar " + data.name));
        var deck = Definitions<UpgradeData>().Where(c => c.TargetWeapon == data && c.Effect == UpgradeEffect.WeaponDamage).ToArray();
        while (weapons.GetWeaponLevel(data) < level) await Choose(deck.Single());
    }

    private static async Task Choose(UpgradeData data)
    {
        typeof(UpgradeSelection).GetField("upgrades", Flags).SetValue(selection, new[] { data });
        var xp = weapons.GetComponent<ExperienceSystem>();
        xp.AddExperience(xp.RequiredExperience - xp.CurrentExperience);
        Require(selection.Options.Count == 1 && selection.Options[0] == data, "Choice unavailable: " + data.name);
        selection.Select(0);
        StopAutomaticAttacks();
        int frame = Time.frameCount; await Until(() => Time.frameCount > frame);
    }

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
        var obj = new GameObject("Synergy check target"); targets.Add(obj);
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
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Synergy check timed out.");
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
