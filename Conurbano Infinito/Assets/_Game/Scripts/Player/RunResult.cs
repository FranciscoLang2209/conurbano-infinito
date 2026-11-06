using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class RunBuildItem
{
    public string Id { get; }
    public string DisplayName { get; }
    public int Level { get; }
    public string EvolutionId { get; }
    public RunBuildItem(string id, string name, int level, string evolutionId = null)
    { Id = id; DisplayName = name; Level = level; EvolutionId = evolutionId; }
}

public sealed class RunEnemyResult
{
    public EnemyScoreCategory Category { get; }
    public long Kills { get; }
    public long Centipoints { get; }
    public RunEnemyResult(EnemyScoreCategory category, ScoreSystem score)
    { Category = category; Kills = score.KillCount(category); Centipoints = score.EnemyCentipoints(category); }
}

// Immutable value snapshot; no Unity references or live collections survive the close.
public sealed class RunResult
{
    public int SchemaVersion => 1;
    public string RunId { get; }
    public string CharacterId { get; }
    public string ModeId { get; }
    public GameMode Mode { get; }
    public string RulesetVersion { get; }
    public string BuildVersion { get; }
    public DateTime StartedUtc { get; }
    public float ActiveSeconds { get; }
    public long Centipoints { get; }
    public long Points => Centipoints / 100;
    public long PoiCount { get; }
    public long PoiCentipoints { get; }
    public long SurvivalSeconds { get; }
    public long SurvivalCentipoints { get; }
    public float FinalQuilombo { get; }
    public GameManager.GameState EndReason { get; }
    public string PracticeReason { get; }
    public bool IsPractice => PracticeReason != null;
    public bool IsRecordEligible => !IsPractice && (EndReason == GameManager.GameState.Defeat || EndReason == GameManager.GameState.Victory);
    public ReadOnlyCollection<RunBuildItem> Weapons { get; }
    public ReadOnlyCollection<RunBuildItem> Passives { get; }
    public ReadOnlyCollection<RunBuildItem> Synergies { get; }
    public ReadOnlyCollection<RunEnemyResult> Enemies { get; }

    public RunResult(string runId, DateTime startedUtc, string buildVersion, RunConfiguration configuration,
        string characterId, float seconds, float quilombo, GameManager.GameState reason,
        ScoreSystem score, WeaponController weapons)
    {
        if (!score.IsFrozen) throw new InvalidOperationException("El puntaje debe congelarse antes del resultado.");
        RunId = runId; StartedUtc = startedUtc; BuildVersion = buildVersion; CharacterId = characterId;
        ModeId = configuration.StableId; Mode = configuration.Mode; RulesetVersion = configuration.RulesetVersion;
        ActiveSeconds = seconds; FinalQuilombo = quilombo; EndReason = reason;
        Centipoints = score.Centipoints; PoiCount = score.PoiCount; PoiCentipoints = score.PoiCentipoints;
        SurvivalSeconds = score.SurvivalSeconds; SurvivalCentipoints = score.SurvivalCentipoints;
        PracticeReason = score.PracticeReason;
        var equipped = new List<RunBuildItem>();
        for (int i = 0; i < weapons.EquippedWeaponCount; i++)
        {
            WeaponData data = weapons.GetEquippedWeapon(i);
            equipped.Add(new RunBuildItem(data.StableId, weapons.GetWeaponDisplayName(data), weapons.GetWeaponLevel(data),
                weapons.GetEvolution(data)?.StableId));
        }
        Weapons = equipped.AsReadOnly();
        var passives = new List<RunBuildItem>();
        foreach (UpgradeData data in weapons.SelectedPassives)
            passives.Add(new RunBuildItem(data.StableId, data.DisplayName, weapons.GetPassiveLevel(data)));
        Passives = passives.AsReadOnly();
        var synergies = new List<RunBuildItem>();
        foreach (SynergyData data in weapons.ActiveSynergies) synergies.Add(new RunBuildItem(data.StableId, data.DisplayName, 1));
        Synergies = synergies.AsReadOnly();
        var enemies = new List<RunEnemyResult>();
        foreach (EnemyScoreCategory category in Enum.GetValues(typeof(EnemyScoreCategory))) enemies.Add(new RunEnemyResult(category, score));
        Enemies = enemies.AsReadOnly();
    }
}
