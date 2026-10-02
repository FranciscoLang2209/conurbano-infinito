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
        if (!controller.IsEquipped(termo))
        {
            throw new System.Exception("Termo Balístico no está equipado al iniciar.");
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
            if (!controller.IsEquipped(data) || acquisition.CanOffer(controller) ||
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

        Debug.Log("Hito 8: adquisición, estadísticas, disparo automático y contactos de las tres armas correctos.");
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
