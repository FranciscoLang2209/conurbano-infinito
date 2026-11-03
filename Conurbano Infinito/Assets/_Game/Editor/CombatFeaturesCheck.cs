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

public static class CombatFeaturesCheck
{
    private const string Root = "Assets/_Game/";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly List<string> messages = new();
    private static int errors;
    private static int editorSearchErrors;
    private static string output;

    public static async void RunBatch()
    {
        output = Environment.GetEnvironmentVariable("CONURBANO_COMBAT_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new Exception("CONURBANO_COMBAT_OUTPUT is required.");
        Directory.CreateDirectory(output);
        Application.logMessageReceived += OnLog;
        try
        {
            CheckAssets();
            Hito9Check.RunBatch();
            Hito11Setup.Check();
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.EnterPlaymode();
            await Until(() => EditorApplication.isPlaying, 30);
            await Task.Delay(250);
            await SetGameViewSize();
            await CheckPlayer();
            CheckEnemyHealthAndPool();
            CheckGomero();
            await CheckDeathAndCapture();
            await FreshMain();
            ExpansionCheck.Run();
            await FreshMain();
            PetardoCheck.Run();
            await FreshMain();
            VisualSlice5Check.Run();
            await Until(() => messages.Any(m => m.StartsWith("Quinta slice Play Mode:")) || errors > 0, 15);
            Require(errors == 0, "Legacy visual check failed.");
            ShortRunCheck.Run();
            await Until(() => messages.Any(m => m.StartsWith("SHORT_RUN_CHECK_PASS")) || errors > 0, 15);
            Require(errors == 0, "Runtime Console errors: " + errors);
            EditorApplication.ExitPlaymode();
            await Until(() => !EditorApplication.isPlaying, 30);
            Debug.Log("COMBAT_FEATURES_CHECK_PASS player=6-frames dog=4-phases ranged=warning,locked-direction,sweep,damage,pause,death,pool range=bounded health=6-prefabs,damage,death,unique-xp,reuse legacy=Expansion,Petardo,VisualSlice5,ShortRun screenshots=True knownEditorSearchErrors=" + editorSearchErrors);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static void CheckAssets()
    {
        var ranged = Prefab("VecinoGomero");
        Require(ranged.GetComponent<EnemyRanged>() != null && ranged.GetComponent<Health>() != null &&
            ranged.GetComponent<ExperienceDrop>() != null && ranged.GetComponent<HitFlash>() != null,
            "Incomplete ranged prefab.");
        Require(new SerializedObject(ranged.GetComponent<EnemyRanged>()).FindProperty("stonePrefab").objectReferenceValue != null,
            "Missing stone reference.");
        var waves = AssetDatabase.LoadAssetAtPath<WaveData>(Root + "Data/Oleadas.asset");
        foreach (WaveData.Phase phase in waves.Phases)
            Require(phase.enemies.Any(e => e.prefab == ranged && e.weight > 0) == (phase.startSeconds >= 120),
                "Vecino must enter from minute two.");
        foreach (string name in new[] { "VecinoGomero", "PerroCallejero" })
        {
            var controller = Prefab(name).GetComponent<Animator>().runtimeAnimatorController;
            int expected = name == "PerroCallejero" ? 4 : 3;
            Require(controller != null && controller.animationClips.Length == expected &&
                controller.animationClips.All(c => c != null), "Missing animation: " + name);
        }
        var walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + "Art/Animations/PlayerWalk.anim");
        var binding = AnimationUtility.GetObjectReferenceCurveBindings(walk).Single();
        Require(AnimationUtility.GetObjectReferenceCurve(walk, binding).Select(k => k.value).Distinct().Count() == 6,
            "Player walk must contain six distinct poses.");
    }

    private static async Task CheckPlayer()
    {
        var player = Object.FindFirstObjectByType<PlayerController>();
        Object.FindFirstObjectByType<WeaponController>().enabled = false;
        Object.FindFirstObjectByType<WaveDirector>().enabled = false;
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        keyboard.MakeCurrent();
        try
        {
            var animator = player.GetComponent<Animator>();
            var renderer = player.GetComponent<SpriteRenderer>();
            Vector3 start = player.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
            InputSystem.Update();
            Invoke(player, "Update");
            Require(animator.GetBool("Walking") && renderer.flipX, "Player does not walk left.");
            await Task.Delay(160);
            Sprite frame = renderer.sprite;
            await Task.Delay(120);
            Require(frame != renderer.sprite && player.transform.position.x < start.x, "Walking does not animate or move.");
            Time.timeScale = 0;
            frame = renderer.sprite;
            Vector3 paused = player.transform.position;
            await Task.Delay(180);
            Require(frame == renderer.sprite && player.transform.position == paused, "Pause advances player animation.");
            Time.timeScale = 1;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();
            Invoke(player, "Update");
            Require(!renderer.flipX, "Player does not face right.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            Invoke(player, "Update");
            Require(!animator.GetBool("Walking"), "Player does not return to idle.");
        }
        finally { InputSystem.RemoveDevice(keyboard); Time.timeScale = 1; }
    }

    private static void CheckEnemyHealthAndPool()
    {
        var spawner = Object.FindFirstObjectByType<SpawnManager>();
        foreach (var (name, maximum) in new[] { ("MosquitoGigante", 3), ("PalomaDePlaza", 15),
            ("BolsaVoladora", 6), ("PerroCallejero", 10), ("VecinoGomero", 12), ("LaJefa", 100) })
        {
            GameObject prefab = Prefab(name);
            Require(prefab.GetComponent<Health>().MaximumHealth == maximum, "Wrong configured health: " + name);
            Health previous = null;
            int xp = new SerializedObject(prefab.GetComponent<ExperienceDrop>()).FindProperty("experienceValue").intValue;
            for (int life = 0; life < 2; life++)
            {
                Health enemy = spawner.Spawn(prefab);
                Require((previous == null || enemy == previous) && !enemy.IsDead &&
                    enemy.MaximumHealth == maximum && enemy.CurrentHealth == maximum,
                    "Health did not reset from prefab: " + name);
                var gemsBefore = Object.FindObjectsByType<ExperienceGem>(FindObjectsSortMode.None);
                Require(enemy.TakeDamage(1) && !enemy.IsDead && enemy.CurrentHealth == maximum - 1,
                    "Nonlethal damage: " + name);
                CheckImpact(enemy);
                Require(Object.FindObjectsByType<ExperienceGem>(FindObjectsSortMode.None).Length == gemsBefore.Length,
                    "XP before death: " + name);
                Require(enemy.TakeDamage(maximum - 1) && enemy.IsDead && !enemy.TakeDamage(1),
                    "Death repeated: " + name);
                var flash = enemy.GetComponent<HitFlash>();
                Set(flash, "impactStartTime", Time.time - 0.06f);
                Invoke(flash, "Update");
                Require(enemy.transform.Find("Impacto visual").GetComponent<SpriteRenderer>().sprite.rect.x == 32,
                    "Lethal hit froze the impact animation: " + name);
                var dropped = Object.FindObjectsByType<ExperienceGem>(FindObjectsSortMode.None).Except(gemsBefore).ToArray();
                Require(dropped.Length == 1 &&
                    (int)typeof(ExperienceGem).GetField("experienceValue", Flags).GetValue(dropped[0]) == xp,
                    "XP missing, changed or duplicated: " + name);
                ObjectPool.Release(dropped[0].gameObject);
                ObjectPool.Release(enemy.gameObject);
                Require(!enemy.transform.Find("Impacto visual").gameObject.activeSelf,
                    "Impact remained visible in the pool: " + name);
                previous = enemy;
            }
        }
        Debug.Log("ENEMY_HEALTH_POOL_PASS prefabs=6 lifecycles=12 damage,death,unique-xp,health-reset=True");
    }

    private static void CheckImpact(Health enemy)
    {
        var flash = enemy.GetComponent<HitFlash>();
        var impact = enemy.transform.Find("Impacto visual").GetComponent<SpriteRenderer>();
        Require(impact.gameObject.activeSelf && impact.sprite.rect == new Rect(0, 0, 32, 32),
            "Damage did not restart the impact at frame zero.");
        for (int i = 0; i < 4; i++)
        {
            Set(flash, "impactStartTime", Time.time - (i + 0.5f) * 0.04f);
            Invoke(flash, "Update");
            Require(impact.sprite.rect == new Rect(i * 32, 0, 32, 32) &&
                impact.sprite.pixelsPerUnit == 48 && impact.color == Color.white,
                "Impact frame, alignment or palette changed.");
        }
        Sprite paused = impact.sprite;
        Set(flash, "impactStartTime", Time.time - 1f);
        Time.timeScale = 0;
        Invoke(flash, "Update");
        Require(impact.gameObject.activeSelf && impact.sprite == paused, "Paused impact advanced.");
        Time.timeScale = 1;
        Invoke(flash, "Update");
        Require(!impact.gameObject.activeSelf, "Completed impact did not hide.");
    }

    private static void CheckGomero()
    {
        var pool = Object.FindFirstObjectByType<ObjectPool>();
        var spawner = Object.FindFirstObjectByType<SpawnManager>();
        Health health = spawner.Spawn(Prefab("VecinoGomero"));
        var enemy = health.gameObject;
        var ranged = enemy.GetComponent<EnemyRanged>();
        var body = enemy.GetComponent<Rigidbody2D>();
        var bait = new GameObject("Blanco de prueba");
        bait.layer = LayerMask.NameToLayer("Player");
        var baitHealth = bait.AddComponent<Health>();
        bait.AddComponent<CircleCollider2D>().radius = 0.3f;
        var friendly = new GameObject("Enemigo fuera de la máscara");
        friendly.layer = LayerMask.NameToLayer("Enemy");
        var friendlyHealth = friendly.AddComponent<Health>();
        friendly.AddComponent<CircleCollider2D>().radius = 0.3f;
        SimulationMode2D simulation = Physics2D.simulationMode;
        try
        {
            Physics2D.simulationMode = SimulationMode2D.Script;
            body.position = new Vector2(200, 200);
            Vector2 origin = body.position;
            bait.transform.position = origin + Vector2.right * 3;
            friendly.transform.position = origin + Vector2.right;
            ranged.SetTarget(bait.transform);
            Set(ranged, "nextAttackTime", Time.time - 1);
            Invoke(ranged, "FixedUpdate");
            var warning = enemy.transform.Find("Trayectoria de gomera");
            Require(warning.gameObject.activeSelf && body.position == origin, "Missing warning or moving while aiming.");
            Quaternion aim = warning.rotation;
            bait.transform.position += Vector3.up * 2;
            Set(ranged, "fireTime", Time.time - 1);
            Time.timeScale = 0;
            Invoke(ranged, "FixedUpdate");
            Require(warning.gameObject.activeSelf && Stones().Length == 0, "Paused enemy fired.");
            Time.timeScale = 1;
            Invoke(ranged, "FixedUpdate");
            Require(!warning.gameObject.activeSelf && warning.rotation == aim, "Aim followed the moving target.");
            EnemyStone stone = Stones().Single();
            Physics2D.SyncTransforms();
            Vector3 shotStart = stone.transform.position;
            Invoke(stone, "FixedUpdate");
            Require(stone.transform.position.x > shotStart.x && stone.transform.position.y == shotStart.y,
                "Stone changed its locked heading.");
            Vector3 paused = stone.transform.position;
            Time.timeScale = 0;
            Invoke(stone, "FixedUpdate");
            Require(stone.transform.position == paused, "Stone moved during pause.");
            Time.timeScale = 1;
            for (int i = 0; i < 100 && stone.gameObject.activeSelf; i++) Invoke(stone, "FixedUpdate");
            Require(!stone.gameObject.activeSelf && baitHealth.CurrentHealth == 5 && friendlyHealth.CurrentHealth == 5,
                "Dodged stone did damage or exceeded its range.");

            var reused = pool.Spawn(Prefab("PiedraGomera"), origin, Quaternion.identity).GetComponent<EnemyStone>();
            Require(reused == stone, "Stone did not reuse the pool.");
            bait.transform.position = origin + Vector2.right * 3;
            Physics2D.SyncTransforms();
            reused.Initialize(health, baitHealth, Vector2.right);
            Set(reused, "speed", 1000f);
            Invoke(reused, "FixedUpdate");
            Require(!reused.gameObject.activeSelf && baitHealth.CurrentHealth == 4 && friendlyHealth.CurrentHealth == 5,
                "Sweep missed the player, damaged an enemy, or hit more than once.");
            Set(reused, "speed", 6f);

            Set(ranged, "nextAttackTime", Time.time - 1);
            Invoke(ranged, "FixedUpdate");
            Require(warning.gameObject.activeSelf, "Second warning did not start.");
            health.TakeDamage(health.MaximumHealth);
            Invoke(ranged, "FixedUpdate");
            Require(!warning.gameObject.activeSelf && Stones().Length == 0, "Dead enemy retained its attack.");
            ObjectPool.Release(enemy);
            Require(spawner.Spawn(Prefab("VecinoGomero")).gameObject == enemy && !health.IsDead &&
                !warning.gameObject.activeSelf && enemy.GetComponent<Animator>().enabled,
                "Ranged enemy did not reset after pooling.");
            ranged.SetTarget(bait.transform);
            Set(ranged, "nextAttackTime", Time.time - 1);
            Invoke(ranged, "FixedUpdate");
            baitHealth.TakeDamage(100);
            Invoke(ranged, "FixedUpdate");
            Require(!warning.gameObject.activeSelf && Stones().Length == 0, "Target death did not cancel the warning.");
        }
        finally
        {
            Time.timeScale = 1;
            Physics2D.simulationMode = simulation;
            foreach (EnemyStone stone in Stones()) ObjectPool.Release(stone.gameObject);
            ObjectPool.Release(enemy);
            Object.Destroy(bait);
            Object.Destroy(friendly);
        }
    }

    private static async Task CheckDeathAndCapture()
    {
        var pool = Object.FindFirstObjectByType<ObjectPool>();
        var player = Object.FindFirstObjectByType<PlayerController>();
        var camera = Camera.main;
        player.transform.position = Vector3.zero;
        player.GetComponent<Rigidbody2D>().position = new Vector2(0, 0);
        camera.GetComponent<CameraFollow>().enabled = false;
        camera.transform.position = new Vector3(0, 0, -10);
        GameObject dog = pool.Spawn(Prefab("PerroCallejero"), new Vector3(-2, 1), Quaternion.identity);
        var charge = dog.GetComponent<EnemyCharge>();
        var animator = dog.GetComponent<Animator>();
        charge.SetTarget(player.transform);
        Set(charge, "nextChargeTime", Time.time - 1);
        Invoke(charge, "FixedUpdate");
        await Task.Delay(60);
        Require(animator.GetInteger("ChargePhase") == 1 && animator.GetCurrentAnimatorStateInfo(0).IsName("DogAim"),
            "Dog warning pose not connected.");
        var dogWarning = dog.transform.Find("Aviso de embestida").GetComponent<SpriteRenderer>();
        Require(dogWarning.gameObject.activeSelf && dogWarning.sortingOrder >
            GameObject.Find("PlazaPiso").GetComponent<Renderer>().sortingOrder,
            "Dog warning is hidden behind the floor.");

        var neighbor = pool.Spawn(Prefab("VecinoGomero"), new Vector3(3, 1), Quaternion.identity);
        var ranged = neighbor.GetComponent<EnemyRanged>();
        ranged.SetTarget(player.transform);
        Set(ranged, "nextAttackTime", Time.time - 1);
        Invoke(ranged, "FixedUpdate");
        await Task.Delay(80);
        Time.timeScale = 0;
        Sprite dogFrame = dog.GetComponent<SpriteRenderer>().sprite;
        await Capture("avisos-perro-gomero");
        Require(dog.GetComponent<SpriteRenderer>().sprite == dogFrame, "Dog animates during pause.");
        Time.timeScale = 1;
        Set(charge, "phaseEndTime", Time.time - 1);
        Invoke(charge, "FixedUpdate");
        await Task.Delay(60);
        Require(animator.GetInteger("ChargePhase") == 2 && animator.GetCurrentAnimatorStateInfo(0).IsName("DogDash"),
            "Dog dash pose not connected.");
        Set(charge, "phaseEndTime", Time.time - 1);
        Invoke(charge, "FixedUpdate");
        await Task.Delay(60);
        Require(animator.GetInteger("ChargePhase") == 3 && animator.GetCurrentAnimatorStateInfo(0).IsName("DogRest"),
            "Dog recovery pose not connected.");
        Time.timeScale = 0;
        await Capture("descanso-perro");
        Time.timeScale = 1;
        ObjectPool.Release(neighbor);
        Vector3 normalScale = dog.transform.localScale;
        dog.GetComponent<Health>().TakeDamage(100);
        await Task.Delay(80);
        Require(!animator.enabled && !dog.GetComponent<Rigidbody2D>().simulated && dog.transform.localScale != normalScale,
            "Death did not animate or retained simulation.");
        Time.timeScale = 0;
        float alpha = dog.GetComponent<SpriteRenderer>().color.a;
        await Task.Delay(280);
        Require(dog.activeSelf && dog.GetComponent<SpriteRenderer>().color.a == alpha,
            "Paused death advanced or returned to the pool.");
        await Capture("muerte-enemigo");
        Time.timeScale = 1;
        await Until(() => !dog.activeSelf, 5);
        Require(pool.Spawn(Prefab("PerroCallejero"), new Vector3(-2, 1), Quaternion.identity) == dog &&
            animator.enabled && dog.transform.localScale == normalScale && dog.transform.rotation == Quaternion.identity &&
            dog.GetComponent<Rigidbody2D>().simulated && animator.GetInteger("ChargePhase") == 0,
            "Dog retained death transforms, disabled animation or a stale phase.");
        ObjectPool.Release(dog);
    }

    private static async Task FreshMain()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Main");
        await Task.Delay(200);
    }

    private static async Task Capture(string name)
    {
        string path = Path.Combine(output, name + ".png");
        DateTime previousWrite = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        ScreenCapture.CaptureScreenshot(path);
        await Until(() => File.Exists(path) && File.GetLastWriteTimeUtc(path) > previousWrite &&
            new FileInfo(path).Length > 1000, 10);
    }

    private static async Task SetGameViewSize()
    {
        Assembly assembly = typeof(Editor).Assembly;
        Type sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        object sizes = sizesType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy).GetValue(null);
        object group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { 0 });
        int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        Type sizeType = assembly.GetType("UnityEditor.GameViewSize");
        object size = Activator.CreateInstance(sizeType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { Enum.ToObject(assembly.GetType("UnityEditor.GameViewSizeType"), 1), 1280, 720, "Combat check" }, null);
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        Type viewType = assembly.GetType("UnityEditor.GameView");
        EditorWindow view = EditorWindow.GetWindow(viewType);
        viewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(view, index);
        view.Repaint();
        await Task.Delay(350);
        Require(Screen.width == 1280 && Screen.height == 720, "Game View size mismatch.");
    }

    private static GameObject Prefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/" + name + ".prefab");
    private static EnemyStone[] Stones() => Object.FindObjectsByType<EnemyStone>(FindObjectsSortMode.None);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Flags).SetValue(target, value);
    private static void Invoke(object target, string method) => target.GetType().GetMethod(method, Flags).Invoke(target, null);
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static async Task Until(Func<bool> condition, int seconds)
    {
        double deadline = EditorApplication.timeSinceStartup + seconds;
        while (!condition())
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Combat check timed out.");
            await Task.Delay(20);
        }
    }
    private static void OnLog(string message, string trace, LogType type)
    {
        messages.Add(message);
        if (type == LogType.Exception && trace.Contains("UnityEditor.Search.SearchDatabase"))
        {
            editorSearchErrors++;
            return;
        }
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
    }
}
