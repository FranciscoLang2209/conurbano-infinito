using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class GameManager : MonoBehaviour
{
    public const float DefaultDurationSeconds = 240f;
    public enum GameState { Playing, Paused, Defeat, Victory, Abandoned }

    [SerializeField] private Health playerHealth;
    [SerializeField] private WeaponController playerWeapon;
    [SerializeField] private SpawnManager spawnManager;
    [SerializeField] private WaveDirector waveDirector;
    [SerializeField, Min(1f)] private float durationSeconds = DefaultDurationSeconds;

    public GameState State { get; private set; } = GameState.Playing;
    public event Action<GameState> StateChanged;
    public float ElapsedSeconds { get; private set; }
    // Infinite has no deadline; callers must gate finite goals with HasTimeLimit.
    public float DurationSeconds => HasTimeLimit ? durationSeconds : 0f;
    public RunConfiguration Configuration { get; private set; }
    public bool HasTimeLimit => Configuration == null || !Configuration.IsEndless;
    public Health BossHealth => waveDirector != null ? waveDirector.ActiveBoss : null;
    public ScoreSystem Score { get; } = new();
    public RunResult Result { get; private set; }
    public event Action<RunResult> RunClosed;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public bool IsFastForwarding { get; private set; }
#else
    public bool IsFastForwarding => false;
#endif

    private UpgradeSelection upgradeSelection;
    private int focusedAction;
    private QuilomboSystem quilombo;
    private RiskZone[] scoreZones;
    private readonly System.Collections.Generic.HashSet<RiskZone> rewardedZones = new();
    private string runId, characterId, buildVersion;
    private DateTime startedUtc;
    private float observedElapsed;
    private float originalInvulnerability;
    private string[] resultBuildText;
    private int firstResultLine;
    private LocalRecords records;
    private RecordsView recordsView;
    private string recordStatus;

    private void Awake()
    {
        Time.timeScale = 1f;
        Configuration = RunRules.Selected.CreateConfiguration();
        runId = Guid.NewGuid().ToString("N");
        startedUtc = DateTime.UtcNow;
        characterId = CharacterData.Selected.StableId;
        buildVersion = Application.version;
        quilombo = GetComponent<QuilomboSystem>();
        scoreZones = FindObjectsByType<RiskZone>(FindObjectsSortMode.None);
#if UNITY_EDITOR
        MarkPractice("Sesión de Editor / QA");
#else
        if (Application.isBatchMode) MarkPractice("Prueba en batchmode");
#endif
        upgradeSelection = GetComponent<UpgradeSelection>();
        if (playerHealth == null || playerWeapon == null || spawnManager == null || waveDirector == null || quilombo == null)
        {
            Debug.LogError("GameManager requiere jugador, SpawnManager, WaveDirector y QuilomboSystem.", this);
            enabled = false;
            return;
        }
        originalInvulnerability = playerHealth.InvulnerabilitySeconds;
        waveDirector.Configure(Configuration);
        spawnManager.GetComponent<ObjectPool>().Configure(Configuration);
    }

    private void OnEnable()
    {
        if (spawnManager != null) spawnManager.EnemyKilled += OnEnemyKilled;
        if (scoreZones != null) foreach (RiskZone zone in scoreZones) zone.RewardEarned += OnPoiReward;
        if (waveDirector != null) waveDirector.BossDefeated += OnBossDefeated;
        if (playerHealth != null)
        {
            playerHealth.Died += OnPlayerDied;
        }
    }

    private void OnDisable()
    {
        if (spawnManager != null) spawnManager.EnemyKilled -= OnEnemyKilled;
        if (scoreZones != null) foreach (RiskZone zone in scoreZones) if (zone != null) zone.RewardEarned -= OnPoiReward;
        if (waveDirector != null) waveDirector.BossDefeated -= OnBossDefeated;
        if (playerHealth != null)
        {
            playerHealth.Died -= OnPlayerDied;
        }

        Time.timeScale = 1f;
    }

    private void OnBossDefeated()
    {
        if (State == GameState.Playing && !playerHealth.IsDead) playerWeapon.RegisterBossDefeated();
    }

    private void Update()
    {
        if (recordsView != null && recordsView.IsOpen) { recordsView.Update(); return; }
        DetectPracticeControls();
        Keyboard keyboard = Keyboard.current;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (keyboard != null && keyboard.f9Key.wasPressedThisFrame)
        {
            MarkPractice("F9: aceleración del reloj");
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
            if (Result != null && keyboard != null)
            {
                if (keyboard.pageUpKey.wasPressedThisFrame) firstResultLine = Math.Max(0, firstResultLine - 5);
                if (keyboard.pageDownKey.wasPressedThisFrame) firstResultLine = Math.Min(resultBuildText.Length - 1, firstResultLine + 5);
            }
            focusedAction = GameUi.Navigate(focusedAction, 3);
            if (State != GameState.Paused && keyboard != null && keyboard.lKey.wasPressedThisFrame)
            {
                ActivateAction(2); return;
            }
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
        ElapsedSeconds += Time.deltaTime * clockMultiplier;
        if (HasTimeLimit && ElapsedSeconds >= durationSeconds)
        {
            ElapsedSeconds = durationSeconds;
            observedElapsed = ElapsedSeconds;
            Score.ObserveActiveTime(ElapsedSeconds);
            return;
        }

        observedElapsed = ElapsedSeconds;
        Score.ObserveActiveTime(ElapsedSeconds);
        waveDirector.Tick(ElapsedSeconds);
    }

    private void LateUpdate()
    {
        // Resolve after gameplay Update: a death on the deadline frame wins over victory.
        if (State != GameState.Playing) return;
        if (playerHealth.IsDead) CloseRun(GameState.Defeat);
        else if (HasTimeLimit && ElapsedSeconds >= durationSeconds) CloseRun(GameState.Victory);
    }

    public void MarkPractice(string reason) => Score.MarkPractice(reason);

    public void SetElapsedForPractice(float seconds)
    {
        if (!(seconds >= 0f) || float.IsInfinity(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (Result != null) return;
        MarkPractice("Reloj inyectado por QA");
        ElapsedSeconds = seconds;
    }

    private void DetectPracticeControls()
    {
        if (Result != null) return;
        if (ElapsedSeconds != observedElapsed) MarkPractice("Reloj modificado");
        if ((Time.timeScale != 0f && Time.timeScale != 1f) || Time.captureDeltaTime != 0f)
            MarkPractice("Tiempo de simulación modificado");
        if (playerHealth != null && (playerHealth.InvulnerabilitySeconds != originalInvulnerability ||
            playerHealth.StartingHealth != CharacterData.Selected.StartingHealth)) MarkPractice("Salud / invulnerabilidad de QA");
    }

    private bool CanScore => State == GameState.Playing && Time.timeScale > 0f && !playerHealth.IsDead && Result == null;
    private void OnEnemyKilled(EnemyScoreCategory category)
    {
        if (!CanScore) return;
        DetectPracticeControls();
        Score.EnemyKilled(category, quilombo.Current);
    }

    private void OnPoiReward(RiskZone zone)
    {
        if (!CanScore || !rewardedZones.Add(zone)) return;
        DetectPracticeControls();
        Score.PoiCompleted();
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
        AbandonGame();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMenu()
    {
        AbandonGame();
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    private void OnApplicationQuit() => AbandonGame();

    private void AbandonGame()
    {
        if (State != GameState.Playing && State != GameState.Paused) return;
        CloseRun(GameState.Abandoned);
    }

    private void OnPlayerDied()
    {
        if (State != GameState.Playing && State != GameState.Paused)
        {
            return;
        }

        CloseRun(GameState.Defeat);
    }

    private void CloseRun(GameState reason)
    {
        if (Result != null) return;
        DetectPracticeControls();
        Score.Freeze();
        Result = new RunResult(runId, startedUtc, buildVersion, Configuration, characterId,
            ElapsedSeconds, quilombo.Current, reason, Score, playerWeapon);
        resultBuildText = BuildResultText(Result);
        focusedAction = 0;
        State = reason;
        StopGame();
        records = LocalRecords.OpenDefault();
        records.Save(Result);
        StoredRun[] comparable = records.Table(Result.ModeId, Result.RulesetVersion);
        recordStatus = comparable.Length == 0 ? "Sin marca comparable en este equipo" : $"Mejor local: {comparable[0].Points} puntos";
        StateChanged?.Invoke(State);
        RunClosed?.Invoke(Result);
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
        if (State != GameState.Paused && index == 2)
            recordsView = new RecordsView(records, Result.ModeId, Result.RulesetVersion);
        else if (State == GameState.Paused && index == 0) ContinueGame();
        else if (index == (State == GameState.Paused ? 1 : 0)) RestartGame();
        else ReturnToMenu();
    }

    private void OnGUI()
    {
        if (State == GameState.Playing) return;
        GUI.depth = 0;
        Matrix4x4 previous = GameUi.Begin();
        GameUi.Overlay();
        if (recordsView != null && recordsView.IsOpen)
        {
            recordsView.Draw(); GameUi.End(previous); return;
        }
        bool paused = State == GameState.Paused;
        bool victory = State == GameState.Victory;
        var panel = new Rect(GameUi.SafeArea.center.x - 450, GameUi.SafeArea.center.y - 258, 900, 516);
        Color accent = victory ? GameUi.Gold : paused ? GameUi.Cyan : GameUi.Red;
        GameUi.Sign(panel, accent);
        GameUi.Surface(new Rect(panel.x + 8, panel.y + 8, 368, 500), "enamel");
        if (paused) DrawBuildSummary(new Rect(panel.x + 24, panel.y + 20, 336, 476));
        else DrawResultBuild(new Rect(panel.x + 24, panel.y + 20, 336, 476));

        float x = panel.x + 412;
        GameUi.Text(new Rect(x, panel.y + 28, 444, 25),
            paused ? "Pausa · Bajá un cambio" : victory ? "Victoria · Cuatro minutos" : "Fin de partida",
            18, GameUi.Petrol);
        GameUi.Text(new Rect(x, panel.y + 63, 444, 108),
            paused ? "Tomate un\nrespiro." : $"{Result?.Points ?? 0}\npuntos",
            paused ? 44 : 42, GameUi.Ink, TextAnchor.UpperLeft);
        if (paused) GameUi.Text(new Rect(x, panel.y + 171, 438, 24), "Cuando quieras, seguimos.", 19, GameUi.Petrol);
        else if (Result != null)
        {
            long enemies = 0;
            foreach (RunEnemyResult item in Result.Enemies)
                enemies = item.Centipoints > long.MaxValue - enemies ? long.MaxValue : enemies + item.Centipoints;
            GameUi.Text(new Rect(x, panel.y + 174, 438, 28), $"Bajas: {FormatCentipoints(enemies)} puntos", 18, GameUi.Petrol);
            GameUi.Text(new Rect(x, panel.y + 204, 438, 28),
                $"Lugares: {Result.PoiCentipoints / 100} · Tiempo: {Result.SurvivalCentipoints / 100}", 18, GameUi.Petrol);
            GameUi.Text(new Rect(x, panel.y + 234, 438, 28),
                $"{Result.ActiveSeconds:0.0} s activos · Quilombo {Result.FinalQuilombo:0.#}", 18, GameUi.Petrol);
        }
        if (!paused && Result != null)
        {
            GameUi.Text(new Rect(x, panel.y + 264, 438, 28), recordStatus, 18, GameUi.Petrol);
        }
        int count = 3;
        for (int i = 0; i < count; i++)
        {
            string label = !paused && i == 2 ? "Récords de este equipo" : paused && i == 0 ? "Seguir en la plaza" :
                i == (paused ? 1 : 0) ? "Otra partida" : "Volver al menú";
            string key = !paused && i == 2 ? "L" : paused && i == 0 ? "ESC" : i == (paused ? 1 : 0) ? "R" : "M";
            if (!GameUi.Button(new Rect(x, panel.y + (paused ? 217 : 305) + i * 59, 438, 49), label, key,
                focusedAction == i, i == 0)) continue;
            GameUi.End(previous);
            focusedAction = i;
            ActivateAction(i);
            GUIUtility.ExitGUI();
        }
        GameUi.Text(new Rect(x, panel.y + 477, 438, 26),
            !paused && records.Notice != null ? "Guardado: revisar aviso en Récords" :
            !paused && Result != null && Result.IsPractice ? "Práctica · no participa en récords" :
            !paused && Result != null && !Result.IsRecordEligible ? "Abandono · sin récord" : "Flechas / Tab + Enter · Mouse", 16, GameUi.Petrol);
        GameUi.End(previous);
    }

    public static string FormatCentipoints(long points) => $"{points / 100}.{points % 100:00}";

    private static string[] BuildResultText(RunResult result)
    {
        return StoredRun.FromResult(result).SummaryLines();
    }

    private void DrawResultBuild(Rect area)
    {
        GameUi.Text(new Rect(area.x, area.y, area.width, 30), "La build de esta partida", 22, GameUi.Gold);
        RecordsView.DrawLines(new Rect(area.x, area.y + 42, area.width, area.height - 42), resultBuildText, ref firstResultLine);
    }

    private void DrawBuildSummary(Rect area)
    {
        GameUi.Text(new Rect(area.x, area.y, area.width, 26), "Afinidad del personaje", 20, GameUi.Gold);
        GameUi.Text(new Rect(area.x, area.y + 30, area.width, 58), playerWeapon.AffinityDescription,
            18, GameUi.Cream, TextAnchor.UpperLeft, true);
        float y = area.y + 94;
        foreach (SynergyData data in SynergyData.All)
        {
            bool active = false;
            foreach (SynergyData owned in playerWeapon.ActiveSynergies)
                if (owned == data) active = true;
            GameUi.Text(new Rect(area.x, y, area.width, 26), data.DisplayName + (active ? " · Activa" : " · Pendiente"),
                18, active ? GameUi.Gold : GameUi.Muted);
            GameUi.Text(new Rect(area.x, y + 27, area.width, 24), data.Requirements, 16, GameUi.Cyan);
            string effect = data.Description;
            if (data.Effect == SynergyEffect.FestejoDelBarrio && playerWeapon.FestejoReady)
                effect = "Carga lista: próximo Petardo +25% de radio.";
            GameUi.Text(new Rect(area.x, y + 52, area.width, 50), effect, 16,
                GameUi.Cream, TextAnchor.UpperLeft, true);
            y += 104;
        }
    }
}
