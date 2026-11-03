using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Personaje", menuName = "Conurbano Infinito/Personaje")]
public sealed class CharacterData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField, Min(1)] private int startingHealth = 5;
    [SerializeField, Min(0.1f)] private float movementSpeed = 5f;
    [SerializeField] private WeaponData startingWeapon;
    [SerializeField] private UpgradeData firstWeaponUpgrade;
    [SerializeField] private Sprite portrait;
    [SerializeField] private RuntimeAnimatorController animatorController;

    public string DisplayName => displayName;
    public string Description => description;
    public int StartingHealth => startingHealth;
    public float MovementSpeed => movementSpeed;
    public WeaponData StartingWeapon => startingWeapon;
    public UpgradeData FirstWeaponUpgrade => firstWeaponUpgrade;
    public Sprite Portrait => portrait;
    public RuntimeAnimatorController AnimatorController => animatorController;

    private static CharacterData[] all;
    private static CharacterData selected;

    public static CharacterData[] All
    {
        get
        {
            if (all != null) return all;
            all = Resources.LoadAll<CharacterData>("Characters");
            Array.Sort(all, (a, b) => string.CompareOrdinal(a.name, b.name));
            if (all.Length == 0) throw new InvalidOperationException("Faltan personajes en Resources/Characters.");
            foreach (CharacterData character in all)
                if (character.startingHealth < 1 || !(character.movementSpeed > 0f) ||
                    float.IsInfinity(character.movementSpeed) || character.startingWeapon == null ||
                    character.firstWeaponUpgrade == null || character.portrait == null ||
                    character.animatorController == null || string.IsNullOrWhiteSpace(character.displayName))
                    throw new InvalidOperationException("Personaje incompleto: " + character.name);
            return all;
        }
    }

    public static CharacterData Selected => selected != null ? selected : All[0];

    public static void Select(CharacterData character)
    {
        if (Array.IndexOf(All, character) < 0) throw new ArgumentException("Personaje fuera del catálogo.");
        selected = character;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSelection()
    {
        all = null;
        selected = null;
    }
}
