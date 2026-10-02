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
        var panel = new Rect(GameUi.Width - 172, GameUi.Height - 65, 160, 53);
        GameUi.Panel(panel, GameUi.Cyan);
        GameUi.Text(new Rect(panel.x + 10, panel.y + 8, 140, 18), $"FPS  {fps}", 14, GameUi.Cream);
        GameUi.Text(new Rect(panel.x + 10, panel.y + 26, 140, 18),
            $"ENEMIGOS  {enemies}", 14, GameUi.Cream);
        GUI.matrix = previous;
    }
#endif
}
