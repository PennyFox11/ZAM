using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
//Akhona Khoali

// One line of an act's mission list. Filled  in the InvestigationChecklist in the Inspector.
[Serializable]
public class ChecklistItem
{
    public string clueId;           // The id that ticks this line, e.g. "usb_stick". See ClueTrigger / the notes below.
    public string label;            // What the player reads, e.g. "Find the USB stick".

    [NonSerialized]
    public TMP_Text lineText;       // Made for you at runtime.
    [NonSerialized]
    public bool isDone;
}

// One act: a title and the lines the player has to tick off before the next act starts.
[Serializable]
public class ChecklistAct
{
    public string actTitle = "Act 1";
    public ChecklistItem[] items;

    [Header("When this act is finished (all optional)")]
    [TextArea(1, 2)]
    public string completeMessage = "";                 // Shown briefly, e.g. "Act 1 complete".
    public TeleportDoorInteractable doorToUnlock;       // Unlocked the moment this act is finished.
    public UnityEvent onActComplete;                    // Anything else that should happen.
}

// Belongs on the canvas. Shows the player's current investigation objectives and ticks them off as they are completed.
// A chain of acts that works as the player's mission list:
//   - only ONE act is on screen at a time. Once every line of it is ticked, the next act appears.
//   - lines can be ticked in any order, and a line ticked early (before its act starts) is already
//     ticked when its act arrives.
//   - the panel stays on screen while the player walks around, and fades away while a screen is
//     open (the computer, the notebook, the pause menu, the report...). It stays during dialogue.
//
// What ticks a line? Anything that reports its Clue Id, which is the same id used everywhere else:
//   a pickup / scanner fingerprint -> ClueTrigger on it with that id
//   a dialogue line                -> the line's "Clue Id" field
//   a computer entry               -> the entry's "Clue Id" field
//   the report finished / a computer unlocked by code / a door used / the interrogation over
//                                  -> the "Checklist Id" field on that object
// The id does NOT have to be a clue in the EvidenceNotebook.
public class InvestigationChecklist : MonoBehaviour
{
    public static InvestigationChecklist Instance { get; private set; }

    [Header("The Acts (in order)")]
    public ChecklistAct[] acts;

    [Header("Checklist Panel")]
    public GameObject checklistPanel;       // Holds the title and the lines. Keep it ACTIVE in the Hierarchy.
    public TMP_Text actTitleText;           // e.g. "ACT 1".
    public Transform lineContainer;         // Inside the panel. Give it a Vertical Layout Group.
    public TMP_Text linePrefab;             // One line of text. Can be a prefab, or a template text inside the container.
    public string finishedTitle = "All objectives complete";
    public float finishedShowSeconds = 5f;  // How long that stays before the panel goes away for good.

    [Header("Visibility")]
    public bool showDuringDialogue = true;  // Stays on screen while talking to someone.
    public float fadeSpeed = 6f;            // Higher = fades in and out faster.

    [Header("Ticking")]
    public float markDelay = 0.6f;          // Pause before the next act appears, so the last tick is seen.
    public float actCompleteSeconds = 2f;   // How long a finished act stays on screen before the next one.
    public Color todoColour = Color.white;
    public Color doneColour = new Color(0.4f, 1f, 0.4f, 1f);

    [Header("Sounds (all optional)")]
    public AudioClip tickClip;
    public AudioClip actCompleteClip;
    public AudioClip allCompleteClip;

    // Every id that has been reported so far, so lines for later acts can be ticked early.
    private HashSet<string> doneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private CanvasGroup panelGroup;
    private AudioSource sfxAudioSource;
    private PlayerInteractor interactor;
    private int currentAct = -1;
    private bool advancing;
    private bool finished;
    private bool finishedHidden;

    public bool IsComplete
    {
        get { return finished; }
    }

    public int CurrentActIndex
    {
        get { return currentAct; }
    }

    private void Awake()
    {
        Instance = this;

        panelGroup = checklistPanel.GetComponent<CanvasGroup>();
        if (panelGroup == null)
        {
            panelGroup = checklistPanel.AddComponent<CanvasGroup>();
        }

        // Starts invisible and fades in once the game is being played. It must never catch clicks.
        panelGroup.alpha = 0f;
        panelGroup.blocksRaycasts = false;
        panelGroup.interactable = false;

        if (linePrefab != null && linePrefab.gameObject.scene.IsValid())
        {
            // A template text placed in the scene is only a model for the real lines.
            linePrefab.gameObject.SetActive(false);
        }

        sfxAudioSource = gameObject.AddComponent<AudioSource>();
        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.pitch = 1f;

        interactor = FindFirstObjectByType<PlayerInteractor>();
    }

    private void Start()
    {
        if (acts == null || acts.Length == 0)
        {
            Debug.LogWarning("InvestigationChecklist: the Acts list is empty, so there is nothing to show.");
            return;
        }

        ShowAct(0);
    }

    // Fades the whole panel in while the player is walking around, out while a screen is open.
    private void Update()
    {
        bool show = !finishedHidden && acts != null && acts.Length > 0 && CanShowNow();

        panelGroup.alpha = Mathf.MoveTowards(panelGroup.alpha, show ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
    }

    private bool CanShowNow()
    {
        if (interactor == null || !interactor.InputLocked) return true;

        // A screen is open. Only a dialogue is allowed to keep the list on screen.
        if (showDuringDialogue && DialogueUI.Instance != null && DialogueUI.Instance.dialoguePanel != null)
        {
            return DialogueUI.Instance.dialoguePanel.activeSelf;
        }

        return false;
    }

    //  Called by ClueTrigger (and the Checklist Id fields) when something is done 

    public void CompleteItem(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        id = id.Trim();

        // Already reported. Everything is remembered, so a repeat does nothing.
        if (!doneIds.Add(id)) return;

        if (finished || advancing || currentAct < 0) return;

        ChecklistAct act = acts[currentAct];
        bool ticked = false;

        foreach (ChecklistItem item in act.items)
        {
            if (!item.isDone && SameId(item.clueId, id))
            {
                item.isDone = true;
                ApplyLine(item);
                StartCoroutine(Punch(item.lineText));
                ticked = true;
            }
        }

        if (ticked)
        {
            PlaySound(tickClip);

            if (AllDone(act))
            {
                StartCoroutine(AdvanceRoutine(false));
            }
        }
    }

    //  Showing an act 

    private void ShowAct(int index)
    {
        currentAct = index;
        ChecklistAct act = acts[index];

        if (actTitleText != null)
        {
            actTitleText.text = act.actTitle;
        }

        ClearLines();

        foreach (ChecklistItem item in act.items)
        {
            // Anything reported before this act started is already ticked.
            item.isDone = !string.IsNullOrEmpty(item.clueId) && doneIds.Contains(item.clueId.Trim());

            TMP_Text line = Instantiate(linePrefab, lineContainer);
            line.gameObject.SetActive(true);
            item.lineText = line;

            ApplyLine(item);
        }

        // Everything was already done early: move straight on.
        if (AllDone(act))
        {
            StartCoroutine(AdvanceRoutine(true));
        }
    }

    private void ClearLines()
    {
        foreach (Transform child in lineContainer)
        {
            // Keep the template text (if there is one inside the container).
            if (linePrefab != null && child.gameObject == linePrefab.gameObject) continue;

            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    //  Finishing an act 

    private IEnumerator AdvanceRoutine(bool quick)
    {
        advancing = true;

        // Don't move on while the player can't see the list (they're in the computer, say).
        // The next act appears the moment they are back to walking around.
        while (panelGroup.alpha < 0.9f)
        {
            yield return null;
        }

        yield return new WaitForSeconds(quick ? 0.3f : markDelay);

        ChecklistAct act = acts[currentAct];

        PlaySound(actCompleteClip);

        if (!string.IsNullOrEmpty(act.completeMessage) && MessageUI.Instance != null)
        {
            MessageUI.Instance.Show(act.completeMessage, 3f);
        }

        if (act.doorToUnlock != null)
        {
            act.doorToUnlock.Unlock();
        }

        if (act.onActComplete != null)
        {
            act.onActComplete.Invoke();
        }

        yield return new WaitForSeconds(actCompleteSeconds);

        advancing = false;

        if (currentAct + 1 < acts.Length)
        {
            ShowAct(currentAct + 1);
        }
        else
        {
            StartCoroutine(FinishRoutine());
        }
    }

    private IEnumerator FinishRoutine()
    {
        finished = true;

        PlaySound(allCompleteClip);

        if (actTitleText != null)
        {
            actTitleText.text = finishedTitle;
        }

        ClearLines();

        yield return new WaitForSeconds(finishedShowSeconds);

        finishedHidden = true;
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

        while (time < duration && target != null)
        {
            time += Time.deltaTime;
            float bump = Mathf.Sin(time / duration * Mathf.PI) * 0.25f;
            target.localScale = originalScale * (1f + bump);
            yield return null;
        }

        if (target != null)
        {
            target.localScale = originalScale;
        }
    }

    private bool AllDone(ChecklistAct act)
    {
        foreach (ChecklistItem item in act.items)
        {
            if (!item.isDone) return false;
        }

        return true;
    }

    // Matches ignoring capital letters and spaces at the ends.
    private bool SameId(string a, string b)
    {
        return a != null && a.Trim().Equals(b.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }
}