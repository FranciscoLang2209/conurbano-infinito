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
    [SerializeField] private UpgradeData firstWeaponUpgrade;

    private readonly Dictionary<UpgradeData, int> levels = new();
    private readonly List<UpgradeData> selectedPassives = new();
    private readonly List<UpgradeData> options = new(3);
    private readonly List<UpgradeData> available = new();
    private int pendingSelections;
    private bool isShowing;
    private int focusedOption;

    public bool IsShowing => isShowing;
    public IReadOnlyList<UpgradeData> Options => options;
    public IReadOnlyList<UpgradeData> SelectedPassives => selectedPassives;
    public int GetLevel(UpgradeData upgrade) => levels.TryGetValue(upgrade, out int level) ? level : 0;

    public int GetWeaponLevel(WeaponData data)
    {
        int level = 1;
        foreach (var entry in levels)
            if (entry.Key.TargetWeapon == data && entry.Key.Effect != UpgradeEffect.EquipWeapon)
                level += entry.Value;
        return level;
    }

    private void Awake()
    {
        firstWeaponUpgrade = CharacterData.Selected.FirstWeaponUpgrade;
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
        focusedOption = 0;
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

        if (experience.Level == 2 && firstWeaponUpgrade != null &&
            available.Contains(firstWeaponUpgrade) && !options.Contains(firstWeaponUpgrade))
            options[0] = firstWeaponUpgrade;

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
        focusedOption = GameUi.Navigate(focusedOption, options.Count, true);
        if (GameUi.Confirm)
        {
            Select(focusedOption);
            return;
        }
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
        GameUi.Overlay();

        var panel = new Rect(GameUi.SafeArea.center.x - 472, GameUi.SafeArea.center.y - 266, 944, 532);
        GameUi.Sign(panel, GameUi.Petrol);
        GameUi.Text(new Rect(panel.x + 22, panel.y + 14, 900, 44), "Elegí una mejora", 28,
            GameUi.Ink, TextAnchor.MiddleCenter);
        GameUi.Text(new Rect(panel.x + 22, panel.y + 60, 900, 30),
            "1 / 2 / 3 · Mouse · Flechas y Enter", 16, GameUi.Petrol, TextAnchor.MiddleCenter);

        for (int i = 0; i < options.Count; i++)
        {
            UpgradeData upgrade = options[i];
            var card = new Rect(panel.x + 22 + i * 304, panel.y + 100, 288, 408);
            bool hover = card.Contains(Event.current.mousePosition);
            var face = new Rect(card.x, card.y - (hover ? 3 : 0), card.width, card.height);
            GameUi.Card(face, hover || focusedOption == i);
            GameUi.Fill(new Rect(face.x + 109, face.y + 24, 70, 70), GameUi.Petrol);
            Sprite icon = upgrade.TargetWeapon != null ? GameUi.WeaponIcon(upgrade.TargetWeapon) : upgrade.Icon;
            if (icon != null)
                GameUi.Icon(icon, new Rect(face.x + 120, face.y + 35, 48, 48));
            else
                GameUi.Icon(GameUi.UpgradeIcon(upgrade.Effect), new Rect(face.x + 120, face.y + 35, 48, 48));
            levels.TryGetValue(upgrade, out int level);
            GameUi.Text(new Rect(face.x + 24, face.y + 106, 240, 96),
                upgrade.DisplayName, 24, GameUi.Ink, TextAnchor.MiddleCenter, true);
            GameUi.Fill(new Rect(face.x + 24, face.y + 212, 240, 1), GameUi.Petrol);
            GameUi.Text(new Rect(face.x + 24, face.y + 230, 240, 88),
                upgrade.Description, 20, GameUi.Ink, TextAnchor.UpperCenter, true);
            GameUi.Tag(new Rect(face.x + 24, face.y + 360, 164, 30),
                $"Nivel {level + 1}/{upgrade.MaximumLevel}", GameUi.Gold);
            GameUi.Key(new Rect(face.x + 222, face.y + 360, 42, 30), (i + 1).ToString());
            if (GUI.Button(card, GUIContent.none, GUIStyle.none))
            {
                Select(i);
                GameUi.End(previous);
                GUIUtility.ExitGUI();
            }
        }
        GameUi.End(previous);
    }
}
