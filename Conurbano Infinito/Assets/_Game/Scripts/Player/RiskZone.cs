using UnityEngine;

[RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
public sealed class RiskZone : MonoBehaviour
{
    public enum ZoneState { Inactive, Active, Completed }

    [SerializeField] private RiskZoneData data;
    [SerializeField] private QuilomboSystem quilombo;
    [SerializeField] private Camera gameCamera;

    public ZoneState State { get; private set; }
    public float Progress { get; private set; }
    public bool HasPendingReward => !rewardStarted;
    public string DisplayName => data.DisplayName;
    public Vector3 ViewportPosition => gameCamera.WorldToViewportPoint(transform.position);

    private bool playerInside;
    private PlayerController player;
    private Health playerHealth;
    private RiskZone[] zones;
    private float feedbackUntil;
    private float rewardEndTime;
    private bool rewardStarted;
    private static Sprite markerSprite;

    private void Awake()
    {
        CircleCollider2D area = GetComponent<CircleCollider2D>();
        if (data == null || quilombo == null ||
            gameCamera == null || !area.isTrigger)
        {
            Debug.LogError("RiskZone requiere datos, Quilombo, cámara y un CircleCollider2D trigger.", this);
            enabled = false;
            return;
        }

        SpriteRenderer marker = GetComponent<SpriteRenderer>();
        if (marker.sprite == null)
        {
            if (markerSprite == null)
                markerSprite = Sprite.Create(Texture2D.whiteTexture,
                    new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            marker.sprite = markerSprite;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (State != ZoneState.Inactive || !other.TryGetComponent(out PlayerController player)) return;
        playerHealth = player.GetComponent<Health>();
        if (playerHealth == null || playerHealth.IsDead) return;
        this.player = player;
        playerInside = true;
        State = ZoneState.Active;
        if (data.RequiredDuration == 0f) StartReward();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PlayerController _)) return;
        playerInside = false;
        player = null;
        playerHealth = null;
        if (rewardStarted || State == ZoneState.Completed) return;
        Progress = 0f;
        State = ZoneState.Inactive;
    }

    private void OnDisable()
    {
        playerInside = false;
        player = null;
        playerHealth = null;
        if (rewardStarted || State == ZoneState.Completed) return;
        Progress = 0f;
        State = ZoneState.Inactive;
    }

    private void Update()
    {
        if (State != ZoneState.Active) return;
        if (rewardStarted)
        {
            if (Time.time >= rewardEndTime) Complete();
            return;
        }
        if (!playerInside || playerHealth == null || playerHealth.IsDead) return;
        Progress = Mathf.Min(data.RequiredDuration, Progress + Time.deltaTime);
        if (Progress >= data.RequiredDuration) StartReward();
    }

    private void StartReward()
    {
        rewardStarted = true;
        quilombo.Add(data.QuilomboReward);
        data.ApplyReward(player);
        if (data.RewardDuration > 0f)
            rewardEndTime = Time.time + data.RewardDuration;
        else
            Complete();
    }

    private void Complete()
    {
        State = ZoneState.Completed;
        feedbackUntil = Time.unscaledTime + 3f;
    }

    private void OnGUI()
    {
        if (data == null || gameCamera == null) return;
        GUI.depth = 19;
        Matrix4x4 previous = GameUi.Begin();
        Vector3 screen = gameCamera.WorldToScreenPoint(transform.position + Vector3.up * 1.4f);
        Vector3 viewport = ViewportPosition;
        if (HasPendingReward && viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f &&
            viewport.y >= 0f && viewport.y <= 1f)
        {
            float x = screen.x * GameUi.Width / Screen.width - 116f;
            float y = GameUi.Height - screen.y * GameUi.Height / Screen.height;
            // World labels yield to the HUD, equipment and POI reward notice.
            if (y >= GameUi.SafeArea.y + 188 && y < GameUi.SafeArea.yMax - 158)
            {
                x = Mathf.Clamp(x, GameUi.SafeArea.x + 16, GameUi.SafeArea.xMax - 248);
                GameUi.Fill(new Rect(x, y, 232, 34), new Color(0.09f, 0.14f, 0.17f, 0.8f));
                GameUi.Icon("pin", new Rect(x + 8, y + 9, 16, 16));
                GameUi.Text(new Rect(x + 32, y + 3, 192, 28), data.DisplayName,
                    18, GameUi.Cream, TextAnchor.MiddleLeft);
            }
        }

        if ((State == ZoneState.Active || Time.unscaledTime < feedbackUntil) && HasNoticePriority())
        {
            bool completed = State == ZoneState.Completed;
            bool showReward = rewardStarted && data.HasReward;
            bool previewReward = !rewardStarted && data.HasReward;
            float panelHeight = showReward || previewReward ? 160f : 120f;
            if (!rewardStarted) panelHeight += 28f;
            if (!completed && rewardStarted) panelHeight += 32f;
            var panel = new Rect(GameUi.SafeArea.center.x - 220f,
                GameUi.SafeArea.yMax - panelHeight - 140f, 440f, panelHeight);
            GameUi.Sign(panel, GameUi.Gold);
            GameUi.Text(new Rect(panel.x + 24f, panel.y + 18f, 392f, 34f),
                rewardStarted ? data.CompletionMessage : data.DisplayName,
                24, GameUi.Ink, TextAnchor.MiddleCenter);
            GameUi.Text(new Rect(panel.x + 24f, panel.y + 58f, 392f, 30f),
                rewardStarted ? $"+{data.QuilomboReward:0.#} de Quilombo" :
                    $"Resistí {Progress:0.0}/{data.RequiredDuration:0.#} s · +{data.QuilomboReward:0.#} Q",
                20, GameUi.Ink, TextAnchor.MiddleCenter);
            if (showReward || previewReward)
                GameUi.Text(new Rect(panel.x + 24f, panel.y + 94f, 392f, 30f),
                    data.RewardText, 20, GameUi.Ink, TextAnchor.MiddleCenter);
            if (rewardStarted && !completed)
                GameUi.Text(new Rect(panel.x + 24f, panel.y + 132f, 392f, 30f),
                    $"{data.TimerLabel}: {Mathf.Max(0f, rewardEndTime - Time.time):0.0} s",
                    20, GameUi.Ink, TextAnchor.MiddleCenter);
            if (!rewardStarted)
                GameUi.Text(new Rect(panel.x + 24f, panel.y + (previewReward ? 128f : 94f), 392f, 28f),
                    "El Quilombo aumenta la presión enemiga.", 18, GameUi.Petrol, TextAnchor.MiddleCenter);
            if (!rewardStarted)
                GameUi.Bar(new Rect(panel.x + 24f, panel.yMax - 22f, 392f, 10f),
                    Progress / data.RequiredDuration, GameUi.Gold);
        }
        GameUi.End(previous);
    }

    private bool HasNoticePriority()
    {
        zones ??= FindObjectsByType<RiskZone>(FindObjectsSortMode.None);
        if (State == ZoneState.Active && !rewardStarted) return true;
        foreach (RiskZone zone in zones)
        {
            if (zone == null || zone == this || !zone.isActiveAndEnabled) continue;
            if (zone.State == ZoneState.Active && !zone.rewardStarted) return false;
            if (Time.unscaledTime < zone.feedbackUntil && (zone.feedbackUntil > feedbackUntil ||
                (zone.feedbackUntil == feedbackUntil && zone.GetInstanceID() > GetInstanceID()))) return false;
        }
        return true;
    }
}
