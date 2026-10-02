using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Hito5Check
{
    [MenuItem("Tools/Conurbano Infinito/Comprobar hito 5")]
    public static void Run()
    {
        if (EditorSceneManager.GetActiveScene().path != "Assets/_Game/Scenes/Main.unity")
        {
            throw new System.Exception("Abrí Main para comprobar el hito 5.");
        }

        var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Player.prefab");
        var mosquito = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/MosquitoGigante.prefab");
        var gem = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/ExperienceGem.prefab");
        var hud = Object.FindFirstObjectByType<GameHud>();
        var experience = Object.FindFirstObjectByType<ExperienceSystem>();
        if (player == null || mosquito == null || gem == null || hud == null || experience == null ||
            player.GetComponent<ExperienceSystem>() == null ||
            mosquito.GetComponent<ExperienceDrop>() == null ||
            gem.layer != LayerMask.NameToLayer("Pickup") ||
            gem.GetComponent<ExperienceGem>() == null ||
            gem.GetComponent<CircleCollider2D>() == null ||
            !gem.GetComponent<CircleCollider2D>().isTrigger)
        {
            throw new System.Exception("Falta conectar progresión, drop, gema o HUD.");
        }

        var dropSettings = new SerializedObject(mosquito.GetComponent<ExperienceDrop>());
        var hudSettings = new SerializedObject(hud);
        if (dropSettings.FindProperty("gemPrefab").objectReferenceValue != gem.GetComponent<ExperienceGem>() ||
            hudSettings.FindProperty("experience").objectReferenceValue != experience ||
            hudSettings.FindProperty("playerHealth").objectReferenceValue !=
                Object.FindFirstObjectByType<PlayerController>().GetComponent<Health>() ||
            hudSettings.FindProperty("gameManager").objectReferenceValue != Object.FindFirstObjectByType<GameManager>())
        {
            throw new System.Exception("Hay referencias serializadas incorrectas en el hito 5.");
        }

        var check = new GameObject("ExperienceSystem check");
        try
        {
            var system = check.AddComponent<ExperienceSystem>();
            int levelUps = 0;
            system.LevelUp += _ => levelUps++;
            system.AddExperience(26);
            if (system.Level != 4 || system.CurrentExperience != 2 ||
                system.RequiredExperience != 14 || levelUps != 3)
            {
                throw new System.Exception("Una gema que cruza varios niveles dejó un estado incorrecto.");
            }

            Debug.Log("Hito 5: referencias y subida de varios niveles correctas. Probá drop, recogida y HUD en Play Mode.");
        }
        finally
        {
            Object.DestroyImmediate(check);
        }
    }
}
