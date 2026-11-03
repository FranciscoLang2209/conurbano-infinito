using System;
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

public static class PitchCheck
{
    private const string Root = "Assets/_Game/";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly string[] Names = { "Kiosco 24hs", "Parrilla del Barrio", "Canchita del Club" };
    private static int errors;
    private static int editorSearchErrors;
    private static string output;

    public static async void RunBatch()
    {
        output = Environment.GetEnvironmentVariable("CONURBANO_PITCH_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new Exception("CONURBANO_PITCH_OUTPUT is required.");
        Directory.CreateDirectory(output);
        Application.logMessageReceived += OnLog;
        try
        {
            EditorSceneManager.OpenScene(Root + "Scenes/Main.unity");
            CheckPlacementAndAssets();
            PoiIndicatorsCheck.Run();
            QuilomboSliceCheck.Run();
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.EnterPlaymode();
            await Until(() => EditorApplication.isPlaying);
            await Task.Delay(250);
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            CheckHealing();
            await SetSize(1280, 720);
            foreach (CharacterData character in CharacterData.All) await CheckPhysicalVisit(character);
            await CheckFullHealthAndDeath();
            await CheckVisitOrders();
            await CheckIndicators();
            Require(errors == 0, "Gameplay Console errors: " + errors);
            EditorApplication.ExitPlaymode();
            await Until(() => !EditorApplication.isPlaying);
            Debug.Log("PITCH_CHECK_PASS characters=2 real-hold=6s exit-reset=True pause=True healing=capped,no-revive,no-invulnerability-reset one-use=True routes=6 quilombo=35 bonus=0.35 restart=True placement-seeds=100 indicators=1536-layouts,4-resolutions knownEditorSearchErrors=" + editorSearchErrors);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static void CheckPlacementAndAssets()
    {
        RiskZone[] zones = Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None);
        var placement = Object.FindFirstObjectByType<QuilomboManager>();
        Require(zones.Length == 3 && Names.All(n => zones.Any(z => z.name == n)), "Three POIs missing.");
        var settings = new SerializedObject(placement);
        var elements = settings.FindProperty("elements");
        Require(elements.arraySize == 3 && Enumerable.Range(0, 3).All(i =>
            zones.Contains(elements.GetArrayElementAtIndex(i).objectReferenceValue as RiskZone)), "Placement registry.");
        float playerDistance = settings.FindProperty("minimumPlayerDistance").floatValue;
        float zoneDistance = settings.FindProperty("minimumElementDistance").floatValue;
        var player = Object.FindFirstObjectByType<PlayerController>();
        for (int seed = 0; seed < 100; seed++)
        {
            UnityEngine.Random.InitState(seed);
            Invoke(placement, "Start");
            foreach (RiskZone zone in zones)
            {
                Require(zone.gameObject.activeSelf && Vector2.Distance(zone.transform.position, player.transform.position) >= playerDistance,
                    "POI too close to player or disabled.");
                foreach (RiskZone other in zones)
                    if (zone != other) Require(Vector2.Distance(zone.transform.position, other.transform.position) >= zoneDistance,
                        "POIs too close.");
                var points = settings.FindProperty("spawnPoints");
                Require(Enumerable.Range(0, points.arraySize).Any(i =>
                    Vector2.Distance(zone.transform.position, ((Transform)points.GetArrayElementAtIndex(i).objectReferenceValue).position) < 0.01f),
                    "POI is not on a spawn point.");
            }
        }
        RiskZone pitch = zones.Single(z => z.name == Names[2]);
        RiskZoneData data = Data(pitch);
        Require(data.RequiredDuration == 6 && data.QuilomboReward == 10 && data.HealingAmount == 2 &&
            data.RewardDuration == 0 && data.HasReward, "Pitch reward configuration.");
        var collider = pitch.GetComponent<CircleCollider2D>();
        Sprite sprite = pitch.GetComponent<SpriteRenderer>().sprite;
        Require(collider.isTrigger && Mathf.Approximately(collider.radius, 1.1f) &&
            sprite != null && sprite.rect.size == Vector2.one * 192 && sprite.pixelsPerUnit == 48 &&
            sprite.pivot == Vector2.one * 96, "Pitch shape/import.");
        var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
        Require(importer.filterMode == FilterMode.Point && !importer.mipmapEnabled &&
            importer.textureCompression == TextureImporterCompression.Uncompressed, "Pitch texture settings.");
        Require(new SerializedObject(pitch).FindProperty("quilombo").objectReferenceValue == Object.FindFirstObjectByType<QuilomboSystem>() &&
            new SerializedObject(pitch).FindProperty("gameCamera").objectReferenceValue == Camera.main, "Pitch wiring.");
        EditorSceneManager.OpenScene(Root + "Scenes/Main.unity");
        Debug.Log("PITCH_ASSETS_PASS POIs=3 placement-seeds=100 import,shape,wiring=True");
    }

    private static void CheckHealing()
    {
        var target = new GameObject("Healing check");
        try
        {
            var health = target.AddComponent<Health>();
            health.TakeDamage(3);
            health.Heal(1);
            Require(health.CurrentHealth == 3 && health.MaximumHealth == 5, "Basic healing.");
            health.Heal(0);
            health.Heal(-1);
            Require(health.CurrentHealth == 3, "Invalid healing changed health.");
            health.Heal(int.MaxValue);
            Require(health.CurrentHealth == 5, "Healing exceeds maximum.");
            health.IncreaseMaximumHealth(2);
            health.TakeDamage(1.2f);
            health.Heal(2);
            Require(health.CurrentHealth == 7 && health.MaximumHealth == 7, "Healing ignores bonus/fractional health.");
            health.TakeDamage(7);
            health.Heal(2);
            Require(health.IsDead && health.CurrentHealth == 0, "Healing revives a dead target.");
        }
        finally { Object.DestroyImmediate(target); }
        var playerHealth = Object.FindFirstObjectByType<PlayerController>().GetComponent<Health>();
        Require(playerHealth.TakeDamage(1), "Invulnerability fixture.");
        playerHealth.Heal(1);
        Require(!playerHealth.TakeDamage(1), "Healing reset invulnerability.");
        Debug.Log("HEALING_CHECK_PASS positive-only,cap,maximum-bonus,fractional,no-revive,preserve-invulnerability=True");
    }

    private static async Task CheckPhysicalVisit(CharacterData character)
    {
        await FreshMain(character);
        RiskZone pitch = Zone(Names[2]);
        var player = Object.FindFirstObjectByType<PlayerController>();
        Health health = player.GetComponent<Health>();
        var game = Object.FindFirstObjectByType<GameManager>();
        var quilombo = Object.FindFirstObjectByType<QuilomboSystem>();
        Require(health.TakeDamage(3), "Damage before pitch.");
        await MoveTo(pitch.transform.position);
        await Until(() => pitch.State == RiskZone.ZoneState.Active);
        await Task.Delay(300);
        float progress = pitch.Progress;
        Require(progress > 0 && progress < 6 && pitch.HasPendingReward && quilombo.Current == 0, "Pitch early progress.");
        game.PauseGame();
        await Task.Delay(250);
        Require(pitch.Progress == progress && quilombo.Current == 0, "Pause advanced pitch.");
        game.ContinueGame();
        if (character.name == "LaPibaDelClub") await Capture("canchita-resistiendo-1280x720");
        await MoveTo(pitch.transform.position + Vector3.right * 3);
        Require(pitch.State == RiskZone.ZoneState.Inactive && pitch.Progress == 0 && pitch.HasPendingReward &&
            quilombo.Current == 0 && health.CurrentHealth == health.MaximumHealth - 3, "Early exit did not reset.");
        await MoveTo(pitch.transform.position);
        float start = Time.time;
        float alreadyHeld = pitch.Progress;
        await Until(() => pitch.State == RiskZone.ZoneState.Completed);
        Require(Time.time - start + alreadyHeld >= 5.98f && !pitch.HasPendingReward && quilombo.Current == 10 &&
            health.CurrentHealth == health.MaximumHealth - 1, "Actual six-second hold/reward.");
        if (character.name == "LaPibaDelClub")
        {
            await Task.Delay(200);
            await Capture("canchita-recuperacion-1280x720");
        }
        Require(health.TakeDamage(1), "Damage after reward.");
        int life = health.CurrentHealth;
        await MoveTo(pitch.transform.position + Vector3.right * 3);
        await MoveTo(pitch.transform.position);
        Require(quilombo.Current == 10 && health.CurrentHealth == life && !pitch.HasPendingReward, "Pitch can be farmed.");
        Debug.Log("PITCH_VISIT_PASS character=" + character.name + " physics=enter,exit real-hold=6s healing=2 pause,one-use=True");
    }

    private static async Task CheckFullHealthAndDeath()
    {
        await FreshMain(CharacterData.All[0]);
        RiskZone pitch = Zone(Names[2]);
        var health = Object.FindFirstObjectByType<PlayerController>().GetComponent<Health>();
        RiskZone kiosk = Zone(Names[0]);
        var experience = health.GetComponent<ExperienceSystem>();
        float radius = experience.CollectionRadius;
        await Complete(kiosk);
        await MoveTo(pitch.transform.position);
        Require(Notice(pitch) && !Notice(kiosk) && experience.CollectionRadius > radius,
            "Kiosk notice masks current challenge or bonus was cancelled.");
        await Capture("canchita-con-kiosco-activo-1280x720");
        SetProgress(pitch);
        Invoke(pitch, "Update");
        Require(health.CurrentHealth == health.MaximumHealth && !pitch.HasPendingReward &&
            Object.FindFirstObjectByType<QuilomboSystem>().Current == 20 && Notice(pitch) && !Notice(kiosk),
            "Full-health visit semantics or reward notice priority.");
        await FreshMain(CharacterData.All[1]);
        pitch = Zone(Names[2]);
        health = Object.FindFirstObjectByType<PlayerController>().GetComponent<Health>();
        await MoveTo(pitch.transform.position);
        Require(pitch.State == RiskZone.ZoneState.Active, "Death fixture did not enter pitch.");
        health.TakeDamage(health.MaximumHealth);
        SetProgress(pitch);
        Invoke(pitch, "Update");
        Data(pitch).ApplyReward(Object.FindFirstObjectByType<PlayerController>());
        Require(health.IsDead && health.CurrentHealth == 0 && pitch.HasPendingReward &&
            Object.FindFirstObjectByType<QuilomboSystem>().Current == 0 &&
            Object.FindFirstObjectByType<GameManager>().State == GameManager.GameState.Defeat,
            "Dead player receives completion or revives.");
        Debug.Log("PITCH_BOUNDARIES_PASS full-health=consumed-once death=no-reward,no-revive");
    }

    private static async Task CheckVisitOrders()
    {
        int[][] orders = { new[]{0,1,2},new[]{0,2,1},new[]{1,0,2},new[]{1,2,0},new[]{2,0,1},new[]{2,1,0} };
        foreach (int[] order in orders)
        {
            await FreshMain(CharacterData.All[0]);
            var quilombo = Object.FindFirstObjectByType<QuilomboSystem>();
            var waves = Object.FindFirstObjectByType<WaveDirector>();
            float expected = 0;
            foreach (int index in order)
            {
                RiskZone zone = Zone(Names[index]);
                await Complete(zone);
                expected += Data(zone).QuilomboReward;
                Require(quilombo.Current == expected && (float)Field(waves, "quilomboThresholdBonus") ==
                    (expected >= 25 ? 0.35f : 0.15f) && !(bool)Field(waves, "elitesEnabled"), "Visit order thresholds.");
            }
            Require(quilombo.Current == 35 && Mathf.Approximately(Object.FindFirstObjectByType<WeaponController>().DamageMultiplier, 1.2f),
                "Three rewards/Parrilla changed.");
            waves.enabled = true;
            waves.Tick(210);
            Require(Mathf.Abs((float)Field(waves, "nextSpawnSeconds") - (210 + 0.57f / 1.7f)) < 0.0001f,
                "Q=35 interval multiplier.");
            waves.enabled = false;
            Object.FindFirstObjectByType<GameManager>().RestartGame();
            await Task.Delay(250);
            Require(Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None).All(z => z.HasPendingReward && z.Progress == 0) &&
                Object.FindFirstObjectByType<QuilomboSystem>().Current == 0 &&
                Object.FindFirstObjectByType<PlayerController>().GetComponent<Health>().CurrentHealth == CharacterData.Selected.StartingHealth,
                "Restart retains visits/health.");
        }
        Debug.Log("PITCH_ROUTES_PASS orders=6 real-contacts=True timer-fixtures=True quilombo=35 thresholds=10,25 multiplier=1.7 restart=True");
    }

    private static async Task CheckIndicators()
    {
        await FreshMain(CharacterData.All[1]);
        Time.timeScale = 0;
        RiskZone[] zones = Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None);
        foreach (var size in new[] { new Vector2Int(1024,768),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2560,1080) })
        {
            await SetSize(size.x, size.y);
            foreach (RiskZone zone in zones)
            {
                float depth = Camera.main.WorldToViewportPoint(zone.transform.position).z;
                zone.transform.position = Camera.main.ViewportToWorldPoint(new Vector3(2,0.05f,depth));
            }
            await Task.Delay(100);
            await Capture("indicadores-3-" + size.x + "x" + size.y);
            var panels = (Rect[])Field(Object.FindFirstObjectByType<GameHud>(), "poiPanels");
            Require(panels.Length == 3 && panels.All(p => p.width > 0 && p.height > 0), "HUD did not place all three indicators.");
            for (int i = 0; i < 3; i++)
                for (int j = i+1; j < 3; j++) Require(!panels[i].Overlaps(panels[j]), "Actual HUD indicators overlap.");
        }
        Debug.Log("PITCH_INDICATORS_PASS actual-HUD=3-on-same-edge resolutions=4");
    }

    private static async Task FreshMain(CharacterData character)
    {
        var previous = Object.FindFirstObjectByType<GameManager>();
        CharacterData.Select(character);
        Time.timeScale = 1;
        SceneManager.LoadScene("Main");
        await Until(() => Object.FindFirstObjectByType<GameManager>() != null &&
            Object.FindFirstObjectByType<GameManager>() != previous &&
            Object.FindFirstObjectByType<GameManager>().ElapsedSeconds > 0);
        Object.FindFirstObjectByType<WaveDirector>().enabled = false;
        Object.FindFirstObjectByType<WeaponController>().enabled = false;
        Object.FindFirstObjectByType<PlayerController>().enabled = false;
    }

    private static async Task MoveTo(Vector3 position)
    {
        float fixedTime = Time.fixedTime;
        var body = Object.FindFirstObjectByType<PlayerController>().GetComponent<Rigidbody2D>();
        body.position = position;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        await Until(() => Time.fixedTime > fixedTime);
    }

    private static async Task Complete(RiskZone zone)
    {
        await MoveTo(zone.transform.position);
        if (zone.HasPendingReward) { SetProgress(zone); Invoke(zone, "Update"); }
        Require(!zone.HasPendingReward, "POI did not complete: " + zone.name);
    }

    private static void SetProgress(RiskZone zone) => typeof(RiskZone).GetField("<Progress>k__BackingField", Flags)
        .SetValue(zone, Data(zone).RequiredDuration);
    private static RiskZoneData Data(RiskZone zone) => (RiskZoneData)Field(zone, "data");
    private static bool Notice(RiskZone zone) => (bool)typeof(RiskZone).GetMethod("HasNoticePriority", Flags).Invoke(zone, null);
    private static RiskZone Zone(string name) => Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None).Single(z => z.name == name);
    private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static void Invoke(object target, string name) => target.GetType().GetMethod(name, Flags).Invoke(target, null);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    private static async Task SetSize(int width, int height) => await (Task)typeof(UiRedesignCheck)
        .GetMethod("SetSize", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { width, height });
    private static async Task Capture(string name)
    {
        string path = Path.Combine(output, name + ".png");
        DateTime before = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        ScreenCapture.CaptureScreenshot(path);
        await Until(() => File.Exists(path) && File.GetLastWriteTimeUtc(path) > before && new FileInfo(path).Length > 1000);
    }
    private static async Task Until(Func<bool> condition)
    {
        double deadline = EditorApplication.timeSinceStartup + 30;
        while (!condition())
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Pitch check timed out.");
            await Task.Delay(20);
        }
    }
    private static void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Exception && trace.Contains("UnityEditor.Search.SearchDatabase")) { editorSearchErrors++; return; }
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
    }
}
