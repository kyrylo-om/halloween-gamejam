using UnityEngine;

/// <summary>
/// Interactable that cycles through different colors each time
/// the player clicks on it. Uses MaterialPropertyBlock for efficiency.
/// </summary>
public class ChangeMaterialOnInteract : MonoBehaviour, IInteractable
{
    public string InteractionPrompt => "[LMB] Change Color";

    private Renderer objectRenderer;
    private int colorIndex = 0;

    private static readonly Color[] colors = new Color[]
    {
        Color.white,
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        Color.magenta,
        Color.cyan
    };

    private MaterialPropertyBlock propBlock;

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
    }

    public void Interact()
    {
        colorIndex = (colorIndex + 1) % colors.Length;

        objectRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_BaseColor", colors[colorIndex]); // URP Lit uses _BaseColor
        objectRenderer.SetPropertyBlock(propBlock);
    }
}

