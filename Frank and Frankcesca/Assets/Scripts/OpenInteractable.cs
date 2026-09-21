using UnityEngine;
//Akhona Khoali
// One script for EVERY door-like object: normal doors, the oven door, drawers,
// lockers, the fridge and the pantry. It slowly moves/rotates the object from
// its closed position to an open one. E opens, E closes.
//

public class OpenableInteractable : MonoBehaviour, IInteractable
{
    [Header("Open Settings")]
    public string objectName = "door";              // Used in the prompt: "Open door".
    public Vector3 openRotation = new Vector3(0f, 90f, 0f);   // Extra rotation when open (degrees).
    public Vector3 openPosition = Vector3.zero;     // Extra sliding when open (for drawers).
    public float openDuration = 1.5f;               // Seconds to fully open. Bigger = slower.

    [Header("Lock Settings")]
    public bool isLocked = false;
    public string lockedMessage = "It's locked.";

    [Header("Audio (optional)")]
    public AudioSource audioSource;                 // If empty, sounds still play from this position.
    public AudioClip openClip;                      // The creak.
    public AudioClip closeClip;
    public AudioClip lockedClip;

    public bool IsOpen { get; private set; }

    private Vector3 closedPosition;
    private Quaternion closedRotation;
    private float progress;                         // 0 = fully closed, 1 = fully open.

    private void Awake()
    {
        closedPosition = transform.localPosition;
        closedRotation = transform.localRotation;
    }

    private void Update()
    {
        float target = IsOpen ? 1f : 0f;

        // Nothing to animate.
        if (Mathf.Approximately(progress, target)) return;

        // Move progress toward the target a little each frame.
        progress = Mathf.MoveTowards(progress, target, Time.deltaTime / openDuration);

        // SmoothStep makes the movement start and end gently.
        float t = Mathf.SmoothStep(0f, 1f, progress);

        transform.localRotation = closedRotation * Quaternion.Euler(openRotation * t);
        transform.localPosition = closedPosition + closedRotation * (openPosition * t);
    }

    // IInteractable 

    // handles feedback
    public virtual string GetPrompt()
    {
        if (isLocked)
        {
            return "Try " + objectName;
        }

        return (IsOpen ? "Close " : "Open ") + objectName;
    }

    public virtual void Interact(PlayerInteractor interactor)
    {
        if (isLocked)
        {
            PlaySound(lockedClip);

            if (MessageUI.Instance != null)
            {
                MessageUI.Instance.Show(lockedMessage);
            }
            return;
        }

        if (IsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    //Public methods other scripts can call 

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        PlaySound(openClip);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        PlaySound(closeClip);
    }

    // The checklist will call Unlock() on the apartment door later.
    public void Unlock()
    {
        isLocked = false;
    }

    public void Lock()
    {
        isLocked = true;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }
}