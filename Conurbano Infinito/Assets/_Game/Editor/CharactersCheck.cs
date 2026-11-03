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

public static class CharactersCheck
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Keyboard keyboard;
    private static int errors;
    private static int editorSearchErrors;
    private static bool shortRunPassed;
    private static string output;

    public static async void RunBatch()
    {
        output = Environment.GetEnvironmentVariable("CONURBANO_CHARACTERS_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new Exception("CONURBANO_CHARACTERS_OUTPUT is required.");
        Directory.CreateDirectory(output);
        Application.logMessageReceived += OnLog;
        try
        {
            CheckPibaArt();
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Menu.unity");
            EditorApplication.EnterPlaymode();
            await Until(() => EditorApplication.isPlaying);
            await Task.Delay(300);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            keyboard = InputSystem.AddDevice<Keyboard>();
            Require(CharacterData.All.Length == 2 && CharacterData.Selected.name == "ElPibeDelBarrio", "Initial character/catalog.");
            await Press(Key.Enter);
            Require(SceneManager.GetActiveScene().name == "Menu", "Jugar bypassed selection.");
            foreach (var size in new[] { new Vector2Int(1024, 768), new Vector2Int(1280, 720),
                new Vector2Int(1920, 1080), new Vector2Int(2560, 1080) })
            {
                await SetSize(size.x, size.y);
                await Capture("selector-" + size.x + "x" + size.y);
            }
            await SetSize(1280, 720);
            await Press(Key.RightArrow);
            await Press(Key.Escape);
            Require(CharacterData.Selected.name == "ElPibeDelBarrio", "Cancel committed selection.");
            await Press(Key.Enter);
            await Press(Key.RightArrow);
            await Press(Key.Enter);
            await Until(() => Object.FindFirstObjectByType<PlayerController>() != null);
            await CheckCharacter("LaPibaDelClub", 4, 5.5f, "Boleadora");
            var player = Object.FindFirstObjectByType<PlayerController>();
            var health = player.GetComponent<Health>();
            var weapons = player.GetComponent<WeaponController>();
            var xp = player.GetComponent<ExperienceSystem>();
            var selection = Object.FindFirstObjectByType<UpgradeSelection>();
            xp.AddExperience(xp.RequiredExperience);
            for (int i = 0; i < 20; i++)
            {
                Require(selection.Options.Count == 3 && selection.Options.Distinct().Count() == 3 &&
                    selection.Options.All(u => u.CanOffer(weapons)) &&
                    selection.Options.Any(u => u == player.Character.FirstWeaponUpgrade) &&
                    !selection.Options.Any(u => u.Effect == UpgradeEffect.EquipWeapon && u.TargetWeapon.name == "Boleadora"),
                    "Invalid Piba first upgrades.");
                typeof(UpgradeSelection).GetMethod("ShowNext", Flags).Invoke(selection, null);
            }
            selection.Select(selection.Options.ToList().IndexOf(player.Character.FirstWeaponUpgrade));
            Require(weapons.EquippedWeaponCount == 2 && weapons.GetEquippedWeapon(1).name == "Termo Balístico", "Piba cannot acquire Termo.");
            health.TakeDamage(1);
            player.IncreaseMovementSpeed(1.2f);
            Object.FindFirstObjectByType<GameManager>().RestartGame();
            await Task.Delay(250);
            await CheckCharacter("LaPibaDelClub", 4, 5.5f, "Boleadora");
            Require(Object.FindFirstObjectByType<ExperienceSystem>().Level == 1 &&
                Object.FindFirstObjectByType<WeaponController>().EquippedWeaponCount == 1, "Restart retained upgrades.");
            shortRunPassed = false;
            ShortRunCheck.Run();
            await Until(() => shortRunPassed || errors > 0);
            Require(shortRunPassed, "Piba short run failed.");
            await Press(Key.Enter);
            Require((int)Field(Object.FindFirstObjectByType<MenuScreen>(), "focusedCharacter") == 1, "Menu forgot Piba.");
            await Capture("selector-piba-1280x720");
            await Press(Key.LeftArrow);
            await Press(Key.Enter);
            await Task.Delay(250);
            await CheckCharacter("ElPibeDelBarrio", 5, 5f, "Termo Balístico");
            Require(CharacterData.Selected.name == "ElPibeDelBarrio", "Cannot change character.");
            shortRunPassed = false;
            ShortRunCheck.Run();
            await Until(() => shortRunPassed || errors > 0);
            Require(shortRunPassed, "Pibe short run failed.");
            Require(errors == 0, "Console errors: " + errors);
            InputSystem.RemoveDevice(keyboard);
            EditorApplication.ExitPlaymode();
            await Until(() => !EditorApplication.isPlaying);
            Debug.Log("CHARACTERS_CHECK_PASS catalog=2 selection=keyboard,cancel,return profiles=health,speed,weapon animation=walk,idle,pause upgrades=20 restart=clean legacy=ShortRun-both screenshots=4-resolutions knownEditorSearchErrors=" + editorSearchErrors);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static void CheckPibaArt()
    {
        const string root = "Assets/_Game/Art/";
        string[] paths = new[] { root + "Sprites/Player/LaPibaDelClub.png", root + "Animations/Frames/PibaIdle2.png" }
            .Concat(Enumerable.Range(1, 6).Select(i => root + $"Animations/Frames/PibaStride{i}.png")).ToArray();
        foreach (string path in paths)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Require(sprite != null && sprite.rect.size == Vector2.one * 48 && sprite.pivot == new Vector2(24, 0) &&
                sprite.pixelsPerUnit == 48 && importer.filterMode == FilterMode.Point && !importer.mipmapEnabled &&
                importer.textureCompression == TextureImporterCompression.Uncompressed,
                "Incorrect Piba frame size, foot pivot or pixel-art import: " + path);
            var texture = new Texture2D(2, 2);
            try
            {
                Require(ImageConversion.LoadImage(texture, File.ReadAllBytes(path)), "Unreadable PNG: " + path);
                Color32[] pixels = texture.GetPixels32();
                Require(pixels.All(p => p.a == 0 || p.a == 255) &&
                    Enumerable.Range(0, pixels.Length).Where(i => pixels[i].a != 0).Min(i => i / 48) == 2,
                    "Soft alpha or drifting foot baseline: " + path);
            }
            finally { Object.DestroyImmediate(texture); }
        }
        foreach (var (name, count) in new[] { ("PibaIdle", 2), ("PibaWalk", 6) })
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(root + "Animations/" + name + ".anim");
            var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
            Require(AnimationUtility.GetObjectReferenceCurve(clip, binding).Select(k => k.value).Distinct().Count() == count,
                "Missing distinct Piba animation poses: " + name);
        }
        Debug.Log("PIBA_ART_PASS frames=8 size=48 pivot=feet baseline=stable alpha=hard idle=2 walk=6");
    }

    private static async Task CheckCharacter(string name, int life, float speed, string weapon)
    {
        var player = Object.FindFirstObjectByType<PlayerController>();
        Object.FindFirstObjectByType<WaveDirector>().enabled = false;
        player.GetComponent<WeaponController>().enabled = false;
        Require(player.Character.name == name && player.GetComponent<Health>().MaximumHealth == life &&
            player.GetComponent<Health>().CurrentHealth == life && Mathf.Approximately(player.MovementSpeed, speed) &&
            player.GetComponent<WeaponController>().GetEquippedWeapon(0).name == weapon,
            "Wrong starting profile: " + name);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
        InputSystem.Update();
        keyboard.MakeCurrent();
        typeof(PlayerController).GetMethod("Update", Flags).Invoke(player, null);
        Vector3 position = player.transform.position;
        await Task.Delay(180);
        var animator = player.GetComponent<Animator>();
        var renderer = player.GetComponent<SpriteRenderer>();
        Sprite first = renderer.sprite;
        await Task.Delay(130);
        Require(player.transform.position.x > position.x && animator.GetBool("Walking") && first != renderer.sprite,
            "Movement/animation did not advance: " + name);
        if (name == "LaPibaDelClub") Require(renderer.sprite.name.StartsWith("Piba"), "Piba shows Pibe frames.");
        var game = Object.FindFirstObjectByType<GameManager>();
        game.PauseGame();
        Sprite paused = renderer.sprite;
        position = player.transform.position;
        await Task.Delay(180);
        Require(paused == renderer.sprite && position == player.transform.position, "Pause advances character.");
        game.ContinueGame();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();
        typeof(PlayerController).GetMethod("Update", Flags).Invoke(player, null);
        await Task.Delay(100);
        Require(!animator.GetBool("Walking"), "Cannot return to idle.");
        if (name == "LaPibaDelClub")
        {
            var idleFrames = new System.Collections.Generic.HashSet<Sprite>();
            for (int i = 0; i < 14; i++)
            {
                await Task.Delay(60);
                idleFrames.Add(renderer.sprite);
            }
            Require(idleFrames.Count == 2 && idleFrames.All(s => s != null &&
                (s.name == "LaPibaDelClub" || s.name == "PibaIdle2")), "Piba breathing idle did not loop through both poses.");
            Debug.Log("PIBA_IDLE_RUNTIME_PASS frames=2 loop=True");
        }
        await Capture("gameplay-" + name);
        player.GetComponent<WeaponController>().enabled = true;
        Object.FindFirstObjectByType<WaveDirector>().enabled = true;
    }

    private static async Task Press(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        InputSystem.Update();
        keyboard.MakeCurrent();
        foreach (Type type in new[] { typeof(MenuScreen), typeof(GameManager) })
        {
            Object target = Object.FindFirstObjectByType(type);
            if (target != null) type.GetMethod("Update", Flags).Invoke(target, null);
        }
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();
        await Task.Delay(100);
    }

    private static async Task SetSize(int width, int height)
    {
        Type check = typeof(UiRedesignCheck);
        await (Task)check.GetMethod("SetSize", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { width, height });
    }

    private static async Task Capture(string name)
    {
        string path = Path.Combine(output, name + ".png");
        DateTime before = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        ScreenCapture.CaptureScreenshot(path);
        await Until(() => File.Exists(path) && File.GetLastWriteTimeUtc(path) > before && new FileInfo(path).Length > 1000);
    }

    private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task Until(Func<bool> condition)
    {
        double deadline = EditorApplication.timeSinceStartup + 30;
        while (!condition())
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Character check timed out.");
            await Task.Delay(30);
        }
    }
    private static void OnLog(string message, string trace, LogType type)
    {
        if (message.StartsWith("SHORT_RUN_CHECK_PASS")) shortRunPassed = true;
        if (type == LogType.Exception && trace.Contains("UnityEditor.Search.SearchDatabase"))
        {
            editorSearchErrors++;
            return;
        }
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
    }
}
