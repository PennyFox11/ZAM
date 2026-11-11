using UnityEngine;
//Akhona Khoali

// Put this on the telephone opposite the interrogation room door (with a Collider).
// It starts silent. InterrogationRoomUI calls StartRinging() the instant an interrogation ends -
// whether the suspect confessed, the timer ran out, or the player left early with E. While it's
// ringing, E answers it and plays the recording (Frank's sister).
public class TelephoneInteractable : MonoBehaviour, IInteractable
{
    [Header("The Call")]
    public AudioClip recordingClip;         // Frank's sister, on the phone.
    [TextArea(2, 4)]
    public string subtitle = "";            // Optional. Shown via MessageUI while it plays.

    [Header("Ringing")]
    public GameObject ringingIndicator;     // Optional: a light, an icon, anything that shows it's ringing.
    public AudioClip ringClip;              // Loops while it rings.
    public AudioSource ringAudioSource;     // Created for you if empty.

    public bool IsRinging { get; private set; }
    public bool HasBeenAnswered { get; private set; }

    private AudioSource voiceAudioSource;

    private void Awake()
    {
        if (ringAudioSource == null)
        {
            ringAudioSource = gameObject.AddComponent<AudioSource>();
        }
        ringAudioSource.playOnAwake = false;
        ringAudioSource.loop = true;

        voiceAudioSource = gameObject.AddComponent<AudioSource>();
        voiceAudioSource.playOnAwake = false;

        if (ringingIndicator != null)
        {
            ringingIndicator.SetActive(false);
        }
    }

    // Called by InterrogationRoomUI when the interrogation ends, however it ends.
    public void StartRinging()
    {
        if (HasBeenAnswered || IsRinging) return;

        IsRinging = true;

        if (ringingIndicator != null)
        {
            ringingIndicator.SetActive(true);
        }

        if (ringClip != null)
        {
            ringAudioSource.clip = ringClip;
            ringAudioSource.Play();
        }
    }

    // IInteractable 

    public string GetPrompt()
    {
        return IsRinging ? "Answer phone" : "Phone";
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!IsRinging) return;

        IsRinging = false;
        HasBeenAnswered = true;

        ringAudioSource.Stop();

        if (ringingIndicator != null)
        {
            ringingIndicator.SetActive(false);
        }

        if (recordingClip != null)
        {
            voiceAudioSource.clip = recordingClip;
            voiceAudioSource.Play();
        }

        if (!string.IsNullOrEmpty(subtitle) && MessageUI.Instance != null)
        {
            float duration = recordingClip != null ? recordingClip.length : 3f;
            MessageUI.Instance.Show(subtitle, duration);
        }
    }
}