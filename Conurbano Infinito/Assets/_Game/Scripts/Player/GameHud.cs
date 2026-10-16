using UnityEngine;

public sealed class GameHud : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private ExperienceSystem experience;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private QuilomboSystem quilombo;

    private WeaponController weapons;
    private UpgradeSelection upgrades;
    private WaveDirector waves;
    private int lastPhase;
    private RiskZone[] pointsOfInterest;
    private Rect[] poiPanels;
    private float displayedQuilombo;
    private float displayedHealth;
    private float displayedExperience;
    private int lastHealth;
    private int lastLevel;
    private float damageFlashUntil;
    private float levelPulseUntil;
    private string quilomboLabel;
    private string feedbackMessage;
    private string queuedFeedback;
    private float feedbackUntil;

    private void Awake()
    {
        if (playerHealth != null) weapons = playerHealth.GetComponent<WeaponController>();
        upgrades = GetComponent<UpgradeSelection>();
        waves = GetComponent<WaveDirector>();
        pointsOfInterest = FindObjectsByType<RiskZone>(FindObjectsSortMode.InstanceID);
        poiPanels = new Rect[pointsOfInterest.Length];
        if (playerHealth != null) displayedHealth = lastHealth = playerHealth.CurrentHealth;
        if (experience != null)
        {
            displayedExperience = experience.CurrentExperience;
            lastLevel = experience.Level;
        }
        if (playerHealth == null || experience == null || gameManager == null || weapons == null || quilombo == null)
        {
            Debug.LogError("GameHud requiere Health, WeaponController, ExperienceSystem, GameManager y QuilomboSystem.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (quilombo == null) return;
        quilombo.OnQuilomboChanged += UpdateQuilombo;
        UpdateQuilombo(quilombo.Current);
    }

    private void OnDisable()
    {
        if (quilombo != null) quilombo.OnQuilomboChanged -= UpdateQuilombo;
    }

    private void UpdateQuilombo(float value)
    {
        displayedQuilombo = value;
        quilomboLabel = $"{value:0.#} / {quilombo.Max:0}";
    }

    public void ShowQuilomboFeedback(string message)
    {
        if (Time.unscaledTime < feedbackUntil)
        {
            queuedFeedback = message;
            return;
        }

        feedbackMessage = message;
        feedbackUntil = Time.unscaledTime + 3f;
    }

    private void Update()
    {
        if (waves != null && waves.PhaseNumber != lastPhase)
        {
            lastPhase = waves.PhaseNumber;
            ShowQuilomboFeedback($"OLEADA {lastPhase}/{waves.PhaseCount} · {waves.PhaseTitle}");
        }
        if (playerHealth.CurrentHealth < lastHealth) damageFlashUntil = Time.unscaledTime + 0.22f;
        if (experience.Level > lastLevel) levelPulseUntil = Time.unscaledTime + 0.35f;
        lastHealth = playerHealth.CurrentHealth;
        lastLevel = experience.Level;
        displayedHealth = Mathf.MoveTowards(displayedHealth, lastHealth,
            playerHealth.MaximumHealth * Time.unscaledDeltaTime * 3f);
        displayedExperience = Mathf.MoveTowards(displayedExperience, experience.CurrentExperience,
            experience.RequiredExperience * Time.unscaledDeltaTime * 3f);
        if (queuedFeedback == null || Time.unscaledTime < feedbackUntil) return;
        feedbackMessage = queuedFeedback;
        queuedFeedback = null;
        feedbackUntil = Time.unscaledTime + 3f;
    }

    private void OnGUI()
    {
        GUI.depth = 20;
        Matrix4x4 previous = GameUi.Begin();
        float width = GameUi.Width;
        float height = GameUi.Height;

        GameUi.Sign(new Rect(12, 10, 270, 170),
            Time.unscaledTime < damageFlashUntil ? GameUi.Red : GameUi.Petrol);
        GameUi.Tag(new Rect(22, 23, 52, 20), "NIVEL",
            Time.unscaledTime < levelPulseUntil ? GameUi.Orange : GameUi.Gold);
        GameUi.Fill(new Rect(22, 45, 52, 31), GameUi.Petrol);
        GameUi.Text(new Rect(23, 44, 50, 31), experience.Level.ToString(), 27,
            GameUi.Cream, TextAnchor.MiddleCenter);
        GameUi.Text(new Rect(83, 21, 187, 25), "EL PIBE DEL BARRIO", 15, GameUi.Ink);
        GameUi.Text(new Rect(84, 45, 80, 17), "♥ VIDA", 12, GameUi.Red);
        GameUi.Text(new Rect(164, 45, 108, 17),
            $"{playerHealth.CurrentHealth}/{playerHealth.MaximumHealth}", 12, GameUi.Ink,
            TextAnchor.MiddleRight);
        GameUi.Bar(new Rect(84, 64, 188, 12),
            displayedHealth / playerHealth.MaximumHealth, GameUi.Red);
        GameUi.Fill(new Rect(22, 83, 250, 2), GameUi.Concrete);
        GameUi.Text(new Rect(22, 88, 154, 18), "EXPERIENCIA", 12, GameUi.Petrol);
        GameUi.Text(new Rect(176, 88, 96, 18),
            $"{experience.CurrentExperience}/{experience.RequiredExperience}", 12,
            GameUi.Ink, TextAnchor.MiddleRight);
        GameUi.Meter(new Rect(22, 109, 250, 13),
            displayedExperience / experience.RequiredExperience, GameUi.Cyan);
        Color quilomboColor = displayedQuilombo >= 25f ? GameUi.Red :
            displayedQuilombo >= 10f ? GameUi.Orange : GameUi.Gold;
        GameUi.Text(new Rect(22, 130, 154, 18), "! QUILOMBO", 12, GameUi.Ink);
        GameUi.Text(new Rect(176, 130, 96, 18), quilomboLabel, 12,
            GameUi.Ink, TextAnchor.MiddleRight);
        GameUi.Meter(new Rect(22, 151, 250, 13), displayedQuilombo / quilombo.Max,
            quilomboColor);

        if (Time.unscaledTime < feedbackUntil)
        {
            var feedback = new Rect((width - 520f) / 2f, 99f, 520f, 54f);
            GameUi.Panel(feedback, GameUi.Red);
            GameUi.Text(new Rect(feedback.x + 12f, feedback.y + 8f, 496f, 38f),
                feedbackMessage, 21, GameUi.Cream, TextAnchor.MiddleCenter);
        }

        int seconds = Mathf.FloorToInt(gameManager.ElapsedSeconds);
        int duration = Mathf.CeilToInt(gameManager.DurationSeconds);
        var timer = new Rect((width - 240) / 2f, 10, 240, 91);
        Color timerAccent = seconds >= duration - 30 ? GameUi.Red : GameUi.Petrol;
        GameUi.Sign(timer, timerAccent);
        GameUi.Text(new Rect(timer.x + 7, timer.y + 8, timer.width - 14, 18),
            $"AGUANTÁ HASTA {duration / 60:00}:{duration % 60:00}", 13, GameUi.Ink, TextAnchor.MiddleCenter);
        GameUi.Text(new Rect(timer.x, timer.y + 24, timer.width, 41),
            $"{seconds / 60:00}:{seconds % 60:00}", 37, timerAccent, TextAnchor.MiddleCenter);
        for (int i = 0; i < 10; i++)
            GameUi.Fill(new Rect(timer.x + 21 + i * 20, timer.y + 72, 16, 6),
                gameManager.ElapsedSeconds >= (i + 1) * gameManager.DurationSeconds / 10f
                    ? timerAccent : GameUi.Concrete);

        int count = weapons.EquippedWeaponCount;
        int passiveCount = upgrades == null ? 0 : upgrades.SelectedPassives.Count;
        var equipment = new Rect(width - 238, 10, 226, 37 + count * 27 +
            (passiveCount > 0 ? 25 + passiveCount * 21 : 0));
        GameUi.Sign(equipment, GameUi.Petrol);
        GameUi.Text(new Rect(equipment.x + 11, equipment.y + 10, 142, 21),
            "EQUIPO / ARMAS", 15, GameUi.Ink);
        GameUi.Text(new Rect(equipment.x + 154, equipment.y + 10, 59, 21),
            $"{count} / {WeaponController.MaxEquippedWeapons}", 15, GameUi.Ink,
            TextAnchor.MiddleRight);
        for (int i = 0; i < count; i++)
        {
            WeaponData item = weapons.GetEquippedWeapon(i);
            float y = equipment.y + 34 + i * 27;
            GameUi.Fill(new Rect(equipment.x + 9, y, 208, 24), GameUi.Muted);
            GameUi.Icon(item.ProjectileSprite, new Rect(equipment.x + 12, y + 2, 20, 20));
            GameUi.Text(new Rect(equipment.x + 38, y, 174, 24), item.name, 13, GameUi.Ink);
        }
        if (passiveCount > 0)
        {
            float y = equipment.y + 37 + count * 27;
            GameUi.Text(new Rect(equipment.x + 11, y, 202, 19), "PASIVAS", 13, GameUi.Petrol);
            for (int i = 0; i < passiveCount; i++)
            {
                UpgradeData passive = upgrades.SelectedPassives[i];
                GameUi.Text(new Rect(equipment.x + 13, y + 20 + i * 21, 195, 20),
                    $"{passive.DisplayName}  ×{upgrades.GetLevel(passive)}", 12, GameUi.Ink);
            }
        }

        DrawPoiIndicators(Rect.MinMaxRect(12f, Mathf.Max(190f, equipment.yMax + 12f),
            width - 12f, height - 140f), new Vector2(width, height));

        Health boss = gameManager.BossHealth;
        if (boss != null && !boss.IsDead)
        {
            var bossPanel = new Rect((width - 420) / 2f, height - 48, 420, 36);
            GameUi.Panel(bossPanel, GameUi.Red);
            GameUi.Bar(new Rect(bossPanel.x + 8, bossPanel.y + 18, 404, 12),
                (float)boss.CurrentHealth / boss.MaximumHealth, GameUi.Red);
            GameUi.Text(new Rect(bossPanel.x + 12, bossPanel.y + 2, 396, 18),
                $"LA JEFA  {boss.CurrentHealth}/{boss.MaximumHealth}", 15, GameUi.Cream,
                TextAnchor.MiddleCenter);
        }

        if (gameManager.IsFastForwarding)
            GameUi.Text(new Rect(12, height - 32, 240, 22), "DEV x10  ·  F9 NORMAL", 14, GameUi.Gold);

        GUI.matrix = previous;
    }

    private void DrawPoiIndicators(Rect area, Vector2 screenSize)
    {
        int placed = 0;
        foreach (RiskZone zone in pointsOfInterest)
        {
            if (zone == null || !zone.isActiveAndEnabled || !zone.HasPendingReward) continue;
            Vector3 viewport = zone.ViewportPosition;
            if (!TryGetPoiPanel(viewport, screenSize, area, out Rect panel)) continue;
            for (int i = 0; i < placed; i++)
                panel = SeparatePoiPanels(panel, poiPanels[i], area);
            poiPanels[placed++] = panel;

            GameUi.Panel(panel, GameUi.Gold);
            var arrow = new Vector2(panel.x + 18f, panel.center.y);
            Vector2 direction = new Vector2(viewport.x * screenSize.x,
                (1f - viewport.y) * screenSize.y) - arrow;
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = previous * Matrix4x4.TRS(new Vector3(arrow.x, arrow.y, 0f),
                Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg), Vector3.one);
            GameUi.Text(new Rect(-12f, -14f, 24f, 28f), ">", 24,
                GameUi.Gold, TextAnchor.MiddleCenter);
            GUI.matrix = previous;
            GameUi.Text(new Rect(panel.x + 36f, panel.y + 6f, panel.width - 42f, 24f),
                zone.DisplayName, 13, GameUi.Cream);
        }
    }

    private static bool TryGetPoiPanel(Vector3 viewport, Vector2 screenSize, Rect area, out Rect panel)
    {
        panel = default;
        if (viewport.z <= 0f || (viewport.x >= 0f && viewport.x <= 1f &&
            viewport.y >= 0f && viewport.y <= 1f)) return false;

        Vector2 half = new Vector2(100f, 18f);
        Rect centers = Rect.MinMaxRect(area.xMin + half.x, area.yMin + half.y,
            area.xMax - half.x, area.yMax - half.y);
        Vector2 origin = screenSize * 0.5f;
        origin.x = Mathf.Clamp(origin.x, centers.xMin, centers.xMax);
        origin.y = Mathf.Clamp(origin.y, centers.yMin, centers.yMax);
        Vector2 direction = new Vector2((viewport.x - 0.5f) * screenSize.x,
            (0.5f - viewport.y) * screenSize.y);
        float horizontal = direction.x == 0f ? float.PositiveInfinity :
            ((direction.x > 0f ? centers.xMax : centers.xMin) - origin.x) / direction.x;
        float vertical = direction.y == 0f ? float.PositiveInfinity :
            ((direction.y > 0f ? centers.yMax : centers.yMin) - origin.y) / direction.y;
        Vector2 center = origin + direction * Mathf.Min(horizontal, vertical);
        panel = new Rect(center - half, half * 2f);
        return true;
    }

    private static Rect SeparatePoiPanels(Rect panel, Rect other, Rect area)
    {
        // shortcut: separación para los dos POIs actuales; revisar distribución al agregar más zonas.
        if (!panel.Overlaps(other)) return panel;
        bool side = Mathf.Approximately(panel.xMin, area.xMin) ||
            Mathf.Approximately(panel.xMax, area.xMax);
        if (side)
            panel.y = other.yMax + 6f + panel.height <= area.yMax ?
                other.yMax + 6f : other.yMin - panel.height - 6f;
        else
            panel.x = other.xMax + 6f + panel.width <= area.xMax ?
                other.xMax + 6f : other.xMin - panel.width - 6f;
        return panel;
    }
}
