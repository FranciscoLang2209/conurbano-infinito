using System;

public enum EnemyScoreCategory { Common, Special, Boss }

// Session state only. Enemy sources guarantee one notification per life.
public sealed class ScoreSystem
{
    private readonly long[] kills = new long[3];
    private readonly long[] enemyPoints = new long[3];
    public long Centipoints { get; private set; }
    public long Points => Centipoints / 100;
    public long PoiCount { get; private set; }
    public long PoiCentipoints { get; private set; }
    public long SurvivalSeconds { get; private set; }
    public long SurvivalCentipoints { get; private set; }
    public bool IsFrozen { get; private set; }
    public string PracticeReason { get; private set; }
    public bool IsPractice => PracticeReason != null;

    public long KillCount(EnemyScoreCategory category) => kills[Index(category)];
    public long EnemyCentipoints(EnemyScoreCategory category) => enemyPoints[Index(category)];

    public void MarkPractice(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Falta motivo de práctica.", nameof(reason));
        if (!IsFrozen) PracticeReason ??= reason;
    }

    public void EnemyKilled(EnemyScoreCategory category, float quilombo)
    {
        int index = Index(category);
        if (float.IsNaN(quilombo) || float.IsInfinity(quilombo)) throw new ArgumentOutOfRangeException(nameof(quilombo));
        if (IsFrozen) return;
        long basis = category == EnemyScoreCategory.Boss ? 1000 : category == EnemyScoreCategory.Special ? 30 : 10;
        // Round fractional Quilombo to the nearest integer, ties away from zero: factor in hundredths.
        long factor = 100 + (long)Math.Round(Math.Clamp((double)quilombo, 0d, 100d), MidpointRounding.AwayFromZero);
        long points = basis * factor;
        kills[index] = Add(kills[index], 1);
        enemyPoints[index] = Add(enemyPoints[index], points);
        Centipoints = Add(Centipoints, points);
    }

    public void PoiCompleted()
    {
        if (IsFrozen) return;
        PoiCount = Add(PoiCount, 1);
        PoiCentipoints = Add(PoiCentipoints, 25000);
        Centipoints = Add(Centipoints, 25000);
    }

    public void ObserveActiveTime(double seconds)
    {
        if (!(seconds >= 0d) || double.IsInfinity(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (IsFrozen) return;
        long completed = seconds >= long.MaxValue ? long.MaxValue : (long)Math.Floor(seconds);
        if (completed <= SurvivalSeconds) return;
        long points = Multiply100(completed - SurvivalSeconds);
        SurvivalSeconds = completed;
        SurvivalCentipoints = Add(SurvivalCentipoints, points);
        Centipoints = Add(Centipoints, points);
    }

    public void Freeze() => IsFrozen = true;
    private static int Index(EnemyScoreCategory category) => category >= EnemyScoreCategory.Common && category <= EnemyScoreCategory.Boss
        ? (int)category : throw new ArgumentOutOfRangeException(nameof(category));
    private static long Multiply100(long value) => value > long.MaxValue / 100 ? long.MaxValue : value * 100;
    private static long Add(long a, long b) => a > long.MaxValue - b ? long.MaxValue : a + b;
}
