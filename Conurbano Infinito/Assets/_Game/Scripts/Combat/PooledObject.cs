using UnityEngine;

public sealed class PooledObject : MonoBehaviour
{
    public ObjectPool Owner { get; private set; }
    public GameObject Prefab { get; private set; }
    internal bool IsProjectile { get; private set; }
    internal ExperienceGem Gem { get; private set; }
    private bool registered;

    public void Initialize(ObjectPool owner, GameObject prefab)
    {
        Owner = owner;
        Prefab = prefab;
        IsProjectile = TryGetComponent<Projectile>(out _) || TryGetComponent<EnemyStone>(out _);
        Gem = GetComponent<ExperienceGem>();
        if (isActiveAndEnabled) OnEnable();
    }

    private void OnEnable()
    {
        if (Owner == null || registered) return;
        registered = true;
        Owner.Activated(this);
    }

    private void OnDisable()
    {
        if (!registered) return;
        registered = false;
        if (Owner != null) Owner.Deactivated(this);
    }
}
