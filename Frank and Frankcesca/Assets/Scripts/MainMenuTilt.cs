using UnityEngine;
using UnityEngine.InputSystem;
//Akhona Khoali
// canvas
// The panel tilts toward the mouse, so the menu feels like it floats in 3D space.

public class MenuTilt : MonoBehaviour
{
    [Header("Tilt Settings")]
    public float maxTiltX = 8f;        // Degrees the panel tilts up/down.
    public float maxTiltY = 12f;       // Degrees the panel turns left/right.
    public float smoothing = 6f;       // Higher = snappier, lower = floatier.

    [Header("Drift Settings (optional)")]
    public float maxOffset = 15f;      // How far the panel slides toward the mouse. Set to 0 to disable.

    private RectTransform rect;
    private Quaternion startRotation;
    private Vector2 startPosition;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        startRotation = rect.localRotation;
        startPosition = rect.anchoredPosition;
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        // Converts the mouse position to a value between -1 and 1 from the centre of the screen.
        Vector2 mouse = Mouse.current.position.ReadValue();
        float x = Mathf.Clamp((mouse.x / Screen.width - 0.5f) * 2f, -1f, 1f);
        float y = Mathf.Clamp((mouse.y / Screen.height - 0.5f) * 2f, -1f, 1f);

        // Mouse right turns the panel toward the right, mouse up tilts the top away.
        Quaternion targetRotation = startRotation * Quaternion.Euler(-y * maxTiltX, x * maxTiltY, 0f);
        Vector2 targetPosition = startPosition + new Vector2(x, y) * maxOffset;

        // Smoothly moves toward the target. Time.unscaledDeltaTime keeps it working if the menu pauses the game.
        float t = smoothing * Time.unscaledDeltaTime;
        rect.localRotation = Quaternion.Slerp(rect.localRotation, targetRotation, t);
        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPosition, t);
    }
}
