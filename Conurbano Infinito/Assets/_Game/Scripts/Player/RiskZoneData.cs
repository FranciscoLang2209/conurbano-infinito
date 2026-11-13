using UnityEngine;

public enum RiskZoneReward { Damage, CollectionRadius, Healing, TemporaryDamage, TemporaryAttack, TemporaryMovement }

[CreateAssetMenu(fileName = "Zona de riesgo", menuName = "Conurbano Infinito/Zona de riesgo")]
public sealed class RiskZoneData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private string completionMessage = "¡Desafío completo!";
    [SerializeField, Min(0f)] private float requiredDuration = 5f;
    [SerializeField, Min(0f)] private float quilomboReward = 15f;
    [SerializeField, Min(1f)] private float damageMultiplier = 1f;
    [SerializeField] private RiskZoneReward reward;
    [SerializeField, Min(1f)] private float collectionRadiusMultiplier = 1f;
    [SerializeField, Min(0f)] private float rewardDuration;
    [SerializeField] private string timerLabel;
    [SerializeField, Min(1)] private int healingAmount = 2;
    [SerializeField, Range(0f, 0.99f)] private float temporaryBonus;

    public string DisplayName => displayName;
    public string CompletionMessage => completionMessage;
    public float RequiredDuration => requiredDuration;
    public float QuilomboReward => quilomboReward;
    public float DamageMultiplier => damageMultiplier;
    public float RewardDuration => rewardDuration;
    public string TimerLabel => timerLabel;
    public int HealingAmount => healingAmount;
    public bool HasReward => reward switch
    {
        RiskZoneReward.Damage => damageMultiplier > 1f,
        RiskZoneReward.CollectionRadius => collectionRadiusMultiplier > 1f,
        RiskZoneReward.Healing => healingAmount > 0,
        RiskZoneReward.TemporaryDamage or RiskZoneReward.TemporaryAttack or RiskZoneReward.TemporaryMovement => temporaryBonus > 0f && rewardDuration > 0f,
        _ => false
    };
    public string RewardText => reward switch
    {
        RiskZoneReward.Damage => $"+{(damageMultiplier - 1f) * 100f:0.#}% de daño en esta partida",
        RiskZoneReward.CollectionRadius => $"+{(collectionRadiusMultiplier - 1f) * 100f:0.#}% de radio de recolección por {rewardDuration:0.#} s",
        RiskZoneReward.Healing => $"Al completar: recuperás hasta {healingAmount} de vida",
        RiskZoneReward.TemporaryDamage => $"+{temporaryBonus * 100f:0.#}% de daño por {rewardDuration:0.#} s",
        RiskZoneReward.TemporaryAttack => $"{temporaryBonus * 100f:0.#}% menos espera entre ataques por {rewardDuration:0.#} s",
        RiskZoneReward.TemporaryMovement => $"+{temporaryBonus * 100f:0.#}% de velocidad por {rewardDuration:0.#} s",
        _ => ""
    };

    public void ApplyReward(PlayerController player)
    {
        if (reward == RiskZoneReward.Damage)
            player.GetComponent<WeaponController>().ApplyDamageMultiplier(damageMultiplier);
        else if (reward == RiskZoneReward.CollectionRadius)
            player.GetComponent<ExperienceSystem>().ApplyCollectionRadiusMultiplier(
                collectionRadiusMultiplier, rewardDuration);
        else if (reward == RiskZoneReward.Healing)
            player.GetComponent<Health>().Heal(healingAmount);
        else if (reward == RiskZoneReward.TemporaryDamage)
            player.GetComponent<WeaponController>().ApplyTemporaryDamage(temporaryBonus, rewardDuration);
        else if (reward == RiskZoneReward.TemporaryAttack)
            player.GetComponent<WeaponController>().ApplyTemporaryAttack(temporaryBonus, rewardDuration);
        else if (reward == RiskZoneReward.TemporaryMovement)
            player.ApplyTemporaryMovement(temporaryBonus, rewardDuration);
    }
}
