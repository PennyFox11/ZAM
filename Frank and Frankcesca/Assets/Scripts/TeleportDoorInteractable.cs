using System.Collections;
using UnityEngine;
//AKhona Khoali

// Anything that can be used while the player is carrying an item implements this.
// Normally E drops the held item. PlayerInteractor checks this first, so E can use
// a door while the player holds the USB. (You can move this into your interfaces file.)
public interface IUsableWhileHolding
{
    bool UsableWhileHolding { get; }
}

// The apartment entrance door. 
// Like every other interactable it implements IInteractable, so PlayerInteractor shows its
// prompt and calls Interact() when E is pressed.
//   Locked   -> E shows the locked message.
//   Unlocked -> E asks "Are you sure...?" with Yes / No. Yes fades out, teleports the player
//               to the Destination in the office, and fades back in. No lets them keep investigating.
// The InvestigationChecklist calls Unlock() when every clue has been found.
// The door does not swing open. It is a teleport door, so the office can be in the same scene.
public class TeleportDoorInteractable : MonoBehaviour, IInteractable, IInteractionScreen, IUsableWhileHolding
{
    [Header("Door Info")]
    public string objectName = "door";
    public bool usableWhileHolding = true;  // true = E uses the door even while carrying the USB.
    public string unlockedPrompt = "Go to the office";
    public bool isLocked = true;
    [TextArea]
    public string lockedMessage = "Locked until the investigation is conducted.";

    [Header("Teleport")]
    public Transform destination;           // An empty object in the office where the player appears.
    [TextArea]
    public string confirmQuestion = "Are you sure you want to go to the office?";
    [TextArea]
    public string reminder = "Don't forget the USB stick!";     // Shown under the question.
    public CanvasGroup fadePanel;           // Optional: a full-screen black Image with a Canvas Group.
    public float fadeTime = 0.5f;

    [Header("Audio (all optional)")]
    public AudioClip lockedClip;            // Rattle when the player tries the locked door.
    public AudioClip unlockedClip;          // When the checklist unlocks it.
    public AudioClip teleportClip;          // Door / footsteps as the player goes through.

    private void Start()
    {
        // The fade panel is invisible until a teleport starts.
        if (fadePanel != null)
        {
            fadePanel.alpha = 0f;

            // A see-through panel must never catch mouse clicks meant for the Yes / No buttons.
            fadePanel.blocksRaycasts = false;
            fadePanel.interactable = false;

            fadePanel.gameObject.SetActive(false);
        }
    }

    //IUsableWhileHolding 

    public bool UsableWhileHolding
    {
        get { return usableWhileHolding; }
    }

    // IInteractabl

    public string GetPrompt()
    {
        return isLocked ? "Try " + objectName : unlockedPrompt;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (isLocked)
        {
            PlaySound(lockedClip);

            if (MessageUI.Instance != null)
            {
                MessageUI.Instance.Show(lockedMessage);
            }
            else
            {
                Debug.LogWarning(name + ": there is no MessageUI in the scene, so the locked message can't show. " + lockedMessage);
            }

            return;
        }

        if (destination == null)
        {
            Debug.LogWarning(name + ": the Destination slot is empty. Drag the office spawn point into it.");
            return;
        }

        if (ConfirmDialogUI.Instance == null)
        {
            Debug.LogWarning(name + ": there is no ConfirmDialogUI in the scene.");
            return;
        }

        // The question, with the reminder on the line below it.
        string message = confirmQuestion;
        if (!string.IsNullOrEmpty(reminder))
        {
            message += "\n" + reminder;
        }

        ConfirmDialogUI.Instance.Ask(message, interactor, () => StartCoroutine(Teleport(interactor)));
    }

    // While the screen fades, E does nothing.
    public void OnInteractPressed()
    {
    }

    // Public methods other scripts can call 

    public void Unlock()
    {
        if (!isLocked) return;

        isLocked = false;
        PlaySound(unlockedClip);
    }

    public void Lock()
    {
        isLocked = true;
    }

    //Teleport 

    private IEnumerator Teleport(PlayerInteractor interactor)
    {
        // Freeze the player during the fade.
        interactor.LockPlayer(this, false);

        yield return Fade(0f, 1f);

        Transform player = interactor.transform;

        // TEMPORARY DEBUG LINE: delete once the doors work.
        Debug.Log("Teleporting player to: " + destination.name + " at " + destination.position);

        // A CharacterController must be switched off, or it pulls the player back to the old spot.
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        player.position = destination.position;
        player.rotation = Quaternion.Euler(0f, destination.eulerAngles.y, 0f);

        if (controller != null)
        {
            controller.enabled = true;
        }

        PlaySound(teleportClip);

        yield return Fade(1f, 0f);

        interactor.UnlockPlayer();
    }

    // Fades the black panel from one alpha to another. Skipped if there is no fade panel.
    private IEnumerator Fade(float from, float to)
    {
        if (fadePanel == null) yield break;

        fadePanel.gameObject.SetActive(true);

        float time = 0f;
        while (time < fadeTime)
        {
            time += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(from, to, time / fadeTime);
            yield return null;
        }

        fadePanel.alpha = to;

        if (to <= 0f)
        {
            fadePanel.gameObject.SetActive(false);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }
}