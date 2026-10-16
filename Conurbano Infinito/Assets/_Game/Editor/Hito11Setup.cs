using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Hito11Setup
{
    private const string Root = "Assets/_Game/";
    private const string MainPath = Root + "Scenes/Main.unity";
    private const string MenuPath = Root + "Scenes/Menu.unity";

    [MenuItem("Tools/Conurbano Infinito/Preparar hito 11")]
    public static void Prepare()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        string[] names = { "Jugador", "Paloma", "Bolsa", "Mosquito", "Jefa", "Gema", "Proyectil" };
        foreach (string name in names)
        {
            string path = Root + "Art/Placeholders/" + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Falta sprite del hito 11", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = name == "Gema" ? 64f : 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        SetSprite("Player", "Jugador", false);
        SetSprite("PalomaDePlaza", "Paloma", true);
        SetSprite("BolsaVoladora", "Bolsa", true);
        SetSprite("MosquitoGigante", "Mosquito", true);
        SetSprite("LaJefa", "Jefa", true);
        SetSprite("ExperienceGem", "Gema", false);
        SetSprite("TermoProjectile", "Proyectil", false);

        EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Single);
        GameObject player = GameObject.Find("Player");
        GameObject manager = GameObject.Find("GameManager");
        if (player == null || manager == null) throw new System.InvalidOperationException("Main necesita Player y GameManager.");
        var audio = manager.GetComponent<AudioDirector>();
        if (audio == null)
        {
            audio = manager.AddComponent<AudioDirector>();
            WireAudio(audio, "Partida", player.GetComponent<Health>(),
                player.GetComponent<ExperienceSystem>(), manager.GetComponent<GameManager>());
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        EditorSceneManager.SaveOpenScenes();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuPath) == null)
        {
            Scene menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.15f, 0.12f);
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<AudioListener>();
            var menuObject = new GameObject("Menu");
            var menuAudio = menuObject.AddComponent<AudioDirector>();
            menuObject.AddComponent<MenuScreen>();
            WireAudio(menuAudio, "Menu", null, null, null);
            EditorSceneManager.SaveScene(menu, MenuPath);
        }

        var scenes = new List<EditorBuildSettingsScene> { new(MenuPath, true) };
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.path != MenuPath) scenes.Add(scene);
        }
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
        Debug.Log("Hito 11 preparado: menú primero, Main después, audio y placeholders conectados.");
    }

    [MenuItem("Tools/Conurbano Infinito/Comprobar hito 11")]
    public static void Check()
    {
        var enabledScenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.enabled) enabledScenes.Add(scene.path);
        if (enabledScenes.Count != 2 || enabledScenes[0] != MenuPath || enabledScenes[1] != MainPath)
            throw new System.InvalidOperationException("La build debe iniciar en Menu y continuar en Main.");

        string[] prefabs = { "Player", "PalomaDePlaza", "BolsaVoladora", "MosquitoGigante",
            "LaJefa", "ExperienceGem", "TermoProjectile" };
        foreach (string name in prefabs)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/" + name + ".prefab");
            if (prefab == null || prefab.GetComponent<SpriteRenderer>()?.sprite == null ||
                (name != "Player" && name != "ExperienceGem" && name != "TermoProjectile" &&
                 prefab.GetComponent<HitFlash>() == null))
                throw new System.InvalidOperationException("Prefab incompleto: " + name);
        }

        string[] clips = { "Menu", "Partida", "Impacto", "Aviso", "Experiencia", "SubirNivel",
            "Daño", "Victoria", "Derrota" };
        foreach (string name in clips)
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/" + name + ".wav") == null)
                throw new FileNotFoundException("Falta audio del hito 11", name);

        Debug.Log("Hito 11: escenas, sprites, feedback y clips correctos.");
    }

    private static void SetSprite(string prefabName, string spriteName, bool addHitFlash)
    {
        string path = Root + "Prefabs/" + prefabName + ".prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var renderer = prefab.GetComponent<SpriteRenderer>();
            if (renderer == null) throw new System.InvalidOperationException(prefabName + " necesita SpriteRenderer.");
            if (renderer.sprite == null)
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/Placeholders/" + spriteName + ".png");
            if (renderer.sprite == null) throw new System.InvalidOperationException("Falta sprite " + spriteName);
            if (addHitFlash && prefab.GetComponent<HitFlash>() == null) prefab.AddComponent<HitFlash>();
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    private static void WireAudio(AudioDirector director, string musicName,
        Health health, ExperienceSystem experience, GameManager game)
    {
        var data = new SerializedObject(director);
        SetClip(data, "music", musicName);
        SetClip(data, "impact", "Impacto");
        SetClip(data, "attackWarning", "Aviso");
        SetClip(data, "experiencePickup", "Experiencia");
        SetClip(data, "levelUp", "SubirNivel");
        SetClip(data, "damage", "Daño");
        SetClip(data, "victory", "Victoria");
        SetClip(data, "defeat", "Derrota");
        data.FindProperty("playerHealth").objectReferenceValue = health;
        data.FindProperty("experience").objectReferenceValue = experience;
        data.FindProperty("gameManager").objectReferenceValue = game;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetClip(SerializedObject data, string field, string clipName)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/" + clipName + ".wav");
        if (clip == null) throw new FileNotFoundException("Falta audio del hito 11", clipName);
        data.FindProperty(field).objectReferenceValue = clip;
    }
}
