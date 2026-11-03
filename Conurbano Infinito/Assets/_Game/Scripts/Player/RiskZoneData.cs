using UnityEngine;

public enum RiskZoneReward { Damage, CollectionRadius, Healing }

[CreateAssetMenu(fileName = "Zona de riesgo", menuName = "Conurbano Infinito/Zona de riesgo")]
public sealed class RiskZoneData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private string completionMessage = "¡DESAFÍO COMPLETADO!";
    [SerializeField, Min(0f)] private float requiredDuration = 5f;
    [SerializeField, Min(0f)] private float quilomboReward = 15f;
    [SerializeField, Min(1f)] private float damageMultiplier = 1f;
    [SerializeField] private RiskZoneReward reward;
    [SerializeField, Min(1f)] private float collectionRadiusMultiplier = 1f;
    [SerializeField, Min(0f)] private float rewardDuration;
    [SerializeField] private string timerLabel;
    [SerializeField, Min(1)] private int healingAmount = 2;

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
        _ => false
    };
    public string RewardText => reward switch
    {
        RiskZoneReward.Damage => $"+{(damageMultiplier - 1f) * 100f:0.#}% de daño",
        RiskZoneReward.CollectionRadius => $"Radio de recolección +{(collectionRadiusMultiplier - 1f) * 100f:0.#}%",
        RiskZoneReward.Healing => $"Hasta +{healingAmount} de vida",
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
    }
}
