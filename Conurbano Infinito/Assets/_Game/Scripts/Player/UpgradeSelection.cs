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

        var panel = new Rect((GameUi.Width - 700) / 2f, (GameUi.Height - 420) / 2f, 700, 420);
        GameUi.Sign(panel, GameUi.Petrol);
        GameUi.Text(new Rect(panel.x + 22, panel.y + 14, 656, 42), "ELEGÍ UNA MEJORA", 28,
            GameUi.Ink, TextAnchor.MiddleCenter);
        GameUi.Text(new Rect(panel.x + 22, panel.y + 53, 656, 25),
            "Elegí con el mouse o las teclas 1, 2 y 3", 16, GameUi.Petrol, TextAnchor.MiddleCenter);

        for (int i = 0; i < options.Count; i++)
        {
            UpgradeData upgrade = options[i];
            var card = new Rect(panel.x + 22 + i * 220, panel.y + 88, 208, 304);
            bool hover = card.Contains(Event.current.mousePosition);
            var face = new Rect(card.x, card.y - (hover ? 3 : 0), card.width, card.height);
            GameUi.Card(face, hover);
            GameUi.Fill(new Rect(face.x + 69, face.y + 24, 70, 70), GameUi.Petrol);
            Sprite icon = upgrade.Icon != null ? upgrade.Icon :
                upgrade.TargetWeapon != null ? upgrade.TargetWeapon.ProjectileSprite : null;
            if (icon != null)
                GameUi.Icon(icon, new Rect(face.x + 80, face.y + 35, 48, 48));
            else
                GameUi.Text(new Rect(face.x + 69, face.y + 24, 70, 70),
                    (i + 1).ToString(), 28, GameUi.Gold, TextAnchor.MiddleCenter);
            levels.TryGetValue(upgrade, out int level);
            GameUi.Text(new Rect(face.x + 14, face.y + 111, 180, 56),
                upgrade.DisplayName, 18, GameUi.Ink, TextAnchor.MiddleCenter, true);
            GameUi.Fill(new Rect(face.x + 20, face.y + 175, 168, 3), GameUi.Petrol);
            GameUi.Text(new Rect(face.x + 16, face.y + 189, 176, 57),
                upgrade.Description, 14, GameUi.Ink, TextAnchor.UpperCenter, true);
            GameUi.Tag(new Rect(face.x + 43, face.y + 254, 96, 26),
                $"NIV {level + 1}/{upgrade.MaximumLevel}", GameUi.Gold);
            GameUi.Key(new Rect(face.x + 151, face.y + 251, 33, 30), (i + 1).ToString());
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
