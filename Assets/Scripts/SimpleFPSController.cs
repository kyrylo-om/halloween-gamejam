using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple first-person camera controller for testing.
/// Uses WASD to move, mouse to look, Q/E to go down/up.
/// Attach this script to the Main Camera.
/// </summary>
public class SimpleFPSController : MonoBehaviour
{
    public float mouseSensitivity = 2f;
    public float moveSpeed = 5f;

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
        if (Mouse.current == null || Keyboard.current == null) return;

        // Mouse look
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        rotationX += mouseDelta.x * mouseSensitivity * 0.1f;
        rotationY -= mouseDelta.y * mouseSensitivity * 0.1f;
        rotationY = Mathf.Clamp(rotationY, -90f, 90f);

        transform.rotation = Quaternion.Euler(rotationY, rotationX, 0f);

        // WASD Movement
        Vector3 input = Vector3.zero;
        if (Keyboard.current.wKey.isPressed) input.z += 1f;
        if (Keyboard.current.sKey.isPressed) input.z -= 1f;
        if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        if (Keyboard.current.dKey.isPressed) input.x += 1f;
        if (Keyboard.current.eKey.isPressed) input.y += 1f;
        if (Keyboard.current.qKey.isPressed) input.y -= 1f;

        Vector3 move = transform.right * input.x + transform.forward * input.z + Vector3.up * input.y;
        transform.position += move * moveSpeed * Time.deltaTime;
    }
}

