using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
//Akhona Khoali

// Belongs under canvas 
// Shows a conversation between two people: the player on the left, the NPC on the right.
// The person who is talking is bright, the other one is dimmed.
public class DialogueUI : MonoBehaviour, IInteractionScreen
{
    // Lets any NPC find the dialogue box with DialogueUI.Instance.
    public static DialogueUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TMP_Text speakerText;
    public TMP_Text dialogueText;
    public Image playerPortraitImage;       // LEFT side.

    [FormerlySerializedAs("portraitImage")] // Keeps your old slot assignment.
    public Image npcPortraitImage;          // RIGHT side.

    [Header("Speaker Highlight")]
    public Color activeColour = Color.white;
    public Color inactiveColour = new Color(0.35f, 0.35f, 0.35f, 1f);

    [Header("Typing Effect")]
    public float typingSpeed = 0.04f;           // Seconds between letters.
    public int soundEveryNLetters = 2;          // Play the sound on every 2nd letter.

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip typingClip;
    public bool loopClipWhileTyping = true;     // ON: one long recording plays while typing. OFF: a short click per letter.

    [Header("Voice Acting")]
    public AudioSource voiceAudioSource;        // Plays each line's recording. Created for you if empty.
    [Range(0f, 1f)]
    public float voiceVolume = 1f;
    public bool syncTypingToVoice = false;      // ON: the text finishes typing exactly when the recording ends.

    private DialogueInteractable currentNPC;
    private DialogueLine[] currentLines;
    private int lineIndex;
    private bool isTyping;
    private Coroutine typingRoutine;
    private PlayerInteractor currentInteractor;

    private void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false);

        // If no Audio Source was assigned
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // A separate Audio Source 
        if (voiceAudioSource == null)
        {
            voiceAudioSource = gameObject.AddComponent<AudioSource>();
            voiceAudioSource.playOnAwake = false;
        }

        voiceAudioSource.loop = false;
        voiceAudioSource.pitch = 1f;                // Always the recording's normal speed.
    }

    public void StartDialogue(DialogueInteractable npc, PlayerInteractor interactor)
    {
        currentNPC = npc;
        currentLines = npc.lines;
        currentInteractor = interactor;
        lineIndex = 0;

        dialoguePanel.SetActive(true);

        // Set both portraits once. A missing portrait is simply hidden.
        SetPortrait(playerPortraitImage, npc.playerPortrait);
        SetPortrait(npcPortraitImage, npc.npcPortrait);

        // Freeze the player. false = keep the cursor hidden (no clicking needed).
        currentInteractor.LockPlayer(this, false);

        ShowLine();
    }

    // Called by PlayerInteractor when the player presses E during dialogue.
    public void OnInteractPressed()
    {
        if (isTyping)
        {
            // First press: finish the current line instantly.
            FinishTyping();
        }
        else
        {
            // Second press: go to the next line, or close the box.
            lineIndex++;

            if (lineIndex < currentLines.Length)
            {
                ShowLine();
            }
            else
            {
                EndDialogue();
            }
        }
    }

    private void ShowLine()
    {
        DialogueLine line = currentLines[lineIndex];
        bool playerSpeaking = line.speaker == DialogueSpeaker.Player;

        // Show who is talking: their name, and their portrait bright while the other is dimmed.
        speakerText.text = playerSpeaking ? currentNPC.playerName : currentNPC.npcName;

        if (playerPortraitImage != null)
        {
            playerPortraitImage.color = playerSpeaking ? activeColour : inactiveColour;
        }

        if (npcPortraitImage != null)
        {
            npcPortraitImage.color = playerSpeaking ? inactiveColour : activeColour;
        }

        // Play this line's recording. A line with no recording just stops the previous voice.
        PlayVoice(line.voiceClip);

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }
        typingRoutine = StartCoroutine(TypeLine(line.text, line.voiceClip));
    }

    private void SetPortrait(Image image, Sprite sprite)
    {
        if (image == null) return;

        // The NPC's portrait wins. If the NPC has none, keep the sprite already set on the Image.
        if (sprite != null)
        {
            image.sprite = sprite;
        }

        // Only hide the portrait if there is no sprite at all.
        image.gameObject.SetActive(image.sprite != null);
    }

    private IEnumerator TypeLine(string fullText, AudioClip voiceClip)
    {
        isTyping = true;

        // Put the whole line in the text box, but hide every letter.
        dialogueText.text = fullText;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        int totalLetters = dialogueText.textInfo.characterCount;

        // Normally the typing speed is fixed. With Sync Typing To Voice, the text takes exactly
        // as long as the recording, so the words appear as they are spoken.
        float letterDelay = typingSpeed;
        if (syncTypingToVoice && voiceClip != null && totalLetters > 0)
        {
            letterDelay = voiceClip.length / totalLetters;
        }

        StartTypingSound();

        // Reveal one letter at a time.
        for (int i = 1; i <= totalLetters; i++)
        {
            dialogueText.maxVisibleCharacters = i;

            // In "click per letter" mode, play a short sound every few letters.
            char letter = dialogueText.textInfo.characterInfo[i - 1].character;
            if (!loopClipWhileTyping && !char.IsWhiteSpace(letter) && i % soundEveryNLetters == 0)
            {
                PlayTypingSound();
            }

            yield return new WaitForSeconds(letterDelay);
        }

        StopTypingSound();
        isTyping = false;
    }

    private void FinishTyping()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        StopTypingSound();

        // A very large number means "show every letter".
        dialogueText.maxVisibleCharacters = 99999;
        isTyping = false;
    }

    private void EndDialogue()
    {
        StopTypingSound();
        StopVoice();
        dialoguePanel.SetActive(false);
        currentInteractor.UnlockPlayer();
    }

    // Voice acting

    // Plays the recording for the line that just appeared, cutting off the previous one.
    private void PlayVoice(AudioClip clip)
    {
        StopVoice();

        if (clip == null || voiceAudioSource == null) return;

        voiceAudioSource.clip = clip;
        voiceAudioSource.volume = voiceVolume;
        voiceAudioSource.Play();
    }

    private void StopVoice()
    {
        if (voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
        }
    }

    // ----- Typing sound -----

    // Long recording: starts when a line starts typing and loops until the line is finished.
    private void StartTypingSound()
    {
        if (!loopClipWhileTyping || audioSource == null || typingClip == null) return;

        audioSource.clip = typingClip;
        audioSource.loop = true;
        audioSource.pitch = 1f;                 // Always the clip's normal speed.
        audioSource.Play();
    }

    private void StopTypingSound()
    {
        if (!loopClipWhileTyping || audioSource == null) return;

        audioSource.Stop();
        audioSource.loop = false;
    }

    // Short click: plays once per few letters.
    private void PlayTypingSound()
    {
        if (audioSource == null || typingClip == null) return;

        audioSource.pitch = 1f;                 // Always the clip's normal speed.
        audioSource.PlayOneShot(typingClip);
    }
}