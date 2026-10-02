using UnityEngine;

public sealed class GameHud : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private ExperienceSystem experience;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private QuilomboSystem quilombo;

    private WeaponController weapons;
    private UpgradeSelection upgrades;
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
        Color quilomboColor = displayedQuilombo >= 75f ? GameUi.Red :
            displayedQuilombo >= 50f ? GameUi.Orange : GameUi.Gold;
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
        var timer = new Rect((width - 240) / 2f, 10, 240, 91);
        Color timerAccent = seconds >= 540 ? GameUi.Red : GameUi.Petrol;
        GameUi.Sign(timer, timerAccent);
        GameUi.Text(new Rect(timer.x + 7, timer.y + 8, timer.width - 14, 18),
            "AGUANTÁ HASTA 10:00", 13, GameUi.Ink, TextAnchor.MiddleCenter);
        GameUi.Text(new Rect(timer.x, timer.y + 24, timer.width, 41),
            $"{seconds / 60:00}:{seconds % 60:00}", 37, timerAccent, TextAnchor.MiddleCenter);
        for (int i = 0; i < 10; i++)
            GameUi.Fill(new Rect(timer.x + 21 + i * 20, timer.y + 72, 16, 6),
                seconds >= (i + 1) * 60 ? timerAccent : GameUi.Concrete);

        int count = weapons.EquippedWeaponCount;
        int passiveCount = upgrades == null ? 0 : upgrades.SelectedPassives.Count;
        var equipment = new Rect(width - 238, 10, 226, 37 + count * 27 +
            (passiveCount > 0 ? 25 + passiveCount * 21 : 0));
        GameUi.Sign(equipment, GameUi.Petrol);
        GameUi.Text(new Rect(equipment.x + 11, equipment.y + 10, 202, 21),
            "EQUIPO / ARMAS", 15, GameUi.Ink);
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
}
