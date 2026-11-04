using System.Collections.Generic;
using UnityEngine;

public sealed class WaveDirector : MonoBehaviour
{
    [SerializeField] private WaveData waves;
    [SerializeField] private SpawnManager spawner;
    [SerializeField] private QuilomboSystem quilombo;
    [SerializeField] private GameObject elitePrefab;
    [SerializeField, Range(0f, 1f)] private float eliteChanceAfter50 = 0.1f;

    private readonly List<Health> activeEnemies = new();
    private int currentPhase = -1;
    private float nextSpawnSeconds;
    private bool bossSpawned;
    private float quilomboThresholdBonus;
    private bool elitesEnabled;
    private RunConfiguration configuration;
    private int lastBossSlot = -1;
    public int EndlessSegment { get; private set; }
    public float NextBossSeconds { get; private set; } = 180f;
    public float CurrentSpawnInterval { get; private set; }
    public int CurrentCommonLimit { get; private set; }
    public int PeakEnemyCount { get; private set; }

    public void Configure(RunConfiguration rules)
    {
        configuration = rules;
        NextBossSeconds = waves.BossSpawnSeconds;
    }

    public Health ActiveBoss { get; private set; }
    public event System.Action BossDefeated;

    private void OnEnable()
    {
        if (ActiveBoss != null && !ActiveBoss.IsDead) ActiveBoss.Died += OnBossDied;
    }

    private void OnDisable()
    {
        if (ActiveBoss != null) ActiveBoss.Died -= OnBossDied;
    }

    private void OnBossDied()
    {
        ActiveBoss.Died -= OnBossDied;
        BossDefeated?.Invoke();
    }
    public int PhaseNumber => currentPhase + 1;
    public int PhaseCount => waves.Phases.Length;
    public string PhaseTitle => currentPhase < 0 ? "" : waves.Phases[currentPhase].title;
    public int ActiveEnemyCount
    {
        get
        {
            int count = ActiveBoss != null && ActiveBoss.gameObject.activeInHierarchy &&
                !ActiveBoss.IsDead ? 1 : 0;
            foreach (Health enemy in activeEnemies)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.IsDead) count++;
            }
            return count;
        }
    }

    private void Awake()
    {
        if (waves == null || spawner == null || quilombo == null ||
            (elitePrefab != null && elitePrefab.GetComponent<Health>() == null) ||
            waves.BossPrefab == null ||
            waves.BossPrefab.GetComponent<Health>() == null ||
            waves.BossPrefab.GetComponent<BossAttack>() == null ||
            waves.Phases == null || waves.Phases.Length == 0)
        {
            Debug.LogError("WaveDirector requiere oleadas, SpawnManager, QuilomboSystem y La Jefa.", this);
            enabled = false;
            return;
        }

        if (waves.Phases[0].startSeconds != 0f)
        {
            Debug.LogError("La primera fase debe comenzar en 0 segundos.", this);
            enabled = false;
            return;
        }

        float previousStart = -1f;
        foreach (WaveData.Phase phase in waves.Phases)
        {
            if (phase.startSeconds <= previousStart || phase.spawnIntervalSeconds <= 0f ||
                phase.maximumAlive <= 0 || phase.enemies == null || phase.enemies.Length == 0)
            {
                Debug.LogError("Las fases deben estar ordenadas y tener intervalo, límite y enemigos.", this);
                enabled = false;
                return;
            }
            foreach (WaveData.SpawnOption option in phase.enemies)
            {
                if (option.prefab == null || option.weight <= 0 ||
                    option.prefab.GetComponent<Health>() == null)
                {
                    Debug.LogError("Cada enemigo de una fase necesita prefab y peso positivo.", this);
                    enabled = false;
                    return;
                }
            }
            previousStart = phase.startSeconds;
        }
    }

    public void SetQuilomboThresholdBonus(float bonus, bool enableElites)
    {
        quilomboThresholdBonus = bonus;
        elitesEnabled = enableElites;
    }

    public void Tick(float elapsedSeconds)
    {
        if (!enabled) return;

        configuration ??= RunRules.Selected.CreateConfiguration();
        EndlessSegment = configuration.SegmentAt(elapsedSeconds);
        int bossSlot = configuration.BossSlotAt(elapsedSeconds, waves.BossSpawnSeconds);
        bool bossDue = configuration.IsEndless ? bossSlot > lastBossSlot : !bossSpawned && bossSlot >= 0;
        if (bossDue)
        {
            lastBossSlot = bossSlot;
            bossSpawned = true;
            if (ActiveBoss == null || ActiveBoss.IsDead || !ActiveBoss.gameObject.activeInHierarchy)
            {
                if (ActiveBoss != null) ActiveBoss.Died -= OnBossDied;
                ActiveBoss = spawner.Spawn(waves.BossPrefab,
                    configuration.HealthFactorAt(elapsedSeconds), configuration.SpeedFactorAt(elapsedSeconds));
                if (ActiveBoss != null) ActiveBoss.Died += OnBossDied;
            }
            NextBossSeconds = configuration.IsEndless ?
                waves.BossSpawnSeconds + ((float)bossSlot + 1f) * configuration.BossRepeatSeconds : 0f;
        }

        WaveData.Phase[] phases = waves.Phases;
        int phaseIndex = waves.PhaseIndexAt(elapsedSeconds);
        WaveData.Phase phase = phases[phaseIndex];
        float multiplier = 1f + quilombo.Current / quilombo.Max + quilomboThresholdBonus;
        CurrentSpawnInterval = configuration.SpawnIntervalAt(elapsedSeconds, phase.spawnIntervalSeconds, multiplier);
        CurrentCommonLimit = configuration.CommonLimitAt(elapsedSeconds, phase.maximumAlive, multiplier);
        PeakEnemyCount = Mathf.Max(PeakEnemyCount, ActiveEnemyCount);
        if (phaseIndex != currentPhase)
        {
            currentPhase = phaseIndex;
            nextSpawnSeconds = elapsedSeconds + CurrentSpawnInterval;
        }

        if (elapsedSeconds < nextSpawnSeconds) return;
        nextSpawnSeconds = elapsedSeconds + CurrentSpawnInterval;
        activeEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy);
        if (activeEnemies.Count >= CurrentCommonLimit) return;

        if (elitesEnabled && elitePrefab != null && Random.value < eliteChanceAfter50)
        {
            Health elite = Spawn(elitePrefab, elapsedSeconds);
            if (elite != null) activeEnemies.Add(elite);
            PeakEnemyCount = Mathf.Max(PeakEnemyCount, ActiveEnemyCount);
            return;
        }

        int totalWeight = 0;
        foreach (WaveData.SpawnOption option in phase.enemies) totalWeight += option.weight;
        int roll = Random.Range(0, totalWeight);
        foreach (WaveData.SpawnOption option in phase.enemies)
        {
            roll -= option.weight;
            if (roll >= 0) continue;
            Health enemy = Spawn(option.prefab, elapsedSeconds);
            if (enemy != null) activeEnemies.Add(enemy);
            PeakEnemyCount = Mathf.Max(PeakEnemyCount, ActiveEnemyCount);
            return;
        }
    }

    private Health Spawn(GameObject prefab, float elapsed) => spawner.Spawn(prefab,
        configuration.HealthFactorAt(elapsed), configuration.SpeedFactorAt(elapsed));
}
