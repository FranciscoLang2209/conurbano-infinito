using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioDirector))]
public sealed class MenuScreen : MonoBehaviour
{
    private AudioDirector audioDirector;
    private bool showingCredits;
    private GUIStyle titleStyle;
    private GUIStyle detailStyle;
    private GUIStyle buttonStyle;

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
        titleStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        detailStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = Color.white }
        };
        buttonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 20 };

        GUI.color = new Color(0.07f, 0.15f, 0.12f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float width = Mathf.Min(500f, Screen.width - 24f);
        float height = showingCredits ? 330f : 400f;
        var panel = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
        GUI.color = new Color(0.04f, 0.08f, 0.08f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Box(panel, GUIContent.none);

        if (showingCredits)
        {
            GUI.Label(new Rect(panel.x + 16, panel.y + 20, width - 32, 48), "CRÉDITOS", titleStyle);
            GUI.Label(new Rect(panel.x + 32, panel.y + 90, width - 64, 125),
                "Conurbano Infinito\nDiseño y desarrollo: Francisco Lang y Jerónimo Engelhard\nArte y audio provisionales: recursos originales\nLicencias: ver Docs/Licencias.md", detailStyle);
            if (GUI.Button(new Rect(panel.x + 60, panel.y + 255, width - 120, 46), "Volver  ·  Esc", buttonStyle))
                showingCredits = false;
            return;
        }

        GUI.Label(new Rect(panel.x + 16, panel.y + 20, width - 32, 52), "CONURBANO INFINITO", titleStyle);
        GUI.Label(new Rect(panel.x + 32, panel.y + 79, width - 64, 34),
            "Sobreviví 10 minutos en la plaza", detailStyle);
        if (GUI.Button(new Rect(panel.x + 60, panel.y + 130, width - 120, 46), "Jugar  ·  Enter", buttonStyle)) Play();
        if (GUI.Button(new Rect(panel.x + 60, panel.y + 187, width - 120, 46), "Créditos  ·  C", buttonStyle))
            showingCredits = true;
        if (GUI.Button(new Rect(panel.x + 60, panel.y + 244, width - 120, 46), "Salir", buttonStyle))
            Application.Quit();

        GUI.Label(new Rect(panel.x + 60, panel.y + 309, width - 120, 25),
            $"Volumen general  {Mathf.RoundToInt(audioDirector.MasterVolume * 100f)}%", detailStyle);
        float volume = GUI.HorizontalSlider(new Rect(panel.x + 65, panel.y + 346, width - 130, 18),
            audioDirector.MasterVolume, 0f, 1f);
        if (!Mathf.Approximately(volume, audioDirector.MasterVolume)) audioDirector.MasterVolume = volume;
    }
}
