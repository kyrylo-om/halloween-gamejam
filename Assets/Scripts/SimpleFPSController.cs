using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Seated first-person camera: mouse to look, no movement.
/// Narrows the field of view slightly while writing on the test paper,
/// and gives a small shake each time a word is written.
/// Attach this script to the Main Camera.
/// </summary>
public class SimpleFPSController : MonoBehaviour
{
    public float mouseSensitivity = 2f;

    [Header("Writing Zoom")]
    public TestPaper testPaper;
    [Tooltip("Field of view while writing.")]
    public float writingFov = 55f;
    [Tooltip("How long the zoom holds after the pen stops, so quick clicks don't make it bob.")]
    public float writingFovHold = 0.6f;
    public float fovSpeed = 4f;

    [Header("Writing Shake")]
    [Tooltip("Strength of the shake when a word is written, in degrees.")]
    public float writeShakeAmount = 0.3f;
    public float writeShakeDecay = 12f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    private Camera cam;
    private float baseFov;
    private float lastWritingTime = float.NegativeInfinity;
    private int lastWordsWritten;
    private float shake;

    void Start()
    {
        Vector3 euler = transform.eulerAngles;
        rotationX = euler.y;
        rotationY = euler.x;

        cam = GetComponent<Camera>();
        if (cam != null) baseFov = cam.fieldOfView;
        if (testPaper == null) testPaper = FindAnyObjectByType<TestPaper>();
        if (testPaper != null) lastWordsWritten = testPaper.WordsWritten;
    }

    void Update()
    {
        UpdateFov();
        UpdateShake();

        if (Cursor.lockState != CursorLockMode.Locked) return;
        if (Mouse.current == null) return;

        // Mouse look
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        rotationX += mouseDelta.x * mouseSensitivity * 0.1f;
        rotationY -= mouseDelta.y * mouseSensitivity * 0.1f;
        rotationY = Mathf.Clamp(rotationY, -90f, 90f);

        Vector3 shakeOffset = Random.insideUnitSphere * writeShakeAmount * shake;
        transform.rotation = Quaternion.Euler(rotationY + shakeOffset.x, rotationX + shakeOffset.y, shakeOffset.z);
    }

    private void UpdateFov()
    {
        if (cam == null) return;

        if (testPaper != null && testPaper.IsWriting) lastWritingTime = Time.time;
        bool writing = Time.time - lastWritingTime < writingFovHold;

        float targetFov = writing ? writingFov : baseFov;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 1f - Mathf.Exp(-fovSpeed * Time.deltaTime));
    }

    private void UpdateShake()
    {
        // A new word started, so kick the shake
        if (testPaper != null && testPaper.WordsWritten != lastWordsWritten)
        {
            lastWordsWritten = testPaper.WordsWritten;
            shake = 1f;
        }
        shake = Mathf.Lerp(shake, 0f, 1f - Mathf.Exp(-writeShakeDecay * Time.deltaTime));
    }
}
