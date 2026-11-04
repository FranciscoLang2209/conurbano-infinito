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
    private ObjectPool pool;
    private float peakFrameMs;
    private long memoryMiB;

    private void Awake()
    {
        waves = GetComponent<WaveDirector>();
        pool = GetComponent<ObjectPool>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) visible = !visible;
        sampleSeconds += Time.unscaledDeltaTime;
        frames++;
        peakFrameMs = Mathf.Max(peakFrameMs, Time.unscaledDeltaTime * 1000f);
        if (sampleSeconds < 1f) return;

        fps = Mathf.RoundToInt(frames / sampleSeconds);
        enemies = waves.ActiveEnemyCount;
        memoryMiB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
        sampleSeconds = 0f;
        frames = 0;
    }

    private void OnGUI()
    {
        if (!visible) return;
        GUI.depth = 30;
        Matrix4x4 previous = GameUi.Begin();
        var panel = new Rect(GameUi.Width - 328, GameUi.Height - 280, 312, 190);
        GameUi.Fill(panel, new Color(0.05f, 0.1f, 0.1f, 0.72f));
        string[] rows = {
            $"FPS {fps} · Pico {peakFrameMs:0.#} ms",
            $"Enemigos {enemies} · Pico {waves.PeakEnemyCount}",
            $"Gemas {pool.ActiveGemCount} · Pico {pool.PeakGemCount}",
            $"Tiros/VFX {pool.ActiveProjectileCount} · Pico {pool.PeakProjectileCount}",
            $"Pool {pool.ActiveCount}/{pool.CreatedCount} · {memoryMiB} MiB",
            $"Agrupados {pool.MergedDrops} · Omitidos {pool.SkippedProjectiles}"
        };
        for (int i = 0; i < rows.Length; i++)
            GameUi.Text(new Rect(panel.x + 12, panel.y + 4 + i * 30, 288, 28), rows[i], 16, GameUi.Cream);
        GameUi.End(previous);
    }
#endif
}
