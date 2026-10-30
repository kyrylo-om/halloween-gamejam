using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The cheat sheet hidden under the desk. Right mouse button slides it out or back.
/// </summary>
public class AnswerPaper : MonoBehaviour
{
    public TextAsset answers;
    public TMP_Text text;
    public Vector3 pullOutOffset = new Vector3(0f, 0f, -0.4f);
    public float slideSpeed = 20f;

    public bool IsOut { get; private set; }

    private Vector3 hiddenPosition;

    void Start()
    {
        hiddenPosition = transform.localPosition;
        text.text = answers.text;
    }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            IsOut = !IsOut;

        Vector3 target = IsOut ? hiddenPosition + pullOutOffset : hiddenPosition;
        transform.localPosition = Vector3.Lerp(transform.localPosition, target, 1f - Mathf.Exp(-slideSpeed * Time.deltaTime));
    }
}
