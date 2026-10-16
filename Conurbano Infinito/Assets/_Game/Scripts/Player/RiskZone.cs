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
        if (rewardStarted || State == ZoneState.Completed) return;
        Progress = 0f;
        State = ZoneState.Inactive;
    }

    private void OnDisable()
    {
        playerInside = false;
        player = null;
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
        if (!playerInside) return;
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
            float x = screen.x * GameUi.Width / Screen.width - 100f;
            float y = GameUi.Height - screen.y * GameUi.Height / Screen.height;
            GameUi.Panel(new Rect(x, y, 200, 27), GameUi.Gold);
            GameUi.Text(new Rect(x + 5, y + 2, 190, 23), data.DisplayName,
                14, GameUi.Cream, TextAnchor.MiddleCenter);
        }

        if (State == ZoneState.Active || Time.unscaledTime < feedbackUntil)
        {
            bool completed = State == ZoneState.Completed;
            bool showReward = rewardStarted && data.HasReward;
            float panelHeight = showReward ? 110f : 94f;
            if (!completed && rewardStarted) panelHeight += 26f;
            var panel = new Rect((GameUi.Width - 320f) / 2f,
                GameUi.Height - panelHeight - 26f, 320f, panelHeight);
            GameUi.Panel(panel, GameUi.Gold);
            GameUi.Text(new Rect(panel.x + 12f, panel.y + 7f, 296f, 27f),
                rewardStarted ? data.CompletionMessage : data.DisplayName.ToUpperInvariant(),
                20, GameUi.Cream, TextAnchor.MiddleCenter);
            GameUi.Text(new Rect(panel.x + 12f, panel.y + 39f, 296f, 27f),
                rewardStarted ? $"+{data.QuilomboReward:0.#} QUILOMBO" :
                    $"Resistiendo... {Progress:0.0} / {data.RequiredDuration:0.#}",
                17, GameUi.Gold, TextAnchor.MiddleCenter);
            if (showReward)
                GameUi.Text(new Rect(panel.x + 12f, panel.y + 71f, 296f, 27f),
                    data.RewardText,
                    data.RewardText.Length > 20 ? 15 : 17, GameUi.Gold, TextAnchor.MiddleCenter);
            if (rewardStarted && !completed)
                GameUi.Text(new Rect(panel.x + 12f, panel.y + 102f, 296f, 27f),
                    $"{data.TimerLabel}: {Mathf.Max(0f, rewardEndTime - Time.time):0.0}s",
                    17, GameUi.Gold, TextAnchor.MiddleCenter);
            if (!rewardStarted)
                GameUi.Bar(new Rect(panel.x + 12f, panel.y + 76f, 296f, 10f),
                    Progress / data.RequiredDuration, GameUi.Gold);
        }
        GUI.matrix = previous;
    }
}
