using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Hito4Check
{
    [MenuItem("Tools/Conurbano Infinito/Comprobar hito 4")]
    public static void Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/_Game/Scenes/Main.unity")
        {
            throw new System.Exception("Abrí la escena Main para comprobar el hito 4.");
        }

        var gameManager = Object.FindFirstObjectByType<GameManager>();
        var spawnManager = Object.FindFirstObjectByType<SpawnManager>();
        var player = Object.FindFirstObjectByType<PlayerController>();
        if (gameManager == null || spawnManager == null || player == null ||
            Object.FindFirstObjectByType<MosquitoChase>() != null)
        {
            throw new System.Exception("Main necesita GameManager, SpawnManager y Player, sin mosquitos iniciales.");
        }

        var game = new SerializedObject(gameManager);
        var spawn = new SerializedObject(spawnManager);
        if (game.FindProperty("playerHealth").objectReferenceValue != player.GetComponent<Health>() ||
            game.FindProperty("playerWeapon").objectReferenceValue != player.GetComponent<WeaponController>() ||
            game.FindProperty("spawnManager").objectReferenceValue != spawnManager ||
            game.FindProperty("waveDirector").objectReferenceValue != Object.FindFirstObjectByType<WaveDirector>() ||
            game.FindProperty("durationSeconds").floatValue != 600f ||
            spawn.FindProperty("player").objectReferenceValue != player.transform ||
            spawn.FindProperty("spawnCamera").objectReferenceValue != Camera.main ||
            spawn.FindProperty("minimumPlayerDistance").floatValue < 0f ||
            spawn.FindProperty("offscreenPadding").floatValue <= 0f)
        {
            throw new System.Exception("Faltan referencias o la duración del hito 4 es incorrecta.");
        }

        Debug.Log("Slice: escena y referencias correctas. Probá 600 s en Play Mode.");
    }

    [MenuItem("Tools/Conurbano Infinito/Comprobar pausa del slice")]
    public static void CheckPause()
    {
        GameManager game = Object.FindFirstObjectByType<GameManager>();
        if (!Application.isPlaying || game == null || game.State != GameManager.GameState.Playing ||
            Time.timeScale != 1f)
        {
            throw new System.Exception("Iniciá Play Mode sin una selección de mejora abierta.");
        }

        game.PauseGame();
        bool paused = game.State == GameManager.GameState.Paused && Time.timeScale == 0f;
        game.ContinueGame();
        if (!paused || game.State != GameManager.GameState.Playing || Time.timeScale != 1f)
        {
            throw new System.Exception("La pausa no detuvo o reanudó la partida.");
        }

        Debug.Log("Slice: pausa y continuación correctas.");
    }

    [MenuItem("Tools/Conurbano Infinito/Comprobar derrota del slice")]
    public static void CheckDefeat()
    {
        GameManager game = Object.FindFirstObjectByType<GameManager>();
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (!Application.isPlaying || game == null || player == null ||
            game.State != GameManager.GameState.Playing || Time.timeScale != 1f)
        {
            throw new System.Exception("Iniciá Play Mode sin una selección de mejora abierta.");
        }

        Health health = player.GetComponent<Health>();
        if (!health.TakeDamage(health.CurrentHealth) || game.State != GameManager.GameState.Defeat ||
            Time.timeScale != 0f || player.GetComponent<WeaponController>().enabled ||
            Object.FindFirstObjectByType<SpawnManager>().enabled)
        {
            throw new System.Exception("La derrota no detuvo la partida.");
        }

        Debug.Log("Slice: derrota correcta. Reiniciá con R.");
    }

    [MenuItem("Tools/Conurbano Infinito/Comprobar victoria del slice")]
    public static void CheckVictory()
    {
        GameManager game = Object.FindFirstObjectByType<GameManager>();
        if (!Application.isPlaying || game == null || game.State != GameManager.GameState.Playing ||
            game.ElapsedSeconds <= 0f || Time.timeScale != 1f)
        {
            throw new System.Exception("Iniciá Play Mode y esperá un segundo de partida.");
        }

        typeof(GameManager).GetField("durationSeconds", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(game, game.ElapsedSeconds);
        typeof(GameManager).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(game, null);
        if (game.State != GameManager.GameState.Victory || Time.timeScale != 0f ||
            Object.FindFirstObjectByType<SpawnManager>().enabled)
        {
            throw new System.Exception("La victoria no detuvo la partida.");
        }

        Debug.Log("Slice: victoria correcta. Reiniciá con R.");
    }
}
