using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class VisualSlice3Setup
{
    private const string Root = "Assets/_Game/";
    private static readonly string[] Names =
        { "Termo Balístico", "Boleadora", "Chancleta Teledirigida", "Fernets Orbitales" };
    private static readonly string[] Images =
        { "TermoBalistico", "Boleadora", "ChancletaTeledirigida", "FernetOrbital" };

    [MenuItem("Tools/Conurbano Infinito/Integrar tercera slice visual")]
    public static void Apply()
    {
        if (Application.isPlaying) throw new Exception("Salí de Play Mode antes de integrar arte.");

        for (int i = 0; i < Names.Length; i++)
        {
            string path = Root + "Art/Sprites/Weapons/" + Images[i] + ".png";
            if (i > 0)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new Exception("Falta sprite: " + path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 48;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(0.5f, 0.5f);
                importer.SetTextureSettings(settings);
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            WeaponData weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(
                Root + "Data/" + Names[i] + ".asset");
            if (sprite == null || weapon == null) throw new Exception("Falta arma o sprite: " + Names[i]);
            var serialized = new SerializedObject(weapon);
            serialized.FindProperty("projectileSprite").objectReferenceValue = sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Tercera slice visual: sprites asignados explícitamente a las cuatro armas.");
    }

    [MenuItem("Tools/Conurbano Infinito/Comprobar tercera slice en Play Mode")]
    public static void CheckPlayMode()
    {
        if (!Application.isPlaying || Time.timeScale != 1f)
            throw new Exception("Iniciá Play Mode en Main, sin pausa.");

        var controller = Object.FindFirstObjectByType<WeaponController>();
        var pool = Object.FindFirstObjectByType<ObjectPool>();
        if (controller == null || pool == null) throw new Exception("Falta jugador o pool.");

        var weapons = new WeaponData[Names.Length];
        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i] = AssetDatabase.LoadAssetAtPath<WeaponData>(Root + "Data/" + Names[i] + ".asset");
            if (weapons[i] == null || weapons[i].ProjectileSprite == null)
                throw new Exception("Falta apariencia de " + Names[i]);
            controller.EquipWeapon(weapons[i]);
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var states = (IEnumerable)typeof(WeaponController).GetField("equipped", flags).GetValue(controller);
        MethodInfo fire = typeof(WeaponController).GetMethod("Fire", flags);
        MethodInfo ensureOrbitals = typeof(WeaponController).GetMethod("EnsureOrbitals", flags);
        var equipped = new Dictionary<WeaponData, object>();
        foreach (object item in states)
            equipped.Add((WeaponData)item.GetType().GetField("Data").GetValue(item), item);

        var enemy = new GameObject("Tercera slice: blanco temporal");
        enemy.layer = LayerMask.NameToLayer("Enemy");
        enemy.transform.position = controller.transform.position + Vector3.right * 2f;
        Health target = enemy.AddComponent<Health>();
        target.IncreaseMaximumHealth(1000);
        enemy.AddComponent<CircleCollider2D>();

        try
        {
            HashSet<Projectile> previous = null;
            for (int round = 0; round < 5; round++)
            {
                for (int i = 0; i < 3; i++)
                {
                    WeaponData data = weapons[i];
                    var before = new HashSet<Projectile>(Object.FindObjectsByType<Projectile>(
                        FindObjectsSortMode.None));
                    fire.Invoke(controller, new[] { equipped[data], target });
                    var spawned = new HashSet<Projectile>(Object.FindObjectsByType<Projectile>(
                        FindObjectsSortMode.None));
                    spawned.ExceptWith(before);
                    if (spawned.Count != data.ProjectileCount ||
                        previous != null && !spawned.Overlaps(previous))
                        throw new Exception("Falló la reutilización del pool para " + Names[i]);
                    foreach (Projectile projectile in spawned)
                    {
                        if (projectile.GetComponent<SpriteRenderer>().sprite != data.ProjectileSprite)
                            throw new Exception("Sprite incorrecto después del pool: " + Names[i]);
                        ObjectPool.Release(projectile.gameObject);
                    }
                    previous = spawned;
                }
            }

            object orbitalState = equipped[weapons[3]];
            ensureOrbitals.Invoke(controller, new[] { orbitalState });
            var orbitals = (IEnumerable)orbitalState.GetType().GetField("Orbitals").GetValue(orbitalState);
            int count = 0;
            foreach (Projectile orbital in orbitals)
            {
                if (!orbital.gameObject.activeSelf ||
                    orbital.GetComponent<SpriteRenderer>().sprite != weapons[3].ProjectileSprite)
                    throw new Exception("Un Fernet orbital perdió su sprite.");
                count++;
            }
            if (count != weapons[3].ProjectileCount)
                throw new Exception("Cantidad incorrecta de Fernets orbitales.");

            Debug.Log("Tercera slice Play Mode: cuatro armas equipadas, 15 tandas con pool reutilizado y Fernets orbitales correctos.");
        }
        finally
        {
            Object.Destroy(enemy);
        }
    }
}
