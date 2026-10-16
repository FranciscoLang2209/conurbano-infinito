using UnityEngine;

public sealed class AudioDirector : MonoBehaviour
{
    private const string VolumeKey = "ConurbanoInfinito.MasterVolume";

    [SerializeField] private AudioClip music;
    [SerializeField] private AudioClip impact;
    [SerializeField] private AudioClip attackWarning;
    [SerializeField] private AudioClip experiencePickup;
    [SerializeField] private AudioClip levelUp;
    [SerializeField] private AudioClip damage;
    [SerializeField] private AudioClip victory;
    [SerializeField] private AudioClip defeat;
    [SerializeField] private Health playerHealth;
    [SerializeField] private ExperienceSystem experience;
    [SerializeField] private GameManager gameManager;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.45f;
    [SerializeField, Range(0f, 1f)] private float effectsVolume = 0.75f;

    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, masterVolume);
            ApplyVolume();
        }
    }

    private AudioSource musicSource;
    private AudioSource effectsSource;
    private float masterVolume;
    private float nextImpactTime;
    private float nextWarningTime;

    private void Awake()
    {
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.8f));
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.clip = music;
        effectsSource = gameObject.AddComponent<AudioSource>();
        effectsSource.playOnAwake = false;
        effectsSource.spatialBlend = 0f;
        ApplyVolume();
    }

    private void OnEnable()
    {
        if (music != null) musicSource.Play();
        Projectile.Hit += PlayImpact;
        EnemyCharge.WarningStarted += PlayWarning;
        BossAttack.WarningStarted += PlayWarning;
        ExperienceGem.Collected += PlayExperience;
        if (playerHealth != null) playerHealth.Damaged += PlayDamage;
        if (experience != null) experience.LevelUp += PlayLevelUp;
        if (gameManager != null) gameManager.StateChanged += PlayGameState;
    }

    private void OnDisable()
    {
        Projectile.Hit -= PlayImpact;
        EnemyCharge.WarningStarted -= PlayWarning;
        BossAttack.WarningStarted -= PlayWarning;
        ExperienceGem.Collected -= PlayExperience;
        if (playerHealth != null) playerHealth.Damaged -= PlayDamage;
        if (experience != null) experience.LevelUp -= PlayLevelUp;
        if (gameManager != null) gameManager.StateChanged -= PlayGameState;
        if (musicSource != null) musicSource.Stop();
    }

    private void ApplyVolume()
    {
        if (musicSource != null) musicSource.volume = masterVolume * musicVolume;
        if (effectsSource != null) effectsSource.volume = masterVolume * effectsVolume;
    }

    private void Play(AudioClip clip)
    {
        if (clip != null) effectsSource.PlayOneShot(clip);
    }

    private void PlayImpact()
    {
        if (Time.unscaledTime < nextImpactTime) return;
        nextImpactTime = Time.unscaledTime + 0.07f;
        Play(impact);
    }

    private void PlayWarning()
    {
        if (Time.timeScale == 0f || Time.unscaledTime < nextWarningTime) return;
        nextWarningTime = Time.unscaledTime + 0.2f;
        Play(attackWarning);
    }

    private void PlayExperience() => Play(experiencePickup);
    private void PlayLevelUp(int _) => Play(levelUp);
    private void PlayDamage() => Play(damage);

    private void PlayGameState(GameManager.GameState state)
    {
        if (state != GameManager.GameState.Victory && state != GameManager.GameState.Defeat) return;
        musicSource.Stop();
        Play(state == GameManager.GameState.Victory ? victory : defeat);
    }
}
