using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class VisualSlice5Check
{
    private const string Root = "Assets/_Game/";

    [MenuItem("Tools/Conurbano Infinito/Comprobar quinta slice en Play Mode")]
    public static async void Run()
    {
        if (!Application.isPlaying || Time.timeScale != 1f)
            throw new Exception("Iniciá Play Mode en Main, sin pausa.");

        ObjectPool pool = Object.FindFirstObjectByType<ObjectPool>();
        PlayerSurvival player = Object.FindFirstObjectByType<PlayerSurvival>();
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            Root + "Prefabs/MosquitoGigante.prefab");
        GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            Root + "Prefabs/TermoProjectile.prefab");
        if (pool == null || player == null || enemyPrefab == null || projectilePrefab == null)
            throw new Exception("Faltan objetos para comprobar la quinta slice.");

        Vector3 farAway = new(1000f, 1000f, 0f);
        GameObject enemy = null;
        GameObject projectile = null;
        try
        {
            enemy = pool.Spawn(enemyPrefab, farAway, Quaternion.identity);
            Health health = enemy.GetComponent<Health>();
            SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
            Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
            Color normalColor = renderer.color;
            Vector3 normalScale = enemy.transform.localScale;
            health.TakeDamage(1);
            if (renderer.color != Color.white ||
                enemy.transform.Find("Impacto visual")?.gameObject.activeSelf != true)
                throw new Exception("No aparecen destello e impacto al dañar un enemigo.");
            await Task.Delay(170);
            if (renderer.color != normalColor)
                throw new Exception("El destello del enemigo no terminó.");

            health.TakeDamage(health.CurrentHealth);
            if (!health.IsDead || !enemy.activeSelf || body.simulated)
                throw new Exception("La muerte no conserva la salida visual sin contactos.");
            await Task.Delay(110);
            if (renderer.color.a >= normalColor.a || !enemy.activeSelf)
                throw new Exception("El enemigo no se desvanece antes de volver al pool.");
            await Task.Delay(170);
            if (enemy.activeSelf) throw new Exception("El enemigo no volvió al pool.");

            GameObject reused = pool.Spawn(enemyPrefab, farAway, Quaternion.identity);
            Transform reusedImpact = enemy.transform.Find("Impacto visual");
            if (reused != enemy || health.IsDead || renderer.color != normalColor ||
                enemy.transform.localScale != normalScale ||
                enemy.transform.rotation != Quaternion.identity || !body.simulated ||
                reusedImpact.gameObject.activeSelf ||
                reusedImpact.GetComponent<SpriteRenderer>().color != Color.white ||
                reusedImpact.localScale != Vector3.one ||
                reusedImpact.localRotation != Quaternion.identity ||
                enemy.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime > 0.1f)
                throw new Exception("El enemigo conservó estado visual al reutilizarse.");
            ObjectPool.Release(enemy);
            enemy = null;

            string[] names = { "Termo Balístico", "Boleadora", "Chancleta Teledirigida", "Fernets Orbitales" };
            MethodInfo tick = typeof(Projectile).GetMethod("FixedUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < names.Length; i++)
            {
                WeaponData data = AssetDatabase.LoadAssetAtPath<WeaponData>(
                    Root + "Data/" + names[i] + ".asset");
                projectile = pool.Spawn(projectilePrefab, farAway, Quaternion.identity);
                Projectile shot = projectile.GetComponent<Projectile>();
                shot.SetAppearance(data.ProjectileSprite, data.ProjectileColor,
                    data.ProjectileScale, data.Pattern);
                if (data.Pattern == WeaponPattern.Orbiting)
                    shot.InitializeOrbiting(player.transform, 0f, data.Damage, data.Range,
                        data.ProjectileSpeed, data.FireInterval);
                else if (data.Pattern == WeaponPattern.Returning)
                    shot.InitializeReturning(player.transform, Vector2.up, data.Damage,
                        data.ProjectileSpeed, data.Range);
                else
                    shot.Initialize(Vector2.up, data.Damage, data.ProjectileSpeed,
                        data.Pierce, data.Range);

                Transform shotVisual = projectile.transform.Find("Visual del proyectil");
                Quaternion before = shotVisual.localRotation;
                tick.Invoke(shot, null);
                if (data.Pattern == WeaponPattern.Straight &&
                    Mathf.Abs(shotVisual.localEulerAngles.z) > 1f ||
                    data.Pattern != WeaponPattern.Straight && shotVisual.localRotation == before ||
                    projectile.transform.rotation != Quaternion.identity)
                    throw new Exception("Falta rotación visual en " + names[i]);

                ObjectPool.Release(projectile);
                projectile = pool.Spawn(projectilePrefab, farAway, Quaternion.identity);
                SpriteRenderer shotRenderer = projectile.GetComponent<SpriteRenderer>();
                SpriteRenderer reusedVisual = projectile.transform.Find("Visual del proyectil")
                    .GetComponent<SpriteRenderer>();
                SpriteRenderer prefabRenderer = projectilePrefab.GetComponent<SpriteRenderer>();
                if (projectile.transform.rotation != Quaternion.identity ||
                    projectile.transform.Find("Visual del proyectil").localRotation != Quaternion.identity ||
                    reusedVisual.transform.localScale != Vector3.one ||
                    projectile.transform.localScale != projectilePrefab.transform.localScale ||
                    shotRenderer.sprite != prefabRenderer.sprite || shotRenderer.color != prefabRenderer.color ||
                    reusedVisual.sprite != prefabRenderer.sprite || reusedVisual.color != prefabRenderer.color)
                    throw new Exception("El proyectil conservó estado visual del arma anterior.");
                ObjectPool.Release(projectile);
                projectile = null;
            }

            Health playerHealth = player.GetComponent<Health>();
            if (!playerHealth.TakeDamage(1) ||
                player.GetComponent<SpriteRenderer>().color == Color.white)
                throw new Exception("El jugador no mostró feedback al recibir daño.");
            await Task.Delay(250);
            if (player.GetComponent<SpriteRenderer>().color != Color.white)
                throw new Exception("El feedback del jugador no terminó.");

            Debug.Log("Quinta slice Play Mode: hit flash, impacto, muerte gradual, feedback del jugador, cuatro rotaciones y reset visual del pool correctos.");
        }
        catch (Exception error)
        {
            Debug.LogException(error);
        }
        finally
        {
            if (enemy != null) ObjectPool.Release(enemy);
            if (projectile != null) ObjectPool.Release(projectile);
        }
    }
}
