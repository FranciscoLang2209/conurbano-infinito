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
    private readonly List<UpgradeData> options = new(3);
    private readonly List<UpgradeData> available = new();
    private int pendingSelections;
    private bool isShowing;
    private int focusedOption;
    private int lastSelectionFrame = -1;
    private bool firstSelection = true;

    public bool IsShowing => isShowing;
    public IReadOnlyList<UpgradeData> Options => options;
    public bool IsFallback => isShowing && options.Count == 0;
    public int OptionCount => IsFallback ? 1 : options.Count;
    public IReadOnlyList<UpgradeData> SelectedPassives => weapon.SelectedPassives;
    public int GetLevel(UpgradeData upgrade) => upgrade.IsPassive ? weapon.GetPassiveLevel(upgrade) :
        levels.TryGetValue(upgrade, out int level) ? level : 0;
    private int FallbackHealing => Mathf.Max(1, Mathf.FloorToInt(health.MaximumHealth * 0.1f));

    public int GetWeaponLevel(WeaponData data) => weapon.GetWeaponLevel(data);

    private void Awake()
    {
        firstWeaponUpgrade = CharacterData.Selected.FirstWeaponUpgrade;
        if (experience == null || gameManager == null || movement == null || weapon == null ||
            health == null || upgrades == null)
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
        if (gameManager.State != GameManager.GameState.Playing || health.IsDead)
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
        UpgradeData evolution = NextEvolution();
        if (evolution != null) options.Add(evolution);
        foreach (UpgradeData upgrade in upgrades)
        {
            if (CanOffer(upgrade) && upgrade.Effect != UpgradeEffect.EvolveWeapon && !available.Contains(upgrade))
            {
                available.Add(upgrade);
            }
        }

        for (int i = 0; options.Count < 3 && i < available.Count; i++)
        {
            int chosen = Random.Range(i, available.Count);
            (available[i], available[chosen]) = (available[chosen], available[i]);
            options.Add(available[i]);
        }

        if (firstSelection && firstWeaponUpgrade != null &&
            available.Contains(firstWeaponUpgrade) && !options.Contains(firstWeaponUpgrade))
            options[options.Count - 1] = firstWeaponUpgrade;

        isShowing = true;
        Time.timeScale = 0f;
    }

    private bool CanOffer(UpgradeData upgrade) =>
        upgrade != null && upgrade.CanOffer(weapon) && GetLevel(upgrade) < upgrade.MaximumLevel;

    private UpgradeData NextEvolution()
    {
        foreach (UpgradeData recipe in UpgradeData.EvolutionCards)
            if (CanOffer(recipe)) return recipe;
        return null;
    }

    private void Update()
    {
        if (!isShowing || gameManager.State != GameManager.GameState.Playing || Keyboard.current == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        focusedOption = GameUi.Navigate(focusedOption, OptionCount, true);
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
            health.IsDead || lastSelectionFrame == Time.frameCount || index < 0 || index >= OptionCount)
        {
            return;
        }

        lastSelectionFrame = Time.frameCount;
        if (IsFallback)
        {
            if (NextEvolution() != null) { ShowNext(); return; }
            // Rebuild if the inventory changed while this choice was open.
            foreach (UpgradeData upgrade in upgrades)
                if (CanOffer(upgrade)) { ShowNext(); return; }
            health.Heal(FallbackHealing);
        }
        else
        {
            UpgradeData selected = options[index];
            if (!CanOffer(selected))
            {
                ShowNext();
                return;
            }
            int level = GetLevel(selected);
            selected.Apply(movement, weapon, health);
            if (!selected.IsPassive) levels[selected] = level + 1;
        }
        firstSelection = false;
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
            $"Armas {weapon.EquippedWeaponCount}/4 · Pasivas {SelectedPassives.Count}/4 · 1 / 2 / 3 · Mouse · Flechas y Enter",
            16, GameUi.Petrol, TextAnchor.MiddleCenter);

        for (int i = 0; i < OptionCount; i++)
        {
            UpgradeData upgrade = IsFallback ? null : options[i];
            var card = new Rect(panel.center.x - (OptionCount * 304 - 16) / 2f + i * 304, panel.y + 100, 288, 408);
            bool hover = card.Contains(Event.current.mousePosition);
            var face = new Rect(card.x, card.y - (hover ? 3 : 0), card.width, card.height);
            GameUi.Card(face, hover || focusedOption == i);
            GameUi.Fill(new Rect(face.x + 109, face.y + 24, 70, 70), GameUi.Petrol);
            Sprite icon = upgrade == null ? null :
                upgrade.TargetWeapon != null ? GameUi.WeaponIcon(upgrade.TargetWeapon) : upgrade.Icon;
            if (icon != null)
                GameUi.Icon(icon, new Rect(face.x + 120, face.y + 35, 48, 48));
            else
                GameUi.Icon(upgrade == null ? "heart" : GameUi.UpgradeIcon(upgrade.Effect), new Rect(face.x + 120, face.y + 35, 48, 48));
            int level = upgrade == null ? 0 : GetLevel(upgrade);
            GameUi.Text(new Rect(face.x + 24, face.y + 106, 240, 96),
                upgrade != null ? upgrade.DisplayName : health.IsFull ? "Continuar" : "Recuperar vida",
                24, GameUi.Ink, TextAnchor.MiddleCenter, true);
            GameUi.Fill(new Rect(face.x + 24, face.y + 212, 240, 1), GameUi.Petrol);
            GameUi.Text(new Rect(face.x + 24, face.y + 230, 240, 88),
                upgrade != null ? upgrade.Description : health.IsFull ? "Vida llena. Seguís sin recompensa." :
                    $"Curás hasta {FallbackHealing} de vida. No quedan mejoras disponibles.",
                20, GameUi.Ink, TextAnchor.UpperCenter, true);
            string action = upgrade == null ? "Catálogo agotado" : upgrade.IsPassive ?
                (level == 0 ? "Adquirir pasiva" : "Mejorar pasiva") :
                upgrade.Effect == UpgradeEffect.EvolveWeapon ? "Evolucionar arma · 8/8" :
                $"{(upgrade.Effect == UpgradeEffect.EquipWeapon ? "Adquirir" : "Mejorar")} arma · {(upgrade.Effect == UpgradeEffect.EquipWeapon ? 1 : weapon.GetWeaponLevel(upgrade.TargetWeapon) + 1)}/{BuildRules.Current.MaximumWeaponLevel}";
            GameUi.Text(new Rect(face.x + 24, face.y + 324, 240, 28), action,
                18, GameUi.Petrol, TextAnchor.MiddleCenter);
            GameUi.Tag(new Rect(face.x + 24, face.y + 360, 164, 30),
                upgrade == null ? "Sin mejoras" : $"{(upgrade.IsPassive ? "Nivel" : "Rango")} {level + 1}/{upgrade.MaximumLevel}", GameUi.Gold);
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
