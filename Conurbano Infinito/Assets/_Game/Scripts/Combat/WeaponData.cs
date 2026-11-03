using UnityEngine;

public enum WeaponPattern
{
    Straight,
    Boleadora,
    Returning,
    Orbiting,
    Explosive,
    Ricochet
}

[CreateAssetMenu(fileName = "Weapon", menuName = "Conurbano Infinito/Arma")]
public sealed class WeaponData : ScriptableObject
{
    [SerializeField] private WeaponPattern pattern;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField, Min(0.01f)] private float fireInterval = 1f;
    [Tooltip("Unidades/segundo; para orbitales, grados/segundo.")]
    [SerializeField, Min(0.01f)] private float projectileSpeed = 8f;
    [SerializeField, Min(1)] private int projectileCount = 1;
    [Tooltip("Impactos adicionales; para Ricochet, rebotes entre enemigos.")]
    [SerializeField, Min(0)] private int pierce;
    [SerializeField, Min(0.01f)] private float range = 6f;
    [SerializeField, Min(0f)] private float spreadDegrees = 20f;
    [SerializeField, Min(0.1f)] private float explosionRadius = 1.5f;
    [SerializeField] private Sprite projectileSprite;
    [SerializeField] private Color projectileColor = Color.white;
    [SerializeField] private Vector2 projectileScale = new(0.2f, 0.4f);

    public WeaponPattern Pattern => pattern;
    public int Damage => damage;
    public float FireInterval => fireInterval;
    public float ProjectileSpeed => projectileSpeed;
    public int ProjectileCount => projectileCount;
    public int Pierce => pierce;
    public float Range => range;
    public float SpreadDegrees => spreadDegrees;
    public float ExplosionRadius => explosionRadius;
    public Sprite ProjectileSprite => projectileSprite;
    public Color ProjectileColor => projectileColor;
    public Vector2 ProjectileScale => projectileScale;
}
