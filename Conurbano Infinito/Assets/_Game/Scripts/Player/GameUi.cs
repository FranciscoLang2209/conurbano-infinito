using System.Collections.Generic;
using UnityEngine;

// Shared IMGUI drawing for the in-game HUD and modal screens.
public static class GameUi
{
    public static readonly Color Ink = new Color32(24, 36, 44, 255);
    public static readonly Color Shadow = new Color32(43, 58, 59, 255);
    public static readonly Color Cream = new Color32(249, 238, 215, 255);
    public static readonly Color Muted = new Color32(232, 215, 178, 255);
    public static readonly Color Gold = new Color32(242, 196, 71, 255);
    public static readonly Color Red = new Color32(226, 94, 76, 255);
    public static readonly Color Cyan = new Color32(97, 187, 211, 255);

    private static readonly Dictionary<int, GUIStyle> Styles = new();
    private static GUIStyle invisibleButton;
    private static float scale;

    public static float Width => Screen.width / scale;
    public static float Height => Screen.height / scale;

    public static Matrix4x4 Begin()
    {
        Matrix4x4 previous = GUI.matrix;
        scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f)) * previous;
        return previous;
    }

    public static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    public static void Panel(Rect rect, Color accent)
    {
        Fill(rect, Ink);
        Fill(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), Muted);
        Fill(new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8), Shadow);
        Fill(new Rect(rect.x + 4, rect.y + 4, rect.width - 8, 3), accent);
        Fill(new Rect(rect.x + 2, rect.y + 2, 7, 7), accent);
        Fill(new Rect(rect.x + rect.width - 9, rect.y + 2, 7, 7), accent);
        Fill(new Rect(rect.x + 2, rect.y + rect.height - 9, 7, 7), accent);
        Fill(new Rect(rect.x + rect.width - 9, rect.y + rect.height - 9, 7, 7), accent);
    }

    public static void Bar(Rect rect, float fraction, Color color)
    {
        Fill(rect, Ink);
        Fill(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4),
            new Color32(81, 78, 75, 255));
        Fill(new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * Mathf.Clamp01(fraction),
            rect.height - 4), color);
    }

    public static void Text(Rect rect, string value, int size, Color color,
        TextAnchor alignment = TextAnchor.MiddleLeft, bool wrap = false)
    {
        if (!Styles.TryGetValue(size, out GUIStyle style))
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip
            };
            Styles.Add(size, style);
        }
        style.normal.textColor = color;
        style.alignment = alignment;
        style.wordWrap = wrap;
        GUI.Label(rect, value, style);
    }

    public static bool Button(Rect rect, string label)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        Fill(rect, hover ? Gold : Ink);
        Fill(new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6),
            hover ? new Color32(64, 121, 186, 255) : new Color32(40, 63, 101, 255));
        invisibleButton ??= GUIStyle.none;
        bool clicked = GUI.Button(rect, GUIContent.none, invisibleButton);
        Text(rect, label, 18, Cream, TextAnchor.MiddleCenter);
        return clicked;
    }

    public static void Icon(Sprite sprite, Rect rect)
    {
        if (sprite == null) return;
        Rect source = sprite.textureRect;
        Texture2D texture = sprite.texture;
        GUI.DrawTextureWithTexCoords(rect, texture, new Rect(source.x / texture.width,
            source.y / texture.height, source.width / texture.width, source.height / texture.height));
    }
}
