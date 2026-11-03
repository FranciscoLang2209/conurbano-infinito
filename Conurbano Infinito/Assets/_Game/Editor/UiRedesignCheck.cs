using System;
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

public static class UiRedesignCheck
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static int errors;
    private static int editorSearchErrors;
    private static Keyboard keyboard;
    private static string output;
    private static readonly System.Collections.Generic.HashSet<string> layoutIssues = new();

    public static async void RunBatch()
    {
        output = Environment.GetEnvironmentVariable("CONURBANO_UI_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new Exception("CONURBANO_UI_OUTPUT is required.");
        Directory.CreateDirectory(output);
        Application.logMessageReceived += OnLog;
        try
        {
            CheckAssets();
            PoiIndicatorsCheck.Run();
            Hito11Setup.Check();
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Main.unity");
            EditorApplication.EnterPlaymode();
            await Until(() => EditorApplication.isPlaying, 30);
            await Task.Delay(300);
            GameUi.CheckTextLayout = CheckTextLayout;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            keyboard = InputSystem.AddDevice<Keyboard>();
            if (Environment.GetEnvironmentVariable("CONURBANO_ART_SAMPLE") == "1") await CaptureArtSample();
            else await Validate();
            File.WriteAllLines(Path.Combine(output, "text-layout.txt"), layoutIssues);
            Require(layoutIssues.Count == 0, "Text overflow: " + string.Join("; ", layoutIssues));
            Require(errors == 0, "Runtime Console errors: " + errors);
            InputSystem.RemoveDevice(keyboard);
            EditorApplication.ExitPlaymode();
            await Until(() => !EditorApplication.isPlaying, 30);
            if (Environment.GetEnvironmentVariable("CONURBANO_ART_SAMPLE") == "1") Debug.Log("ART_CHECK_PASS errors=0 knownEditorSearchErrors=" + editorSearchErrors);
            else Debug.Log("UI_REDESIGN_PASS assets=27 icons=native-size weapons=7 font=Spanish HUD=health,xp,level,quilombo,weapons pause=keyboard mouse=not-tested restart=clean menu=True defeat=True victory=True screenshots=4-resolutions knownEditorSearchErrors=" + editorSearchErrors);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            File.WriteAllLines(Path.Combine(output, "text-layout.txt"), layoutIssues);
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static void CheckAssets()
    {
        const string ninja = "Assets/ThirdParty/NinjaAdventure/Resources/NinjaAdventure/";
        foreach (string asset in new[] { "focus", "impact" })
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(ninja + asset + ".png");
            Require(importer != null && importer.filterMode == FilterMode.Point && !importer.mipmapEnabled &&
                importer.textureCompression == TextureImporterCompression.Uncompressed &&
                importer.npotScale == TextureImporterNPOTScale.None &&
                importer.sRGBTexture == (asset == "impact"), "Incorrect Ninja Adventure import: " + asset);
        }
        Sprite pointer = Resources.Load<Sprite>("NinjaAdventure/focus");
        Texture2D impact = Resources.Load<Texture2D>("NinjaAdventure/impact");
        Require(pointer != null && pointer.rect.size == new Vector2(13, 13) &&
            impact != null && impact.width == 128 && impact.height == 32, "Missing or resized free assets.");
        string[] paths = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Game/Resources/BarrioUi" })
            .Select(AssetDatabase.GUIDToAssetPath).Where(path => path.EndsWith(".png")).ToArray();
        Require(paths.Length == 27, "Expected 27 original UI textures, found " + paths.Length);
        foreach (string path in paths)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Require(importer.textureType == TextureImporterType.Sprite && importer.filterMode == FilterMode.Point &&
                !importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed &&
                importer.spritePixelsPerUnit == 48, "Incorrect pixel-art import: " + path);
            Require(AssetDatabase.LoadAssetAtPath<Sprite>(path) != null, "Missing sprite: " + path);
        }
        string[] indicators = { "heart", "xp", "flame", "clock", "crest", "shoe", "fist", "rapid", "arrow", "pin", "mate" };
        int[] sizes = { 24, 16, 30, 32, 48, 24, 24, 24, 24, 16, 32 };
        for (int i = 0; i < indicators.Length; i++)
            Require(GameUi.Sprite(indicators[i]).rect.size == Vector2.one * sizes[i],
                "Wrong native icon size: " + indicators[i]);
        foreach (string name in new[] { "Termo Balístico", "Aerosol", "Boleadora", "Chancleta Teledirigida", "Fernets Orbitales", "Petardo", "Pelota de Potrero" })
        {
            WeaponData weapon = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/" + name + ".asset");
            Sprite icon = GameUi.WeaponIcon(weapon);
            Require(icon != null && icon != weapon.ProjectileSprite && icon.rect.size == Vector2.one * 40,
                "Missing native object icon: " + name);
        }
        Font font = Resources.Load<Font>("BarrioUi/AtkinsonHyperlegible-Regular");
        const string spanish = "áéíóúüñÁÉÍÓÚÜÑ¿¡";
        font.RequestCharactersInTexture(spanish, 24);
        foreach (char c in spanish) Require(font.HasCharacter(c), "Missing Spanish glyph: " + c);
        Font bold = Resources.Load<Font>("BarrioUi/AtkinsonHyperlegible-Bold");
        bold.RequestCharactersInTexture(spanish, 32);
        foreach (char c in spanish) Require(bold.HasCharacter(c), "Missing Spanish title glyph: " + c);
        Require(GameUi.Sprite("paper").border == new Vector4(6, 6, 6, 6), "Missing nine-slice border.");
        string enamel = "Assets/_Game/Resources/BarrioUi/enamel.png";
        var probe = (TextureImporter)AssetImporter.GetAtPath(enamel);
        probe.isReadable = true;
        probe.SaveAndReimport();
        Color pixel = AssetDatabase.LoadAssetAtPath<Texture2D>(enamel).GetPixel(16, 16);
        Require(Mathf.Abs(pixel.r - Mathf.GammaToLinearSpace(40 / 255f)) < 0.005f, "IMGUI palette conversion failed.");
        probe.isReadable = false;
        probe.SaveAndReimport();
        Debug.Log("UI_ASSETS_PASS 27 textures, 18 native-size icons, 7 object icons, point, no mipmaps/compression, nine-slice, Spanish accents.");
    }

    private static async Task CaptureArtSample()
    {
        var player = Object.FindFirstObjectByType<PlayerController>();
        var game = Object.FindFirstObjectByType<GameManager>();
        Object.FindFirstObjectByType<SpawnManager>().enabled = false;
        Object.FindFirstObjectByType<WaveDirector>().enabled = false;
        Object.FindFirstObjectByType<CameraFollow>().enabled = false;
        Camera.main.transform.position = new Vector3(0, 0, -10);
        player.GetComponent<Rigidbody2D>().position = Vector2.zero;
        player.enabled = false;
        var weapons = player.GetComponent<WeaponController>();
        weapons.enabled = false;
        foreach (string name in new[] { "Aerosol", "Boleadora", "Pelota de Potrero" })
            Require(weapons.EquipWeapon(AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/" + name + ".asset")), "Sample equipment.");
        player.GetComponent<Health>().TakeDamage(1);
        player.GetComponent<ExperienceSystem>().AddExperience(2);
        Object.FindFirstObjectByType<QuilomboSystem>().Add(25);
        typeof(GameManager).GetField("<ElapsedSeconds>k__BackingField", Flags).SetValue(game, 75f);
        game.enabled = false;
        var pool = Object.FindFirstObjectByType<ObjectPool>();
        string[] enemies = { "MosquitoGigante", "PalomaDePlaza", "PerroCallejero", "VecinoGomero" };
        Vector2[] positions = { new(-2, 1), new(2, -1), new(-3, -2), new(3, 2) };
        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject actor = pool.Spawn(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/" + enemies[i] + ".prefab"), positions[i], Quaternion.identity);
            foreach (MonoBehaviour behavior in actor.GetComponents<MonoBehaviour>()) behavior.enabled = false;
            actor.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        }
        foreach (RiskZone zone in Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None))
            zone.transform.position = zone.name.StartsWith("Kiosco") ? new Vector3(-5, 2, 1) :
                zone.name.StartsWith("Parrilla") ? new Vector3(5, 2, 1) : new Vector3(6, -3, 1);
        Time.timeScale = 0;
        await Task.Delay(3300);
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1024, 768),
            new Vector2Int(1920, 1080), new Vector2Int(2560, 1080) })
        {
            await SetSize(size.x, size.y);
            await Capture("plaza-hud-" + size.x + "x" + size.y);
        }
        RiskZone objective = Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None)
            .First(zone => zone.name.StartsWith("Canchita"));
        objective.SendMessage("OnTriggerEnter2D", player.GetComponent<Collider2D>());
        Require(objective.State == RiskZone.ZoneState.Active, "Representative objective did not activate.");
        typeof(RiskZone).GetField("<Progress>k__BackingField", Flags).SetValue(objective, 3f);
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1024, 768),
            new Vector2Int(1920, 1080), new Vector2Int(2560, 1080) })
        {
            await SetSize(size.x, size.y);
            await Capture("objetivo-" + size.x + "x" + size.y);
        }
        objective.SendMessage("OnTriggerExit2D", player.GetComponent<Collider2D>());
        SceneManager.LoadScene("Menu");
        await Task.Delay(300);
        await Press(Key.Enter);
        Require((bool)Field(Object.FindFirstObjectByType<MenuScreen>(), "showingCharacters"), "Sample selector missing.");
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1024, 768),
            new Vector2Int(1920, 1080), new Vector2Int(2560, 1080) })
        {
            await SetSize(size.x, size.y);
            await Capture("selector-" + size.x + "x" + size.y);
        }
        await SetSize(1280, 720);
        await Press(Key.RightArrow);
        await Capture("selector-piba-1280x720");
        var menu = Object.FindFirstObjectByType<MenuScreen>();
        if (typeof(MenuScreen).GetField("characterFocus", Flags) != null)
        {
            await Press(Key.Tab);
            Require((int)Field(menu, "characterFocus") == 2, "Tab cannot reach Back.");
            await Capture("selector-foco-volver-1280x720");
            await Press(Key.Tab);
            Require((int)Field(menu, "characterFocus") == 3, "Tab cannot reach Play.");
            await Capture("selector-foco-jugar-1280x720");
            await Press(Key.UpArrow);
            Require((int)Field(menu, "characterFocus") == 1, "Up cannot return to character.");
            await Press(Key.DownArrow);
            Require((int)Field(menu, "characterFocus") == 3, "Down cannot reach Play.");
        }
        await Press(Key.Enter);
        await Task.Delay(300);
        player = Object.FindFirstObjectByType<PlayerController>();
        Require(player.Character.name == "LaPibaDelClub" && player.GetComponent<Health>().MaximumHealth == 4 &&
            Mathf.Approximately(player.MovementSpeed, 5.5f), "Sample changed Piba gameplay.");
        Animator animator = player.GetComponent<Animator>();
        animator.SetBool("Walking", true);
        animator.Update(0.12f);
        Sprite first = player.GetComponent<SpriteRenderer>().sprite;
        animator.Update(0.12f);
        Require(first != player.GetComponent<SpriteRenderer>().sprite, "Walk frames do not animate.");
        animator.SetBool("Walking", false);
        animator.Update(0.4f);
        Require(animator.GetCurrentAnimatorStateInfo(0).IsName("PlayerIdle"), "Idle state unavailable.");
        Debug.Log("ART_SAMPLE_PASS screenshots=4-aspects scene=Main+Menu profiles=unchanged animation=walk+idle");
    }

    private static async Task Validate()
    {
        GameManager game = Object.FindFirstObjectByType<GameManager>();
        WeaponController weapons = Object.FindFirstObjectByType<WeaponController>();
        Health health = weapons.GetComponent<Health>();
        ExperienceSystem xp = weapons.GetComponent<ExperienceSystem>();
        UpgradeSelection selection = Object.FindFirstObjectByType<UpgradeSelection>();
        GameHud hud = Object.FindFirstObjectByType<GameHud>();
        QuilomboSystem quilombo = Object.FindFirstObjectByType<QuilomboSystem>();
        Object.FindFirstObjectByType<SpawnManager>().enabled = false;
        Require(health.TakeDamage(1), "Damage fixture failed.");
        xp.AddExperience(2);
        quilombo.Add(25);
        await Task.Delay(500);
        Require((int)Field(hud, "lastHealth") == health.CurrentHealth &&
            (float)Field(hud, "displayedQuilombo") == 25 && xp.CurrentExperience == 2,
            "HUD did not follow health/experience/Quilombo.");
        Require(!(bool)Field(Object.FindFirstObjectByType<PerformanceOverlay>(), "visible"), "Diagnostics visible by default.");
        await Press(Key.F3);
        Require((bool)Field(Object.FindFirstObjectByType<PerformanceOverlay>(), "visible"), "F3 does not show diagnostics.");
        await Press(Key.F3);
        xp.AddExperience(xp.RequiredExperience);
        Require(selection.IsShowing && Time.timeScale == 0, "Upgrade not paused.");
        int focused = (int)Field(selection, "focusedOption");
        await Press(Key.RightArrow);
        Require((int)Field(selection, "focusedOption") == (focused + 1) % 3, "Upgrade arrows failed.");
        await SetSize(1280, 720);
        await CaptureAtSizes("mejoras");
        await CheckAllUpgradeLayouts(selection);
        await Press(Key.Escape);
        Require(game.State == GameManager.GameState.Paused, "Esc cannot pause an upgrade.");
        await Press(Key.Escape);
        Require(game.State == GameManager.GameState.Playing && Time.timeScale == 0, "Resume bypassed pending upgrade.");
        int aerosol = selection.Options.ToList().FindIndex(x => x.Effect == UpgradeEffect.EquipWeapon &&
            x.TargetWeapon.name == "Aerosol");
        Require(aerosol >= 0, "First selection lacks Aerosol.");
        await Press((Key)((int)Key.Digit1 + aerosol));
        Require(!selection.IsShowing && weapons.EquippedWeaponCount == 2 && Time.timeScale == 1, "Digit selection failed.");
        WeaponData data = weapons.GetEquippedWeapon(1);
        Require(selection.GetWeaponLevel(data) == 1, "New weapon should start at N1.");
        xp.AddExperience(xp.RequiredExperience);
        // Fix this one random offer to exercise a real weapon upgrade through Select.
        var options = (System.Collections.Generic.List<UpgradeData>)selection.Options;
        options[0] = AssetDatabase.LoadAssetAtPath<UpgradeData>("Assets/_Game/Data/Aerosol - Daño.asset");
        await Press(Key.Digit1);
        Require(selection.GetWeaponLevel(data) == 2, "Weapon slot level did not increase after its upgrade.");
        foreach (string name in new[] { "Alfajor Triple", "Zapatillas Flecha", "Yerba Premium", "Chimichurri Casero" })
        {
            xp.AddExperience(xp.RequiredExperience);
            options[0] = AssetDatabase.LoadAssetAtPath<UpgradeData>("Assets/_Game/Data/" + name + ".asset");
            int frame = Time.frameCount;
            selection.Select(0);
            await Until(() => Time.frameCount > frame, 5);
            Require(!selection.IsShowing, "Passive fixture left an unconsumed choice.");
        }
        foreach (string name in new[] { "Boleadora", "Petardo" })
            weapons.EquipWeapon(AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/" + name + ".asset"));
        Require(weapons.EquippedWeaponCount == 4, "Full equipment fixture failed.");
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1024, 768),
            new Vector2Int(1920, 1080), new Vector2Int(2560, 1080) })
        {
            await SetSize(size.x, size.y);
            await Capture("hud-" + size.x + "x" + size.y);
            await Press(Key.Escape);
            float elapsed = game.ElapsedSeconds;
            float animation = Time.unscaledTime;
            await Task.Delay(250);
            Require(game.State == GameManager.GameState.Paused && Time.timeScale == 0 && game.ElapsedSeconds == elapsed &&
                Time.unscaledTime > animation, "Pause did not freeze simulation or stopped unscaled time.");
            await Capture("pausa-" + size.x + "x" + size.y);
            await Press(Key.DownArrow);
            Require((int)Field(game, "focusedAction") == 1, "Pause arrows failed.");
            await Press(Key.UpArrow);
            await Press(Key.Enter);
            Require(game.State == GameManager.GameState.Playing && Time.timeScale == 1, "Enter did not resume.");
        }
        weapons.enabled = false;
        ((System.Collections.IList)Field(weapons, "equipped")).Clear();
        foreach (string name in new[] { "Termo Balístico", "Chancleta Teledirigida", "Fernets Orbitales", "Aerosol" })
            Require(weapons.EquipWeapon(AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/" + name + ".asset")),
                "Alternative equipment fixture failed: " + name);
        await SetSize(1280, 720);
        await Capture("hud-armas-alternativas-1280x720");
        await SetSize(1024, 768);
        await Capture("hud-armas-alternativas-1024x768");
        await Press(Key.Escape);
        await Press(Key.R);
        await Task.Delay(200);
        game = Object.FindFirstObjectByType<GameManager>();
        weapons = Object.FindFirstObjectByType<WeaponController>();
        Require(game.ElapsedSeconds < 2 && weapons.EquippedWeaponCount == 1 &&
            weapons.GetComponent<ExperienceSystem>().Level == 1 &&
            weapons.GetComponent<Health>().CurrentHealth == weapons.GetComponent<Health>().MaximumHealth,
            "Restart retained session data.");
        await SetSize(1280, 720);
        weapons.GetComponent<Health>().TakeDamage(100);
        Require(game.State == GameManager.GameState.Defeat, "Missing defeat.");
        await CaptureAtSizes("derrota");
        await Press(Key.M);
        await Task.Delay(200);
        Require(SceneManager.GetActiveScene().name == "Menu" && Time.timeScale == 1, "M did not return to menu.");
        await CaptureAtSizes("menu");
        await Press(Key.C);
        Require((bool)Field(Object.FindFirstObjectByType<MenuScreen>(), "showingCredits"), "C did not open credits.");
        await CaptureAtSizes("creditos");
        await Press(Key.Escape);
        await Press(Key.Enter);
        Require((bool)Field(Object.FindFirstObjectByType<MenuScreen>(), "showingCharacters"), "Menu did not open character selection.");
        await CaptureAtSizes("selector");
        await Press(Key.Enter);
        await Task.Delay(200);
        game = Object.FindFirstObjectByType<GameManager>();
        Require(game != null, "Menu Enter did not start Main.");
        typeof(GameManager).GetField("<ElapsedSeconds>k__BackingField", Flags).SetValue(game, 240f);
        await Task.Delay(100);
        Require(game.State == GameManager.GameState.Victory && Time.timeScale == 0, "Missing victory.");
        await CaptureAtSizes("victoria");
        await Press(Key.M);
        Debug.Log("UI_RUNTIME_PASS indicators, weapon N1/N2, four slots, F3, upgrades, Esc, arrow/Enter, R, M, menu, credits, victory.");
    }

    private static async Task CheckAllUpgradeLayouts(UpgradeSelection selection)
    {
        var options = (System.Collections.Generic.List<UpgradeData>)selection.Options;
        UpgradeData[] original = options.ToArray();
        UpgradeData[] all = AssetDatabase.FindAssets("t:UpgradeData", new[] { "Assets/_Game/Data" })
            .Select(id => AssetDatabase.LoadAssetAtPath<UpgradeData>(AssetDatabase.GUIDToAssetPath(id))).ToArray();
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1024, 768),
            new Vector2Int(1920, 1080), new Vector2Int(2560, 1080) })
        {
            await SetSize(size.x, size.y);
            for (int start = 0; start < all.Length; start += 3)
            {
                options.Clear();
                options.AddRange(all.Skip(start).Take(3));
                await Task.Delay(150);
            }
        }
        options.Clear();
        options.AddRange(original);
        await SetSize(1280, 720);
        Debug.Log("UI_UPGRADE_COPY_PASS cards=" + all.Length + " resolutions=4");
    }

    private static void CheckTextLayout(Rect rect, string value, GUIStyle style)
    {
        if (string.IsNullOrEmpty(value) || Event.current.type != EventType.Repaint) return;
        var content = new GUIContent(value);
        Vector2 needed = style.CalcSize(content);
        float height = style.wordWrap ? style.CalcHeight(content, rect.width) : needed.y;
        if (height > rect.height + 2 || (!style.wordWrap && needed.x > rect.width + 2))
            layoutIssues.Add($"{Screen.width}x{Screen.height}: '{value}' needs {needed.x:0}x{height:0}, has {rect.width:0}x{rect.height:0}");
    }

    private static async Task CaptureAtSizes(string name)
    {
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1024, 768),
            new Vector2Int(1920, 1080), new Vector2Int(2560, 1080) })
        {
            await SetSize(size.x, size.y);
            await Capture(name + "-" + size.x + "x" + size.y);
        }
        await SetSize(1280, 720);
    }

    private static async Task Press(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        InputSystem.Update();
        keyboard.MakeCurrent();
        Require(keyboard[key].wasPressedThisFrame, "Synthetic key was not delivered: " + key);
        // Batchmode does not receive OS input; dispatch through the real input handlers.
        foreach (Type type in new[] { typeof(GameManager), typeof(UpgradeSelection), typeof(MenuScreen), typeof(PerformanceOverlay) })
        {
            Object target = Object.FindFirstObjectByType(type);
            if (target != null) type.GetMethod("Update", Flags).Invoke(target, null);
        }
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();
        await Task.Delay(80);
    }

    private static async Task SetSize(int width, int height)
    {
        Assembly assembly = typeof(Editor).Assembly;
        Type sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        object sizes = sizesType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy).GetValue(null);
        object group = sizesType.GetProperty("currentGroup").GetValue(sizes);
        int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        Type sizeType = assembly.GetType("UnityEditor.GameViewSize");
        int index = -1;
        for (int i = 0; i < count; i++)
        {
            object existing = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
            if ((int)sizeType.GetProperty("width").GetValue(existing) == width &&
                (int)sizeType.GetProperty("height").GetValue(existing) == height) { index = i; break; }
        }
        if (index < 0)
        {
            object size = Activator.CreateInstance(sizeType, Flags, null,
                new object[] { Enum.ToObject(assembly.GetType("UnityEditor.GameViewSizeType"), 1), width, height, "UI check" }, null);
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            index = count;
        }
        Type viewType = assembly.GetType("UnityEditor.GameView");
        EditorWindow view = EditorWindow.GetWindow(viewType);
        foreach (EditorWindow other in Resources.FindObjectsOfTypeAll<EditorWindow>())
            if (other != view && other.GetType() == viewType) other.Close();
        for (int attempt = 0; attempt < 20; attempt++)
        {
            // Unity's selection callback also updates the render target; the index setter alone does not.
            MethodInfo select = viewType.GetMethod("SizeSelectionCallback", Flags);
            select.Invoke(view, new object[] { 0, null });
            select.Invoke(view, new object[] { index, null });
            view.Focus();
            view.Repaint();
            await Task.Delay(250);
            if (Screen.width != width || Screen.height != height) continue;
            await Task.Delay(150);
            if (Screen.width == width && Screen.height == height) return;
        }
        throw new Exception($"Game View resolution mismatch: {Screen.width}x{Screen.height}, requested {width}x{height}, index {index}");
    }

    private static async Task Capture(string name)
    {
        string path = Path.Combine(output, name + ".png");
        DateTime previousWrite = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        ScreenCapture.CaptureScreenshot(path);
        await Until(() => File.Exists(path) && File.GetLastWriteTimeUtc(path) > previousWrite &&
            new FileInfo(path).Length > 1000, 10);
    }

    private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static async Task Until(Func<bool> condition, int timeout)
    {
        double deadline = EditorApplication.timeSinceStartup + timeout;
        while (!condition())
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("UI check timed out.");
            await Task.Delay(30);
        }
    }
    private static void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Exception && trace.Contains("UnityEditor.Search.SearchDatabase"))
        {
            editorSearchErrors++;
            return;
        }
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
    }
}
