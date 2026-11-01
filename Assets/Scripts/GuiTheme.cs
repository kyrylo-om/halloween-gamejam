using UnityEngine;

/// <summary>
/// Shared look for all OnGUI drawing: a dark palette (ink, bone, dried blood, candle light)
/// plus a few drawing helpers so the HUD and end screens look like one set.
/// </summary>
public static class GuiTheme
{
    public static readonly Color Ink = new Color(0.03f, 0.02f, 0.02f, 1f);
    public static readonly Color Bone = new Color(0.86f, 0.80f, 0.68f, 1f);
    public static readonly Color BoneDim = new Color(0.86f, 0.80f, 0.68f, 0.45f);
    public static readonly Color Blood = new Color(0.62f, 0.06f, 0.05f, 1f);
    public static readonly Color BloodBright = new Color(0.85f, 0.12f, 0.08f, 1f);
    public static readonly Color Candle = new Color(0.95f, 0.66f, 0.30f, 1f);
    public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.85f);

    private static Texture2D white;
    private static Texture2D dot;
    private static Texture2D vignette;
    private static Texture2D fadeRight;

    public static Texture2D White => white != null ? white : (white = MakeWhite());
    /// <summary>Soft anti-aliased disc.</summary>
    public static Texture2D Dot => dot != null ? dot : (dot = MakeRadial(32, d => Mathf.Clamp01((1f - d) * 16f)));
    /// <summary>Transparent center, black edges.</summary>
    public static Texture2D Vignette => vignette != null ? vignette : (vignette = MakeRadial(128, d => Mathf.SmoothStep(0f, 1f, d * 0.85f)));
    /// <summary>Opaque on the left, fading out to the right.</summary>
    public static Texture2D FadeRight => fadeRight != null ? fadeRight : (fadeRight = MakeFadeRight(64));

    public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, c.a * a);

    public static void Rect(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, White);
        GUI.color = Color.white;
    }

    public static void Frame(Rect r, Color c, float t = 1f)
    {
        Rect(new Rect(r.x, r.y, r.width, t), c);
        Rect(new Rect(r.x, r.yMax - t, r.width, t), c);
        Rect(new Rect(r.x, r.y + t, t, r.height - 2f * t), c);
        Rect(new Rect(r.xMax - t, r.y + t, t, r.height - 2f * t), c);
    }

    public static void Texture(Rect r, Texture2D tex, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, tex);
        GUI.color = Color.white;
    }

    /// <summary>Label with a soft drop shadow.</summary>
    public static void Label(Rect r, string text, GUIStyle style, Color c, float alpha = 1f)
    {
        style.normal.textColor = WithAlpha(Shadow, alpha);
        GUI.Label(new Rect(r.x + 1f, r.y + 2f, r.width, r.height), text, style);
        style.normal.textColor = WithAlpha(c, alpha);
        GUI.Label(r, text, style);
    }

    /// <summary>Thin line that fades out at both ends.</summary>
    public static void Divider(float cx, float y, float width, Color c)
    {
        float half = width * 0.5f;
        GUI.color = c;
        GUI.DrawTexture(new Rect(cx, y, half, 1f), FadeRight);
        GUI.DrawTextureWithTexCoords(new Rect(cx - half, y, half, 1f), FadeRight, new Rect(1f, 0f, -1f, 1f));
        GUI.color = Color.white;
        Rect(new Rect(cx - 2f, y - 2f, 5f, 5f), c);
    }

    /// <summary>Outlined button that fills with dried blood on hover. Returns true when clicked.</summary>
    public static bool Button(Rect r, string text, GUIStyle style, float alpha = 1f)
    {
        bool hover = r.Contains(Event.current.mousePosition);
        Rect(r, WithAlpha(hover ? Blood : new Color(0.05f, 0.03f, 0.03f, 0.7f), alpha * (hover ? 0.85f : 1f)));
        Frame(r, WithAlpha(hover ? BloodBright : BoneDim, alpha));
        Label(r, text, style, hover ? Bone : WithAlpha(Bone, 0.85f), alpha);
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    /// <summary>
    /// Full-screen end card: tinted darkness, vignette, title, divider, message and a button.
    /// Returns true when the button is clicked.
    /// </summary>
    public static bool EndScreen(float alpha, Color tint, string title, Color titleColor, string message,
                                 string button, GUIStyle titleStyle, GUIStyle messageStyle, GUIStyle buttonStyle)
    {
        Rect screen = new Rect(0f, 0f, GuiScale.Width, GuiScale.Height);
        Rect(screen, WithAlpha(tint, alpha));
        Texture(screen, Vignette, new Color(0f, 0f, 0f, alpha));

        float cx = GuiScale.Width * 0.5f;
        float cy = GuiScale.Height * 0.5f;
        float rise = (1f - alpha) * 12f;

        Label(new Rect(cx - 400f, cy - 150f + rise, 800f, 100f), title, titleStyle, titleColor, alpha);
        Divider(cx, cy - 42f + rise, 360f, WithAlpha(BoneDim, alpha));
        Label(new Rect(cx - 360f, cy - 28f + rise, 720f, 64f), message, messageStyle, Bone, alpha * 0.9f);

        return Button(new Rect(cx - 130f, cy + 60f + rise, 260f, 48f), button, buttonStyle, alpha);
    }

    private static Texture2D MakeWhite()
    {
        var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        t.SetPixel(0, 0, Color.white);
        t.Apply();
        return t;
    }

    private static Texture2D MakeRadial(int size, System.Func<float, float> alphaAtDistance)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp
        };
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                t.SetPixel(x, y, new Color(1f, 1f, 1f, alphaAtDistance(d)));
            }
        }
        t.Apply();
        return t;
    }

    private static Texture2D MakeFadeRight(int width)
    {
        var t = new Texture2D(width, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp
        };
        for (int x = 0; x < width; x++)
        {
            t.SetPixel(x, 0, new Color(1f, 1f, 1f, 1f - x / (width - 1f)));
        }
        t.Apply();
        return t;
    }
}
