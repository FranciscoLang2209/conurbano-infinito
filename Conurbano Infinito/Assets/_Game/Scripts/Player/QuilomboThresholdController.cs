using UnityEngine;

public sealed class QuilomboThresholdController : MonoBehaviour
{
    [SerializeField] private QuilomboSystem quilombo;
    [SerializeField] private WaveDirector waves;
    [SerializeField] private GameHud hud;

    private bool reached25;
    private bool reached50;

    private void Awake()
    {
        if (quilombo != null && waves != null && hud != null) return;
        Debug.LogError("QuilomboThresholdController requiere QuilomboSystem, WaveDirector y GameHud.", this);
        enabled = false;
    }

    private void OnEnable()
    {
        quilombo.OnQuilomboChanged += OnQuilomboChanged;
        OnQuilomboChanged(quilombo.Current);
    }

    private void OnDisable()
    {
        if (quilombo != null) quilombo.OnQuilomboChanged -= OnQuilomboChanged;
    }

    private void OnQuilomboChanged(float value)
    {
        if (!reached25 && value >= 25f)
        {
            reached25 = true;
            waves.SetQuilomboThresholdBonus(0.15f, false);
            hud.ShowQuilomboFeedback("EL BARRIO SE ESTÁ PICANDO");
        }

        if (!reached50 && value >= 50f)
        {
            reached50 = true;
            waves.SetQuilomboThresholdBonus(0.35f, true);
            hud.ShowQuilomboFeedback("SE ESTÁ YENDO TODO AL CARAJO");
        }
    }
}
