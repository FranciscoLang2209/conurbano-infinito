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
    private string quilomboLabel;
    private string feedbackMessage;
    private string queuedFeedback;
    private float feedbackUntil;

    private void Awake()
    {
        if (playerHealth != null) weapons = playerHealth.GetComponent<WeaponController>();
        upgrades = GetComponent<UpgradeSelection>();
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

        GameUi.Panel(new Rect(12, 10, 242, 134), GameUi.Red);
        GameUi.Text(new Rect(23, 18, 156, 22), $"EL PIBE  ·  NIV {experience.Level}", 16, GameUi.Cream);
        GameUi.Text(new Rect(178, 18, 65, 22),
            $"{playerHealth.CurrentHealth}/{playerHealth.MaximumHealth}", 14, GameUi.Cream,
            TextAnchor.MiddleRight);
        GameUi.Bar(new Rect(23, 43, 220, 15),
            (float)playerHealth.CurrentHealth / playerHealth.MaximumHealth, GameUi.Red);
        GameUi.Text(new Rect(23, 63, 218, 17),
            $"EXPERIENCIA  {experience.CurrentExperience}/{experience.RequiredExperience}", 12, GameUi.Cream);
        GameUi.Bar(new Rect(23, 83, 220, 10),
            (float)experience.CurrentExperience / experience.RequiredExperience, GameUi.Cyan);
        GameUi.Text(new Rect(23, 99, 218, 17), $"QUILOMBO  {quilomboLabel}", 12, GameUi.Gold);
        GameUi.Bar(new Rect(23, 118, 220, 10), displayedQuilombo / quilombo.Max, GameUi.Gold);

        if (Time.unscaledTime < feedbackUntil)
        {
            var feedback = new Rect((width - 520f) / 2f, 99f, 520f, 54f);
            GameUi.Panel(feedback, GameUi.Red);
            GameUi.Text(new Rect(feedback.x + 12f, feedback.y + 8f, 496f, 38f),
                feedbackMessage, 21, GameUi.Cream, TextAnchor.MiddleCenter);
        }

        int seconds = Mathf.FloorToInt(gameManager.ElapsedSeconds);
        var timer = new Rect((width - 204) / 2f, 10, 204, 91);
        GameUi.Panel(timer, GameUi.Gold);
        GameUi.Text(new Rect(timer.x + 7, timer.y + 8, timer.width - 14, 18),
            "AGUANTÁ HASTA 10:00", 13, GameUi.Gold, TextAnchor.MiddleCenter);
        GameUi.Text(new Rect(timer.x, timer.y + 24, timer.width, 41),
            $"{seconds / 60:00}:{seconds % 60:00}", 34, GameUi.Cream, TextAnchor.MiddleCenter);
        for (int i = 0; i < 10; i++)
            GameUi.Fill(new Rect(timer.x + 12 + i * 18, timer.y + 72, 14, 6),
                seconds >= (i + 1) * 60 ? GameUi.Gold : GameUi.Ink);

        int count = weapons.EquippedWeaponCount;
        int passiveCount = upgrades == null ? 0 : upgrades.SelectedPassives.Count;
        var equipment = new Rect(width - 238, 10, 226, 37 + count * 27 +
            (passiveCount > 0 ? 25 + passiveCount * 21 : 0));
        GameUi.Panel(equipment, GameUi.Cyan);
        GameUi.Text(new Rect(equipment.x + 11, equipment.y + 10, 202, 21),
            "EQUIPO / ARMAS", 15, GameUi.Cream);
        for (int i = 0; i < count; i++)
        {
            WeaponData item = weapons.GetEquippedWeapon(i);
            float y = equipment.y + 34 + i * 27;
            GameUi.Fill(new Rect(equipment.x + 9, y, 208, 24), GameUi.Ink);
            GameUi.Icon(item.ProjectileSprite, new Rect(equipment.x + 12, y + 2, 20, 20));
            GameUi.Text(new Rect(equipment.x + 38, y, 174, 24), item.name, 13, GameUi.Cream);
        }
        if (passiveCount > 0)
        {
            float y = equipment.y + 37 + count * 27;
            GameUi.Text(new Rect(equipment.x + 11, y, 202, 19), "PASIVAS", 13, GameUi.Gold);
            for (int i = 0; i < passiveCount; i++)
            {
                UpgradeData passive = upgrades.SelectedPassives[i];
                GameUi.Text(new Rect(equipment.x + 13, y + 20 + i * 21, 195, 20),
                    $"{passive.DisplayName}  ×{upgrades.GetLevel(passive)}", 12, GameUi.Cream);
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
