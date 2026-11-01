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
    public Color normalColor = Color.white;
    public Color interactableColor = Color.green;
    public Font uiFont;

    [Header("Raycast Settings")]
    public float interactRange = 10f;
    public LayerMask interactLayer = ~0;

    private Texture2D reticleTexture;
    private Color currentColor;
    private IInteractable currentTarget;
    private string currentPrompt = "";
    private GUIStyle promptStyle;

    void Start()
    {
        // Create a 1x1 white texture used to draw the reticle lines
        reticleTexture = new Texture2D(1, 1);
        reticleTexture.SetPixel(0, 0, Color.white);
        reticleTexture.Apply();
        currentColor = normalColor;

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

        // Raycast from camera center
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, interactLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                currentTarget = interactable;
                currentColor = interactableColor;
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
        currentColor = normalColor;
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
                fontSize = 18
            };
        }

        GuiScale.Apply();
        Vector2 center = new Vector2(GuiScale.Width / 2f, GuiScale.Height / 2f);

        GUI.color = currentColor;

        // Horizontal line
        GUI.DrawTexture(
            new Rect(center.x - reticleSize / 2f, center.y - reticleThickness / 2f,
                      reticleSize, reticleThickness),
            reticleTexture);

        // Vertical line
        GUI.DrawTexture(
            new Rect(center.x - reticleThickness / 2f, center.y - reticleSize / 2f,
                      reticleThickness, reticleSize),
            reticleTexture);

        // Center dot
        float dotSize = reticleThickness + 2f;
        GUI.DrawTexture(
            new Rect(center.x - dotSize / 2f, center.y - dotSize / 2f, dotSize, dotSize),
            reticleTexture);

        GUI.color = Color.white;

        // Interaction prompt text below reticle
        if (!string.IsNullOrEmpty(currentPrompt))
        {
            Rect promptRect = new Rect(center.x - 150f, center.y + reticleSize + 10f, 300f, 30f);

            // Shadow
            promptStyle.normal.textColor = Color.black;
            GUI.Label(new Rect(promptRect.x + 1, promptRect.y + 1,
                               promptRect.width, promptRect.height),
                      currentPrompt, promptStyle);

            // Text
            promptStyle.normal.textColor = Color.white;
            GUI.Label(promptRect, currentPrompt, promptStyle);
        }
    }

    void OnDestroy()
    {
        if (reticleTexture != null)
            Destroy(reticleTexture);
    }
}

