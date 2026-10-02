using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class Hito10Check
{
    [MenuItem("Tools/Conurbano Infinito/Comprobar hito 10")]
    public static async void Run()
    {
        if (!Application.isPlaying) throw new System.Exception("Iniciá Play Mode en Main.");

        ObjectPool pool = Object.FindFirstObjectByType<ObjectPool>();
        if (pool == null || Object.FindFirstObjectByType<PerformanceOverlay>() == null ||
            !Physics2D.GetIgnoreLayerCollision(LayerMask.NameToLayer("Enemy"),
                LayerMask.NameToLayer("Enemy")))
        {
            throw new System.Exception("Falta el pool, el medidor o Enemy-Enemy sigue activo.");
        }

        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Game/Prefabs/MosquitoGigante.prefab");
        GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Game/Prefabs/TermoProjectile.prefab");
        GameObject gemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Game/Prefabs/ExperienceGem.prefab");
        if (enemyPrefab == null || projectilePrefab == null || gemPrefab == null)
            throw new System.Exception("Faltan prefabs de prueba.");

        Vector3 farAway = new(1000f, 1000f, 0f);
        GameObject enemy = null;
        GameObject projectile = null;
        GameObject gem = null;
        try
        {
            enemy = pool.Spawn(enemyPrefab, farAway, Quaternion.identity);
            Health health = enemy.GetComponent<Health>();
            health.TakeDamage(health.CurrentHealth);
            if (!health.IsDead || !enemy.activeSelf)
                throw new Exception("El enemigo no inició su salida visual.");
            await Task.Delay(300);
            if (enemy.activeSelf) throw new Exception("El enemigo muerto no volvió al pool.");

            foreach (ExperienceGem candidate in Object.FindObjectsByType<ExperienceGem>(
                FindObjectsSortMode.None))
            {
                if (candidate.transform.position == farAway) gem = candidate.gameObject;
            }
            if (gem == null) throw new System.Exception("El enemigo no soltó la gema.");

            GameObject reusedEnemy = pool.Spawn(enemyPrefab, farAway, Quaternion.identity);
            if (reusedEnemy != enemy || !health.gameObject.activeSelf ||
                health.CurrentHealth != health.MaximumHealth)
                throw new System.Exception("El enemigo no se reutilizó con vida completa.");

            projectile = pool.Spawn(projectilePrefab, farAway, Quaternion.identity);
            projectile.GetComponent<Projectile>().Initialize(Vector2.right, 1, 3f, 0, 2f);
            ObjectPool.Release(projectile);
            GameObject reusedProjectile = pool.Spawn(projectilePrefab, farAway, Quaternion.identity);
            if (reusedProjectile != projectile ||
                projectile.GetComponent<Rigidbody2D>().linearVelocity != Vector2.zero)
                throw new System.Exception("El proyectil no se reutilizó limpio.");

            ObjectPool.Release(gem);
            if (pool.Spawn(gemPrefab, farAway, Quaternion.identity) != gem)
                throw new System.Exception("La gema no se reutilizó.");

            Debug.Log("Hito 10: pool de enemigos, proyectiles y gemas; vida reiniciada; Enemy-Enemy desactivado.");
        }
        catch (Exception error)
        {
            Debug.LogException(error);
        }
        finally
        {
            if (enemy != null) ObjectPool.Release(enemy);
            if (projectile != null) ObjectPool.Release(projectile);
            if (gem != null) ObjectPool.Release(gem);
        }
    }
}
