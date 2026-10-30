using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Seated first-person camera: mouse to look, no movement.
/// Attach this script to the Main Camera.
/// </summary>
public class SimpleFPSController : MonoBehaviour
{
    public float mouseSensitivity = 2f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        Vector3 euler = transform.eulerAngles;
        rotationX = euler.y;
        rotationY = euler.x;
    }

    void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        if (Mouse.current == null) return;

        // Mouse look
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        rotationX += mouseDelta.x * mouseSensitivity * 0.1f;
        rotationY -= mouseDelta.y * mouseSensitivity * 0.1f;
        rotationY = Mathf.Clamp(rotationY, -90f, 90f);

        transform.rotation = Quaternion.Euler(rotationY, rotationX, 0f);
    }
}

