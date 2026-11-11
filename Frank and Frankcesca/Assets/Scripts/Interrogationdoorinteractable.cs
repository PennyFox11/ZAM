using UnityEngine;

// Belongs to the interrogation room's door (with a Collider), like any other interactable.
// E hands everything over to InterrogationRoomUI with THIS suspect's clues, confession
// settings, timer and teleport points.
//
// This is a ONE SHOT: the moment the player steps through, the door is used up for good -
// confesses, runs out of time, or leaves early with E, it doesn't matter, E does nothing more
// here afterwards.
public class InterrogationDoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Suspect")]
    public string suspectName = "the suspect";

    [Header("Clues (only ones the player can bring up)")]
    public InterrogationClue[] clues;

    [Header("Confession")]
    [Tooltip("How many incriminating clues it takes to fill the bar and get a confession.")]
    public int incriminatingCluesToConfess = 3;
    [TextArea(2, 4)]
    public string confessionLine = "Alright... alright, I did it.";
    public AudioClip confessionVoiceClip;

    [Header("Timer")]
    public float timeLimitSeconds = 120f;       // 2 minutes by default.

    [Header("Teleport")]
    public Transform roomPoint;                 // Where the player appears inside, facing the suspect.
    public Transform returnPoint;                // Where the player appears back outside, by this door.

    [Header("When It Ends")]
    public TelephoneInteractable phone;         // Rings the moment the interrogation ends, however it ends.
    public string checklistId = "";             // Optional. Ticks this task on the checklist when it ends, e.g. "interrogate_vivek".

    [Header("Audio (optional)")]
    public AudioClip lockedClip;                // Played if the player tries again after using their shot.

    public bool HasBeenUsed { get; private set; }

    // IInteractable 

    public string GetPrompt()
    {
        return HasBeenUsed ? "Nothing more to ask" : "Interrogate " + suspectName;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (HasBeenUsed)
        {
            PlaySound(lockedClip);

            if (MessageUI.Instance != null)
            {
                MessageUI.Instance.Show("There's nothing more to ask " + suspectName + ".");
            }
            return;
        }

        if (roomPoint == null || returnPoint == null)
        {
            Debug.LogWarning(name + ": drag the Room Point and Return Point transforms in, or the teleport has nowhere to go.");
            return;
        }

        if (InterrogationRoomUI.Instance == null)
        {
            Debug.LogWarning(name + ": there is no InterrogationRoomUI in the scene.");
            return;
        }

        InterrogationRoomUI.Instance.Begin(this, interactor);
    }

    // Called by InterrogationRoomUI the instant the player steps in - this IS the one shot,
    // spent whether or not anything comes of it.
    public void MarkUsed()
    {
        HasBeenUsed = true;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }
}