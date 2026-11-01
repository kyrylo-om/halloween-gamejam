using UnityEngine;

/// <summary>
/// Manages player hand stamina: writing drains stamina, and resting allows it to recover.
/// If stamina reaches 0, the hand becomes exhausted and cannot write until recovered.
/// Displays a smooth stamina bar on screen.
/// </summary>
public class StaminaSystem : MonoBehaviour
{
    private static StaminaSystem instance;
    public static StaminaSystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<StaminaSystem>();
                if (instance == null)
                {
                    GameObject go = new GameObject("StaminaSystem");
                    instance = go.AddComponent<StaminaSystem>();
                }
            }
            return instance;
        }
    }

    [Header("Stamina Settings")]
    [Tooltip("Maximum stamina pool.")]
    public float maxStamina = 100f;

    [Tooltip("Stamina recovered per second when not writing.")]
    public float recoveryRate = 28f;

    [Tooltip("Delay in seconds after writing before recovery begins.")]
    public float recoveryDelay = 0.45f;

    [Tooltip("Stamina threshold required to recover from full exhaustion.")]
    public float recoveryThreshold = 20f;

    [Header("UI Bar Settings")]
    public Font uiFont;
    public bool showUI = true;
    public Vector2 barSize = new Vector2(240f, 40f);
    public Vector2 barOffsetFromBottom = new Vector2(0f, 48f);

    public float CurrentStamina { get; private set; }
    public float NormalizedStamina => Mathf.Clamp01(CurrentStamina / Mathf.Max(1f, maxStamina));
    public bool IsExhausted { get; private set; }

    private float displayStamina;
    private float timeSinceLastWrite;
    private float uiAlpha;
    private GUIStyle labelStyle;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        CurrentStamina = maxStamina;
        displayStamina = maxStamina;
        timeSinceLastWrite = recoveryDelay;
    }

    void Update()
    {
        timeSinceLastWrite += Time.deltaTime;

        // Stamina Recovery
        if (timeSinceLastWrite >= recoveryDelay && CurrentStamina < maxStamina)
        {
            CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + recoveryRate * Time.deltaTime);

            // Recover from exhaustion once above threshold
            if (IsExhausted && CurrentStamina >= recoveryThreshold)
            {
                IsExhausted = false;
            }
        }

        // Smooth visual bar interpolation
        displayStamina = Mathf.Lerp(displayStamina, CurrentStamina, 1f - Mathf.Exp(-16f * Time.deltaTime));

        // Bar fades out while the hand is fully rested
        uiAlpha = Mathf.MoveTowards(uiAlpha, CurrentStamina < maxStamina ? 1f : 0f, Time.deltaTime * 2.5f);
    }

    /// <summary>
    /// Checks if the player has enough stamina to write.
    /// </summary>
    public bool CanWrite(float cost = 0f)
    {
        if (IsExhausted) return false;
        return CurrentStamina >= cost && CurrentStamina > 0.01f;
    }

    /// <summary>
    /// Deducts stamina when writing. Returns true if successfully written.
    /// </summary>
    public bool UseStamina(float amount)
    {
        if (!CanWrite(amount))
        {
            IsExhausted = true;
            return false;
        }

        CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);
        timeSinceLastWrite = 0f;

        if (CurrentStamina <= 0.01f)
        {
            CurrentStamina = 0f;
            IsExhausted = true;
        }

        return true;
    }

    void OnGUI()
    {
        if (!showUI || uiAlpha <= 0f || Event.current.type != EventType.Repaint) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                alignment = TextAnchor.LowerCenter,
                fontSize = 24
            };
        }

        // Position: Bottom center by default
        GuiScale.Apply();
        float barX = (GuiScale.Width - barSize.x) * 0.5f + barOffsetFromBottom.x;
        float barY = GuiScale.Height - barSize.y - barOffsetFromBottom.y;

        float fillRatio = Mathf.Clamp01(displayStamina / Mathf.Max(1f, maxStamina));
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 7f);

        // Bone when rested, candle-light when tiring, dried blood when exhausted
        Color fillColor;
        if (IsExhausted)
        {
            fillColor = Color.Lerp(GuiTheme.Blood, GuiTheme.BloodBright, pulse);
        }
        else
        {
            fillColor = Color.Lerp(GuiTheme.Candle, GuiTheme.Bone, Mathf.InverseLerp(0.2f, 0.6f, fillRatio));
        }

        // Tally marks in groups of five; each one drains from the top, right to left
        const int markCount = 10;
        float markWidth = barSize.y * 0.14f;
        float outline = Mathf.Max(1f, barSize.y * 0.05f);
        float groupGap = barSize.x * 0.08f;
        float step = (barSize.x - groupGap - markWidth) / (markCount - 1);
        Matrix4x4 baseMatrix = GUI.matrix;

        for (int i = 0; i < markCount; i++)
        {
            float x = barX + i * step + (i >= markCount / 2 ? groupGap : 0f);
            float fill = Mathf.Clamp01(fillRatio * markCount - i);

            // Hand-drawn look: each mark gets a fixed slight tilt and height variation; exhaustion makes them shake
            float tilt = (Mathf.PerlinNoise(i * 7.31f, 0.5f) - 0.5f) * 14f;
            float height = barSize.y * (0.9f + 0.1f * Mathf.PerlinNoise(i * 3.17f, 2.5f));
            Vector2 shake = IsExhausted
                ? new Vector2(Mathf.PerlinNoise(Time.time * 25f, i) - 0.5f, Mathf.PerlinNoise(i, Time.time * 25f) - 0.5f) * barSize.y * 0.12f
                : Vector2.zero;

            Vector2 pivot = new Vector2(x + markWidth * 0.5f, barY + barSize.y * 0.5f) + shake;
            GUI.matrix = baseMatrix * Matrix4x4.TRS(pivot, Quaternion.Euler(0f, 0f, tilt), Vector3.one);

            Rect mark = new Rect(-markWidth * 0.5f, -height * 0.5f, markWidth, height);
            GuiTheme.Rect(new Rect(mark.x - outline, mark.y - outline, mark.width + 2f * outline, mark.height + 2f * outline), new Color(0f, 0f, 0f, 0.75f * uiAlpha));
            GuiTheme.Rect(mark, GuiTheme.WithAlpha(GuiTheme.BoneDim, 0.4f * uiAlpha));
            if (fill > 0.001f)
            {
                float h = height * fill;
                GuiTheme.Rect(new Rect(mark.x, mark.yMax - h, mark.width, h), GuiTheme.WithAlpha(fillColor, uiAlpha));
            }
        }
        GUI.matrix = baseMatrix;

        // Label above the bar
        Rect labelRect = new Rect(barX - 150f, barY - 40f, barSize.x + 300f, 34f);
        if (IsExhausted)
        {
            GuiTheme.Label(labelRect, "your hand trembles... rest", labelStyle, Color.Lerp(GuiTheme.Blood, GuiTheme.BloodBright, pulse), uiAlpha);
        }
        else
        {
            GuiTheme.Label(labelRect, "stamina", labelStyle, GuiTheme.WithAlpha(GuiTheme.Bone, 0.8f), uiAlpha);
        }
    }
}
