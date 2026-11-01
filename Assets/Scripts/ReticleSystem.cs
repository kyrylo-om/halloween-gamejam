using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Draws a crosshair reticle on screen. Raycasts from the camera to detect
/// IInteractable objects and lets the player interact with left mouse button.
/// Attach this script to the Main Camera.
/// </summary>
public class ReticleSystem : MonoBehaviour
{
    [Header("Reticle Settings")]
    public float reticleSize = 20f;
    public float reticleThickness = 2f;
    public Font uiFont;

    [Header("Raycast Settings")]
    public float interactRange = 10f;
    public LayerMask interactLayer = ~0;

    private float hoverBlend;
    private IInteractable currentTarget;
    private string currentPrompt = "";
    private GUIStyle promptStyle;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Press Escape to unlock cursor
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        hoverBlend = Mathf.MoveTowards(hoverBlend, currentTarget != null ? 1f : 0f, Time.unscaledDeltaTime * 8f);

        // Raycast from camera center
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, interactLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                currentTarget = interactable;
                currentPrompt = interactable.InteractionPrompt;

                // Left mouse button to interact
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    interactable.Interact();
                }
                return;
            }
        }

        currentTarget = null;
        currentPrompt = "";

        // Click to re-lock cursor when not on interactable
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void OnGUI()
    {
        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20
            };
        }

        GuiScale.Apply();
        Vector2 center = new Vector2(GuiScale.Width / 2f, GuiScale.Height / 2f);
        Color color = Color.Lerp(GuiTheme.WithAlpha(GuiTheme.Bone, 0.6f), GuiTheme.Candle, hoverBlend);

        // Center dot with a dark halo so it reads on bright surfaces
        float dotSize = reticleThickness + 2f + hoverBlend;
        GuiTheme.Texture(new Rect(center.x - dotSize, center.y - dotSize, dotSize * 2f, dotSize * 2f), GuiTheme.Dot, new Color(0f, 0f, 0f, 0.35f));
        GuiTheme.Texture(new Rect(center.x - dotSize / 2f, center.y - dotSize / 2f, dotSize, dotSize), GuiTheme.Dot, color);

        // Corner brackets close in around interactables
        if (hoverBlend > 0f)
        {
            float half = reticleSize * Mathf.Lerp(1.4f, 0.75f, hoverBlend);
            float arm = reticleSize * 0.3f;
            float t = reticleThickness * 0.75f;
            Color bracket = GuiTheme.WithAlpha(GuiTheme.Candle, hoverBlend);
            for (int i = 0; i < 4; i++)
            {
                float sx = (i & 1) == 0 ? -1f : 1f;
                float sy = (i & 2) == 0 ? -1f : 1f;
                float cx = center.x + sx * half;
                float cy = center.y + sy * half;
                GuiTheme.Rect(new Rect(sx < 0f ? cx : cx - arm, sy < 0f ? cy : cy - t, arm, t), bracket);
                GuiTheme.Rect(new Rect(sx < 0f ? cx : cx - t, sy < 0f ? cy : cy - arm, t, arm), bracket);
            }
        }

        // Interaction prompt text below reticle
        if (!string.IsNullOrEmpty(currentPrompt))
        {
            Rect promptRect = new Rect(center.x - 200f, center.y + reticleSize + 14f, 400f, 30f);
            GuiTheme.Label(promptRect, currentPrompt, promptStyle, GuiTheme.Bone, Mathf.Max(0.3f, hoverBlend));
        }
    }
}
