using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ExpansionCheck
{
    private const string Root = "Assets/_Game/";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Conurbano Infinito/Comprobar Aerosol y Perro en Play Mode")]
    public static void Run()
    {
        if (!Application.isPlaying || Time.timeScale != 1f)
            throw new Exception("Iniciá una partida nueva de Main sin pausa.");
        WeaponController controller = Object.FindFirstObjectByType<WeaponController>();
        UpgradeSelection selection = Object.FindFirstObjectByType<UpgradeSelection>();
        if (controller == null || selection == null || controller.EquippedWeaponCount != 1)
            throw new Exception("La comprobación necesita una partida nueva.");

        Vector3 originalPosition = controller.transform.position;
        SimulationMode2D simulation = Physics2D.simulationMode;
        FieldInfo deckField = typeof(UpgradeSelection).GetField("upgrades", Flags);
        var originalDeck = (UpgradeData[])deckField.GetValue(selection);
        var targets = new List<GameObject>();
        var shots = new List<Projectile>();
        GameObject dog = null;
        try
        {
            Physics2D.simulationMode = SimulationMode2D.Script;
            controller.transform.position = new Vector3(200, 200);
            Vector2 origin = controller.transform.position;
            WeaponData aerosol = AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Data/Aerosol.asset");
            UpgradeData equip = AssetDatabase.LoadAssetAtPath<UpgradeData>(Root + "Data/Equipar Aerosol.asset");
            UpgradeData[] upgrades = {
                AssetDatabase.LoadAssetAtPath<UpgradeData>(Root + "Data/Aerosol - Daño.asset"),
                AssetDatabase.LoadAssetAtPath<UpgradeData>(Root + "Data/Aerosol - Cadencia.asset"),
                AssetDatabase.LoadAssetAtPath<UpgradeData>(Root + "Data/Aerosol - Cobertura.asset")
            };
            Require(aerosol != null && aerosol.ProjectileSprite != null && equip != null && equip.Icon != null &&
                Array.IndexOf(originalDeck, equip) >= 0 && equip.CanOffer(controller), "Falta integrar Aerosol.");
            foreach (UpgradeData card in upgrades)
                Require(card != null && Array.IndexOf(originalDeck, card) >= 0 && !card.CanOffer(controller),
                    "Mejora ausente o disponible antes de adquirir Aerosol.");
            Select(selection, equip, deckField);
            Require(controller.IsEquipped(aerosol) && !equip.CanOffer(controller), "La adquisición falló.");

            Health near = Target(targets, origin + Vector2.right, "Enemy");
            Health behindNear = Target(targets, origin + Vector2.right * 2f, "Enemy");
            Health edge = Target(targets, origin + new Vector2(1.64f, 1.15f), "Enemy");
            Health outside = Target(targets, origin + Vector2.up * 2f, "Enemy");
            Health distant = Target(targets, origin + Vector2.right * 4f, "Enemy");
            Health friendly = Target(targets, origin + Vector2.right * 1.5f, "Player");
            near.gameObject.AddComponent<CircleCollider2D>().radius = 0.12f;
            Physics2D.SyncTransforms();
            Fire(controller, aerosol, behindNear, shots);
            Require(shots.Count == 5, "La ráfaga inicial no tiene cinco nubes.");
            SimulateShots(shots);
            Require(near.CurrentHealth < 45 && behindNear.CurrentHealth < 45 && edge.CurrentHealth < 45,
                "El abanico no alcanzó el borde o no perforó el grupo.");
            Require(near.CurrentHealth >= 40 && outside.CurrentHealth == 45 &&
                distant.CurrentHealth == 45 && friendly.CurrentHealth == 45,
                "El Aerosol repitió daño por collider o dañó fuera de alcance/capa.");
            foreach (Projectile shot in shots) Require(!shot.gameObject.activeSelf, "Una nube no volvió al pool.");

            foreach (UpgradeData card in upgrades)
            {
                for (int rank = 0; rank < card.MaximumLevel; rank++) Select(selection, card, deckField);
                deckField.SetValue(selection, new[] { card });
                GainLevel(controller);
                Require(!selection.IsShowing && Time.timeScale == 1f, "Una mejora agotada bloquea la partida.");
            }
            near.ResetForReuse();
            near.IncreaseMaximumHealth(40);
            behindNear.ResetForReuse();
            behindNear.IncreaseMaximumHealth(40);
            shots.Clear();
            Fire(controller, aerosol, behindNear, shots);
            Require(shots.Count == 8, "Cobertura no añadió tres nubes.");
            Collider2D collider = near.GetComponent<Collider2D>();
            Invoke(shots[0], "OnTriggerEnter2D", collider);
            Invoke(shots[0], "OnTriggerEnter2D", collider);
            Require(near.CurrentHealth == 41, "Daño o protección contra impactos duplicados incorrectos.");
            object state = State(controller, aerosol);
            Require(Mathf.Approximately((float)state.GetType().GetField("IntervalMultiplier").GetValue(state),
                Mathf.Pow(0.9f, 3)), "Cadencia no se aplicó.");
            ObjectPool pool = Object.FindFirstObjectByType<ObjectPool>();
            Projectile reused = shots[0];
            ObjectPool.Release(reused.gameObject);
            Projectile prefab = (Projectile)new SerializedObject(controller).FindProperty("projectilePrefab").objectReferenceValue;
            Projectile termo = pool.Spawn(prefab.gameObject, origin, Quaternion.identity).GetComponent<Projectile>();
            termo.Initialize(Vector2.right, 1f, 8f, 0, 6f);
            Invoke(termo, "OnTriggerEnter2D", collider);
            Require(termo == reused && near.CurrentHealth == 40 && !termo.gameObject.activeSelf,
                "El pool conservó perforación o blancos del Aerosol al reutilizarlo como Termo.");

            GameObject dogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/PerroCallejero.prefab");
            WaveData waves = AssetDatabase.LoadAssetAtPath<WaveData>(Root + "Data/Oleadas.asset");
            foreach (WaveData.Phase phase in waves.Phases)
            {
                bool included = Array.Exists(phase.enemies, option => option.prefab == dogPrefab && option.weight > 0);
                Require(included == (phase.startSeconds >= 60f), "El perro no respeta las fases desde el minuto 1.");
            }
            Health dogHealth = Object.FindFirstObjectByType<SpawnManager>().Spawn(dogPrefab);
            dog = dogHealth.gameObject;
            EnemyCharge charge = dog.GetComponent<EnemyCharge>();
            var body = dog.GetComponent<Rigidbody2D>();
            body.position = origin + Vector2.up * 10f;
            Health bait = Target(targets, body.position + Vector2.right * 3f, "Player");
            charge.SetTarget(bait.transform);
            Vector2 start = body.position;
            Set(charge, "nextChargeTime", Time.time - 1f);
            Invoke(charge, "FixedUpdate");
            SpriteRenderer warning = dog.transform.Find("Aviso de embestida").GetComponent<SpriteRenderer>();
            Physics2D.Simulate(Time.fixedDeltaTime);
            Require(warning.gameObject.activeSelf && body.position == start, "Falta aviso o el perro se mueve durante él.");
            bait.transform.position += Vector3.up * 3f;
            Set(charge, "phaseEndTime", Time.time - 1f);
            Invoke(charge, "FixedUpdate");
            Physics2D.Simulate(Time.fixedDeltaTime);
            Require(!warning.gameObject.activeSelf && body.position.x > start.x &&
                Mathf.Approximately(body.position.y, start.y), "La embestida siguió al jugador después del aviso.");
            Set(charge, "phaseEndTime", Time.time - 1f);
            Invoke(charge, "FixedUpdate");
            Physics2D.Simulate(Time.fixedDeltaTime);
            Vector2 recovery = body.position;
            Transform recoveryIndicator = dog.transform.Find("Ventana de contraataque");
            Require(recoveryIndicator != null && recoveryIndicator.gameObject.activeSelf,
                "El descanso del perro no muestra la ventana de contraataque.");
            Invoke(charge, "FixedUpdate");
            Physics2D.Simulate(Time.fixedDeltaTime);
            Require(body.position == recovery, "El perro no descansa después de embestir.");
            Time.timeScale = 0f;
            Set(charge, "phaseEndTime", Time.time - 1f);
            Invoke(charge, "FixedUpdate");
            Require(body.position == recovery && !warning.gameObject.activeSelf, "La pausa avanzó la embestida.");
            Time.timeScale = 1f;
            ObjectPool.Release(dog);
            dog = Object.FindFirstObjectByType<SpawnManager>().Spawn(dogPrefab).gameObject;
            Require(dog == dogHealth.gameObject && dogHealth.CurrentHealth == 8 &&
                !warning.gameObject.activeSelf && !recoveryIndicator.gameObject.activeSelf,
                "El perro no se reinició al salir del pool.");
            body.position = start;
            bait.transform.position = start + Vector2.right * 3f;
            charge.SetTarget(bait.transform);
            Set(charge, "nextChargeTime", Time.time - 1f);
            Invoke(charge, "FixedUpdate");
            Require(warning.gameObject.activeSelf, "El perro reutilizado no volvió a atacar.");
            bait.TakeDamage(100);
            Invoke(charge, "FixedUpdate");
            Require(!warning.gameObject.activeSelf, "La muerte del blanco no canceló el aviso.");
            int gems = Object.FindObjectsByType<ExperienceGem>(FindObjectsSortMode.None).Length;
            dogHealth.TakeDamage(100);
            Invoke(charge, "FixedUpdate");
            Require(!warning.gameObject.activeSelf && dogHealth.IsDead &&
                Object.FindObjectsByType<ExperienceGem>(FindObjectsSortMode.None).Length > gems,
                "El perro no murió o no soltó experiencia.");
            Debug.Log("EXPANSION_CHECK_PASS: Aerosol, adquisición, mejoras, alcance, perforación, capas, pool; perro, fases, aviso, dirección fija, descanso, pausa, reuso y drop.");
        }
        finally
        {
            Time.timeScale = 1f;
            Physics2D.simulationMode = simulation;
            controller.transform.position = originalPosition;
            deckField.SetValue(selection, originalDeck);
            foreach (Projectile shot in shots)
                if (shot != null && shot.gameObject.activeSelf) ObjectPool.Release(shot.gameObject);
            if (dog != null && dog.activeSelf) ObjectPool.Release(dog);
            foreach (GameObject target in targets) Object.Destroy(target);
            Physics2D.SyncTransforms();
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, Flags).SetValue(target, value);

    private static void Invoke(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, Flags).Invoke(target, args);

    private static void GainLevel(WeaponController controller)
    {
        ExperienceSystem xp = controller.GetComponent<ExperienceSystem>();
        xp.AddExperience(xp.RequiredExperience - xp.CurrentExperience);
    }

    private static void Select(UpgradeSelection selection, UpgradeData card, FieldInfo deck)
    {
        deck.SetValue(selection, new[] { card });
        GainLevel(Object.FindFirstObjectByType<WeaponController>());
        Require(selection.IsShowing && selection.Options.Count == 1 && selection.Options[0] == card,
            "La selección no ofreció " + card.DisplayName);
        selection.Select(0);
    }

    private static Health Target(List<GameObject> targets, Vector2 position, string layer)
    {
        var target = new GameObject("Expansion check: " + layer);
        targets.Add(target);
        target.layer = LayerMask.NameToLayer(layer);
        target.transform.position = position;
        target.AddComponent<CircleCollider2D>().radius = 0.12f;
        var health = target.AddComponent<Health>();
        health.IncreaseMaximumHealth(40);
        return health;
    }

    private static object State(WeaponController controller, WeaponData weapon)
    {
        foreach (object state in (IEnumerable)typeof(WeaponController).GetField("equipped", Flags).GetValue(controller))
            if ((WeaponData)state.GetType().GetField("Data").GetValue(state) == weapon) return state;
        throw new Exception("Arma sin equipar.");
    }

    private static void Fire(WeaponController controller, WeaponData weapon, Health target, List<Projectile> shots)
    {
        var before = new HashSet<Projectile>(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None));
        Invoke(controller, "Fire", State(controller, weapon), target);
        foreach (Projectile shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            if (!before.Contains(shot)) shots.Add(shot);
    }

    private static void SimulateShots(List<Projectile> shots)
    {
        for (int step = 0; step < 40; step++)
        {
            foreach (Projectile shot in shots)
                if (shot.gameObject.activeSelf) Invoke(shot, "FixedUpdate");
            Physics2D.Simulate(Time.fixedDeltaTime);
        }
    }
}
