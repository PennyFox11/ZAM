using UnityEngine;
using UnityEngine.Events;
//Akhona Khoali
//  Belongs to any object the player can pick up.

[RequireComponent(typeof(Rigidbody))]
public class PickupInteractable : MonoBehaviour, IInteractable
{
    [Header("Item Info (shown on the left side panel)")]
    public string itemId = "";              
    public string itemName = "Item";
    [TextArea(3, 6)]
    public string description = "";

    [Header("Hold Style")]
    public bool showInfoPanel = true;       // Show the name/description panel while held.
    public bool useCentreHoldPoint = false; // true = centre of the screen (notes).
    public bool spinWhileHeld = true;       // Slowly rotate while held.
    public float rotateSpeed = 30f;         // Degrees per second while spinning.
    public Vector3 heldEulerAngles = Vector3.zero;  // Rotation used when NOT spinning (adjust per model).
    public float heldScale = 1f;            // Size while held. Use 0.3 - 0.5 for big objects.
    public float moveToHoldSpeed = 10f;     // Higher = snaps to the hold point faster.

    [Header("Events")]
    public UnityEvent onPickedUp;           // Later: tick the checklist, show the notebook notification...

    private Rigidbody rb;
    private Collider[] colliders;
    private bool isHeld;

    private Transform holdTarget;           // The hold point we are following.
    private float holdBlend;                // 0 = just picked up, 1 = fully at the hold point.
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Quaternion heldRotation;
    private Vector3 originalScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        originalScale = transform.localScale;
    }


    private void LateUpdate()
    {
        if (!isHeld || holdTarget == null) return;

        // Blend from where the item was picked up to the hold point.
        holdBlend = Mathf.Clamp01(holdBlend + moveToHoldSpeed * Time.deltaTime);
        float t = Mathf.SmoothStep(0f, 1f, holdBlend);

        transform.position = Vector3.Lerp(startPosition, holdTarget.position, t);
        transform.localScale = Vector3.Lerp(originalScale, originalScale * heldScale, t);

        if (spinWhileHeld)
        {
            // Slowly spin around the world's up axis so the player can see the object.
            heldRotation = Quaternion.AngleAxis(rotateSpeed * Time.deltaTime, Vector3.up) * heldRotation;
        }
        else
        {
            // Turn to the chosen held rotation relative to the camera (a note faces the player).
            Quaternion targetRotation = holdTarget.rotation * Quaternion.Euler(heldEulerAngles);
            heldRotation = Quaternion.Slerp(startRotation, targetRotation, t);
        }

        transform.rotation = heldRotation;
    }

    // IInteractable 

    public virtual string GetPrompt()
    {
        return "Pick up " + itemName;
    }

    public void Interact(PlayerInteractor interactor)
    {
        // The interactor keeps track of what is being held, so ask it to hold us.
        interactor.Hold(this);
    }

    // Called by PlayerInteractor 

    public void PickUp(Transform point)
    {
        isHeld = true;
        holdTarget = point;
        holdBlend = 0f;

        startPosition = transform.position;
        startRotation = transform.rotation;
        heldRotation = transform.rotation;

        // Stop all movement BEFORE making the Rigidbody kinematic.
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Kinematic means physics stops pushing the object around while it is held.
        rb.isKinematic = true;

        // Turn colliders off so the held object cannot block the interaction ray
        // or shove the player around.
        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }

        onPickedUp.Invoke();
    }

    public void Drop()
    {
        isHeld = false;
        holdTarget = null;

        // Back to normal size.
        transform.localScale = originalScale;

        foreach (Collider c in colliders)
        {
            c.enabled = true;
        }

        // Physics takes over again, so the object falls naturally.
        rb.isKinematic = false;
    }

    // Used by the computer's USB slot to "plug in" the item and hold it in place.
    public void SnapTo(Transform point)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        transform.SetPositionAndRotation(point.position, point.rotation);
    }
}