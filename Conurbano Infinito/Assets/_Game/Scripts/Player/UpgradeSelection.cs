using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class UpgradeSelection : MonoBehaviour
{
    [SerializeField] private ExperienceSystem experience;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerController movement;
    [SerializeField] private WeaponController weapon;
    [SerializeField] private Health health;
    [SerializeField] private UpgradeData[] upgrades;

    private readonly Dictionary<UpgradeData, int> levels = new();
    private readonly List<UpgradeData> selectedPassives = new();
    private readonly List<UpgradeData> options = new(3);
    private readonly List<UpgradeData> available = new();
    private int pendingSelections;
    private bool isShowing;

    public bool IsShowing => isShowing;
    public IReadOnlyList<UpgradeData> Options => options;
    public IReadOnlyList<UpgradeData> SelectedPassives => selectedPassives;
    public int GetLevel(UpgradeData upgrade) => levels.TryGetValue(upgrade, out int level) ? level : 0;

    private void Awake()
    {
        if (experience == null || gameManager == null || movement == null || weapon == null ||
            health == null || upgrades == null || upgrades.Length == 0)
        {
            Debug.LogError("UpgradeSelection requiere experiencia, partida, jugador y mejoras.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (experience != null)
        {
            experience.LevelUp += OnLevelUp;
        }
    }

    private void OnDisable()
    {
        if (experience != null)
        {
            experience.LevelUp -= OnLevelUp;
        }

        if (isShowing)
        {
            isShowing = false;
            if (gameManager != null && gameManager.State == GameManager.GameState.Playing)
            {
                Time.timeScale = 1f;
            }
        }
    }

    private void OnLevelUp(int level)
    {
        if (gameManager.State != GameManager.GameState.Playing)
        {
            return;
        }

        pendingSelections++;
        if (!isShowing)
        {
            ShowNext();
        }
    }

    private void ShowNext()
    {
        options.Clear();
        available.Clear();
        foreach (UpgradeData upgrade in upgrades)
        {
            if (upgrade != null && upgrade.CanOffer(weapon) && !available.Contains(upgrade) &&
                (!levels.TryGetValue(upgrade, out int level) || level < upgrade.MaximumLevel))
            {
                available.Add(upgrade);
            }
        }

        for (int i = 0; i < 3 && i < available.Count; i++)
        {
            int chosen = Random.Range(i, available.Count);
            (available[i], available[chosen]) = (available[chosen], available[i]);
            options.Add(available[i]);
        }

        if (options.Count == 0)
        {
            pendingSelections = 0;
            isShowing = false;
            Time.timeScale = 1f;
            return;
        }

        isShowing = true;
        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (!isShowing || gameManager.State != GameManager.GameState.Playing || Keyboard.current == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
        {
            Select(0);
        }
        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
        {
            Select(1);
        }
        else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
        {
            Select(2);
        }
    }

    public void Select(int index)
    {
        if (!isShowing || gameManager.State != GameManager.GameState.Playing ||
            index < 0 || index >= options.Count)
        {
            return;
        }

        UpgradeData selected = options[index];
        selected.Apply(movement, weapon, health);
        levels.TryGetValue(selected, out int level);
        levels[selected] = level + 1;
        if (selected.Effect <= UpgradeEffect.MaximumHealth && level == 0)
            selectedPassives.Add(selected);
        pendingSelections--;
        isShowing = false;

        if (pendingSelections > 0)
        {
            ShowNext();
        }
        else
        {
            options.Clear();
            Time.timeScale = 1f;
        }
    }

    private void OnGUI()
    {
        if (!isShowing || gameManager.State != GameManager.GameState.Playing)
        {
            return;
        }
        GUI.depth = -10;
        Matrix4x4 previous = GameUi.Begin();
        GameUi.Fill(new Rect(0, 0, GameUi.Width, GameUi.Height), new Color(0.09f, 0.14f, 0.17f, 0.84f));

        var panel = new Rect((GameUi.Width - 700) / 2f, (GameUi.Height - 420) / 2f, 700, 420);
        GameUi.Panel(panel, GameUi.Cyan);
        GameUi.Text(new Rect(panel.x + 22, panel.y + 14, 656, 42), "ELEGÍ UNA MEJORA", 28,
            GameUi.Cream, TextAnchor.MiddleCenter);
        GameUi.Text(new Rect(panel.x + 22, panel.y + 53, 656, 25),
            "Elegí con el mouse o las teclas 1, 2 y 3", 16, GameUi.Muted, TextAnchor.MiddleCenter);

        for (int i = 0; i < options.Count; i++)
        {
            UpgradeData upgrade = options[i];
            var card = new Rect(panel.x + 22, panel.y + 88 + i * 102, 656, 92);
            bool hover = card.Contains(Event.current.mousePosition);
            GameUi.Fill(card, hover ? GameUi.Gold : GameUi.Ink);
            GameUi.Fill(new Rect(card.x + 3, card.y + 3, card.width - 6, card.height - 6),
                hover ? new Color32(40, 63, 101, 255) : GameUi.Shadow);
            GameUi.Fill(new Rect(card.x + 11, card.y + 12, 68, 68), GameUi.Ink);
            if (upgrade.Icon != null)
                GameUi.Icon(upgrade.Icon, new Rect(card.x + 21, card.y + 22, 48, 48));
            else
                GameUi.Text(new Rect(card.x + 11, card.y + 12, 68, 68),
                    (i + 1).ToString(), 28, GameUi.Gold, TextAnchor.MiddleCenter);
            levels.TryGetValue(upgrade, out int level);
            GameUi.Text(new Rect(card.x + 91, card.y + 9, 500, 29),
                $"{i + 1}  {upgrade.DisplayName}", 20, GameUi.Cream);
            GameUi.Text(new Rect(card.x + 91, card.y + 37, 510, 37),
                upgrade.Description, 15, GameUi.Muted, TextAnchor.MiddleLeft, true);
            GameUi.Text(new Rect(card.x + 588, card.y + 10, 54, 26),
                $"{level + 1}/{upgrade.MaximumLevel}", 15, GameUi.Gold, TextAnchor.MiddleRight);
            if (GUI.Button(card, GUIContent.none, GUIStyle.none))
            {
                Select(i);
                GUI.matrix = previous;
                GUIUtility.ExitGUI();
            }
        }
        GUI.matrix = previous;
    }
}
