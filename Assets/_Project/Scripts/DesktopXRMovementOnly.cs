using UnityEngine;
using UnityEngine.InputSystem;

public class DesktopXRMovementOnly : MonoBehaviour
{
    [Header("XR References")]
    public Transform xrOrigin;
    public Transform mainCamera;

    [Header("Movement")]
    public float moveSpeed = 3.0f;
    public float verticalSpeed = 2.0f;

    private void Reset()
    {
        xrOrigin = transform;
    }

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (xrOrigin == null || mainCamera == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        float x = 0f;
        float z = 0f;
        float y = 0f;

        if (keyboard.wKey.isPressed)
        {
            z += 1f;
        }

        if (keyboard.sKey.isPressed)
        {
            z -= 1f;
        }

        if (keyboard.aKey.isPressed)
        {
            x -= 1f;
        }

        if (keyboard.dKey.isPressed)
        {
            x += 1f;
        }

        if (keyboard.eKey.isPressed)
        {
            y += 1f;
        }

        if (keyboard.qKey.isPressed)
        {
            y -= 1f;
        }

        Vector3 forward = mainCamera.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = mainCamera.right;
        right.y = 0f;
        right.Normalize();

        Vector3 move =
            forward * z +
            right * x +
            Vector3.up * y * verticalSpeed;

        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }
        xrOrigin.position += move * moveSpeed * Time.deltaTime;
    }
}
