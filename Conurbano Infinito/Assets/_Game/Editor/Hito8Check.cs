using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class Hito8Check
{
    [MenuItem("Tools/Conurbano Infinito/Comprobar hito 8")]
    public static void Run()
    {
        if (!Application.isPlaying)
        {
            throw new System.Exception("Iniciá Play Mode en Main antes de comprobar el hito 8.");
        }

        WeaponController controller = Object.FindFirstObjectByType<WeaponController>();
        PlayerController movement = Object.FindFirstObjectByType<PlayerController>();
        GameManager game = Object.FindFirstObjectByType<GameManager>();
        if (controller == null || movement == null || game == null ||
            game.State != GameManager.GameState.Playing || Time.timeScale != 1f)
        {
            throw new System.Exception("Main debe estar en juego y sin pausa.");
        }

        Health playerHealth = movement.GetComponent<Health>();
        WeaponData termo = LoadWeapon("Termo Balístico", WeaponPattern.Straight);
        if (!controller.IsEquipped(termo) || controller.EquippedWeaponCount != 1)
        {
            throw new System.Exception("Main debe comenzar con solo Termo Balístico equipado.");
        }
        if (controller.CanEquipWeapon(null) || controller.EquipWeapon(null) ||
            controller.CanEquipWeapon(termo) || controller.EquipWeapon(termo) ||
            controller.EquippedWeaponCount != 1)
        {
            throw new System.Exception("Se aceptó un arma nula o duplicada con espacios libres.");
        }

        string[] names = { "Boleadora", "Chancleta Teledirigida", "Fernets Orbitales" };
        WeaponPattern[] patterns =
            { WeaponPattern.Boleadora, WeaponPattern.Returning, WeaponPattern.Orbiting };
        for (int i = 0; i < names.Length; i++)
        {
            WeaponData data = LoadWeapon(names[i], patterns[i]);
            UpgradeData acquisition = LoadUpgrade("Equipar " + names[i], data, UpgradeEffect.EquipWeapon);
            string thirdStat = i == 2 ? "Giro" : "Cadencia";
            UpgradeEffect thirdEffect = i == 2 ? UpgradeEffect.WeaponProjectileSpeed :
                UpgradeEffect.WeaponFireInterval;
            UpgradeData damage = LoadUpgrade(names[i] + " - Daño", data, UpgradeEffect.WeaponDamage);
            UpgradeData rate = LoadUpgrade(names[i] + " - " + thirdStat, data, thirdEffect);
            UpgradeData count = LoadUpgrade(names[i] + " - Cantidad", data,
                UpgradeEffect.WeaponProjectileCount);
            if (!acquisition.CanOffer(controller) || damage.CanOffer(controller) ||
                rate.CanOffer(controller) || count.CanOffer(controller))
            {
                throw new System.Exception("Las cartas de " + names[i] + " no respetan el equipo inicial.");
            }

            acquisition.Apply(movement, controller, playerHealth);
            if (controller.EquippedWeaponCount != i + 2 ||
                !controller.IsEquipped(data) || acquisition.CanOffer(controller) ||
                !damage.CanOffer(controller) || !rate.CanOffer(controller) || !count.CanOffer(controller))
            {
                throw new System.Exception("La adquisición de " + names[i] + " no habilitó sus estadísticas.");
            }
        }

        LoadUpgrade("Termo Balístico - Daño", termo, UpgradeEffect.WeaponDamage);
        LoadUpgrade("Termo Balístico - Cadencia", termo, UpgradeEffect.WeaponFireInterval);
        LoadUpgrade("Termo Balístico - Cantidad", termo, UpgradeEffect.WeaponProjectileCount);

        var enemy = new GameObject("Hito 8 enemy");
        enemy.layer = LayerMask.NameToLayer("Enemy");
        enemy.transform.position = movement.transform.position + Vector3.right;
        enemy.AddComponent<Health>();
        enemy.AddComponent<CircleCollider2D>();
        try
        {
            var states = (IEnumerable)typeof(WeaponController)
                .GetField("equipped", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(controller);
            foreach (object state in states)
            {
                state.GetType().GetField("NextAttackTime").SetValue(state, 0f);
            }

            int before = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;
            typeof(WeaponController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
            int after = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;
            if (after - before < 4)
            {
                throw new System.Exception("Las cuatro armas no generaron sus proyectiles automáticamente.");
            }
        }
        finally
        {
            Object.Destroy(enemy);
        }

        CheckProjectileContacts(controller, movement.transform);
        CheckWeaponLimit(controller, movement, playerHealth, termo);

        Debug.Log("Hito 8: adquisición, estadísticas, disparo automático, contactos y límite de cuatro armas correctos.");
    }

    private static void CheckWeaponLimit(WeaponController controller, PlayerController movement,
        Health playerHealth, WeaponData termo)
    {
        UpgradeSelection selection = Object.FindFirstObjectByType<UpgradeSelection>();
        ExperienceSystem experience = movement.GetComponent<ExperienceSystem>();
        if (selection == null || experience == null)
            throw new System.Exception("Falta la selección de mejoras o la experiencia.");

        var extraWeapon = ScriptableObject.CreateInstance<WeaponData>();
        var acquisition = ScriptableObject.CreateInstance<UpgradeData>();
        FieldInfo upgradesField = typeof(UpgradeSelection).GetField("upgrades",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var originalUpgrades = (UpgradeData[])upgradesField.GetValue(selection);
        UpgradeData passive = null;
        UpgradeData damage = null;
        try
        {
            passive = AssetDatabase.LoadAssetAtPath<UpgradeData>(
                "Assets/_Game/Data/Chimichurri Casero.asset");
            damage = LoadUpgrade("Termo Balístico - Daño", termo, UpgradeEffect.WeaponDamage);
            var serializedAcquisition = new SerializedObject(acquisition);
            serializedAcquisition.FindProperty("effect").enumValueIndex = (int)UpgradeEffect.EquipWeapon;
            serializedAcquisition.FindProperty("targetWeapon").objectReferenceValue = extraWeapon;
            serializedAcquisition.ApplyModifiedPropertiesWithoutUndo();

            if (WeaponController.MaxEquippedWeapons != 4 || controller.EquippedWeaponCount != 4 ||
                controller.CanEquipWeapon(extraWeapon) || controller.EquipWeapon(extraWeapon) ||
                acquisition.CanOffer(controller))
                throw new System.Exception("Se permitió equipar u ofrecer una quinta arma.");

            acquisition.Apply(movement, controller, playerHealth);
            if (controller.EquippedWeaponCount != 4 || controller.IsEquipped(extraWeapon))
                throw new System.Exception("Aplicar una adquisición saltó el límite de cuatro armas.");

            if (passive == null || !passive.CanOffer(controller) || !damage.CanOffer(controller))
                throw new System.Exception("El límite bloqueó una pasiva o una mejora del arma equipada.");

            upgradesField.SetValue(selection, new[] { acquisition, passive, damage });
            experience.AddExperience(experience.RequiredExperience - experience.CurrentExperience);
            if (!selection.IsShowing || Time.timeScale != 0f || selection.Options.Count != 2 ||
                !((selection.Options[0] == passive && selection.Options[1] == damage) ||
                  (selection.Options[0] == damage && selection.Options[1] == passive)))
                throw new System.Exception("La selección no filtró la quinta arma conservando las mejoras válidas.");

            selection.Select(0);
            if (selection.IsShowing || selection.Options.Count != 0 || Time.timeScale != 1f ||
                controller.EquippedWeaponCount != 4)
                throw new System.Exception("Elegir una mejora válida no reanudó la partida con cuatro armas.");
        }
        finally
        {
            // Cerrar también la selección si una comprobación falló antes de elegir.
            if (selection.IsShowing)
                for (int i = 0; i < selection.Options.Count; i++)
                    if (selection.Options[i] == passive || selection.Options[i] == damage)
                    {
                        selection.Select(i);
                        break;
                    }
            upgradesField.SetValue(selection, originalUpgrades);
            Object.Destroy(acquisition);
            Object.Destroy(extraWeapon);
        }
    }

    private static void CheckProjectileContacts(WeaponController controller, Transform owner)
    {
        var prefab = new SerializedObject(controller).FindProperty("projectilePrefab")
            .objectReferenceValue as Projectile;
        if (prefab == null)
        {
            throw new System.Exception("Falta el prefab de proyectil.");
        }

        var first = new GameObject("Hito 8 first target");
        var second = new GameObject("Hito 8 second target");
        first.layer = LayerMask.NameToLayer("Enemy");
        second.layer = first.layer;
        Health firstHealth = first.AddComponent<Health>();
        Health secondHealth = second.AddComponent<Health>();
        Collider2D firstCollider = first.AddComponent<CircleCollider2D>();
        Collider2D secondCollider = second.AddComponent<CircleCollider2D>();
        Projectile ball = null;
        Projectile shoe = null;
        Projectile orbit = null;
        try
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo enter = typeof(Projectile).GetMethod("OnTriggerEnter2D", flags);
            MethodInfo exit = typeof(Projectile).GetMethod("OnTriggerExit2D", flags);
            MethodInfo tick = typeof(Projectile).GetMethod("FixedUpdate", flags);
            WeaponData boleadora = LoadWeapon("Boleadora", WeaponPattern.Boleadora);
            ball = Object.Instantiate(prefab);
            ball.Initialize(Vector2.right, 1, boleadora.ProjectileSpeed,
                boleadora.Pierce, boleadora.Range);
            enter.Invoke(ball, new object[] { firstCollider });
            enter.Invoke(ball, new object[] { firstCollider });
            enter.Invoke(ball, new object[] { secondCollider });
            if (firstHealth.CurrentHealth != 4 || secondHealth.CurrentHealth != 4)
            {
                throw new System.Exception("La Boleadora no atraviesa dos enemigos sin repetir daño.");
            }

            WeaponData chancleta = LoadWeapon("Chancleta Teledirigida", WeaponPattern.Returning);
            shoe = Object.Instantiate(prefab);
            shoe.InitializeReturning(owner, Vector2.right, 1, chancleta.ProjectileSpeed, chancleta.Range);
            enter.Invoke(shoe, new object[] { firstCollider });
            shoe.GetComponent<Rigidbody2D>().position = (Vector2)owner.position +
                Vector2.right * (chancleta.Range + 0.2f);
            tick.Invoke(shoe, null);
            enter.Invoke(shoe, new object[] { firstCollider });
            if (firstHealth.CurrentHealth != 2)
            {
                throw new System.Exception("La Chancleta no dañó en la ida y en la vuelta.");
            }

            WeaponData fernets = LoadWeapon("Fernets Orbitales", WeaponPattern.Orbiting);
            orbit = Object.Instantiate(prefab);
            orbit.InitializeOrbiting(owner, 0f, 1, fernets.Range,
                fernets.ProjectileSpeed, fernets.FireInterval);
            enter.Invoke(orbit, new object[] { firstCollider });
            enter.Invoke(orbit, new object[] { firstCollider });
            if (firstHealth.CurrentHealth != 1)
            {
                throw new System.Exception("El Fernet dañó dos veces durante el mismo contacto.");
            }
            exit.Invoke(orbit, new object[] { firstCollider });
            enter.Invoke(orbit, new object[] { firstCollider });
            if (firstHealth.CurrentHealth != 0)
            {
                throw new System.Exception("El Fernet no volvió a dañar en un contacto nuevo.");
            }
        }
        finally
        {
            if (ball != null) Object.Destroy(ball.gameObject);
            if (shoe != null) Object.Destroy(shoe.gameObject);
            if (orbit != null) Object.Destroy(orbit.gameObject);
            Object.Destroy(first);
            Object.Destroy(second);
        }
    }

    private static WeaponData LoadWeapon(string name, WeaponPattern pattern)
    {
        WeaponData data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/" + name + ".asset");
        if (data == null || data.Pattern != pattern || data.ProjectileCount < 1 ||
            pattern == WeaponPattern.Boleadora && (data.ProjectileCount < 2 || data.Pierce < 1))
        {
            throw new System.Exception("Falta configurar " + name + ".");
        }
        return data;
    }

    private static UpgradeData LoadUpgrade(string name, WeaponData weapon, UpgradeEffect effect)
    {
        UpgradeData data = AssetDatabase.LoadAssetAtPath<UpgradeData>("Assets/_Game/Data/" + name + ".asset");
        if (data == null || data.TargetWeapon != weapon || data.Effect != effect)
        {
            throw new System.Exception("Falta configurar " + name + ".");
        }
        return data;
    }
}
