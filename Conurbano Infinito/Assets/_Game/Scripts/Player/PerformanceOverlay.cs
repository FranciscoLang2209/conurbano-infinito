using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PerformanceOverlay : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private WaveDirector waves;
    private bool visible;
    private float sampleSeconds;
    private int frames;
    private int fps;
    private int enemies;

    private void Awake() => waves = GetComponent<WaveDirector>();

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) visible = !visible;
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
        if (!visible) return;
        GUI.depth = 30;
        Matrix4x4 previous = GameUi.Begin();
        var panel = new Rect(GameUi.Width - 212, GameUi.Height - 156, 196, 70);
        GameUi.Fill(panel, new Color(0.05f, 0.1f, 0.1f, 0.72f));
        GameUi.Text(new Rect(panel.x + 12, panel.y + 6, 172, 28), $"FPS  {fps}", 18, GameUi.Cream);
        GameUi.Text(new Rect(panel.x + 12, panel.y + 36, 172, 28),
            $"Enemigos  {enemies}", 18, GameUi.Cream);
        GameUi.End(previous);
    }
#endif
}
