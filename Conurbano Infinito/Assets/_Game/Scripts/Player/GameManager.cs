using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class GameManager : MonoBehaviour
{
    public const float DefaultDurationSeconds = 240f;
    public enum GameState { Playing, Paused, Defeat, Victory }

    [SerializeField] private Health playerHealth;
    [SerializeField] private WeaponController playerWeapon;
    [SerializeField] private SpawnManager spawnManager;
    [SerializeField] private WaveDirector waveDirector;
    [SerializeField, Min(1f)] private float durationSeconds = DefaultDurationSeconds;

    public GameState State { get; private set; } = GameState.Playing;
    public event Action<GameState> StateChanged;
    public float ElapsedSeconds { get; private set; }
    public float DurationSeconds => durationSeconds;
    public Health BossHealth => waveDirector != null ? waveDirector.ActiveBoss : null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public bool IsFastForwarding { get; private set; }
#else
    public bool IsFastForwarding => false;
#endif

    private UpgradeSelection upgradeSelection;
    private int focusedAction;

    private void Awake()
    {
        Time.timeScale = 1f;
        upgradeSelection = GetComponent<UpgradeSelection>();
        if (playerHealth == null || playerWeapon == null || spawnManager == null || waveDirector == null)
        {
            Debug.LogError("GameManager requiere jugador, SpawnManager y WaveDirector.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died += OnPlayerDied;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= OnPlayerDied;
        }

        Time.timeScale = 1f;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (keyboard != null && keyboard.f9Key.wasPressedThisFrame)
        {
            IsFastForwarding = !IsFastForwarding;
        }
#endif
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (State == GameState.Paused)
            {
                ContinueGame();
                return;
            }

            if (State == GameState.Playing)
            {
                PauseGame();
                return;
            }
        }

        if (State != GameState.Playing)
        {
            focusedAction = GameUi.Navigate(focusedAction, State == GameState.Paused ? 3 : 2);
            if (GameUi.Confirm)
            {
                ActivateAction(focusedAction);
                return;
            }
            if (keyboard != null && keyboard.mKey.wasPressedThisFrame)
            {
                ReturnToMenu();
                return;
            }
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                RestartGame();
            }
            return;
        }

        if (Time.timeScale == 0f)
        {
            return;
        }

        float clockMultiplier = IsFastForwarding ? 10f : 1f;
        ElapsedSeconds = Mathf.Min(durationSeconds, ElapsedSeconds + Time.deltaTime * clockMultiplier);
        if (ElapsedSeconds >= durationSeconds)
        {
            focusedAction = 0;
            State = GameState.Victory;
            StateChanged?.Invoke(State);
            StopGame();
            return;
        }

        waveDirector.Tick(ElapsedSeconds);
    }

    public void PauseGame()
    {
        if (State != GameState.Playing)
        {
            return;
        }

        focusedAction = 0;
        State = GameState.Paused;
        Time.timeScale = 0f;
    }

    public void ContinueGame()
    {
        if (State != GameState.Paused)
        {
            return;
        }

        State = GameState.Playing;
        Time.timeScale = upgradeSelection != null && upgradeSelection.IsShowing ? 0f : 1f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    private void OnPlayerDied()
    {
        if (State != GameState.Playing && State != GameState.Paused)
        {
            return;
        }

        focusedAction = 0;
        State = GameState.Defeat;
        StateChanged?.Invoke(State);
        StopGame();
    }

    private void StopGame()
    {
        spawnManager.enabled = false;
        waveDirector.enabled = false;
        playerWeapon.enabled = false;
        Time.timeScale = 0f;
    }

    private void ActivateAction(int index)
    {
        if (State == GameState.Paused && index == 0) ContinueGame();
        else if (index == (State == GameState.Paused ? 1 : 0)) RestartGame();
        else ReturnToMenu();
    }

    private void OnGUI()
    {
        if (State == GameState.Playing) return;
        GUI.depth = 0;
        Matrix4x4 previous = GameUi.Begin();
        GameUi.Overlay();
        bool paused = State == GameState.Paused;
        bool victory = State == GameState.Victory;
        var panel = new Rect(GameUi.SafeArea.center.x - 450, GameUi.SafeArea.center.y - 226, 900, 452);
        Color accent = victory ? GameUi.Gold : paused ? GameUi.Cyan : GameUi.Red;
        GameUi.Sign(panel, accent);
        GameUi.Surface(new Rect(panel.x + 8, panel.y + 8, 368, 436), "enamel");
        GameUi.Icon("plaza", new Rect(panel.x + 24, panel.y + 26, 336, 224));
        GameUi.Icon("mate", new Rect(panel.x + 30, panel.y + 272, 32, 32));
        GameUi.Text(new Rect(panel.x + 74, panel.y + 271, 276, 32),
            paused ? "La plaza sigue ahí." : victory ? "El barrio te saluda." : "Mañana será otro día.",
            21, GameUi.Cream);
        int seconds = Mathf.FloorToInt(ElapsedSeconds);
        GameUi.Text(new Rect(panel.x + 32, panel.y + 304, 304, 30), "Tiempo de supervivencia", 18, GameUi.Cream);
        GameUi.Text(new Rect(panel.x + 30, panel.y + 334, 304, 68),
            $"{seconds / 60:00}:{seconds % 60:00}", 52, GameUi.Cream);
        GameUi.Text(new Rect(panel.x + 32, panel.y + 405, 304, 30), "Conurbano Infinito", 17, GameUi.Muted);

        float x = panel.x + 412;
        GameUi.Text(new Rect(x, panel.y + 28, 444, 25),
            paused ? "Pausa · Bajá un cambio" : victory ? "Victoria · Cuatro minutos" : "Fin de partida",
            18, GameUi.Petrol);
        GameUi.Text(new Rect(x, panel.y + 63, 444, 108),
            paused ? "Tomate un\nrespiro." : victory ? "La plaza\nes tuya." : "Hoy no\nse pudo.",
            44, GameUi.Ink, TextAnchor.UpperLeft);
        GameUi.Text(new Rect(x, panel.y + 171, 438, 24),
            paused ? "Cuando quieras, seguimos." : victory ? "Aguantaste hasta el final. ¿Sale otra?" : "Sacudite el polvo. Todavía queda barrio.",
            19, GameUi.Petrol);
        int count = paused ? 3 : 2;
        for (int i = 0; i < count; i++)
        {
            string label = paused && i == 0 ? "Seguir en la plaza" :
                i == (paused ? 1 : 0) ? "Otra partida" : "Volver al menú";
            string key = paused && i == 0 ? "ESC" : i == (paused ? 1 : 0) ? "R" : "M";
            if (!GameUi.Button(new Rect(x, panel.y + 217 + i * 59, 438, 49), label, key,
                focusedAction == i, i == 0)) continue;
            GameUi.End(previous);
            ActivateAction(i);
            GUIUtility.ExitGUI();
        }
        GameUi.Text(new Rect(x, panel.y + 405, 438, 23), "Flechas / Tab + Enter · Mouse", 16, GameUi.Petrol);
        GameUi.End(previous);
    }
}
