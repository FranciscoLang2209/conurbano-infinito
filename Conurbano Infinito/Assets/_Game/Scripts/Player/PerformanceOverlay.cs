using UnityEngine;

public sealed class PerformanceOverlay : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private WaveDirector waves;
    private float sampleSeconds;
    private int frames;
    private int fps;
    private int enemies;

    private void Awake() => waves = GetComponent<WaveDirector>();

    private void Update()
    {
        sampleSeconds += Time.unscaledDeltaTime;
        frames++;
        if (sampleSeconds < 1f) return;

        fps = Mathf.RoundToInt(frames / sampleSeconds);
        enemies = waves.ActiveEnemyCount;
        sampleSeconds = 0f;
        frames = 0;
    }

    private void OnGUI()
    {
        GUI.depth = 30;
        Matrix4x4 previous = GameUi.Begin();
        var panel = new Rect(GameUi.Width - 134, GameUi.Height - 53, 122, 41);
        GameUi.Fill(panel, new Color(0.05f, 0.1f, 0.1f, 0.72f));
        GameUi.Text(new Rect(panel.x + 6, panel.y + 3, 110, 16), $"FPS  {fps}", 11, GameUi.Cyan);
        GameUi.Text(new Rect(panel.x + 6, panel.y + 20, 110, 16),
            $"ENEMIGOS  {enemies}", 11, GameUi.Concrete);
        GUI.matrix = previous;
    }
#endif
}
