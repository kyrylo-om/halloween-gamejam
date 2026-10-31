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
    public bool showUI = true;
    public Vector2 barSize = new Vector2(220f, 18f);
    public Vector2 barOffsetFromBottom = new Vector2(0f, 40f);
    public Color highStaminaColor = new Color(0.2f, 0.85f, 0.35f, 0.95f);
    public Color mediumStaminaColor = new Color(0.95f, 0.8f, 0.2f, 0.95f);
    public Color lowStaminaColor = new Color(0.9f, 0.25f, 0.2f, 0.95f);
    public Color exhaustedColor = new Color(0.7f, 0.1f, 0.1f, 0.9f);
    public Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.8f);
    public Color borderColor = new Color(0.25f, 0.25f, 0.25f, 0.9f);

    public float CurrentStamina { get; private set; }
    public float NormalizedStamina => Mathf.Clamp01(CurrentStamina / Mathf.Max(1f, maxStamina));
    public bool IsExhausted { get; private set; }

    private float displayStamina;
    private float timeSinceLastWrite;
    private Texture2D whiteTexture;
    private GUIStyle labelStyle;
    private GUIStyle warningStyle;

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

    void Start()
    {
        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
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
        if (!showUI || Event.current.type != EventType.Repaint) return;

        if (whiteTexture == null)
        {
            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
        }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
        }

        if (warningStyle == null)
        {
            warningStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }

        // Position: Bottom center by default
        float centerX = (Screen.width - barSize.x) * 0.5f + barOffsetFromBottom.x;
        float barY = Screen.height - barSize.y - barOffsetFromBottom.y;

        Rect borderRect = new Rect(centerX - 2f, barY - 2f, barSize.x + 4f, barSize.y + 4f);
        Rect bgRect = new Rect(centerX, barY, barSize.x, barSize.y);

        float fillRatio = Mathf.Clamp01(displayStamina / Mathf.Max(1f, maxStamina));
        Rect fillRect = new Rect(centerX, barY, barSize.x * fillRatio, barSize.y);

        // Pick color based on stamina level and exhaustion
        Color fillColor;
        if (IsExhausted)
        {
            // Pulsing red when exhausted
            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 8f);
            fillColor = Color.Lerp(exhaustedColor, Color.red, pulse);
        }
        else if (fillRatio > 0.5f)
        {
            fillColor = Color.Lerp(mediumStaminaColor, highStaminaColor, (fillRatio - 0.5f) * 2f);
        }
        else
        {
            fillColor = Color.Lerp(lowStaminaColor, mediumStaminaColor, fillRatio * 2f);
        }

        // Draw border
        GUI.color = borderColor;
        GUI.DrawTexture(borderRect, whiteTexture);

        // Draw background
        GUI.color = backgroundColor;
        GUI.DrawTexture(bgRect, whiteTexture);

        // Draw filled stamina bar
        if (fillRatio > 0.001f)
        {
            GUI.color = fillColor;
            GUI.DrawTexture(fillRect, whiteTexture);
        }

        // Draw label text
        GUI.color = Color.white;
        string statusText = IsExhausted ? "EXHAUSTED - REST HAND" : $"STAMINA {Mathf.CeilToInt(CurrentStamina)}%";
        labelStyle.normal.textColor = IsExhausted ? Color.red : Color.white;

        // Shadow
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.Label(new Rect(bgRect.x + 1, bgRect.y + 1, bgRect.width, bgRect.height), statusText, labelStyle);

        // Foreground text
        GUI.color = Color.white;
        GUI.Label(bgRect, statusText, labelStyle);
    }

    void OnDestroy()
    {
        if (whiteTexture != null)
        {
            Destroy(whiteTexture);
        }
    }
}

