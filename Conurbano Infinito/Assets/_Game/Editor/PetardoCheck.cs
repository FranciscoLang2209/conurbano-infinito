using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class PetardoCheck
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string Root = "Assets/_Game/Data/";

    [MenuItem("Tools/Conurbano Infinito/Comprobar Petardo en Play Mode")]
    public static async void Run() => await RunChecks();

    public static async Task RunChecks()
    {
        if (!Application.isPlaying || Time.timeScale != 1f)
            throw new Exception("Iniciá una partida nueva en Main, sin pausa.");
        WeaponController controller = Object.FindFirstObjectByType<WeaponController>();
        UpgradeSelection selection = Object.FindFirstObjectByType<UpgradeSelection>();
        ObjectPool pool = Object.FindFirstObjectByType<ObjectPool>();
        WeaponData weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Petardo.asset");
        if (controller == null || selection == null || pool == null || weapon == null ||
            weapon.Pattern != WeaponPattern.Explosive || weapon.ProjectileSprite == null ||
            controller.EquippedWeaponCount != 1)
            throw new Exception("Falta integrar el Petardo o la partida no está recién iniciada.");

        UpgradeData[] cards = {
            LoadCard("Equipar Petardo", weapon, UpgradeEffect.EquipWeapon),
            LoadCard("Petardo - Daño", weapon, UpgradeEffect.WeaponDamage),
            LoadCard("Petardo - Radio", weapon, UpgradeEffect.WeaponExplosionRadius),
            LoadCard("Petardo - Cadencia", weapon, UpgradeEffect.WeaponFireInterval)
        };
        FieldInfo deck = typeof(UpgradeSelection).GetField("upgrades", Flags);
        var originalDeck = (UpgradeData[])deck.GetValue(selection);
        foreach (UpgradeData card in cards)
            if (Array.IndexOf(originalDeck, card) < 0)
                throw new Exception("Una carta del Petardo no está en Main.");
        if (!cards[0].CanOffer(controller) || cards[1].CanOffer(controller) ||
            cards[2].CanOffer(controller) || cards[3].CanOffer(controller))
            throw new Exception("Las mejoras del Petardo aparecen antes de equiparlo.");

        var targets = new List<GameObject>();
        Projectile shot = null;
        try
        {
            await SelectCard(selection, cards[0], deck);
            if (!controller.IsEquipped(weapon) || cards[0].CanOffer(controller))
                throw new Exception("La adquisición no equipó el Petardo una sola vez.");
            Vector2 center = (Vector2)controller.transform.position + new Vector2(200, 200);
            Health first = Target(targets, center, "Enemy");
            Health inside = Target(targets, center + Vector2.right * 1.4f, "Enemy");
            inside.gameObject.AddComponent<CircleCollider2D>().radius = 0.05f;
            Health outside = Target(targets, center + Vector2.right * 1.7f, "Enemy");
            Health friendly = Target(targets, center, "Player");
            Physics2D.SyncTransforms();

            shot = Fire(controller, weapon, first);
            shot.GetComponent<Rigidbody2D>().position = center;
            Tick(shot);
            if (first.CurrentHealth != 3 || inside.CurrentHealth != 3 ||
                outside.CurrentHealth != 5 || friendly.CurrentHealth != 5 ||
                shot.GetComponent<Rigidbody2D>().simulated)
                throw new Exception("La explosión no respeta daño, radio, duplicados o capa Enemy.");
            Contact(shot, first.GetComponent<Collider2D>());
            Tick(shot);
            if (first.CurrentHealth != 3 || inside.CurrentHealth != 3)
                throw new Exception("La explosión repitió daño durante el efecto.");
            Projectile original = shot;
            Finish(shot);
            if (shot.gameObject.activeSelf || !shot.GetComponent<Rigidbody2D>().simulated)
                throw new Exception("El efecto no terminó o no restauró la simulación del pool.");

            Projectile prefab = new SerializedObject(controller).FindProperty("projectilePrefab")
                .objectReferenceValue as Projectile;
            shot = pool.Spawn(prefab.gameObject, center, Quaternion.identity).GetComponent<Projectile>();
            WeaponData termo = AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Termo Balístico.asset");
            shot.SetAppearance(termo.ProjectileSprite, termo.ProjectileColor, termo.ProjectileScale);
            shot.Initialize(Vector2.right, 1f, 8f, 0, 6f);
            Contact(shot, first.GetComponent<Collider2D>());
            if (shot != original || first.CurrentHealth != 2 || inside.CurrentHealth != 3 ||
                shot.gameObject.activeSelf)
                throw new Exception("Reutilizar el Petardo como Termo conservó daño de área o el efecto.");

            first.ResetForReuse();
            inside.ResetForReuse();
            Physics2D.SyncTransforms();
            shot = Fire(controller, weapon, first);
            shot.GetComponent<Rigidbody2D>().position = center;
            Contact(shot, first.GetComponent<Collider2D>());
            Contact(shot, first.GetComponent<Collider2D>());
            if (first.CurrentHealth != 3 || inside.CurrentHealth != 3)
                throw new Exception("El impacto directo no explotó una sola vez tras reutilizar el pool.");
            Finish(shot);

            foreach (UpgradeData card in new[] { cards[1], cards[2], cards[3] })
                for (int rank = 0; rank < (card.Effect == UpgradeEffect.WeaponFireInterval ? 1 : card.MaximumLevel); rank++)
                    await SelectCard(selection, card, deck);
            var crowd = new List<Health>();
            for (int i = 0; i < 150; i++)
                crowd.Add(Target(targets, center + new Vector2(i % 15, i / 15) * 0.03f, "Enemy"));
            Physics2D.SyncTransforms();
            shot = Fire(controller, weapon, first);
            shot.GetComponent<Rigidbody2D>().position = center;
            Tick(shot);
            foreach (Health enemy in crowd)
                if (!enemy.IsDead) throw new Exception("La explosión omitió enemigos de un grupo de 150.");
            if (!outside.IsDead || friendly.CurrentHealth != 5 ||
                !Mathf.Approximately(shot.transform.localScale.x, 2f * 1.5f * Mathf.Pow(1.2f, 3)))
                throw new Exception("Las mejoras de daño y radio no llegaron a la explosión.");
            Finish(shot);

            // La selección debe dejar de ofrecer una mejora que llegó a su rango máximo.
            deck.SetValue(selection, new[] { cards[2] });
            ExperienceSystem xp = controller.GetComponent<ExperienceSystem>();
            xp.AddExperience(xp.RequiredExperience - xp.CurrentExperience);
            if (!selection.IsFallback || selection.Options.Count != 0 || Time.timeScale != 0f)
                throw new Exception("Una mejora agotada no ofrece continuar o curar.");
            int fallbackFrame = Time.frameCount;
            selection.Select(0);
            while (Time.frameCount == fallbackFrame) await Task.Delay(10);
            if (selection.IsShowing || Time.timeScale != 1f)
                throw new Exception("Una mejora agotada bloqueó la partida.");

            object state = State(controller, weapon);
            float interval = weapon.FireInterval * (float)state.GetType().GetField("IntervalMultiplier").GetValue(state);
            if (!Mathf.Approximately(interval, 3f * Mathf.Pow(0.9f, 1)))
                throw new Exception("La mejora de cadencia no redujo el intervalo.");
            controller.EquipWeapon(AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Boleadora.asset"));
            controller.EquipWeapon(AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Fernets Orbitales.asset"));
            WeaponData extra = AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Aerosol.asset");
            if (controller.EquippedWeaponCount != 4 || controller.CanEquipWeapon(extra) || controller.EquipWeapon(extra))
                throw new Exception("El Petardo saltó el límite de cuatro armas.");
            Debug.Log("Petardo: adquisición, daño/radio al máximo, cadencia rango 1 y arma nivel 8, impacto y destino, daño único, capas, radio, 150 enemigos y pool correctos.");
        }
        finally
        {
            deck.SetValue(selection, originalDeck);
            if (shot != null && shot.gameObject.activeSelf) ObjectPool.Release(shot.gameObject);
            foreach (GameObject target in targets) Object.Destroy(target);
        }
    }

    private static UpgradeData LoadCard(string name, WeaponData weapon, UpgradeEffect effect)
    {
        UpgradeData card = AssetDatabase.LoadAssetAtPath<UpgradeData>(Root + name + ".asset");
        if (card == null || card.TargetWeapon != weapon || card.Effect != effect)
            throw new Exception("Falta configurar " + name);
        return card;
    }

    private static async Task SelectCard(UpgradeSelection selection, UpgradeData card, FieldInfo deck)
    {
        deck.SetValue(selection, new[] { card });
        ExperienceSystem xp = Object.FindFirstObjectByType<PlayerController>().GetComponent<ExperienceSystem>();
        xp.AddExperience(xp.RequiredExperience - xp.CurrentExperience);
        if (!selection.IsShowing || selection.Options.Count != 1 || selection.Options[0] != card)
            throw new Exception("La selección no ofreció " + card.DisplayName);
        int frame = Time.frameCount;
        selection.Select(0);
        while (Time.frameCount == frame) await Task.Delay(10);
    }

    private static Health Target(List<GameObject> targets, Vector2 position, string layer)
    {
        var target = new GameObject("Petardo check: " + layer);
        targets.Add(target);
        target.layer = LayerMask.NameToLayer(layer);
        target.transform.position = position;
        target.AddComponent<CircleCollider2D>().radius = 0.05f;
        return target.AddComponent<Health>();
    }

    private static object State(WeaponController controller, WeaponData weapon)
    {
        foreach (object state in (IEnumerable)typeof(WeaponController).GetField("equipped", Flags).GetValue(controller))
            if ((WeaponData)state.GetType().GetField("Data").GetValue(state) == weapon) return state;
        throw new Exception("El Petardo no está equipado.");
    }

    private static Projectile Fire(WeaponController controller, WeaponData weapon, Health target)
    {
        var before = new HashSet<Projectile>(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None));
        typeof(WeaponController).GetMethod("Fire", Flags).Invoke(controller, new[] { State(controller, weapon), target });
        foreach (Projectile projectile in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            if (!before.Contains(projectile)) return projectile;
        throw new Exception("El Petardo no disparó.");
    }

    private static void Contact(Projectile shot, Collider2D target) =>
        typeof(Projectile).GetMethod("OnTriggerEnter2D", Flags).Invoke(shot, new object[] { target });

    private static void Tick(Projectile shot) => typeof(Projectile).GetMethod("FixedUpdate", Flags).Invoke(shot, null);

    private static void Finish(Projectile shot)
    {
        typeof(Projectile).GetField("explosionStartTime", Flags).SetValue(shot, Time.time - 1f);
        Tick(shot);
    }
}
