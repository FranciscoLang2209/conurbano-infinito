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
            ShowQuilomboFeedback($"Oleada {lastPhase}/{waves.PhaseCount} · {waves.PhaseTitle}");
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
        Rect safe = GameUi.SafeArea;
        float left = safe.x + GameUi.Margin, right = safe.xMax - GameUi.Margin, top = safe.y + 14;
        float width = GameUi.Width, height = safe.yMax;

        GameUi.Surface(new Rect(left, top, 340, 104), "enamel");
        GameUi.Text(new Rect(left + 12, top + 10, 56, 26), "Nivel", 18, GameUi.Cream, TextAnchor.MiddleCenter);
        GameUi.Icon("crest", new Rect(left + 16, top + 40, 48, 48));
        GameUi.Text(new Rect(left + 16, top + 43, 48, 36), experience.Level.ToString(), 28,
            Time.unscaledTime < levelPulseUntil ? GameUi.Red : GameUi.Ink, TextAnchor.MiddleCenter);
        GameUi.Icon("heart", new Rect(left + 84, top + 15, 24, 24));
        GameUi.Text(new Rect(left + 116, top + 8, 80, 36), "Vida", 24, GameUi.Cream);
        GameUi.Text(new Rect(left + 208, top + 6, 116, 40),
            $"{playerHealth.CurrentHealth}/{playerHealth.MaximumHealth}", 32, GameUi.Cream, TextAnchor.MiddleRight);
        GameUi.Bar(new Rect(left + 84, top + 48, 240, 12), displayedHealth / playerHealth.MaximumHealth,
            Time.unscaledTime < damageFlashUntil ? GameUi.Cream : GameUi.Red);
        GameUi.Icon("xp", new Rect(left + 84, top + 76, 16, 16));
        GameUi.Bar(new Rect(left + 108, top + 78, 106, 10),
            displayedExperience / experience.RequiredExperience, GameUi.Cyan);
        GameUi.Text(new Rect(left + 224, top + 68, 100, 30),
            $"XP {experience.CurrentExperience}/{experience.RequiredExperience}", 20, GameUi.Cream, TextAnchor.MiddleRight);

        int seconds = Mathf.FloorToInt(gameManager.ElapsedSeconds);
        int duration = Mathf.CeilToInt(gameManager.DurationSeconds);
        var timer = new Rect(safe.center.x - 112, top, 224, 104);
        GameUi.Surface(timer, "paper");
        GameUi.Text(new Rect(timer.x + 12, timer.y + 6, 200, 26), "Tiempo en la plaza", 18,
            GameUi.Ink, TextAnchor.MiddleCenter);
        GameUi.Icon("clock", new Rect(timer.x + 16, timer.y + 36, 32, 32));
        GameUi.Text(new Rect(timer.x + 52, timer.y + 25, 160, 56),
            $"{seconds / 60:00}:{seconds % 60:00}", 44,
            seconds >= duration - 30 ? GameUi.Red : GameUi.Ink, TextAnchor.MiddleCenter);
        GameUi.Text(new Rect(timer.x + 12, timer.y + 75, 200, 26),
            $"Meta {duration / 60:00}:{duration % 60:00}", 18, GameUi.Ink, TextAnchor.MiddleCenter);

        Color danger = displayedQuilombo >= 25 ? GameUi.Red : displayedQuilombo >= 10 ? GameUi.Orange : GameUi.Gold;
        float qx = right - 284;
        GameUi.Surface(new Rect(qx, top, 284, 104), "enamel");
        GameUi.Icon("flame", new Rect(qx + 16, top + 14, 30, 30));
        GameUi.Text(new Rect(qx + 58, top + 9, 118, 32), "Quilombo", 24, GameUi.Cream);
        GameUi.Text(new Rect(qx + 178, top + 9, 90, 32), quilomboLabel, 20, GameUi.Cream, TextAnchor.MiddleRight);
        GameUi.Meter(new Rect(qx + 58, top + 48, 210, 12), displayedQuilombo / quilombo.Max, danger, 10);
        GameUi.Text(new Rect(qx + 58, top + 68, 210, 28),
            displayedQuilombo >= 25 ? "Al rojo vivo" : displayedQuilombo >= 10 ? "Se picó" : "Barrio tranqui",
            20, GameUi.Cream);

        if (Time.unscaledTime < feedbackUntil && gameManager.State == GameManager.GameState.Playing &&
            (upgrades == null || !upgrades.IsShowing))
        {
            float slide = Mathf.Clamp01((feedbackUntil - Time.unscaledTime) * 5f);
            var feedback = new Rect(safe.center.x - 320, top + 116 - (1 - slide) * 8, 640, 44);
            GameUi.Panel(feedback, GameUi.Gold);
            GameUi.Text(new Rect(feedback.x + 20, feedback.y + 6, 600, 32),
                feedbackMessage, 20, GameUi.Cream, TextAnchor.MiddleCenter);
        }

        int count = weapons.EquippedWeaponCount;
        float equipmentY = height - 94;
        GameUi.Surface(new Rect(left, equipmentY - 38, 296, 116), "enamel");
        GameUi.Text(new Rect(left + 12, equipmentY - 34, 272, 30), "A mano · " + count + " de 4", 20, GameUi.Cream);
        for (int i = 0; i < WeaponController.MaxEquippedWeapons; i++)
        {
            var slot = new Rect(left + 12 + i * (62 + GameUi.Gap), equipmentY, 62, 70);
            GameUi.Surface(slot, "slot");
            if (i < count) GameUi.Outline(slot, GameUi.Muted, 1);
            if (i < count)
            {
                WeaponData item = weapons.GetEquippedWeapon(i);
                GameUi.Icon(GameUi.WeaponIcon(item), new Rect(slot.x + 11, slot.y + 3, 40, 40));
                GameUi.Text(new Rect(slot.x, slot.y + 40, 62, 28),
                    "N " + weapons.GetWeaponLevel(item), 20, GameUi.Cream, TextAnchor.MiddleCenter);
                if (slot.Contains(Event.current.mousePosition))
                {
                    GameUi.Panel(new Rect(left, equipmentY - 62, 300, 32), GameUi.Gold);
                    GameUi.Text(new Rect(left + 12, equipmentY - 60, 276, 28), item.name, 18, GameUi.Cream);
                }
            }
            else GameUi.Text(slot, "—", 26, GameUi.Concrete, TextAnchor.MiddleCenter);
        }
        if (upgrades != null)
        {
            GameUi.Text(new Rect(left + 312, height - 88, 86, 30), "Pasivas", 18, GameUi.Cream);
            for (int i = 0; i < WeaponController.MaxEquippedPassives; i++)
            {
                var slot = new Rect(left + 312 + i * 56, height - 54, 50, 34);
                GameUi.Surface(slot, "slot");
                if (i >= upgrades.SelectedPassives.Count)
                {
                    GameUi.Text(slot, "—", 20, GameUi.Concrete, TextAnchor.MiddleCenter);
                    continue;
                }
                UpgradeData passive = upgrades.SelectedPassives[i];
                GameUi.Icon(GameUi.UpgradeIcon(passive.Effect), new Rect(slot.x, slot.y, 24, 24));
                GameUi.Text(new Rect(slot.x + 26, slot.y, 24, 28), upgrades.GetLevel(passive).ToString(), 16, GameUi.Cream);
                if (slot.Contains(Event.current.mousePosition))
                {
                    GameUi.Panel(new Rect(left + 260, equipmentY - 26, 280, 32), GameUi.Cyan);
                    GameUi.Text(new Rect(left + 272, equipmentY - 24, 256, 28), passive.DisplayName, 18, GameUi.Cream);
                }
            }
        }
        DrawPoiIndicators(Rect.MinMaxRect(left, top + 172, right, height - 158), new Vector2(width, GameUi.Height));

        Health boss = gameManager.BossHealth;
        if (boss != null && !boss.IsDead)
        {
            var bossPanel = new Rect(safe.center.x - 210, height - 118, 420, 43);
            GameUi.Panel(bossPanel, GameUi.Red);
            GameUi.Text(new Rect(bossPanel.x + 12, bossPanel.y + 3, 396, 22),
                $"La Jefa  {boss.CurrentHealth}/{boss.MaximumHealth}", 18, GameUi.Cream, TextAnchor.MiddleCenter);
            GameUi.Bar(new Rect(bossPanel.x + 12, bossPanel.y + 28, 396, 7),
                (float)boss.CurrentHealth / boss.MaximumHealth, GameUi.Red);
        }
        GameUi.Surface(new Rect(right - 262, height - 50, 262, 34), "enamel");
        GameUi.Text(new Rect(right - 250, height - 48, 238, 30), "Esc · Tomate un respiro", 18,
            GameUi.Cream, TextAnchor.MiddleRight);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (gameManager.IsFastForwarding)
            GameUi.Text(new Rect(right - 240, height - 68, 240, 22), "DEV x10  ·  F9 NORMAL", 14, GameUi.Gold);
#endif
        GameUi.End(previous);
    }

    private void DrawPoiIndicators(Rect area, Vector2 screenSize)
    {
        int placed = 0;
        foreach (RiskZone zone in pointsOfInterest)
        {
            if (zone == null || !zone.isActiveAndEnabled || !zone.HasPendingReward) continue;
            Vector3 viewport = zone.ViewportPosition;
            if (!TryGetPoiPanel(viewport, screenSize, area, out Rect panel)) continue;
            panel = SeparatePoiPanels(panel, poiPanels, placed, area);
            poiPanels[placed++] = panel;

            GameUi.Fill(panel, new Color(0.09f, 0.14f, 0.17f, 0.85f));
            var arrow = new Vector2(panel.x + 18f, panel.center.y);
            Vector2 direction = new Vector2(viewport.x * screenSize.x,
                (1f - viewport.y) * screenSize.y) - arrow;
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = previous * Matrix4x4.TRS(new Vector3(arrow.x, arrow.y, 0f),
                Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg), Vector3.one);
            GameUi.Icon("arrow", new Rect(-12f, -12f, 24f, 24f), GameUi.Muted);
            GUI.matrix = previous;
            GameUi.Text(new Rect(panel.x + 36f, panel.y + 6f, panel.width - 48f, 28f),
                zone.DisplayName, 18, GameUi.Cream);
        }
    }

    private static bool TryGetPoiPanel(Vector3 viewport, Vector2 screenSize, Rect area, out Rect panel)
    {
        panel = default;
        if (viewport.z <= 0f || (viewport.x >= 0f && viewport.x <= 1f &&
            viewport.y >= 0f && viewport.y <= 1f)) return false;

        Vector2 half = new Vector2(116f, 20f);
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

    private static Rect SeparatePoiPanels(Rect panel, Rect[] others, int count, Rect area)
    {
        bool side = Mathf.Approximately(panel.xMin, area.xMin) ||
            Mathf.Approximately(panel.xMax, area.xMax);
        float origin = side ? panel.y : panel.x;
        float stepSize = (side ? panel.height : panel.width) + 6f;
        for (int step = 0; step <= count * 2; step++)
        {
            for (int sign = 0; sign < (step == 0 ? 1 : 2); sign++)
            {
                Rect candidate = panel;
                float position = origin + step * stepSize * (sign == 0 ? 1 : -1);
                if (side) candidate.y = Mathf.Clamp(position, area.yMin, area.yMax - panel.height);
                else candidate.x = Mathf.Clamp(position, area.xMin, area.xMax - panel.width);
                bool free = true;
                for (int i = 0; i < count; i++)
                {
                    Rect occupied = others[i];
                    occupied.xMin -= 6f;
                    occupied.xMax += 6f;
                    occupied.yMin -= 6f;
                    occupied.yMax += 6f;
                    if (candidate.Overlaps(occupied)) { free = false; break; }
                }
                if (free) return candidate;
            }
        }
        // shortcut: tres POIs caben en los bordes actuales; agrupar si más zonas saturan el área.
        return panel;
    }
}
