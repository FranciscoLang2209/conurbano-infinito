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
    public bool HasPendingReward => !runClosed && !rewardStarted;
    public string DisplayName => data.DisplayName;
    public event System.Action<RiskZone> RewardEarned;
    public Vector3 ViewportPosition => gameCamera.WorldToViewportPoint(transform.position);

    private bool playerInside;
    private PlayerController player;
    private Health playerHealth;
    private RiskZone[] zones;
    private float feedbackUntil;
    private float rewardEndTime;
    private bool rewardStarted;
    private bool runClosed;
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
        if (runClosed || Time.timeScale == 0f || State != ZoneState.Inactive ||
            !other.TryGetComponent(out PlayerController player)) return;
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
        if (runClosed || rewardStarted || playerHealth == null || playerHealth.IsDead || Time.timeScale == 0f) return;
        rewardStarted = true;
        feedbackUntil = Time.unscaledTime + 3f;
        quilombo.Add(data.QuilomboReward);
        data.ApplyReward(player);
        RewardEarned?.Invoke(this);
        if (data.RewardDuration > 0f)
            rewardEndTime = Time.time + data.RewardDuration;
        else
            Complete();
    }

    private void Complete()
    {
        State = ZoneState.Completed;
        if (data.RewardDuration == 0f) feedbackUntil = Time.unscaledTime + 3f;
    }

    public void CloseRun()
    {
        runClosed = true;
        playerInside = false;
        player = null;
        playerHealth = null;
        Progress = 0f;
        feedbackUntil = rewardEndTime = 0f;
        State = ZoneState.Completed;
    }

    public bool TryGetNoticePanel(out Rect panel)
    {
        panel = default;
        if (runClosed || Time.timeScale == 0f || data == null ||
            !((State == ZoneState.Active && !rewardStarted) || Time.unscaledTime < feedbackUntil) || !HasNoticePriority())
            return false;
        float height = data.HasReward ? 184f : 120f;
        if (!rewardStarted) height += 28f;
        if (rewardStarted && State != ZoneState.Completed) height += 32f;
        panel = new Rect(GameUi.SafeArea.center.x - 220f, GameUi.SafeArea.yMax - height - 140f, 440f, height);
        return true;
    }

    private void OnGUI()
    {
        if (runClosed || Time.timeScale == 0f || data == null || gameCamera == null) return;
        GUI.depth = 19;
        Matrix4x4 previous = GameUi.Begin();
        if (TryGetNoticePanel(out Rect panel))
        {
            bool completed = State == ZoneState.Completed;
            bool showReward = rewardStarted && data.HasReward;
            bool previewReward = !rewardStarted && data.HasReward;
            GameUi.Sign(panel, GameUi.Gold);
            GameUi.Text(new Rect(panel.x + 24f, panel.y + 18f, 392f, 34f),
                rewardStarted ? data.CompletionMessage : data.DisplayName,
                24, GameUi.Ink, TextAnchor.MiddleCenter);
            GameUi.Text(new Rect(panel.x + 24f, panel.y + 58f, 392f, 30f),
                rewardStarted ? $"+{data.QuilomboReward:0.#} Quilombo · +250 puntos" :
                    $"Quedate en la zona: {Progress:0.0} de {data.RequiredDuration:0.#} s",
                20, GameUi.Ink, TextAnchor.MiddleCenter);
            if (showReward || previewReward)
                GameUi.Text(new Rect(panel.x + 24f, panel.y + 94f, 392f, 52f),
                    data.RewardText, 20, GameUi.Ink, TextAnchor.MiddleCenter, true);
            if (rewardStarted && !completed)
                GameUi.Text(new Rect(panel.x + 24f, panel.y + 156f, 392f, 30f),
                    $"Quedan {Mathf.Max(0f, rewardEndTime - Time.time):0.0} s",
                    20, GameUi.Ink, TextAnchor.MiddleCenter);
            if (!rewardStarted)
                GameUi.Text(new Rect(panel.x + 24f, panel.y + (previewReward ? 152f : 94f), 392f, 28f),
                    $"+{data.QuilomboReward:0.#} Quilombo · +250 puntos", 18, GameUi.Petrol, TextAnchor.MiddleCenter);
            if (!rewardStarted)
                GameUi.Bar(new Rect(panel.x + 24f, panel.yMax - 22f, 392f, 10f),
                    Progress / data.RequiredDuration, GameUi.Gold);
        }
        GameUi.End(previous);
    }

    public void DrawWorldLabel(Rect[] reserved, int count, Rect area)
    {
        if (runClosed || data == null || gameCamera == null) return;
        Vector3 screen = gameCamera.WorldToScreenPoint(transform.position + Vector3.up * 1.4f);
        Vector3 viewport = ViewportPosition;
        if (viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f &&
            viewport.y >= 0f && viewport.y <= 1f)
        {
            float x = screen.x * GameUi.Width / Screen.width - 116f;
            float y = GameUi.Height - screen.y * GameUi.Height / Screen.height;
            // World labels yield to the HUD, equipment and POI reward notice.
            if (y >= Mathf.Max(GameUi.SafeArea.y + 188, area.yMin) && y + 52 <= area.yMax)
            {
                x = Mathf.Clamp(x, GameUi.SafeArea.x + 16, GameUi.SafeArea.xMax - 248);
                var panel = new Rect(x, y, 232, 52);
                if (!WorldLabelFits(panel, reserved, count)) return;
                GameUi.Fill(panel, new Color(0.09f, 0.14f, 0.17f, 0.8f));
                GameUi.Icon("pin", new Rect(x + 8, y + 9, 16, 16));
                GameUi.Text(new Rect(x + 32, y + 3, 192, 26), data.DisplayName,
                    18, GameUi.Cream, TextAnchor.MiddleLeft);
                GameUi.Text(new Rect(x + 32, y + 29, 192, 18), HasPendingReward ? "Disponible" : "Ya pasaste por acá",
                    14, GameUi.Cream, TextAnchor.MiddleLeft);
            }
        }
    }

    public static bool WorldLabelFits(Rect label, Rect[] reserved, int count)
    {
        label.xMin -= 6; label.xMax += 6;
        label.yMin -= 6; label.yMax += 6;
        for (int i = 0; i < count; i++)
            if (label.Overlaps(reserved[i])) return false;
        return true;
    }

    private bool HasNoticePriority()
    {
        if (runClosed) return false;
        zones ??= FindObjectsByType<RiskZone>(FindObjectsSortMode.None);
        if (State == ZoneState.Active && !rewardStarted) return true;
        foreach (RiskZone zone in zones)
        {
            if (zone == null || zone == this || !zone.isActiveAndEnabled) continue;
            if (zone.State == ZoneState.Active && !zone.rewardStarted) return false;
            if (Time.unscaledTime < zone.feedbackUntil && (zone.feedbackUntil > feedbackUntil ||
                (zone.feedbackUntil == feedbackUntil && zone.GetInstanceID() > GetInstanceID()))) return false;
        }
        return Time.unscaledTime < feedbackUntil;
    }
}
