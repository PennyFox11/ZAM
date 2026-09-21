using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
//Akhona Khoali

//This script is for the investigation checklist that shows the player what clues they have found and what they still need to find.

//It also unlocks the entrance door when all clues are found.

[Serializable]
public class ChecklistItem
{
    public string clueId;           // Must match the Clue Id on the ClueTrigger of the clue, e.g. "usb_stick".
    public string label;            // What the player reads, e.g. "Find the USB stick".
    public TMP_Text lineText;       // The text line inside the checklist panel that shows this item.

    [NonSerialized]
    public bool isDone;
}

// Under canvas 
// The checklist panel stays hidden, then:
//   - flickers slowly on screen for a few seconds, every 30 seconds, until it is complete
//   - pops up straight away when the player finds one of its clues, and ticks that line
//     while the player is watching
// When every line is ticked it shows "Investigation complete" and unlocks the entrance door.
// Clues report to this through ClueTrigger, so nothing else needs wiring.
public class InvestigationChecklist : MonoBehaviour
{
    public static InvestigationChecklist Instance { get; private set; }

    [Header("Items (the clues that unlock the door)")]
    public ChecklistItem[] items;

    [Header("Checklist Panel")]
    public GameObject checklistPanel;       // Holds the title and the item lines. Starts hidden.
    public TMP_Text completeText;           // Optional. Inside the panel, shown when everything is done.
    [TextArea]
    public string completeMessage = "Investigation complete. The entrance is open.";

    [Header("When It Appears On Its Own")]
    public float firstShowDelay = 5f;       // Seconds after the game starts.
    public float showEverySeconds = 30f;    // Then again every 30 seconds.
    public float showForSeconds = 5f;       // Stays for 5 seconds each time.
    public float flickerSpeed = 1f;         // Lower = slower flicker.
    [Range(0f, 1f)]
    public float flickerMinAlpha = 0.4f;    // How faint it gets at the dimmest point.

    [Header("When a Clue Is Found")]
    public float markDelay = 0.8f;          // Pause before the line is ticked, so the player sees it happen.
    public float afterMarkSeconds = 3f;     // How long the list stays after a tick.
    public float completeShowSeconds = 5f;  // How long it stays once everything is done.
    public Color todoColour = Color.white;
    public Color doneColour = new Color(0.4f, 1f, 0.4f, 1f);

    [Header("Door")]
    public TeleportDoorInteractable entranceDoor;   // Unlocked when everything is done.
    public UnityEvent onComplete;                   // Anything else that should happen at the end.

    [Header("Sounds (all optional)")]
    public AudioClip tickClip;
    public AudioClip completeClip;

    private CanvasGroup panelGroup;
    private AudioSource sfxAudioSource;
    private Coroutine displayRoutine;
    private Coroutine periodicRoutine;
    private bool isComplete;

    private void Awake()
    {
        Instance = this;

        // The flicker fades a CanvasGroup, so add one if the panel doesn't have it.
        panelGroup = checklistPanel.GetComponent<CanvasGroup>();
        if (panelGroup == null)
        {
            panelGroup = checklistPanel.AddComponent<CanvasGroup>();
        }

        checklistPanel.SetActive(false);

        if (completeText != null)
        {
            completeText.gameObject.SetActive(false);
        }

        sfxAudioSource = gameObject.AddComponent<AudioSource>();
        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.pitch = 1f;

        foreach (ChecklistItem item in items)
        {
            ApplyLine(item);
        }
    }

    private void Start()
    {
        periodicRoutine = StartCoroutine(PeriodicRoutine(firstShowDelay));
    }

    //Called by ClueTrigger when the player finds a clue 

    public void CompleteItem(string clueId)
    {
        if (isComplete) return;

        ChecklistItem item = FindItem(clueId);

        // Most clues are not on the checklist, so quietly ignore those.
        if (item == null || item.isDone) return;

        item.isDone = true;
        StartCoroutine(MarkRoutine(item));
    }

    public bool IsComplete
    {
        get { return isComplete; }
    }

    private IEnumerator MarkRoutine(ChecklistItem item)
    {
        // Pop the list up straight away, steady so it is easy to read.
        ShowPanel(markDelay + afterMarkSeconds, false);

        // A short pause so the player sees the tick happen.
        yield return new WaitForSeconds(markDelay);

        ApplyLine(item);
        PlaySound(tickClip);
        yield return Punch(item.lineText);

        if (AllDone())
        {
            FinishInvestigation();
        }
        else
        {
            // The 30 second timer starts again from this moment.
            RestartTimer();
        }
    }

    private void FinishInvestigation()
    {
        if (isComplete) return;

        isComplete = true;

        if (periodicRoutine != null)
        {
            StopCoroutine(periodicRoutine);
            periodicRoutine = null;
        }

        PlaySound(completeClip);

        // "Investigation complete" shows inside the checklist, or through MessageUI if there is no text for it.
        if (completeText != null)
        {
            completeText.text = completeMessage;
            completeText.gameObject.SetActive(true);
        }
        else if (MessageUI.Instance != null)
        {
            MessageUI.Instance.Show(completeMessage, 4f);
        }

        ShowPanel(completeShowSeconds, false);

        if (entranceDoor != null)
        {
            entranceDoor.Unlock();
        }
        else
        {
            Debug.LogWarning("InvestigationChecklist: the Entrance Door slot is empty, so no door was unlocked.");
        }

        onComplete.Invoke();
    }

    //Showing and hiding the panel 

    // Every 30 seconds (unless a clue popped it up recently) the list flickers on screen for a few seconds.
    private IEnumerator PeriodicRoutine(float firstWait)
    {
        yield return new WaitForSeconds(firstWait);

        while (!isComplete)
        {
            // Skip this one if the panel is already showing because of a clue.
            if (displayRoutine == null)
            {
                ShowPanel(showForSeconds, true);
            }

            yield return new WaitForSeconds(showEverySeconds);
        }
    }

    private void RestartTimer()
    {
        if (periodicRoutine != null)
        {
            StopCoroutine(periodicRoutine);
        }

        periodicRoutine = StartCoroutine(PeriodicRoutine(showEverySeconds));
    }

    private void ShowPanel(float duration, bool flicker)
    {
        if (displayRoutine != null)
        {
            StopCoroutine(displayRoutine);
        }

        displayRoutine = StartCoroutine(DisplayRoutine(duration, flicker));
    }

    private IEnumerator DisplayRoutine(float duration, bool flicker)
    {
        checklistPanel.SetActive(true);

        float endTime = Time.time + duration;

        while (Time.time < endTime)
        {
            if (flicker)
            {
                // Slowly rises and falls between the faint level and fully visible.
                float wave = Mathf.PingPong(Time.time * flickerSpeed, 1f);
                panelGroup.alpha = Mathf.Lerp(flickerMinAlpha, 1f, wave);
            }
            else
            {
                panelGroup.alpha = 1f;
            }

            yield return null;
        }

        checklistPanel.SetActive(false);
        displayRoutine = null;
    }

    // Lines 

    // Shows a line as either not done "[ ] text" or done "[X] text" with a line through it.
    private void ApplyLine(ChecklistItem item)
    {
        if (item.lineText == null) return;

        if (item.isDone)
        {
            item.lineText.text = "[X] <s>" + item.label + "</s>";
            item.lineText.color = doneColour;
        }
        else
        {
            item.lineText.text = "[ ] " + item.label;
            item.lineText.color = todoColour;
        }
    }

    // A quick grow-and-shrink on the line that was just ticked.
    private IEnumerator Punch(TMP_Text text)
    {
        if (text == null) yield break;

        Transform target = text.transform;
        Vector3 originalScale = target.localScale;
        float duration = 0.3f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float bump = Mathf.Sin(time / duration * Mathf.PI) * 0.25f;
            target.localScale = originalScale * (1f + bump);
            yield return null;
        }

        target.localScale = originalScale;
    }

    private bool AllDone()
    {
        foreach (ChecklistItem item in items)
        {
            if (!item.isDone) return false;
        }

        return true;
    }

    // Matches ignoring capital letters and spaces at the ends.
    private ChecklistItem FindItem(string clueId)
    {
        if (clueId == null) return null;

        string wanted = clueId.Trim();

        foreach (ChecklistItem item in items)
        {
            if (item.clueId != null && item.clueId.Trim().Equals(wanted, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return null;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }
}