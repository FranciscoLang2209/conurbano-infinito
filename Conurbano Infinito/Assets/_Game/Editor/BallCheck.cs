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

public static class BallCheck
{
    private const string Root = "Assets/_Game/";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly List<GameObject> targets = new();
    private static WeaponData ball;
    private static UpgradeData[] cards;
    private static int errors;
    private static int editorSearchErrors;
    private static string output;

    public static async void RunBatch()
    {
        output = Environment.GetEnvironmentVariable("CONURBANO_BALL_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new Exception("CONURBANO_BALL_OUTPUT is required.");
        Directory.CreateDirectory(output);
        Application.logMessageReceived += OnLog;
        try
        {
            ball = AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Data/Pelota de Potrero.asset");
            Require(ball != null && ball.Pattern == WeaponPattern.Ricochet && ball.Pierce == 2 &&
                ball.ProjectileCount == 1 && ball.Damage == 2 && ball.Range == 5, "Ball configuration.");
            cards = new[] { "Equipar Pelota de Potrero", "Pelota - Daño", "Pelota - Cadencia", "Pelota - Rebotes" }
                .Select(name => AssetDatabase.LoadAssetAtPath<UpgradeData>(Root + "Data/" + name + ".asset")).ToArray();
            Require(cards.All(c => c != null && c.TargetWeapon == ball), "Ball cards.");
            Require(GameUi.WeaponIcon(ball) != ball.ProjectileSprite && GameUi.WeaponIcon(ball).rect.size == Vector2.one * 40,
                "Ball needs its native-size UI icon.");
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(ball.ProjectileSprite));
            Require(importer.spritePixelsPerUnit == 48 && importer.filterMode == FilterMode.Point && !importer.mipmapEnabled &&
                importer.textureCompression == TextureImporterCompression.Uncompressed &&
                ball.ProjectileSprite.pivot == Vector2.one * 24, "Ball import/pivot.");
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene(Root + "Scenes/Main.unity");
            EditorApplication.EnterPlaymode();
            await Until(() => EditorApplication.isPlaying);
            await Task.Delay(250);
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            foreach (CharacterData character in CharacterData.All)
            {
                await FreshMain(character);
                CheckAcquisitionAndUpgrades();
                Debug.Log("BALL_CARDS_PASS character=" + character.name + " acquisition,damage,cadence,bounces,max-ranks,four-slots=True");
            }
            await FreshMain(CharacterData.All[0]);
            var controller = Object.FindFirstObjectByType<WeaponController>();
            Acquire(controller);
            CheckControlledFlights(controller);
            await CheckPhysicsAndScreenshots();
            Require(errors == 0, "Gameplay Console errors: " + errors);
            EditorApplication.ExitPlaymode();
            await Until(() => !EditorApplication.isPlaying);
            Debug.Log("BALL_CHECK_PASS characters=2 base=3-targets upgraded=6-targets nearest=True unique=True layers=True range=True pool=ball-straight-ball physics=True pause=True cards=4 ranks=3 HUD=True knownEditorSearchErrors=" + editorSearchErrors);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static void CheckAcquisitionAndUpgrades()
    {
        var controller = Object.FindFirstObjectByType<WeaponController>();
        var selection = Object.FindFirstObjectByType<UpgradeSelection>();
        var deck = (UpgradeData[])Field(selection, "upgrades");
        Require(cards.All(deck.Contains) && cards[0].CanOffer(controller) && cards.Skip(1).All(c => !c.CanOffer(controller)),
            "Card wiring or improvements offered before acquisition.");
        Acquire(controller);
        Require(!cards[0].CanOffer(controller) && cards.Skip(1).All(c => c.CanOffer(controller)), "Acquisition/filtering.");
        foreach (UpgradeData card in cards.Skip(1))
        {
            for (int rank = 0; rank < card.MaximumLevel; rank++) SelectCard(card);
            Require(selection.GetLevel(card) == card.MaximumLevel, "Upgrade ranks.");
            WithDeck(new[] { card }, () => {
                var xp = controller.GetComponent<ExperienceSystem>();
                xp.AddExperience(xp.RequiredExperience - xp.CurrentExperience);
                Require(!selection.IsShowing && Time.timeScale == 1, "Maxed card blocked play.");
            });
        }
        object state = State(controller);
        float interval = (float)typeof(WeaponController).GetMethod("GetInterval", Flags).Invoke(controller, new[] { state });
        Require(Mathf.Approximately(interval, 1.8f * Mathf.Pow(0.9f, 3)), "Cadence upgrade did not reach weapon.");
        Require(selection.GetWeaponLevel(ball) == 10, "HUD weapon rank omitted ball upgrades.");
        Vector2 start = (Vector2)controller.transform.position + Vector2.one * 200;
        var crowd = Enumerable.Range(0, 7).Select(i => Target(start + Vector2.right * (i + 1))).ToArray();
        Physics2D.SyncTransforms();
        Projectile shot = Fire(controller, crowd[0]);
        for (int i = 0; i < 6; i++) ContactAt(shot, crowd[i]);
        Require(!shot.gameObject.activeSelf && crowd.Take(6).All(t => t.CurrentHealth == 15) && crowd[6].CurrentHealth == 20,
            "Damage/bounce upgrades do not produce six unique impacts.");
        ClearTargets();
        foreach (string name in new[] { "Termo Balístico", "Aerosol", "Fernets Orbitales", "Chancleta Teledirigida" })
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Data/" + name + ".asset");
            if (controller.EquippedWeaponCount < 4 && !controller.IsEquipped(weapon)) Require(controller.EquipWeapon(weapon), "Fill weapon slots.");
        }
        var extra = AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Data/Petardo.asset");
        Require(controller.EquippedWeaponCount == 4 && !controller.CanEquipWeapon(extra) && !controller.EquipWeapon(extra),
            "Ball bypasses four slots.");
    }

    private static void CheckControlledFlights(WeaponController controller)
    {
        Vector2 start = (Vector2)controller.transform.position + Vector2.one * 200;
        Health first = Target(start + Vector2.right);
        first.gameObject.AddComponent<CircleCollider2D>().radius = 0.1f;
        Health second = Target(start + new Vector2(2, 1));
        Health third = Target(start + new Vector2(3, 0));
        Health fourth = Target(start + new Vector2(4, 1));
        Health friendly = Target(start + new Vector2(1.1f, 0), "Player");
        Health dead = Target(start + new Vector2(1.2f, 0));
        dead.TakeDamage(20);
        Health far = Target(start + Vector2.right * 10);
        Physics2D.SyncTransforms();
        Projectile shot = Fire(controller, first);
        ContactAt(shot, first);
        Vector2 heading = (second.transform.position - shot.transform.position).normalized;
        Require(Vector2.Dot(shot.GetComponent<Rigidbody2D>().linearVelocity.normalized, heading) > 0.999f, "Did not choose nearest living enemy.");
        ContactAt(shot, first);
        typeof(Projectile).GetMethod("OnTriggerStay2D", Flags).Invoke(shot,
            new object[] { first.GetComponents<Collider2D>()[1] });
        Require(first.CurrentHealth == 18, "Repeated impact against same enemy/collider.");
        ContactAt(shot, second);
        ContactAt(shot, third);
        Require(!shot.gameObject.activeSelf && second.CurrentHealth == 18 && third.CurrentHealth == 18 &&
            fourth.CurrentHealth == 20 && friendly.CurrentHealth == 20 && far.CurrentHealth == 20,
            "Base hit limit/layers/range.");
        var pool = Object.FindFirstObjectByType<ObjectPool>();
        var prefab = (Projectile)new SerializedObject(controller).FindProperty("projectilePrefab").objectReferenceValue;
        Projectile reused = pool.Spawn(prefab.gameObject, start, Quaternion.identity).GetComponent<Projectile>();
        Require(reused == shot, "Pool did not reuse ball.");
        var termo = AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Data/Termo Balístico.asset");
        reused.SetAppearance(termo.ProjectileSprite, Color.white, termo.ProjectileScale);
        reused.Initialize(Vector2.right, 1, 8, 0, 5);
        ContactAt(reused, first);
        Require(!reused.gameObject.activeSelf && first.CurrentHealth == 17 && second.CurrentHealth == 18, "Ball leaked flight/hit state into Termo.");
        shot = Fire(controller, first);
        ContactAt(shot, first);
        Require(shot == reused && shot.gameObject.activeSelf && first.CurrentHealth == 15, "Reused ball retained hit targets.");
        ObjectPool.Release(shot.gameObject);
        foreach (Health other in new[] { second, third, fourth }) other.gameObject.SetActive(false);
        shot = Fire(controller, first);
        ContactAt(shot, first);
        Require(!shot.gameObject.activeSelf && friendly.CurrentHealth == 20, "Isolated target did not end chain.");
        shot = Fire(controller, first);
        shot.GetComponent<Rigidbody2D>().position = (Vector2)controller.transform.position + Vector2.right * 5.1f;
        typeof(Projectile).GetMethod("FixedUpdate", Flags).Invoke(shot, null);
        Require(!shot.gameObject.activeSelf, "Unhit shot did not expire at range.");
        ClearTargets();
        Debug.Log("BALL_FLIGHT_PASS base=3-targets nearest,layers,dead,duplicates,no-neighbor,range,pool=True");
    }

    private static async Task CheckPhysicsAndScreenshots()
    {
        await FreshMain(CharacterData.All[1]);
        var controller = Object.FindFirstObjectByType<WeaponController>();
        Acquire(controller);
        var game = Object.FindFirstObjectByType<GameManager>();
        await (Task)typeof(UiRedesignCheck).GetMethod("SetSize", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { 1280, 720 });
        Vector2 origin = controller.transform.position;
        var group = new[] { new Vector2(2,0), new Vector2(3,1.5f), new Vector2(4.5f,1.5f), new Vector2(6,1.5f) }
            .Select(p => Target(origin+p)).ToArray();
        foreach (Health target in group)
        {
            var renderer = target.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/Sprites/Enemies/PalomaDePlaza.png");
            renderer.color = Color.gray;
            target.GetComponent<CircleCollider2D>().radius = 0.3f;
            var body = target.gameObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        Physics2D.SyncTransforms();
        Projectile shot = Fire(controller, group[0]);
        await Task.Delay(50);
        game.PauseGame();
        Vector3 paused = shot.transform.position;
        int[] life = group.Select(h => h.CurrentHealth).ToArray();
        await Task.Delay(220);
        Require(paused == shot.transform.position && group.Select(h => h.CurrentHealth).SequenceEqual(life), "Pause advances ball or damage.");
        game.ContinueGame();
        await Until(() => group[0].CurrentHealth == 18);
        await Capture("pelota-en-juego-1280x720");
        await Until(() => !shot.gameObject.activeSelf);
        Require(group.Take(3).All(t => t.CurrentHealth == 18) && group[3].CurrentHealth == 20, "Real physics did not chain three targets.");
        var selection = Object.FindFirstObjectByType<UpgradeSelection>();
        WithDeck(cards.Skip(1).ToArray(), () => {
            var xp = controller.GetComponent<ExperienceSystem>();
            xp.AddExperience(xp.RequiredExperience - xp.CurrentExperience);
        });
        Require(selection.IsShowing && selection.Options.Count == 3, "Improvement cards missing.");
        await Capture("mejoras-pelota-1280x720");
        selection.Select(selection.Options.ToList().IndexOf(cards[3]));
        Require(selection.GetWeaponLevel(ball) == 2, "Bounce upgrade omitted from HUD rank.");
        await Capture("hud-pelota-n2-1280x720");
        ClearTargets();
        Debug.Log("BALL_PHYSICS_PASS actual-collisions=3 pause=True screenshots=gameplay,cards,HUD");
    }

    private static async Task FreshMain(CharacterData character)
    {
        CharacterData.Select(character);
        SceneManager.LoadScene("Main");
        await Task.Delay(250);
        Object.FindFirstObjectByType<WaveDirector>().enabled = false;
        Object.FindFirstObjectByType<WeaponController>().enabled = false;
    }

    private static void Acquire(WeaponController controller)
    {
        SelectCard(cards[0]);
        Require(controller.IsEquipped(ball), "Cannot acquire ball.");
    }

    private static void SelectCard(UpgradeData card) => WithDeck(new[] { card }, () => {
        var selection = Object.FindFirstObjectByType<UpgradeSelection>();
        var xp = Object.FindFirstObjectByType<ExperienceSystem>();
        xp.AddExperience(xp.RequiredExperience - xp.CurrentExperience);
        Require(selection.IsShowing && selection.Options.Count == 1 && selection.Options[0] == card, "Card cannot be offered: " + card.name);
        selection.Select(0);
    });

    private static void WithDeck(UpgradeData[] cards, Action action)
    {
        var selection = Object.FindFirstObjectByType<UpgradeSelection>();
        var field = typeof(UpgradeSelection).GetField("upgrades", Flags);
        object original = field.GetValue(selection);
        try { field.SetValue(selection, cards); action(); }
        finally { field.SetValue(selection, original); }
    }

    private static Health Target(Vector2 position, string layer = "Enemy")
    {
        var target = new GameObject("Pelota check: " + layer);
        targets.Add(target);
        target.layer = LayerMask.NameToLayer(layer);
        target.transform.position = position;
        target.AddComponent<CircleCollider2D>().radius = 0.1f;
        var health = target.AddComponent<Health>();
        health.ConfigureStartingHealth(20);
        return health;
    }

    private static void ClearTargets()
    {
        foreach (GameObject target in targets) Object.DestroyImmediate(target);
        targets.Clear();
    }

    private static object State(WeaponController controller) => ((IEnumerable)Field(controller, "equipped")).Cast<object>()
        .Single(state => (WeaponData)state.GetType().GetField("Data").GetValue(state) == ball);

    private static Projectile Fire(WeaponController controller, Health target)
    {
        var before = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
        typeof(WeaponController).GetMethod("Fire", Flags).Invoke(controller, new[] { State(controller), target });
        return Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Except(before).Single();
    }

    private static void ContactAt(Projectile shot, Health target)
    {
        shot.GetComponent<Rigidbody2D>().position = target.transform.position;
        Physics2D.SyncTransforms();
        typeof(Projectile).GetMethod("OnTriggerEnter2D", Flags).Invoke(shot, new object[] { target.GetComponent<Collider2D>() });
    }

    private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task Until(Func<bool> condition)
    {
        double deadline = EditorApplication.timeSinceStartup + 30;
        while (!condition())
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Ball check timed out.");
            await Task.Delay(20);
        }
    }

    private static async Task Capture(string name)
    {
        string path = Path.Combine(output, name + ".png");
        DateTime before = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        ScreenCapture.CaptureScreenshot(path);
        await Until(() => File.Exists(path) && File.GetLastWriteTimeUtc(path) > before && new FileInfo(path).Length > 1000);
    }

    private static void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Exception && trace.Contains("UnityEditor.Search.SearchDatabase")) { editorSearchErrors++; return; }
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
    }
}
