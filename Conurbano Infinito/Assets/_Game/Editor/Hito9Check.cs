using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Hito9Check
{
    [MenuItem("Tools/Conurbano Infinito/Comprobar hito 9")]
    public static void Run()
    {
        if (EditorSceneManager.GetActiveScene().path != "Assets/_Game/Scenes/Main.unity")
        {
            throw new System.Exception("Abrí Main antes de comprobar el hito 9.");
        }

        WaveData waves = AssetDatabase.LoadAssetAtPath<WaveData>("Assets/_Game/Data/Oleadas.asset");
        GameManager game = Object.FindFirstObjectByType<GameManager>();
        if (waves == null || game == null || waves.Phases.Length != 6 ||
            waves.BossSpawnSeconds != 300f || waves.BossPrefab == null ||
            waves.BossPrefab.GetComponent<BossAttack>() == null ||
            waves.BossPrefab.GetComponent<Health>() == null)
        {
            throw new System.Exception("Faltan oleadas o La Jefa.");
        }

        float[] starts = { 0f, 120f, 240f, 300f, 360f, 540f };
        for (int i = 0; i < starts.Length; i++)
        {
            if (waves.Phases[i].startSeconds != starts[i] ||
                waves.PhaseIndexAt(starts[i]) != i ||
                (i > 0 && waves.PhaseIndexAt(starts[i] - 0.01f) != i - 1))
            {
                throw new System.Exception("Límite de oleada incorrecto: " + starts[i]);
            }
        }

        var settings = new SerializedObject(game);
        if (settings.FindProperty("durationSeconds").floatValue != 600f ||
            settings.FindProperty("waveDirector").objectReferenceValue !=
            Object.FindFirstObjectByType<WaveDirector>())
        {
            throw new System.Exception("Main no tiene duración de 10 minutos o WaveDirector conectado.");
        }

        Debug.Log("Hito 9: fases, miniboss y duración configurados correctamente.");
    }

    public static void RunBatch()
    {
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/Main.unity");
        Hito4Check.Run();
        Run();
    }
}
