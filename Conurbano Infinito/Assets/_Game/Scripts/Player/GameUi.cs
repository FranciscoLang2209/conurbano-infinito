using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Shared IMGUI skin keeps the existing scene wiring and input intact.
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
    private static readonly Dictionary<string, Sprite> Sprites = new();
    private static Font font;
    private static Font titleFont;
    private static Sprite focusPointer;
#if UNITY_EDITOR
    public static System.Action<Rect, string, GUIStyle> CheckTextLayout;
#endif
    public const float Margin = 16f;
    public const float Gap = 8f;
    private static float scale = 1f;
    public static float Width => Screen.width / scale;
    public static float Height => Screen.height / scale;
    public static Rect SafeArea { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSkin()
    {
        Styles.Clear();
        Sprites.Clear();
        font = titleFont = null;
        focusPointer = null;
#if UNITY_EDITOR
        CheckTextLayout = null;
#endif
    }
    public static bool Confirm => Keyboard.current != null &&
        (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
         Keyboard.current.spaceKey.wasPressedThisFrame);

    public static Matrix4x4 Begin()
    {
        Matrix4x4 previous = GUI.matrix;
        scale = Mathf.Max(0.01f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f)) * previous;
        Rect safe = Screen.safeArea;
        SafeArea = new Rect(safe.x / scale, (Screen.height - safe.yMax) / scale,
            safe.width / scale, safe.height / scale);
        return previous;
    }

    public static void End(Matrix4x4 previous)
    {
        GUI.matrix = previous;
    }

    public static int Navigate(int selected, int count, bool horizontal = false)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || count == 0) return selected;
        bool back = horizontal ? keyboard.leftArrowKey.wasPressedThisFrame : keyboard.upArrowKey.wasPressedThisFrame;
        bool forward = horizontal ? keyboard.rightArrowKey.wasPressedThisFrame : keyboard.downArrowKey.wasPressedThisFrame;
        if (keyboard.tabKey.wasPressedThisFrame)
        {
            if (keyboard.shiftKey.isPressed) back = true;
            else forward = true;
        }
        return (selected + (forward ? 1 : back ? -1 : 0) + count) % count;
    }

    public static Sprite Sprite(string name)
    {
        if (!Sprites.TryGetValue(name, out Sprite sprite))
        {
            sprite = Resources.Load<Sprite>("BarrioUi/" + name);
            Sprites.Add(name, sprite);
        }
        return sprite;
    }

    public static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = DisplayColor(color);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    public static void Surface(Rect rect, string name)
    {
        bool paper = name == "paper";
        Fill(rect, paper ? Cream : Ink);
        Outline(rect, paper ? Concrete : Night, 1);
        if (name == "enamel") Fill(new Rect(rect.x + 1, rect.yMax - 4, rect.width - 2, 3), Night);
    }

    public static void Panel(Rect rect, Color accent)
    {
        Surface(rect, "enamel");
        Fill(new Rect(rect.x + 1, rect.y + 1, 3, rect.height - 2), accent);
    }

    public static void Sign(Rect rect, Color accent)
    {
        Surface(rect, "paper");
        Fill(new Rect(rect.x + 16, rect.y + 10, 38, 4), accent);
    }

    public static void Tag(Rect rect, string label, Color color)
    {
        Fill(rect, color);
        Text(rect, label, 16, Ink, TextAnchor.MiddleCenter);
    }

    public static void Key(Rect rect, string label)
    {
        Fill(rect, Ink);
        Text(rect, label, 16, Cream, TextAnchor.MiddleCenter);
    }

    public static void Card(Rect rect, bool selected)
    {
        Sign(rect, selected ? Gold : Petrol);
        if (selected)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, 4), Gold);
            Fill(new Rect(rect.x, rect.yMax - 4, rect.width, 4), Gold);
        }
    }

    public static void Outline(Rect rect, Color color, float thickness = 2f)
    {
        Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
        Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
        Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    public static void Overlay() => Fill(new Rect(0, 0, Width, Height), new Color(0.035f, 0.08f, 0.1f, 0.88f));

    public static void Bar(Rect rect, float fraction, Color color)
    {
        Fill(rect, Ink);
        Fill(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), Shadow);
        Fill(new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * Mathf.Clamp01(fraction), rect.height - 4), color);
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
            if (filled > 0f) Fill(new Rect(x, rect.y + 2f, segmentWidth * filled, rect.height - 4f), color);
        }
    }

    public static float ResultTextHeight(string value, float width)
    {
        GUIStyle style = TextStyle(18);
        style.wordWrap = true;
        return style.CalcHeight(new GUIContent(value), width * scale) / scale;
    }

    private static GUIStyle TextStyle(int size)
    {
        // Rasterize the dynamic font at the output resolution, not a stretched reference atlas.
        int pixels = Mathf.Max(16, Mathf.RoundToInt(Mathf.Max(18, size) * scale));
        bool heading = size >= 24;
        int key = pixels * 2 + (heading ? 1 : 0);
        if (!Styles.TryGetValue(key, out GUIStyle style))
        {
            font ??= Resources.Load<Font>("BarrioUi/AtkinsonHyperlegible-Regular");
            titleFont ??= Resources.Load<Font>("BarrioUi/AtkinsonHyperlegible-Bold");
            style = new GUIStyle(GUI.skin.label)
            {
                font = heading ? titleFont : font, fontSize = pixels, fontStyle = FontStyle.Normal,
                clipping = TextClipping.Clip, padding = new RectOffset(0, 0, 0, 0)
            };
            Styles.Add(key, style);
        }
        return style;
    }

    public static void Text(Rect rect, string value, int size, Color color,
        TextAnchor alignment = TextAnchor.MiddleLeft, bool wrap = false)
    {
        GUIStyle style = TextStyle(size);
        if (style.font.material.mainTexture != null) style.font.material.mainTexture.filterMode = FilterMode.Bilinear;
        style.normal.textColor = style.hover.textColor = style.active.textColor =
            style.focused.textColor = DisplayColor(color);
        style.alignment = alignment;
        style.wordWrap = wrap;
        Rect output = new Rect(rect.x * scale, rect.y * scale, rect.width * scale, rect.height * scale);
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = previous * Matrix4x4.Scale(new Vector3(1f / scale, 1f / scale, 1f));
#if UNITY_EDITOR
        CheckTextLayout?.Invoke(output, value, style);
#endif
        GUI.Label(output, value, style);
        GUI.matrix = previous;
    }

    public static bool Button(Rect rect, string label, string shortcut = null, bool selected = false, bool primary = false)
    {
        bool hovered = GUI.enabled && rect.Contains(Event.current.mousePosition);
        bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
        Fill(rect, !GUI.enabled ? Shadow : selected || primary ? Gold : hovered ? Cream : Night);
        Outline(rect, selected || primary ? Gold : hovered ? Cream : Concrete, 1);
        Color text = !GUI.enabled ? Concrete : selected || primary || hovered ? Ink : Cream;
        if (selected) FocusPointer(new Rect(rect.x + 10, rect.center.y - 13, 26, 26));
        float keyWidth = shortcut == null ? 0 : shortcut.Length > 3 ? 60 : 38;
        Text(new Rect(rect.x + 42, rect.y, rect.width - 58 - keyWidth, rect.height), label, 23, text);
        if (shortcut != null) Key(new Rect(rect.xMax - keyWidth - 12, rect.center.y - 13, keyWidth, 26), shortcut);
        return clicked;
    }

    public static void FocusPointer(Rect rect)
    {
        focusPointer ??= Resources.Load<Sprite>("NinjaAdventure/focus");
        rect.x += Mathf.Floor(Mathf.PingPong(Time.unscaledTime * 2, 2)) * 2;
        Matrix4x4 previous = GUI.matrix;
        GUIUtility.RotateAroundPivot(-90f, rect.center);
        Icon(focusPointer, rect, 2f);
        GUI.matrix = previous;
    }

    public static void Icon(string name, Rect rect) => Icon(Sprite(name), rect);
    public static void Icon(string name, Rect rect, Color tint)
    {
        Color previous = GUI.color;
        GUI.color = DisplayColor(tint);
        Icon(name, rect);
        GUI.color = previous;
    }

    public static void Icon(Sprite sprite, Rect rect, float maxScale = 1f)
    {
        if (sprite == null) return;
        Rect source = sprite.textureRect;
        Texture2D texture = sprite.texture;
        // Keep small sprites at their authored size, including inside larger upgrade cards.
        float fit = Mathf.Min(maxScale, Mathf.Min(rect.width / source.width, rect.height / source.height));
        Vector2 size = source.size * fit;
        rect = new Rect(rect.center - size * 0.5f, size);
        rect.x = Mathf.Round(rect.x * scale) / scale;
        rect.y = Mathf.Round(rect.y * scale) / scale;
        GUI.DrawTextureWithTexCoords(rect, texture, new Rect(source.x / texture.width,
            source.y / texture.height, source.width / texture.width, source.height / texture.height));
    }

    public static Sprite WeaponIcon(WeaponData weapon)
    {
        string name = weapon.name switch
        {
            "Termo Balístico" => "weapon_thermos",
            "Aerosol" => "weapon_aerosol",
            "Boleadora" => "weapon_boleadora",
            "Chancleta Teledirigida" => "weapon_slipper",
            "Fernets Orbitales" => "weapon_fernet",
            "Petardo" => "weapon_firecracker",
            "Pelota de Potrero" => "weapon_ball",
            _ => null
        };
        return name == null ? weapon.ProjectileSprite : Sprite(name);
    }

    public static string UpgradeIcon(UpgradeEffect effect) => effect == UpgradeEffect.MaximumHealth ? "heart" :
        effect == UpgradeEffect.PassiveExplosionRadius ? "flame" : effect == UpgradeEffect.PassiveProjectileCount ? "rapid" :
        effect == UpgradeEffect.MovementSpeed ? "shoe" : effect == UpgradeEffect.FireInterval ? "rapid" : "fist";

    private static Color DisplayColor(Color color) => QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
}
