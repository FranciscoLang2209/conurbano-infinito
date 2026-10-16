using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioDirector))]
public sealed class MenuScreen : MonoBehaviour
{
    private AudioDirector audioDirector;
    private bool showingCredits;
    private GUIStyle volumeThumb;

    private void Awake()
    {
        Time.timeScale = 1f;
        audioDirector = GetComponent<AudioDirector>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (showingCredits && keyboard.escapeKey.wasPressedThisFrame) showingCredits = false;
        else if (!showingCredits && keyboard.enterKey.wasPressedThisFrame) Play();
        else if (!showingCredits && keyboard.cKey.wasPressedThisFrame) showingCredits = true;
    }

    private void Play() => SceneManager.LoadScene("Main");

    private void OnGUI()
    {
        Matrix4x4 previous = GameUi.Begin();
        GameUi.Fill(new Rect(0, 0, GameUi.Width, GameUi.Height), GameUi.Petrol);
        GameUi.Fill(new Rect(0, GameUi.Height - 64, GameUi.Width, 64), GameUi.Shadow);
        GameUi.Fill(new Rect(0, GameUi.Height - 68, GameUi.Width, 4), GameUi.Gold);

        float height = showingCredits ? 330f : 470f;
        var panel = new Rect((GameUi.Width - 500f) / 2f, (GameUi.Height - height) / 2f, 500f, height);
        GameUi.Sign(panel, GameUi.Gold);

        if (showingCredits)
        {
            GameUi.Tag(new Rect(panel.x + 151, panel.y + 23, 198, 33), "CRÉDITOS", GameUi.Gold);
            GameUi.Text(new Rect(panel.x + 35, panel.y + 77, 430, 143),
                "Conurbano Infinito\nDiseño y desarrollo: Francisco Lang y Jerónimo Engelhard\nArte y audio provisionales: recursos originales\nLicencias: ver Docs/Licencias.md",
                17, GameUi.Ink, TextAnchor.MiddleCenter, true);
            if (GameUi.Button(new Rect(panel.x + 60, panel.y + 255, 380, 46), "VOLVER", "ESC"))
                showingCredits = false;
            GUI.matrix = previous;
            return;
        }

        GameUi.Fill(new Rect(panel.x + 23, panel.y + 24, 454, 53), GameUi.Petrol);
        GameUi.Text(new Rect(panel.x + 16, panel.y + 24, 468, 52),
            "CONURBANO INFINITO", 30, GameUi.Cream, TextAnchor.MiddleCenter);
        GameUi.Text(new Rect(panel.x + 32, panel.y + 79, 436, 34),
            $"Aguantá {GameManager.DefaultDurationSeconds / 60:0} minutos · La Jefa llega a los 3",
            17, GameUi.Ink, TextAnchor.MiddleCenter);
        if (GameUi.Button(new Rect(panel.x + 60, panel.y + 130, 380, 46), "JUGAR", "ENTER")) Play();
        if (GameUi.Button(new Rect(panel.x + 60, panel.y + 187, 380, 46), "CRÉDITOS", "C"))
            showingCredits = true;
        if (GameUi.Button(new Rect(panel.x + 60, panel.y + 244, 380, 46), "SALIR"))
            Application.Quit();

        GameUi.Text(new Rect(panel.x + 60, panel.y + 309, 380, 25),
            $"Volumen general  {Mathf.RoundToInt(audioDirector.MasterVolume * 100f)}%",
            16, GameUi.Ink, TextAnchor.MiddleCenter);
        volumeThumb ??= new GUIStyle(GUI.skin.horizontalSliderThumb)
        {
            fixedWidth = 14,
            fixedHeight = 20,
            normal = { background = Texture2D.whiteTexture },
            hover = { background = Texture2D.whiteTexture },
            active = { background = Texture2D.whiteTexture }
        };
        var track = new Rect(panel.x + 65, panel.y + 354, 370, 7);
        GameUi.Fill(track, GameUi.Ink);
        GameUi.Fill(new Rect(track.x + 2, track.y + 2, (track.width - 4) * audioDirector.MasterVolume, 3),
            GameUi.Gold);
        Color previousColor = GUI.color;
        GUI.color = GameUi.Petrol;
        float volume = GUI.HorizontalSlider(new Rect(panel.x + 65, panel.y + 346, 370, 22),
            audioDirector.MasterVolume, 0f, 1f, GUIStyle.none, volumeThumb);
        GUI.color = previousColor;
        if (!Mathf.Approximately(volume, audioDirector.MasterVolume)) audioDirector.MasterVolume = volume;
        GameUi.Text(new Rect(panel.x + 25, panel.y + 389, 450, 24),
            "WASD / flechas · ataque automático · Esc pausa", 14,
            GameUi.Ink, TextAnchor.MiddleCenter);
        GameUi.Text(new Rect(panel.x + 25, panel.y + 417, 450, 35),
            "Perro: esquivá de costado. Aerosol: atacá al descansar.", 14,
            GameUi.Petrol, TextAnchor.MiddleCenter, true);
        GUI.matrix = previous;
    }
}
