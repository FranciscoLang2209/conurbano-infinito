using System.Collections.Generic;
using UnityEditor;
using System.Threading.Tasks;
using UnityEngine;

public static class Hito6Check
{
    [MenuItem("Tools/Conurbano Infinito/Mostrar selección hito 6")]
    public static void ShowSelection()
    {
        if (!Application.isPlaying)
        {
            throw new System.Exception("Iniciá Play Mode en Main para mostrar una selección.");
        }

        ExperienceSystem experience = Object.FindFirstObjectByType<ExperienceSystem>();
        experience.AddExperience(experience.RequiredExperience - experience.CurrentExperience);
    }

    [MenuItem("Tools/Conurbano Infinito/Comprobar hito 6")]
    public static async void Run()
    {
        if (!Application.isPlaying)
        {
            throw new System.Exception("Iniciá Play Mode en Main y ejecutá esta comprobación de inmediato.");
        }

        UpgradeSelection selection = Object.FindFirstObjectByType<UpgradeSelection>();
        ExperienceSystem experience = Object.FindFirstObjectByType<ExperienceSystem>();
        PlayerController movement = Object.FindFirstObjectByType<PlayerController>();
        WeaponController weapon = Object.FindFirstObjectByType<WeaponController>();
        Health health = movement != null ? movement.GetComponent<Health>() : null;
        GameManager game = Object.FindFirstObjectByType<GameManager>();
        if (selection == null || experience == null || movement == null || weapon == null || health == null ||
            game == null || game.State != GameManager.GameState.Playing || experience.Level != 1 ||
            experience.CurrentExperience != 0 || Time.timeScale != 1f)
        {
            throw new System.Exception("Main debe estar recién iniciada, sin experiencia previa.");
        }

        var chosenLevels = new Dictionary<UpgradeData, int>();
        experience.AddExperience(26);
        for (int i = 0; i < 3; i++)
        {
            if (!selection.IsShowing || Time.timeScale != 0f || selection.Options.Count != 3 ||
                new HashSet<UpgradeData>(selection.Options).Count != 3)
            {
                throw new System.Exception("La selección no pausó, no ofreció tres mejoras distintas o perdió una subida.");
            }

            // Leave room for the four legacy passives whose effects are checked below.
            int chosenIndex = 0;
            while (selection.Options[chosenIndex].Effect == UpgradeEffect.PassiveExplosionRadius ||
                selection.Options[chosenIndex].Effect == UpgradeEffect.PassiveProjectileCount) chosenIndex++;
            UpgradeData chosen = selection.Options[chosenIndex];
            chosenLevels.TryGetValue(chosen, out int level);
            chosenLevels[chosen] = level + 1;
            int frame = Time.frameCount;
            selection.Select(chosenIndex);
            while (Time.frameCount == frame) await Task.Delay(10);
        }

        if (selection.IsShowing || Time.timeScale != 1f || selection.Options.Count != 0)
        {
            throw new System.Exception("La última selección no restauró el tiempo o dejó el panel abierto.");
        }

        UpgradeData damage = Load("Chimichurri Casero");
        UpgradeData interval = Load("Yerba Premium");
        UpgradeData speed = Load("Zapatillas Flecha");
        UpgradeData life = Load("Alfajor Triple");

        int oldDamage = weapon.Damage;
        damage.Apply(movement, weapon, health);
        float oldInterval = weapon.FireInterval;
        interval.Apply(movement, weapon, health);
        float oldSpeed = movement.MovementSpeed;
        speed.Apply(movement, weapon, health);
        int oldMaximumHealth = health.MaximumHealth;
        int oldCurrentHealth = health.CurrentHealth;
        life.Apply(movement, weapon, health);

        if (weapon.Damage != oldDamage + 1 ||
            !Mathf.Approximately(weapon.FireInterval, oldInterval * 0.9f) ||
            !Mathf.Approximately(movement.MovementSpeed, oldSpeed * 1.1f) ||
            health.MaximumHealth != oldMaximumHealth + 1 || health.CurrentHealth != oldCurrentHealth + 1)
        {
            throw new System.Exception("Una de las cuatro mejoras no aplicó su efecto inmediatamente.");
        }

        var serializedSelection = new SerializedObject(selection);
        SerializedProperty configuredUpgrades = serializedSelection.FindProperty("upgrades");
        int remainingRanks = -3;
        for (int i = 0; i < configuredUpgrades.arraySize; i++)
        {
            var configured = configuredUpgrades.GetArrayElementAtIndex(i).objectReferenceValue as UpgradeData;
            if (configured != null)
            {
                remainingRanks += configured.MaximumLevel;
            }
        }
        for (int i = 0; i <= remainingRanks; i++)
        {
            experience.AddExperience(experience.RequiredExperience - experience.CurrentExperience);
            if (selection.IsFallback) break;
            var distinct = new HashSet<UpgradeData>(selection.Options);
            if (!selection.IsShowing || Time.timeScale != 0f || distinct.Count != selection.Options.Count ||
                selection.Options.Count == 0 || selection.Options.Count > 3)
            {
                throw new System.Exception("La selección ofreció opciones repetidas o perdió una subida.");
            }

            foreach (UpgradeData option in selection.Options)
            {
                if (!option.CanOffer(weapon) ||
                    chosenLevels.TryGetValue(option, out int level) && level >= option.MaximumLevel)
                {
                    throw new System.Exception("Se ofreció una mejora agotada.");
                }
            }

            UpgradeData chosen = selection.Options[0];
            chosenLevels.TryGetValue(chosen, out int chosenLevel);
            chosenLevels[chosen] = chosenLevel + 1;
            int frame = Time.frameCount;
            selection.Select(0);
            while (Time.frameCount == frame) await Task.Delay(10);
        }

        experience.AddExperience(experience.RequiredExperience - experience.CurrentExperience);
        if (!selection.IsFallback || selection.OptionCount != 1 || selection.Options.Count != 0)
            throw new System.Exception("El catálogo agotado no ofrece continuar o curar.");
        while (selection.IsShowing)
        {
            int frame = Time.frameCount;
            selection.Select(0);
            while (Time.frameCount == frame) await Task.Delay(10);
        }
        if (selection.IsShowing || Time.timeScale != 1f || selection.Options.Count != 0)
        {
            throw new System.Exception("Las mejoras agotadas bloquearon la partida.");
        }

        Debug.Log("Hito 6: opciones únicas, límites de nivel, subidas encadenadas, pausa y cuatro efectos correctos.");
    }

    private static UpgradeData Load(string name)
    {
        UpgradeData upgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>($"Assets/_Game/Data/{name}.asset");
        if (upgrade == null || upgrade.MaximumLevel < 1)
        {
            throw new System.Exception($"Falta configurar {name}.");
        }

        return upgrade;
    }
}
