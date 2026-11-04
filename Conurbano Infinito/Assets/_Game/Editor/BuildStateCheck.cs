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

public static class BuildStateCheck
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string Root = "Assets/_Game/";
    private static int errors;
    private static int searchErrors;
    private static bool hito6Completed;

    public static async void RunBatch()
    {
        Application.logMessageReceived += OnLog;
        try
        {
            var definitions = AssetDatabase.FindAssets("t:WeaponData t:UpgradeData t:CharacterData t:BuildRules", new[] { Root })
                .Select(g => AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            var snapshots = definitions.ToDictionary(d => AssetDatabase.GetAssetPath(d), d => EditorJsonUtility.ToJson(d));
            var ids = definitions.Select(d => (string)d.GetType().GetProperty("StableId").GetValue(d)).ToArray();
            Require(definitions.Length == 46 && ids.All(id => !string.IsNullOrWhiteSpace(id)) &&
                ids.Distinct().Count() == ids.Length, "Missing or duplicate stable IDs.");
            foreach (var definition in definitions)
            {
                var renamed = Object.Instantiate(definition);
                renamed.name = "Nombre visible cambiado";
                Require(Equals(renamed.GetType().GetProperty("StableId").GetValue(renamed),
                    definition.GetType().GetProperty("StableId").GetValue(definition)), "Identity depends on name.");
                Object.DestroyImmediate(renamed);
            }
            Require(BuildRules.Current.MaximumWeaponLevel == 8 && BuildRules.Current.MinimumAttackInterval == 0.12f &&
                BuildRules.Current.MaximumProjectileCount == 12 && BuildRules.Current.MaximumMovementSpeed == 10f, "Rules profile.");
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene(Root + "Scenes/Main.unity");
            EditorApplication.EnterPlaymode();
            await Until(() => EditorApplication.isPlaying);
            await Task.Delay(200);
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            foreach (WeaponData data in definitions.OfType<WeaponData>()) await CheckWeapon(data);
            await CheckStatisticsAndProjectiles();
            await CheckSelection();
            foreach (CharacterData character in CharacterData.All)
            {
                await FreshMain(character);
                var controller = Object.FindFirstObjectByType<WeaponController>();
                var movement = controller.GetComponent<PlayerController>();
                Require(controller.EquippedWeaponCount == 1 && controller.GetWeaponLevel(character.StartingWeapon) == 1 &&
                    controller.DamageMultiplier == 1f && controller.Damage == character.StartingWeapon.Damage &&
                    Mathf.Approximately(movement.MovementSpeed, character.MovementSpeed) &&
                    controller.GetComponent<Health>().MaximumHealth == character.StartingHealth, "Session reset: " + character.name);
            }
            await FreshMain(CharacterData.All[0]);
            Hito6Check.Run();
            await Until(() => hito6Completed || errors > 0);
            foreach (var entry in snapshots) Require(EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<ScriptableObject>(entry.Key)) == entry.Value, "Definition mutated: " + entry.Key);
            Require(errors == 0, "Gameplay Console errors: " + errors);
            EditorApplication.ExitPlaymode();
            await Until(() => !EditorApplication.isPlaying);
            Debug.Log("BUILD_STATE_CHECK_PASS ids=46 weapons=7 level=1-8 card-caps=True formulas=True order=True future-weapons=True clamps=True snapshots=True orbitals=True queue=True double-input=True revalidation=True reset=2-characters immutable-definitions=True knownEditorSearchErrors=" + searchErrors);
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    private static async Task CheckWeapon(WeaponData data)
    {
        await FreshMain(CharacterData.All[0]);
        var controller = Object.FindFirstObjectByType<WeaponController>();
        var selection = Object.FindFirstObjectByType<UpgradeSelection>();
        var deck = (UpgradeData[])Field(selection, "upgrades");
        if (!controller.IsEquipped(data))
            await SelectCard(deck.Single(c => c.TargetWeapon == data && c.Effect == UpgradeEffect.EquipWeapon));
        Require(controller.GetWeaponLevel(data) == 1, "Acquisition must start at 1.");
        var state = State(controller, data);
        Require(Mathf.Approximately(Stat(controller, "GetDamage", state), data.Damage * (1f + (data.DirectProjectile ? CharacterData.All[0].DirectProjectileDamageBonus : 0f))) && Stat(controller, "GetInterval", state) == data.FireInterval &&
            Stat(controller, "GetSpeed", state) == data.ProjectileSpeed, "Base statistics changed: " + data.name);
        var cards = deck.Where(c => c.TargetWeapon == data && c.Effect != UpgradeEffect.EquipWeapon).ToArray();
        Require(cards.Sum(c => c.MaximumLevel) >= 7, "Cannot reach level 8: " + data.name);
        foreach (var card in cards)
        {
            for (int rank = 0; rank < card.MaximumLevel && controller.GetWeaponLevel(data) < 8; rank++)
            {
                int previous = controller.GetWeaponLevel(data);
                await SelectCard(card);
                Require(controller.GetWeaponLevel(data) == previous + 1 && selection.GetWeaponLevel(data) == previous + 1,
                    "Weapon level has multiple sources.");
            }
            if (selection.GetLevel(card) == card.MaximumLevel && controller.GetWeaponLevel(data) < 8)
                await RequireNoOffer(selection, card);
        }
        Require(controller.GetWeaponLevel(data) == 8 && cards.All(c => !c.CanOffer(controller)), "Global weapon cap.");
        float damage = Stat(controller, "GetDamage", state);
        foreach (var card in cards)
        {
            await RequireNoOffer(selection, card);
            card.Apply(controller.GetComponent<PlayerController>(), controller, controller.GetComponent<Health>());
        }
        Require(controller.GetWeaponLevel(data) == 8 && damage == Stat(controller, "GetDamage", state), "Applied upgrade after cap.");
        Debug.Log("BUILD_WEAPON_PASS " + data.name + " base=True acquisition=1 directed=7 cap=8 individual-cap=True");
    }

    private static async Task CheckStatisticsAndProjectiles()
    {
        var termo = LoadWeapon("Termo Balístico");
        var fernet = LoadWeapon("Fernets Orbitales");
        // Stage 3: the Pibe affinity adds 10% to the same percentage sum as Parrilla.
        float expected = (termo.Damage + 2 + 3) * 1.3f;
        foreach (bool percentageFirst in new[] { true, false })
        {
            await FreshMain(CharacterData.All[0]);
            var controller = Object.FindFirstObjectByType<WeaponController>();
            if (percentageFirst) controller.ApplyDamageMultiplier(1.2f);
            controller.IncreaseDamage(2);
            controller.IncreaseWeaponDamage(termo, 3);
            if (!percentageFirst) controller.ApplyDamageMultiplier(1.2f);
            var state = State(controller, termo);
            Require(Mathf.Approximately(Stat(controller, "GetDamage", state), expected), "Flat/percentage order.");
            if (percentageFirst)
            {
                controller.ReduceWeaponFireInterval(termo, 0.9f);
                controller.ReduceFireInterval(0.9f);
            }
            else
            {
                controller.ReduceFireInterval(0.9f);
                controller.ReduceWeaponFireInterval(termo, 0.9f);
            }
            Require(Mathf.Approximately(controller.FireInterval, termo.FireInterval * 0.81f), "Interval factors.");
            controller.EquipWeapon(fernet);
            var orbitState = State(controller, fernet);
            Require(Mathf.Approximately(Stat(controller, "GetDamage", orbitState), (fernet.Damage + 2) * 1.2f) &&
                Mathf.Approximately(Stat(controller, "GetInterval", orbitState), fernet.FireInterval * 0.9f), "Future weapon lost general modifiers.");
            Invoke(controller, "EnsureOrbitals", orbitState);
            var orbitals = ((IEnumerable)orbitState.GetType().GetField("Orbitals").GetValue(orbitState)).Cast<Projectile>().ToArray();
            controller.IncreaseDamage(1);
            controller.IncreaseWeaponProjectileSpeed(fernet, 1.2f);
            controller.ReduceWeaponFireInterval(fernet, 0.9f);
            Invoke(controller, "EnsureOrbitals", orbitState);
            Require(orbitals.Length == 2 && orbitals.All(p => p.gameObject.activeSelf &&
                Mathf.Approximately((float)Field(p, "damage"), (fernet.Damage + 3) * 1.2f) &&
                Mathf.Approximately((float)Field(p, "speed"), fernet.ProjectileSpeed * 1.2f) &&
                Mathf.Approximately((float)Field(p, "orbitInterval"), fernet.FireInterval * 0.81f)), "Orbital refresh/reuse.");
            Require(((IEnumerable)orbitState.GetType().GetField("Orbitals").GetValue(orbitState)).Cast<Projectile>().SequenceEqual(orbitals), "Recreated orbitals.");
            controller.ReduceFireInterval(0.0001f);
            state.GetType().GetField("NextAttackTime").SetValue(state, Time.time + 1f);
            controller.ReduceFireInterval(0.5f);
            Require(controller.FireInterval == 0.12f && Mathf.Approximately((float)state.GetType().GetField("NextAttackTime").GetValue(state), Time.time + 1f),
                "Clamped interval accelerated pending attack: interval=" + controller.FireInterval.ToString("R") +
                " pending=" + ((float)state.GetType().GetField("NextAttackTime").GetValue(state)).ToString("R") +
                " expected=" + (Time.time + 1f).ToString("R"));
            controller.IncreaseWeaponProjectileCount(termo, 100);
            var target = new GameObject("Build snapshot target");
            target.layer = LayerMask.NameToLayer("Enemy");
            target.transform.position = controller.transform.position + Vector3.right * 2;
            var health = target.AddComponent<Health>();
            var before = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
            Invoke(controller, "Fire", state, health);
            var shots = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Except(before).ToArray();
            Require(shots.Length == 12, "Projectile cap.");
            float snapshotDamage = (float)Field(shots[0], "damage");
            controller.IncreaseDamage(1);
            Require((float)Field(shots[0], "damage") == snapshotDamage, "Active projectile lost snapshot.");
            foreach (var shot in shots) ObjectPool.Release(shot.gameObject);
            Object.DestroyImmediate(target);
            controller.IncreaseWeaponProjectileCount(fernet, 100);
            Invoke(controller, "EnsureOrbitals", orbitState);
            var capped = ((IEnumerable)orbitState.GetType().GetField("Orbitals").GetValue(orbitState)).Cast<Projectile>().ToArray();
            Invoke(controller, "EnsureOrbitals", orbitState);
            Require(capped.Length == 12 && capped.SequenceEqual(((IEnumerable)orbitState.GetType().GetField("Orbitals").GetValue(orbitState)).Cast<Projectile>()), "Orbital count clamp/recreation.");
            var movement = controller.GetComponent<PlayerController>();
            movement.IncreaseMovementSpeed(100f);
            movement.IncreaseMovementSpeed(float.NaN);
            movement.IncreaseMovementSpeed(float.PositiveInfinity);
            controller.ReduceFireInterval(float.NaN);
            controller.ReduceWeaponFireInterval(termo, float.NaN);
            Require(movement.MovementSpeed == 10f && controller.FireInterval == 0.12f, "Movement cap or nonfinite input.");
        }
        await FreshMain(CharacterData.All[0]);
        var fractionalController = Object.FindFirstObjectByType<WeaponController>();
        fractionalController.ApplyDamageMultiplier(1.2f);
        var fractionalTarget = new GameObject("Fractional damage target");
        fractionalTarget.layer = LayerMask.NameToLayer("Enemy");
        fractionalTarget.AddComponent<CircleCollider2D>();
        var fractionalHealth = fractionalTarget.AddComponent<Health>();
        fractionalHealth.ConfigureStartingHealth(10);
        var projectilesBefore = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
        Invoke(fractionalController, "Fire", State(fractionalController, termo), fractionalHealth);
        var fractionalShot = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Except(projectilesBefore).Single();
        Invoke(fractionalShot, "OnTriggerEnter2D", fractionalTarget.GetComponent<Collider2D>());
        Require(Mathf.Approximately((float)Field(fractionalHealth, "remainingHealth"), 8.7f) && fractionalController.Damage == 1,
            "Fractional damage rounded before Health.");
        Object.DestroyImmediate(fractionalTarget);
    }

    private static async Task CheckSelection()
    {
        await FreshMain(CharacterData.All[0]);
        var controller = Object.FindFirstObjectByType<WeaponController>();
        var selection = Object.FindFirstObjectByType<UpgradeSelection>();
        var card = AssetDatabase.LoadAssetAtPath<UpgradeData>(Root + "Data/Chimichurri Casero.asset");
        var deckField = typeof(UpgradeSelection).GetField("upgrades", Flags);
        deckField.SetValue(selection, new[] { card });
        controller.GetComponent<ExperienceSystem>().AddExperience(26);
        Require(selection.IsShowing && (int)Field(selection, "pendingSelections") == 3 && Time.timeScale == 0f, "XP queue.");
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit1));
            InputSystem.Update();
            keyboard.MakeCurrent();
            Invoke(selection, "Update");
            selection.Select(0); // Same entry used by the mouse, in the keyboard's frame.
            Invoke(selection, "Update");
            Require(selection.GetLevel(card) == 1 && (int)Field(selection, "pendingSelections") == 2 && Time.timeScale == 0f,
                "Same-frame input consumed multiple queued selections.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.PauseGame();
            selection.Select(0);
            Require(selection.GetLevel(card) == 1, "Selected during pause.");
            game.ContinueGame();
            Require(Time.timeScale == 0f, "Pause lost queued choice.");
            await NextFrame();
            selection.Select(0);
            await NextFrame();
            selection.Select(0);
            Require(selection.GetLevel(card) == 3 && !selection.IsShowing && Time.timeScale == 1f, "Queue did not resume.");
        }
        finally { InputSystem.RemoveDevice(keyboard); }
        await FreshMain(CharacterData.All[0]);
        controller = Object.FindFirstObjectByType<WeaponController>();
        selection = Object.FindFirstObjectByType<UpgradeSelection>();
        var termo = LoadWeapon("Termo Balístico");
        card = AssetDatabase.LoadAssetAtPath<UpgradeData>(Root + "Data/Termo Balístico - Daño.asset");
        deckField.SetValue(selection, new[] { card });
        controller.GetComponent<ExperienceSystem>().AddExperience(5);
        for (int i = 0; i < 7; i++) card.Apply(controller.GetComponent<PlayerController>(), controller, controller.GetComponent<Health>());
        float damage = controller.Damage;
        selection.Select(0);
        Require(controller.GetWeaponLevel(termo) == 8 && controller.Damage == damage && selection.GetLevel(card) == 0 &&
            selection.IsFallback && Time.timeScale == 0f, "Stale choice applied after weapon reached cap.");
        await NextFrame();
        selection.Select(0);
        Require(!selection.IsShowing && Time.timeScale == 1f, "Stale choice lost fallback.");
    }

    private static async Task SelectCard(UpgradeData card)
    {
        var selection = Object.FindFirstObjectByType<UpgradeSelection>();
        var field = typeof(UpgradeSelection).GetField("upgrades", Flags);
        object original = field.GetValue(selection);
        try
        {
            field.SetValue(selection, new[] { card });
            var xp = Object.FindFirstObjectByType<ExperienceSystem>();
            xp.AddExperience(xp.RequiredExperience - xp.CurrentExperience);
            Require(selection.Options.Count == 1 && selection.Options[0] == card, "Unavailable card " + card.name);
            selection.Select(0);
            await NextFrame();
        }
        finally { field.SetValue(selection, original); }
    }

    private static async Task RequireNoOffer(UpgradeSelection selection, UpgradeData card)
    {
        var field = typeof(UpgradeSelection).GetField("upgrades", Flags);
        object original = field.GetValue(selection);
        try
        {
            field.SetValue(selection, new[] { card });
            var xp = Object.FindFirstObjectByType<ExperienceSystem>();
            xp.AddExperience(xp.RequiredExperience - xp.CurrentExperience);
            Require(selection.IsFallback && selection.Options.Count == 0 && Time.timeScale == 0f, "Exhausted card missing fallback.");
            selection.Select(0);
            await NextFrame();
            Require(!selection.IsShowing && Time.timeScale == 1f, "Exhausted card blocked play.");
        }
        finally { field.SetValue(selection, original); }
    }

    private static async Task FreshMain(CharacterData character)
    {
        CharacterData.Select(character);
        SceneManager.LoadScene("Main");
        await Task.Delay(150);
        Object.FindFirstObjectByType<WaveDirector>().enabled = false;
        Object.FindFirstObjectByType<WeaponController>().enabled = false;
    }
    private static WeaponData LoadWeapon(string name) => AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Data/" + name + ".asset");
    private static object State(WeaponController controller, WeaponData data) => ((IEnumerable)Field(controller, "equipped")).Cast<object>()
        .Single(s => (WeaponData)s.GetType().GetField("Data").GetValue(s) == data);
    private static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    private static object Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
    private static float Stat(WeaponController controller, string name, object state) => (float)Invoke(controller, name, state);
    private static async Task NextFrame() { int frame = Time.frameCount; await Until(() => Time.frameCount > frame); }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task Until(Func<bool> condition)
    {
        double deadline = EditorApplication.timeSinceStartup + 30;
        while (!condition())
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Build state check timed out.");
            await Task.Delay(10);
        }
    }
    private static void OnLog(string message, string trace, LogType type)
    {
        if (message.StartsWith("Hito 6: opciones únicas")) hito6Completed = true;
        if (type == LogType.Exception && trace.Contains("UnityEditor.Search.SearchDatabase")) { searchErrors++; return; }
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
    }
}
