using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class Hito3Check
{
    [MenuItem("Tools/Conurbano Infinito/Comprobar hito 3")]
    public static void Run()
    {
        var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Player.prefab");
        var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/TermoProjectile.prefab");
        var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Termo Balístico.asset");
        var controller = player != null ? player.GetComponent<WeaponController>() : null;
        if (controller == null || projectilePrefab == null ||
            projectilePrefab.layer != LayerMask.NameToLayer("Projectile") ||
            projectilePrefab.GetComponent<Projectile>() == null ||
            !projectilePrefab.GetComponent<Collider2D>().isTrigger || weapon == null)
        {
            throw new System.Exception("Falta conectar el arma o el proyectil del hito 3.");
        }
        var settings = new SerializedObject(controller);
        if (settings.FindProperty("weapon").objectReferenceValue != weapon ||
            settings.FindProperty("projectilePrefab").objectReferenceValue !=
                projectilePrefab.GetComponent<Projectile>())
        {
            throw new System.Exception("El jugador no tiene las referencias del Termo Balístico.");
        }

        var projectileObject = Object.Instantiate(projectilePrefab);
        var enemy = new GameObject("Enemy check");
        try
        {
            enemy.layer = LayerMask.NameToLayer("Enemy");
            var health = enemy.AddComponent<Health>();
            typeof(Health).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(health, null);
            var collider = enemy.AddComponent<CircleCollider2D>();
            var projectile = projectileObject.GetComponent<Projectile>();
            typeof(Projectile).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(projectile, null);
            projectile.Initialize(Vector2.right, 2, 8f, 1, 6f);
            var hit = typeof(Projectile).GetMethod("OnTriggerEnter2D",
                BindingFlags.Instance | BindingFlags.NonPublic);
            hit.Invoke(projectile, new object[] { collider });
            hit.Invoke(projectile, new object[] { collider });
            if (health.CurrentHealth != 3)
            {
                throw new System.Exception("El proyectil no aplica el daño correcto una sola vez por enemigo.");
            }

            Debug.Log("Hito 3: referencias y daño de proyectil correctos.");
        }
        finally
        {
            Object.DestroyImmediate(projectileObject);
            Object.DestroyImmediate(enemy);
        }
    }
}
