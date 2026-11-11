using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
//Akhona Khoali 

// Belongs on any object the player can pick up.
// The object also needs a Collider and (this script adds it for you) a Rigidbody.
// NoteInteractable inherits from this script, so notes work the same way.

// The held object is NOT parented to the camera. Instead it copies the hold point's
// position every frame. Parenting under a scaled object (like a stretched player or
// camera) squashes and skews whatever is inside it, so this avoids that completely.
[RequireComponent(typeof(Rigidbody))]
public class PickupInteractable : MonoBehaviour, IInteractable
{
    [Header("Item Info (shown on the left side panel)")]
    public string itemId = "";              // Optional. Other scripts look for this, e.g. "USB".
    public string itemName = "Item";
    [TextArea(3, 6)]
    public string description = "";

    [Header("Hold Style")]
    public bool showInfoPanel = true;       // Show the name/description panel while held.
    public bool useCentreHoldPoint = false; // true = centre of the screen (notes).
    public Vector3 heldEulerAngles = Vector3.zero;  // Rotation relative to the camera (adjust per model).
    public float heldScale = 1f;            // Size while held. Use 0.3 - 0.5 for big objects.
    public float moveToHoldSpeed = 10f;     // Higher = snaps to the hold point faster.

    [Header("Manual Rotation")]
    public bool allowManualRotate = true;   // Hold the left mouse button and drag to spin it yourself.
    public float manualRotateSpeed = 0.3f;  // Degrees per pixel of mouse movement.

    [Header("Sound / Recording (optional)")]
    public AudioClip pickupClip;            // Plays when the player picks this up: a sound, a voice note, a recording...
    [Range(0f, 1f)]
    public float pickupClipVolume = 1f;
    public bool stopClipWhenDropped = false;    // Tick for a long recording that should cut off when the item is put down.
    public bool playClipOnlyOnce = false;       // Tick so picking it up a second time stays silent.

    [Header("Events")]
    public UnityEvent onPickedUp;          // Later: tick the checklist, show the notebook notification...

    private Rigidbody rb;
    private Collider[] colliders;
    private bool isHeld;
    private AudioSource clipSource;
    private bool clipHasPlayed;

    private Transform holdTarget;          // The hold point we are following.
    private float holdBlend;                // 0 = just picked up, 1 = fully at the hold point.
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Quaternion heldRotation;
    private Vector3 originalScale;
    private Quaternion manualRotationOffset = Quaternion.identity;  // Built up by dragging. Never changes on its own.

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        originalScale = transform.localScale;
    }

    // LateUpdate runs after FPController has moved the camera this frame,
    // so the held object never lags behind or jitters.
    private void LateUpdate()
    {
        if (!isHeld || holdTarget == null) return;

        // Blend from where the item was picked up to the hold point.
        holdBlend = Mathf.Clamp01(holdBlend + moveToHoldSpeed * Time.deltaTime);
        float t = Mathf.SmoothStep(0f, 1f, holdBlend);

        transform.position = Vector3.Lerp(startPosition, holdTarget.position, t);
        transform.localScale = Vector3.Lerp(originalScale, originalScale * heldScale, t);

        // The object never spins by itself. Holding the left mouse button and dragging spins it
        // around the camera's up/right axes instead, and that offset sticks until the item is dropped.
        if (allowManualRotate && Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            manualRotationOffset = Quaternion.AngleAxis(-delta.x * manualRotateSpeed, Vector3.up) * manualRotationOffset;
            manualRotationOffset = Quaternion.AngleAxis(delta.y * manualRotateSpeed, Vector3.right) * manualRotationOffset;
        }

        // Turn to face the camera (a note/document faces the player), plus whatever the player has dragged.
        Quaternion targetRotation = holdTarget.rotation * Quaternion.Euler(heldEulerAngles) * manualRotationOffset;
        heldRotation = Quaternion.Slerp(startRotation, targetRotation, t);

        transform.rotation = heldRotation;
    }

    //  IInteractable 

    public virtual string GetPrompt()
    {
        return "Pick up " + itemName;
    }

    public virtual void Interact(PlayerInteractor interactor)
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
        manualRotationOffset = Quaternion.identity;  // Start facing the camera square-on each time.

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

        PlayPickupClip();

        onPickedUp.Invoke();
    }

    // Plays the sound / recording through its own 2D AudioSource (created on first use),
    // so it is heard at full volume wherever the player is looking.
    private void PlayPickupClip()
    {
        if (pickupClip == null) return;
        if (playClipOnlyOnce && clipHasPlayed) return;

        if (clipSource == null)
        {
            clipSource = gameObject.AddComponent<AudioSource>();
            clipSource.playOnAwake = false;
            clipSource.spatialBlend = 0f;
        }

        clipHasPlayed = true;
        clipSource.Stop();
        clipSource.clip = pickupClip;
        clipSource.volume = pickupClipVolume;
        clipSource.Play();
    }

    public virtual void Drop()
    {
        isHeld = false;
        holdTarget = null;

        if (stopClipWhenDropped && clipSource != null)
        {
            clipSource.Stop();
        }

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