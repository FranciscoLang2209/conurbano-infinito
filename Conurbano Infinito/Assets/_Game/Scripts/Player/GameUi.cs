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
    public static readonly Color Night = new Color32(40, 63, 101, 255);
    public static readonly Color Petrol = new Color32(30, 76, 78, 255);
    public static readonly Color Orange = new Color32(239, 139, 69, 255);
    public static readonly Color Concrete = new Color32(171, 175, 159, 255);

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
        Fill(new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8), Night);
        Fill(new Rect(rect.x + 4, rect.y + 4, rect.width - 8, 4), accent);
        Fill(new Rect(rect.x, rect.y, 6, 6), Ink);
        Fill(new Rect(rect.x + rect.width - 6, rect.y, 6, 6), Ink);
        Fill(new Rect(rect.x, rect.y + rect.height - 6, 6, 6), Ink);
        Fill(new Rect(rect.x + rect.width - 6, rect.y + rect.height - 6, 6, 6), Ink);
    }

    public static void Sign(Rect rect, Color accent)
    {
        Fill(new Rect(rect.x + 4, rect.y + 4, rect.width, rect.height), Ink);
        Fill(rect, Ink);
        Fill(new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6), accent);
        Fill(new Rect(rect.x + 6, rect.y + 6, rect.width - 12, rect.height - 12), Cream);
        Fill(new Rect(rect.x + 6, rect.y + 6, rect.width - 12, 4), accent);
        Rivet(rect.x + 11, rect.y + 13);
        Rivet(rect.x + rect.width - 15, rect.y + 13);
        Rivet(rect.x + 11, rect.y + rect.height - 16);
        Rivet(rect.x + rect.width - 15, rect.y + rect.height - 16);
    }

    private static void Rivet(float x, float y)
    {
        Fill(new Rect(x, y, 5, 5), Ink);
        Fill(new Rect(x + 1, y + 1, 2, 2), Concrete);
    }

    public static void Tag(Rect rect, string label, Color color)
    {
        Fill(rect, Ink);
        Fill(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), color);
        Text(rect, label, 12, Ink, TextAnchor.MiddleCenter);
    }

    public static void Key(Rect rect, string label)
    {
        Fill(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), Ink);
        Fill(rect, Petrol);
        Fill(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), Cream);
        Text(rect, label, 12, Ink, TextAnchor.MiddleCenter);
    }

    public static void Card(Rect rect, bool selected)
    {
        Fill(new Rect(rect.x + 4, rect.y + 5, rect.width, rect.height), Ink);
        Fill(rect, selected ? Gold : Petrol);
        Fill(new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6), Cream);
        Fill(new Rect(rect.x + 3, rect.y + 3, rect.width - 6, 5), selected ? Gold : Cyan);
    }

    public static void Overlay()
    {
        Fill(new Rect(0, 0, Width, Height), new Color(0.04f, 0.12f, 0.12f, 0.62f));
    }

    public static void Bar(Rect rect, float fraction, Color color)
    {
        Fill(rect, Ink);
        Fill(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4),
            Shadow);
        Fill(new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * Mathf.Clamp01(fraction),
            rect.height - 4), color);
    }

    public static void Meter(Rect rect, float fraction, Color color, int segments = 12)
    {
        Fill(rect, Ink);
        float segmentWidth = (rect.width - 4f - (segments - 1) * 2f) / segments;
        fraction = Mathf.Clamp01(fraction) * segments;
        for (int i = 0; i < segments; i++)
        {
            float x = rect.x + 2f + i * (segmentWidth + 2f);
            Fill(new Rect(x, rect.y + 2f, segmentWidth, rect.height - 4f), Shadow);
            float filled = Mathf.Clamp01(fraction - i);
            if (filled > 0f)
                Fill(new Rect(x, rect.y + 2f, segmentWidth * filled, rect.height - 4f), color);
        }
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

    public static bool Button(Rect rect, string label, string shortcut = null)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        bool pressed = hover && Event.current.type == EventType.MouseDown;
        Rect face = new Rect(rect.x, rect.y + (pressed ? 3 : hover ? -2 : 0), rect.width, rect.height - 3);
        Fill(new Rect(rect.x + 3, rect.y + 4, rect.width - 3, rect.height - 3), Ink);
        Fill(face, Ink);
        Fill(new Rect(face.x + 3, face.y + 3, face.width - 6, face.height - 6), hover ? Gold : Muted);
        if (hover) Fill(new Rect(face.x + 8, face.y + 13, 8, 14), Petrol);
        invisibleButton ??= GUIStyle.none;
        bool clicked = GUI.Button(rect, GUIContent.none, invisibleButton);
        Text(new Rect(face.x + 20, face.y + 3, face.width - 40, face.height - 6),
            label, 18, Ink, TextAnchor.MiddleCenter);
        if (shortcut != null)
        {
            float keyWidth = shortcut.Length > 3 ? 58 : shortcut.Length > 1 ? 38 : 27;
            Key(new Rect(face.x + face.width - keyWidth - 12, face.y + 8, keyWidth, 25), shortcut);
        }
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
