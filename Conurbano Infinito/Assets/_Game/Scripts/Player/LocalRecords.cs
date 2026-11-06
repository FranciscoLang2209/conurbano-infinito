using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class StoredBuildItem
{
    public string id, displayName, evolutionId;
    public int level;
}

[Serializable]
public sealed class StoredEnemyResult
{
    public EnemyScoreCategory category;
    public long kills, centipoints;
}

// JSON values only; names are presentation snapshots, IDs define identity.
[Serializable]
public sealed class StoredRun
{
    public int schemaVersion;
    public string runId, characterId, modeId, rulesetVersion, buildVersion, startedUtc, practiceReason;
    public GameMode mode;
    public GameManager.GameState endReason;
    public bool isRecordEligible;
    public float activeSeconds, finalQuilombo;
    public long centipoints, poiCount, poiCentipoints, survivalSeconds, survivalCentipoints;
    public StoredBuildItem[] weapons, passives, synergies;
    public StoredEnemyResult[] enemies;
    public long Points => centipoints / 100;

    public static StoredRun FromResult(RunResult result) => new()
    {
        schemaVersion = 1,
        runId = result.RunId, characterId = result.CharacterId, modeId = result.ModeId, mode = result.Mode,
        rulesetVersion = result.RulesetVersion, buildVersion = result.BuildVersion,
        startedUtc = result.StartedUtc.ToString("O", CultureInfo.InvariantCulture),
        practiceReason = result.PracticeReason, endReason = result.EndReason, isRecordEligible = result.IsRecordEligible,
        activeSeconds = result.ActiveSeconds, finalQuilombo = result.FinalQuilombo, centipoints = result.Centipoints,
        poiCount = result.PoiCount, poiCentipoints = result.PoiCentipoints,
        survivalSeconds = result.SurvivalSeconds, survivalCentipoints = result.SurvivalCentipoints,
        weapons = result.Weapons.Select(CopyItem).ToArray(), passives = result.Passives.Select(CopyItem).ToArray(),
        synergies = result.Synergies.Select(CopyItem).ToArray(),
        enemies = result.Enemies.Select(e => new StoredEnemyResult { category = e.Category, kills = e.Kills, centipoints = e.Centipoints }).ToArray()
    };

    private static StoredBuildItem CopyItem(RunBuildItem item) => new()
    { id = item.Id, displayName = item.DisplayName, level = item.Level, evolutionId = item.EvolutionId };

    internal bool IsValid()
    {
        if (schemaVersion != 1 || string.IsNullOrWhiteSpace(runId) || string.IsNullOrWhiteSpace(characterId) ||
            string.IsNullOrWhiteSpace(modeId) || string.IsNullOrWhiteSpace(rulesetVersion) || buildVersion == null ||
            !DateTime.TryParse(startedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime date) || date.Kind != DateTimeKind.Utc ||
            (mode != GameMode.Classic && mode != GameMode.Endless) ||
            (endReason != GameManager.GameState.Defeat && endReason != GameManager.GameState.Victory && endReason != GameManager.GameState.Abandoned) ||
            !(activeSeconds >= 0) || float.IsInfinity(activeSeconds) || !(finalQuilombo >= 0 && finalQuilombo <= 100) ||
            centipoints < 0 || poiCount < 0 || poiCentipoints < 0 || survivalSeconds < 0 || survivalCentipoints < 0 ||
            isRecordEligible != (string.IsNullOrEmpty(practiceReason) && endReason != GameManager.GameState.Abandoned)) return false;
        if (weapons == null || passives == null || synergies == null || enemies == null || weapons.Length > 4 || passives.Length > 4 || enemies.Length != 3) return false;
        foreach (StoredBuildItem[] items in new[] { weapons, passives, synergies })
            if (items.Any(i => i == null || string.IsNullOrWhiteSpace(i.id) || i.displayName == null || i.level < 1) ||
                items.Select(i => i.id).Distinct(StringComparer.Ordinal).Count() != items.Length) return false;
        long total = Saturate(poiCentipoints, survivalCentipoints);
        var categories = new HashSet<EnemyScoreCategory>();
        foreach (StoredEnemyResult enemy in enemies)
        {
            if (enemy == null || !Enum.IsDefined(typeof(EnemyScoreCategory), enemy.category) || !categories.Add(enemy.category) || enemy.kills < 0 || enemy.centipoints < 0) return false;
            total = Saturate(total, enemy.centipoints);
        }
        return total == centipoints;
    }

    private static long Saturate(long a, long b) => a > long.MaxValue - b ? long.MaxValue : a + b;

    public string[] SummaryLines()
    {
        var lines = new List<string>();
        foreach (StoredBuildItem item in weapons) lines.Add($"{item.displayName} · N{item.level}" + (string.IsNullOrEmpty(item.evolutionId) ? "" : " · Evolución"));
        lines.Add("\nPasivas");
        foreach (StoredBuildItem item in passives) lines.Add($"{item.displayName} · N{item.level}");
        if (passives.Length == 0) lines.Add("Sin pasivas");
        lines.Add("\nSinergias");
        foreach (StoredBuildItem item in synergies) lines.Add(item.displayName);
        if (synergies.Length == 0) lines.Add("Sin sinergias");
        lines.Add("\nBajas por categoría");
        foreach (StoredEnemyResult item in enemies)
            lines.Add($"{(item.category == EnemyScoreCategory.Common ? "Comunes" : item.category == EnemyScoreCategory.Special ? "Especiales" : "Jefes")}: {item.kills} · {GameManager.FormatCentipoints(item.centipoints)} pt");
        lines.Add($"Lugares: {poiCount} · {GameManager.FormatCentipoints(poiCentipoints)} pt");
        lines.Add($"Tiempo: {activeSeconds:0.0} s · {survivalCentipoints / 100} pt");
        lines.Add($"Quilombo final: {finalQuilombo:0.#}");
        lines.Add("Cierre: " + (endReason == GameManager.GameState.Victory ? "Victoria" : endReason == GameManager.GameState.Defeat ? "Derrota" : "Abandono"));
        lines.Add(isRecordEligible ? "Elegible para récord local" : "Sin récord: " + (string.IsNullOrEmpty(practiceReason) ? "Abandono" : practiceReason));
        lines.Add("Reglas: " + rulesetVersion);
        lines.Add("Build: " + buildVersion);
        lines.Add("Inicio UTC: " + startedUtc);
        lines.Add("Personaje ID: " + characterId);
        lines.Add("Run ID: " + runId);
        return string.Join("\n", lines).Split('\n');
    }
}

public sealed class LocalRecords
{
    [Serializable]
    private sealed class SaveData
    {
        public int schemaVersion;
        public StoredRun lastResult;
        public List<StoredRun> records;
        // shortcut: IDs grow with completed runs; compact only if the idempotency contract changes.
        public List<string> registeredRunIds;
    }

    private readonly string path;
    private SaveData data = new() { schemaVersion = 1, records = new(), registeredRunIds = new() };
    private bool recovered, damaged, readOnly;
    public string Notice { get; private set; }
    public StoredRun LastResult => data.lastResult;
    public static LocalRecords OpenDefault() => new(Path.Combine(Application.persistentDataPath,
        Application.isEditor || Application.isBatchMode ? "local-records-qa.json" : "local-records.json"));

    public LocalRecords(string filePath)
    {
        path = filePath;
        try
        {
            if (File.Exists(path))
            {
                try { data = Read(path); return; }
                catch (NotSupportedException) { readOnly = true; Notice = "Formato de récords incompatible; archivo conservado."; return; }
                catch (Exception e) when (IsFileError(e)) { damaged = true; }
            }
            if (File.Exists(path + ".bak"))
            {
                data = Read(path + ".bak"); recovered = true;
                Notice = "Récords recuperados del respaldo.";
            }
            else if (damaged) Notice = "Archivo dañado; se conserva antes del próximo guardado.";
        }
        catch (NotSupportedException) { readOnly = true; Notice = "Respaldo incompatible; archivos conservados."; }
        catch (Exception e) when (IsFileError(e))
        { Notice = "No se pudieron leer los récords; archivos conservados."; damaged = true; }
    }

    private static bool IsFileError(Exception e) => e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException || e is System.Security.SecurityException;

    private static SaveData Read(string file)
    {
        string json = File.ReadAllText(file);
        SaveData loaded = JsonUtility.FromJson<SaveData>(json);
        if (loaded == null || !json.TrimStart().StartsWith("{", StringComparison.Ordinal)) throw new IOException("JSON inválido.");
        if (loaded.schemaVersion != 1) throw new NotSupportedException("Esquema incompatible.");
        if (loaded.records == null || loaded.registeredRunIds == null ||
            loaded.lastResult != null && !loaded.lastResult.IsValid() ||
            loaded.records.Any(r => r == null || !r.IsValid() || !r.isRecordEligible) ||
            loaded.registeredRunIds.Any(string.IsNullOrWhiteSpace) ||
            loaded.registeredRunIds.Distinct(StringComparer.Ordinal).Count() != loaded.registeredRunIds.Count ||
            loaded.records.Select(r => r.runId).Distinct(StringComparer.Ordinal).Count() != loaded.records.Count ||
            loaded.lastResult != null && !loaded.registeredRunIds.Contains(loaded.lastResult.runId) ||
            loaded.records.Any(r => !loaded.registeredRunIds.Contains(r.runId)) ||
            loaded.records.GroupBy(r => (r.modeId, r.rulesetVersion)).Any(g => g.Count() > 20)) throw new IOException("Datos de récord inválidos.");
        return loaded;
    }

    public StoredRun[] Table(string modeId, string rulesetVersion) => Order(data.records.Where(r =>
        r.modeId == modeId && r.rulesetVersion == rulesetVersion)).ToArray();
    public string[] Versions(string modeId, string current) => data.records.Where(r => r.modeId == modeId)
        .Select(r => r.rulesetVersion).Append(current).Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToArray();
    private static IOrderedEnumerable<StoredRun> Order(IEnumerable<StoredRun> runs) => runs.OrderByDescending(r => r.centipoints)
        .ThenByDescending(r => r.activeSeconds).ThenBy(r => r.runId, StringComparer.Ordinal);

    public bool Save(RunResult result)
    {
        if (readOnly) return false;
        if (data.registeredRunIds.Contains(result.RunId)) return true;
        StoredRun stored = StoredRun.FromResult(result);
        if (!stored.IsValid()) { Notice = "Resultado inválido; no se guardó."; return false; }
        var next = new SaveData { schemaVersion = 1, lastResult = stored, records = new List<StoredRun>(data.records), registeredRunIds = new List<string>(data.registeredRunIds) { stored.runId } };
        if (stored.isRecordEligible) next.records.Add(stored);
        next.records = next.records.GroupBy(r => (r.modeId, r.rulesetVersion)).SelectMany(g => Order(g).Take(20)).ToList();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            using (var stream = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(next, true));
                stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
            }
            Read(path + ".tmp");
            if (damaged && File.Exists(path)) File.Copy(path, path + ".corrupt-" + Guid.NewGuid().ToString("N"));
            if (!recovered && (!File.Exists(path) || damaged))
            {
                if (File.Exists(path + ".bak")) File.Copy(path + ".bak", path + ".bak.corrupt-" + Guid.NewGuid().ToString("N"));
                File.Copy(path + ".tmp", path + ".bak", true);
            }
            if (File.Exists(path)) File.Replace(path + ".tmp", path, recovered || damaged ? null : path + ".bak");
            else File.Move(path + ".tmp", path);
            data = next; recovered = damaged = false; Notice = null;
            return true;
        }
        catch (Exception e) when (IsFileError(e))
        { Notice = "No se pudo guardar el resultado en este equipo."; return false; }
    }
}
