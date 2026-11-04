using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioDirector))]
public sealed class MenuScreen : MonoBehaviour
{
    private AudioDirector audioDirector;
    private bool showingCredits;
    private bool showingCharacters;
    private int focusedCharacter;
    private int characterFocus;
    private GUIStyle volumeThumb;
    private int focusedAction;

    private void Awake()
    {
        Time.timeScale = 1f;
        audioDirector = GetComponent<AudioDirector>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (showingCharacters)
        {
            int count = CharacterData.All.Length;
            characterFocus = GameUi.Navigate(characterFocus, count + 4, true);
            if (keyboard.downArrowKey.wasPressedThisFrame) characterFocus = count + 1;
            if (keyboard.upArrowKey.wasPressedThisFrame) characterFocus = focusedCharacter;
            if (characterFocus < count) focusedCharacter = characterFocus;
            if (keyboard.escapeKey.wasPressedThisFrame || (GameUi.Confirm && characterFocus == count)) showingCharacters = false;
            else if (GameUi.Confirm && characterFocus >= count + 2)
                RunRules.Select(characterFocus == count + 2 ? GameMode.Classic : GameMode.Endless);
            else if (GameUi.Confirm) StartGame();
            return;
        }
        if (showingCredits)
        {
            if (keyboard.escapeKey.wasPressedThisFrame || GameUi.Confirm) showingCredits = false;
            return;
        }
        focusedAction = GameUi.Navigate(focusedAction, 4);
        if (keyboard.cKey.wasPressedThisFrame) showingCredits = true;
        else if (GameUi.Confirm) ActivateAction(focusedAction);
        if (focusedAction == 3)
        {
            if (keyboard.leftArrowKey.wasPressedThisFrame) audioDirector.MasterVolume -= 0.05f;
            if (keyboard.rightArrowKey.wasPressedThisFrame) audioDirector.MasterVolume += 0.05f;
        }
    }

    private void ActivateAction(int index)
    {
        if (index == 0)
        {
            focusedCharacter = System.Array.IndexOf(CharacterData.All, CharacterData.Selected);
            characterFocus = focusedCharacter;
            showingCharacters = true;
        }
        else if (index == 1) showingCredits = true;
        else if (index == 2) Application.Quit();
    }

    private void OnGUI()
    {
        Matrix4x4 previous = GameUi.Begin();
        GameUi.Fill(new Rect(0, 0, GameUi.Width, GameUi.Height), GameUi.Ink);
        // The awning stripe repeats at an integer UI pixel spacing.
        for (int i = 0; i < GameUi.Width / 64 + 1; i++)
            GameUi.Fill(new Rect(i * 64, 0, 32, 10), GameUi.Gold);
        if (showingCharacters)
        {
            bool started = DrawCharacters();
            GameUi.End(previous);
            if (started) GUIUtility.ExitGUI();
            return;
        }
        var panel = new Rect(GameUi.SafeArea.center.x - 480, GameUi.SafeArea.center.y - 280, 960, 560);
        GameUi.Sign(panel, GameUi.Gold);
        GameUi.Surface(new Rect(panel.x + 8, panel.y + 8, 500, 544), "enamel");
        GameUi.Text(new Rect(panel.x + 30, panel.y + 26, 452, 30), "Supervivencia de barrio", 20, GameUi.Cream);
        GameUi.Text(new Rect(panel.x + 28, panel.y + 60, 456, 140),
            "Conurbano\nInfinito", 56, GameUi.Cream, TextAnchor.UpperLeft);
        GameUi.Icon("plaza", new Rect(panel.x + 24, panel.y + 190, 468, 312));
        GameUi.Text(new Rect(panel.x + 30, panel.y + 516, 452, 24), "Una plaza. Un termo. Cero tranquilidad.",
            16, GameUi.Muted);
        float x = panel.x + 544;
        if (showingCredits)
        {
            GameUi.Text(new Rect(x, panel.y + 39, 380, 50), "Hecho en el barrio", 34, GameUi.Ink);
            GameUi.Text(new Rect(x, panel.y + 112, 370, 280),
                "Diseño y desarrollo\nFrancisco Lang\nJerónimo Engelhard\n\nArte y audio originales.\nIlustración creada con OpenAI.\nAtkinson Hyperlegible: Braille Institute\nSIL Open Font License 1.1\n\nLicencias: Docs/Licencias.md", 20,
                GameUi.Petrol, TextAnchor.UpperLeft, true);
            if (GameUi.Button(new Rect(x, panel.y + 448, 376, 50), "Volver", "ESC", true)) showingCredits = false;
            GameUi.End(previous);
            return;
        }
        GameUi.Text(new Rect(x, panel.y + 36, 376, 52), "¿Salimos a la plaza?", 34, GameUi.Ink);
        GameUi.Text(new Rect(x, panel.y + 98, 376, 60),
            "Clásico: 4 minutos.\nInfinito: hasta donde aguantes.", 23, GameUi.Petrol, TextAnchor.UpperLeft);
        string[] labels = { "Jugar", "Créditos", "Salir" };
        string[] keys = { "ENTER", "C", null };
        for (int i = 0; i < labels.Length; i++)
        {
            if (!GameUi.Button(new Rect(x, panel.y + 172 + i * 62, 376, 50), labels[i], keys[i],
                focusedAction == i, i == 0)) continue;
            GameUi.End(previous);
            ActivateAction(i);
            GUIUtility.ExitGUI();
        }
        GameUi.Text(new Rect(x, panel.y + 378, 376, 28),
            $"Volumen  {Mathf.RoundToInt(audioDirector.MasterVolume * 100)}%" + (focusedAction == 3 ? "  < / >" : ""),
            20, focusedAction == 3 ? GameUi.Ink : GameUi.Petrol);
        volumeThumb ??= new GUIStyle(GUI.skin.horizontalSliderThumb)
        {
            fixedWidth = 16, fixedHeight = 24,
            normal = { background = Texture2D.whiteTexture },
            hover = { background = Texture2D.whiteTexture },
            active = { background = Texture2D.whiteTexture }
        };
        var track = new Rect(x, panel.y + 424, 376, 6);
        GameUi.Fill(track, GameUi.Ink);
        GameUi.Fill(new Rect(track.x, track.y, track.width * audioDirector.MasterVolume, track.height), GameUi.Petrol);
        Color previousColor = GUI.color;
        GUI.color = QualitySettings.activeColorSpace == ColorSpace.Linear ? GameUi.Petrol.linear : GameUi.Petrol;
        float volume = GUI.HorizontalSlider(new Rect(x, panel.y + 414, 376, 24), audioDirector.MasterVolume,
            0, 1, GUIStyle.none, volumeThumb);
        GUI.color = previousColor;
        if (!Mathf.Approximately(volume, audioDirector.MasterVolume)) audioDirector.MasterVolume = volume;
        GameUi.Text(new Rect(x, panel.y + 468, 376, 82),
            "WASD / flechas · Ataque automático\nEsc: pausa · Mejoras: 1 / 2 / 3\nFlechas / Tab y Enter para navegar", 17,
            GameUi.Petrol, TextAnchor.UpperLeft);
        GameUi.End(previous);
    }

    private void StartGame()
    {
        CharacterData.Select(CharacterData.All[focusedCharacter]);
        SceneManager.LoadScene("Main");
    }

    private bool DrawCharacters()
    {
        var panel = new Rect(GameUi.SafeArea.center.x - 508, GameUi.SafeArea.center.y - 316, 1016, 632);
        GameUi.Sign(panel, GameUi.Gold);
        GameUi.Text(new Rect(panel.x + 24, panel.y + 14, 968, 44), "¿Quién sale a la plaza?", 36,
            GameUi.Ink, TextAnchor.MiddleCenter);
        GameUi.Text(new Rect(panel.x + 24, panel.y + 62, 968, 28),
            "Elegí personaje y modo · Tab: recorrer · Enter: confirmar", 18, GameUi.Petrol, TextAnchor.MiddleCenter);
        CharacterData[] characters = CharacterData.All;
        // shortcut: grilla de dos personajes, adaptar al incorporar un tercero.
        for (int i = 0; i < characters.Length; i++)
        {
            CharacterData character = characters[i];
            var card = new Rect(panel.x + 24 + i * 496, panel.y + 106, 472, 414);
            bool selected = focusedCharacter == i;
            bool hovered = card.Contains(Event.current.mousePosition);
            GameUi.Card(card, selected);
            if (hovered) GameUi.Outline(new Rect(card.x + 5, card.y + 5, card.width - 10, card.height - 10), GameUi.Cyan);
            GameUi.Surface(new Rect(card.x + 12, card.y + 12, 448, 190), "enamel");
            GameUi.Icon(character.Portrait, new Rect(card.x + 148, card.y + 18, 176, 176), 2f);
            if (selected) GameUi.Tag(new Rect(card.x + 20, card.y + 20, 120, 24), "Elegido", GameUi.Gold);
            if (characterFocus == i)
            {
                GameUi.FocusPointer(new Rect(card.x + 24, card.y + 151, 26, 26));
                GameUi.Text(new Rect(card.x + 56, card.y + 152, 90, 24), "Foco", 16, GameUi.Cream);
            }
            GameUi.Text(new Rect(card.x + 16, card.y + 206, 440, 40), character.DisplayName, 30,
                GameUi.Ink, TextAnchor.MiddleCenter);
            GameUi.Text(new Rect(card.x + 24, card.y + 246, 424, 50), character.AffinityDescription, 20,
                GameUi.Petrol, TextAnchor.UpperCenter, true);
            GameUi.Icon("heart", new Rect(card.x + 24, card.y + 300, 24, 24));
            GameUi.Text(new Rect(card.x + 56, card.y + 298, 120, 28), "Vida", 20, GameUi.Petrol);
            GameUi.Text(new Rect(card.x + 164, card.y + 296, 36, 34), character.StartingHealth.ToString(), 24, GameUi.Ink);
            GameUi.Icon("shoe", new Rect(card.x + 230, card.y + 300, 24, 24));
            GameUi.Text(new Rect(card.x + 262, card.y + 298, 130, 28), "Velocidad", 20, GameUi.Petrol);
            GameUi.Text(new Rect(card.x + 400, card.y + 296, 48, 34), character.MovementSpeed.ToString("0.#"), 24, GameUi.Ink);
            GameUi.Fill(new Rect(card.x + 24, card.y + 338, 424, 2), GameUi.Muted);
            GameUi.Icon(GameUi.WeaponIcon(character.StartingWeapon), new Rect(card.x + 24, card.y + 354, 40, 40));
            GameUi.Text(new Rect(card.x + 76, card.y + 346, 348, 26), "Arranca con", 16, GameUi.Petrol);
            GameUi.Text(new Rect(card.x + 76, card.y + 370, 348, 28), character.StartingWeapon.name, 23, GameUi.Ink);
            if (GUI.Button(card, GUIContent.none, GUIStyle.none)) characterFocus = focusedCharacter = i;
        }
        for (int i = 0; i < 2; i++)
        {
            bool selected = (int)RunRules.SelectedMode == i;
            var modeButton = new Rect(panel.x + 24 + i * 244, panel.y + 530, 232, 42);
            if (GameUi.Button(modeButton, i == 0 ? "Clásico · 4 min" : "Infinito", null,
                characterFocus == characters.Length + 2 + i, selected))
            {
                RunRules.Select((GameMode)i);
                characterFocus = characters.Length + 2 + i;
            }
            if (selected) GameUi.Outline(modeButton, GameUi.Gold, 3);
        }
        if (GameUi.Button(new Rect(panel.x + 24, panel.y + 580, 300, 38), "Volver", "ESC", characterFocus == characters.Length))
            showingCharacters = false;
        if (GameUi.Button(new Rect(panel.x + 544, panel.y + 548, 448, 66), "A la plaza", "ENTER",
            characterFocus == characters.Length + 1, true))
        {
            StartGame();
            return true;
        }
        return false;
    }
}
