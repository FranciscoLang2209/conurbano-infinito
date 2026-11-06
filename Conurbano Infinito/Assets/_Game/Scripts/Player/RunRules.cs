using System;
using UnityEngine;

public enum GameMode { Classic, Endless }

[CreateAssetMenu(fileName = "RunRules", menuName = "Conurbano Infinito/Reglas de partida")]
public sealed class RunRules : ScriptableObject
{
    [SerializeField] private string stableId;
    [SerializeField] private GameMode mode;
    [SerializeField] private string rulesetVersion = "builds-infinito-1";
    [SerializeField, Min(1f)] private float openingSeconds = 240f;
    [SerializeField, Min(1f)] private float segmentSeconds = 120f;
    [SerializeField, Min(0f)] private float healthPerSegment = 0.2f;
    [SerializeField, Min(0f)] private float speedPerSegment = 0.02f;
    [SerializeField, Min(1f)] private float maximumSpeedFactor = 1.3f;
    [SerializeField, Min(0f)] private float pressurePerSegment = 0.08f;
    [SerializeField, Min(1f)] private float maximumFrequencyFactor = 2f;
    [SerializeField, Min(0.01f)] private float minimumSpawnInterval = 0.2f;
    [SerializeField, Min(1)] private int maximumCommonEnemies = 150;
    [SerializeField, Min(1f)] private float bossRepeatSeconds = 240f;
    [SerializeField, Min(1)] private int maximumGems = 256;
    [SerializeField, Min(1)] private int maximumProjectiles = 256;

    private static RunRules[] all;
    public static GameMode SelectedMode { get; private set; }
    public static RunRules Selected => All[(int)SelectedMode];
    public static RunRules[] All
    {
        get
        {
            if (all != null) return all;
            RunRules[] loaded = Resources.LoadAll<RunRules>("RunRules");
            Array.Sort(loaded, (a, b) => a.mode.CompareTo(b.mode));
            if (loaded.Length != 2 || loaded[0].mode != GameMode.Classic || loaded[1].mode != GameMode.Endless)
                throw new InvalidOperationException("Faltan los dos perfiles en Resources/RunRules.");
            foreach (RunRules rules in loaded) rules.CreateConfiguration();
            if (loaded[0].stableId == loaded[1].stableId) throw new InvalidOperationException("IDs de modo duplicados.");
            return all = loaded;
        }
    }

    public static void Select(GameMode mode)
    {
        if (mode != GameMode.Classic && mode != GameMode.Endless) throw new ArgumentOutOfRangeException(nameof(mode));
        SelectedMode = mode;
    }

    public RunConfiguration CreateConfiguration() => new(stableId, mode, openingSeconds, segmentSeconds,
        healthPerSegment, speedPerSegment, maximumSpeedFactor, pressurePerSegment, maximumFrequencyFactor,
        minimumSpawnInterval, maximumCommonEnemies, bossRepeatSeconds, maximumGems, maximumProjectiles, rulesetVersion);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSelection() { all = null; SelectedMode = GameMode.Classic; }
}

// Snapshot por partida: nunca se guarda estado de juego en los perfiles compartidos.
public sealed class RunConfiguration
{
    public string StableId { get; }
    public GameMode Mode { get; }
    public string RulesetVersion { get; }
    public bool IsEndless => Mode == GameMode.Endless;
    public float OpeningSeconds { get; }
    public float SegmentSeconds { get; }
    public float BossRepeatSeconds { get; }
    public int MaximumCommonEnemies { get; }
    public int MaximumGems { get; }
    public int MaximumProjectiles { get; }
    private readonly float healthPerSegment, speedPerSegment, maximumSpeedFactor;
    private readonly float pressurePerSegment, maximumFrequencyFactor, minimumSpawnInterval;

    public RunConfiguration(string id, GameMode mode, float opening, float segment, float health, float speed,
        float maxSpeed, float pressure, float maxFrequency, float minInterval, int enemies,
        float bossRepeat, int gems, int projectiles, string rulesetVersion = "builds-infinito-1")
    {
        foreach (float value in new[] { opening, segment, maxSpeed, maxFrequency, minInterval, bossRepeat })
            if (!(value > 0f) || float.IsInfinity(value)) throw new ArgumentException("Regla de partida inválida.");
        foreach (float value in new[] { health, speed, pressure })
            if (!(value >= 0f) || float.IsInfinity(value)) throw new ArgumentException("Curva de partida inválida.");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(rulesetVersion) || (mode != GameMode.Classic && mode != GameMode.Endless) ||
            maxSpeed < 1f || maxFrequency < 1f || enemies < 1 || gems < 1 || projectiles < 1)
            throw new ArgumentException("Perfil de partida inválido.");
        StableId = id; Mode = mode; OpeningSeconds = opening; SegmentSeconds = segment;
        RulesetVersion = rulesetVersion;
        healthPerSegment = health; speedPerSegment = speed; maximumSpeedFactor = maxSpeed;
        pressurePerSegment = pressure; maximumFrequencyFactor = maxFrequency; minimumSpawnInterval = minInterval;
        MaximumCommonEnemies = enemies; BossRepeatSeconds = bossRepeat; MaximumGems = gems; MaximumProjectiles = projectiles;
    }

    public int SegmentAt(float elapsed) => !IsEndless || elapsed < OpeningSeconds ? 0 :
        (int)Math.Min(int.MaxValue, 1d + Math.Floor(((double)elapsed - OpeningSeconds) / SegmentSeconds));
    public float HealthFactorAt(float elapsed) => 1f + healthPerSegment * SegmentAt(elapsed);
    public float SpeedFactorAt(float elapsed) => Mathf.Min(1f + speedPerSegment * SegmentAt(elapsed), maximumSpeedFactor);
    public float SpawnIntervalAt(float elapsed, float baseInterval, float quilomboFactor)
    {
        float frequency = Mathf.Min(1f + pressurePerSegment * SegmentAt(elapsed), maximumFrequencyFactor);
        float interval = baseInterval / frequency / quilomboFactor;
        return IsEndless ? Mathf.Max(minimumSpawnInterval, interval) : interval;
    }
    public int CommonLimitAt(float elapsed, int baseLimit, float quilomboFactor)
    {
        double limit = Math.Ceiling(baseLimit * (1d + pressurePerSegment * SegmentAt(elapsed)) * quilomboFactor);
        return (int)Math.Min(IsEndless ? MaximumCommonEnemies : int.MaxValue, limit);
    }
    public int BossSlotAt(float elapsed, float firstBoss) => elapsed < firstBoss ? -1 : !IsEndless ? 0 :
        (int)Math.Min(int.MaxValue, Math.Floor(((double)elapsed - firstBoss) / BossRepeatSeconds));
}
