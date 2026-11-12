using UnityEngine;

// Shared look for the HUD and menu text (call only from OnGUI).
public static class TextDrawing
{
    public static readonly Color Gold = new(1f, 0.82f, 0.16f);
    public static readonly Color Good = new(0.6f, 1f, 0.6f);
    public static readonly Color Bad = new(1f, 0.6f, 0.6f);

    public static GUIStyle BaseStyle() =>
        new(GUI.skin.label) { fontSize = Mathf.Max(18, Screen.height / 25), fontStyle = FontStyle.Bold };

    public static void Shadowed(Rect rect, string text, GUIStyle style) => Shadowed(rect, text, style, Color.white);

    public static void Shadowed(Rect rect, string text, GUIStyle style, Color color)
    {
        var original = style.normal.textColor;
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, style);
        style.normal.textColor = color;
        GUI.Label(rect, text, style);
        style.normal.textColor = original;
    }
}
