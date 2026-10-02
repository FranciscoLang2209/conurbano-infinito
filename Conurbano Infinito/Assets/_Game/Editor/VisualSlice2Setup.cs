using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class VisualSlice2Setup
{
    private const string Art = "Assets/_Game/Art/Sprites/";
    private const string Prefabs = "Assets/_Game/Prefabs/";

    [MenuItem("Tools/Conurbano Infinito/Integrar segunda slice visual")]
    public static void Apply()
    {
        if (Application.isPlaying) throw new Exception("Salí de Play Mode antes de integrar arte.");
        SetSprite("Enemies/BolsaVoladora.png", "BolsaVoladora", 48, new Vector2(0.5f, 0f),
            new Color(0.78f, 0.78f, 0.78f));
        SetSprite("Pickups/GemaExperiencia.png", "ExperienceGem", 64, new Vector2(0.5f, 0.5f),
            Color.white);
        SetSprite("Enemies/LaJefa.png", "LaJefa", 48, new Vector2(0.5f, 0f),
            new Color(0.78f, 0.78f, 0.78f));

        string floorPath = Art + "Environment/PlazaPiso.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(floorPath);
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(floorPath));
        material.SetTextureScale("_BaseMap", Vector2.one * 1875f);
        string materialPath = Art + "Environment/PlazaPiso.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) == null)
            AssetDatabase.CreateAsset(material, materialPath);
        else
        {
            EditorUtility.CopySerialized(material, AssetDatabase.LoadAssetAtPath<Material>(materialPath));
            Object.DestroyImmediate(material);
        }

        const string scenePath = "Assets/_Game/Scenes/Main.unity";
        EditorSceneManager.OpenScene(scenePath);
        GameObject floor = GameObject.Find("PlazaPiso");
        if (floor == null)
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            floor.name = "PlazaPiso";
            Object.DestroyImmediate(floor.GetComponent<Collider>());
        }
        floor.transform.SetPositionAndRotation(new Vector3(0f, 0f, 5f),
            Quaternion.identity);
        floor.transform.localScale = new Vector3(20000f, 20000f, 1f);
        floor.GetComponent<MeshRenderer>().sharedMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        EditorSceneManager.MarkSceneDirty(floor.scene);
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Segunda slice visual integrada.");
    }

    [MenuItem("Tools/Conurbano Infinito/Comprobar segunda slice en Play Mode")]
    public static async void CheckPlayMode()
    {
        if (!Application.isPlaying) throw new Exception("Iniciá Play Mode en Main.");

        var player = Object.FindFirstObjectByType<PlayerController>();
        var weapons = player.GetComponent<WeaponController>();
        var health = player.GetComponent<Health>();
        var experience = player.GetComponent<ExperienceSystem>();
        var pool = Object.FindFirstObjectByType<ObjectPool>();
        var spawner = Object.FindFirstObjectByType<SpawnManager>();
        var waves = Object.FindFirstObjectByType<WaveDirector>();
        var camera = Object.FindFirstObjectByType<CameraFollow>();
        weapons.enabled = false;
        health.IncreaseMaximumHealth(100);

        try
        {
            int beforeXp = experience.CurrentExperience;
            var gem = pool.Spawn(AssetDatabase.LoadAssetAtPath<GameObject>(
                Prefabs + "ExperienceGem.prefab"), player.transform.position, Quaternion.identity)
                .GetComponent<ExperienceGem>();
            gem.Initialize(2);
            await Task.Delay(400);
            if (experience.CurrentExperience != beforeXp + 2 || gem.gameObject.activeSelf)
                throw new Exception("La Gema no se recogió o no otorgó experiencia.");

            var bag = spawner.Spawn(AssetDatabase.LoadAssetAtPath<GameObject>(
                Prefabs + "BolsaVoladora.prefab"));
            bag.GetComponent<Rigidbody2D>().position = (Vector2)player.transform.position +
                Vector2.right * 3f;
            float bagStart = Vector2.Distance(bag.transform.position, player.transform.position);
            await Task.Delay(500);
            if (bag == null || !bag.gameObject.activeSelf ||
                Vector2.Distance(bag.transform.position, player.transform.position) >= bagStart)
                throw new Exception("Bolsa no apareció o no avanzó hacia el Pibe.");

            waves.Tick(300f);
            Health boss = waves.ActiveBoss;
            if (boss == null || boss.GetComponent<BossAttack>() == null)
                throw new Exception("La Jefa no apareció al minuto cinco.");
            boss.GetComponent<Rigidbody2D>().position = (Vector2)player.transform.position +
                Vector2.right * 5f;

            float cameraStart = camera.transform.position.x;
            player.GetComponent<Rigidbody2D>().position += Vector2.right * 10f;
            await Task.Delay(600);
            if (camera.transform.position.x <= cameraStart + 5f ||
                GameObject.Find("PlazaPiso").GetComponent<Collider>() != null)
                throw new Exception("La cámara no siguió al Pibe o el fondo tiene collider.");

            bool warned = false;
            for (int i = 0; i < 40; i++)
            {
                await Task.Delay(200);
                if (GameObject.Find("Aviso de ataque de La Jefa") == null) continue;
                warned = true;
                break;
            }
            if (!warned) throw new Exception("La Jefa no mostró su aviso de ataque.");
            int healthBeforeStrike = health.CurrentHealth;
            await Task.Delay(1500);
            if (GameObject.Find("Aviso de ataque de La Jefa") != null ||
                health.CurrentHealth >= healthBeforeStrike)
                throw new Exception("La Jefa no completó el ataque.");

            Debug.Log("Segunda slice Play Mode: Gema, Bolsa, Jefa y cámara correctas.");
        }
        catch (Exception error)
        {
            Debug.LogException(error);
        }
        finally
        {
            if (weapons != null) weapons.enabled = true;
        }
    }

    private static void SetSprite(string image, string prefabName, int ppu, Vector2 pivot, Color tint)
    {
        string imagePath = Art + image;
        var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        string prefabPath = Prefabs + prefabName + ".prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
            renderer.color = tint;
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }
}
