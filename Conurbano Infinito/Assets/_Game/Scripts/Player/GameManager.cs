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

    private void OnGUI()
    {
        if (State == GameState.Playing)
        {
            return;
        }
        GUI.depth = 0;
        Matrix4x4 previous = GameUi.Begin();
        GameUi.Overlay();

        float panelHeight = State == GameState.Paused ? 320f : 280f;
        var panel = new Rect((GameUi.Width - 440) / 2f, (GameUi.Height - panelHeight) / 2f,
            440, panelHeight);
        Color accent = State == GameState.Victory ? GameUi.Gold :
            State == GameState.Defeat ? GameUi.Red : GameUi.Cyan;
        GameUi.Sign(panel, accent);
        GameUi.Fill(new Rect(panel.x + 28, panel.y + 24, 384, 49), accent);

        string title = State == GameState.Paused ? "PAUSA" :
            State == GameState.Defeat ? "DERROTA" : "VICTORIA";
        string detail = State == GameState.Paused ? "La plaza te espera · Esc para volver" :
            State == GameState.Defeat ? "El Pibe cayó en la plaza" :
            $"Resististe {Mathf.FloorToInt(ElapsedSeconds) / 60:00}:{Mathf.FloorToInt(ElapsedSeconds) % 60:00}";
        GameUi.Text(new Rect(panel.x + 20, panel.y + 21, 400, 52), title, 34,
            GameUi.Ink, TextAnchor.MiddleCenter);
        GameUi.Fill(new Rect(panel.x + 90, panel.y + 82, 260, 4), GameUi.Petrol);
        GameUi.Text(new Rect(panel.x + 25, panel.y + 96, 390, 35), detail, 18,
            GameUi.Ink, TextAnchor.MiddleCenter);

        if (State == GameState.Paused &&
            GameUi.Button(new Rect(panel.x + 34, panel.y + 145, 372, 44), "CONTINUAR", "ESC"))
            ContinueGame();

        float restartY = State == GameState.Paused ? 200f : 157f;
        if (GameUi.Button(new Rect(panel.x + 34, panel.y + restartY, 372, 44), "REINICIAR", "R"))
        {
            GUI.matrix = previous;
            RestartGame();
            GUIUtility.ExitGUI();
        }

        float menuY = State == GameState.Paused ? 255f : 212f;
        if (GameUi.Button(new Rect(panel.x + 34, panel.y + menuY, 372, 44), "MENÚ PRINCIPAL", "M"))
        {
            GUI.matrix = previous;
            ReturnToMenu();
            GUIUtility.ExitGUI();
        }
        GUI.matrix = previous;
    }
}
