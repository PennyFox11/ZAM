using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
//Akhona Khoali
// One question on the report. Must be filled in on the ReportExamUI in the Inspector, one per
// blank already laid out on the form in the Canvas.
[Serializable]
public class ReportQuestion
{
    [TextArea(1, 2)]
    public string questionLabel;            // Just for your own reference in the Inspector, e.g. "Who killed Frank?".
    public ReportAnswerSlot answerSlot;     // The blank the player drags evidence onto.

    [Tooltip("Clue Ids from the EvidenceNotebook. Dropping ANY ONE of these into the blank counts as correct.")]
    public string[] acceptableClueIds;
}

// Sits on Canvas objects that are the blanks on the report form. The player drags a clue chip onto it.
// Shift opens and closes the case report. Instead of typing, the player drags evidence they have
// collected in the EvidenceNotebook (the chips in the tray) onto the blanks, then presses Submit:
//   - a correct blank turns green, a wrong or empty one turns red
//   - the result line says how many are wrong; the player fixes them and submits again
//   - changing a blank clears its colour until the next Submit
//   - once every blank is right, the player is notified and the blanks lock (nothing is unlocked
//     automatically - IsComplete is public, so hook up whatever should happen next yourself,
//     e.g. by checking ReportExamUI.Instance.IsComplete elsewhere)
// Only clues the player has actually found show up in the tray, and it refreshes every time the
// report is opened. Blanks keep whatever was dropped on them between openings.
// Works like EvidenceNotebook/PauseMenuUI: hook a "Report" action bound to Shift in your Input
// Actions asset, Player Input calling this script's OnReport(...).
public class ReportExamUI : MonoBehaviour, IInteractionScreen
{
    public static ReportExamUI Instance { get; private set; }

    [Header("Report UI")]
    public GameObject reportPanel;          // The whole report screen. Starts hidden.
    public ReportQuestion[] questions;
    public TMP_Text resultText;             // "There are 2 wrong answers." / the success message.
    [TextArea(1, 2)]
    public string allCorrectMessage = "Report complete.";

    [Header("Evidence Tray")]
    public Transform chipContainer;         // Where the draggable chips go (give it a Grid / Layout Group).
    public ReportClueChip chipPrefab;

    [Header("Clue Description (optional)")]
    public GameObject infoPanel;            // A box that appears while hovering a chip or a filled blank. Starts hidden.
    public TMP_Text infoTitleText;          // Optional. The clue's title.
    public TMP_Text infoDescriptionText;    // The clue's description, straight from the EvidenceNotebook.

    [Header("Answer Colours")]
    public Color correctColour = new Color(0.15f, 0.7f, 0.2f);   // Green.
    public Color wrongColour = new Color(0.75f, 0.1f, 0.1f);     // Red.

    [Header("On Completion")]
    [TextArea(2, 3)]
    public string completionMessage = "The boss is ready to see you.";
    public string checklistId = "report_submitted";     // Ticks this task on the checklist. Leave empty for none.

    [Header("Sounds (all optional)")]
    public AudioClip openClip;
    public AudioClip wrongClip;
    public AudioClip successClip;

    public bool IsComplete { get; private set; }

    private PlayerInteractor interactor;
    private AudioSource sfxAudioSource;
    private bool isOpen;

    private void Awake()
    {
        Instance = this;
        reportPanel.SetActive(false);

        if (resultText != null)
        {
            resultText.text = "";
        }

        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }

        interactor = FindFirstObjectByType<PlayerInteractor>();

        sfxAudioSource = gameObject.AddComponent<AudioSource>();
        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.pitch = 1f;
    }

    // Join this up in the Player Input component: Report action -> ReportExamUI.OnReport.
    public void OnReport(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (isOpen)
        {
            Close();
        }
        else
        {
            // Don't open over a dialogue, safe, computer, notebook or pause screen.
            if (interactor != null && interactor.InputLocked) return;

            Open();
        }
    }

    // E does nothing here: the mouse drives the form directly.
    public void OnInteractPressed()
    {
    }

    private void Open()
    {
        isOpen = true;
        reportPanel.SetActive(true);

        BuildChips();

        if (interactor != null)
        {
            interactor.LockPlayer(this, true);
        }

        PlaySound(openClip);
    }

    // Called by a chip or filled blank while the mouse is over it.
    public void ShowClueInfo(string clueId)
    {
        if (infoPanel == null || EvidenceNotebook.Instance == null) return;

        ClueEntry clue = EvidenceNotebook.Instance.GetClue(clueId);
        if (clue == null) return;

        if (infoTitleText != null)
        {
            infoTitleText.text = clue.title;
        }

        if (infoDescriptionText != null)
        {
            infoDescriptionText.text = clue.description;
        }

        infoPanel.SetActive(true);
    }

    public void HideClueInfo()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }
    }

    private void Close()
    {
        isOpen = false;
        HideClueInfo();
        reportPanel.SetActive(false);

        if (interactor != null)
        {
            interactor.UnlockPlayer();
        }
    }

    // One draggable chip for every clue the player has found, rebuilt on every open so
    // anything found since last time appears.
    private void BuildChips()
    {
        if (chipContainer == null || chipPrefab == null)
        {
            Debug.LogError("ReportExamUI: drag the tray's container into Chip Container and the chip prefab into Chip Prefab.", this);
            return;
        }

        foreach (Transform child in chipContainer)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        EvidenceNotebook notebook = EvidenceNotebook.Instance;
        if (notebook == null || notebook.clues == null) return;

        foreach (ClueEntry clue in notebook.clues)
        {
            if (!notebook.IsClueDiscovered(clue.clueId)) continue;

            ReportClueChip chip = Instantiate(chipPrefab, chipContainer);
            chip.Setup(clue.clueId.Trim(), clue.title, clue.image);
        }
    }

    // Wire the form's Submit button to this.
    public void OnSubmitPressed()
    {
        if (IsComplete) return;

        int wrongCount = 0;

        foreach (ReportQuestion question in questions)
        {
            bool correct = IsCorrect(question);

            if (question.answerSlot != null)
            {
                question.answerSlot.SetResultColour(correct ? correctColour : wrongColour);
            }

            if (!correct)
            {
                wrongCount++;
            }
        }

        if (wrongCount == 0)
        {
            Complete();
        }
        else if (resultText != null)
        {
            resultText.color = wrongColour;
            resultText.text = wrongCount == 1
                ? "There is 1 wrong answer."
                : "There are " + wrongCount + " wrong answers.";

            PlaySound(wrongClip);
        }
    }

    // An empty blank is wrong. Otherwise the dropped clue must be one of the accepted ids.
    private bool IsCorrect(ReportQuestion question)
    {
        if (question.answerSlot == null || question.answerSlot.IsEmpty) return false;
        if (question.acceptableClueIds == null) return false;

        string given = question.answerSlot.ClueId.Trim();

        foreach (string accepted in question.acceptableClueIds)
        {
            if (accepted != null && given.Equals(accepted.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void Complete()
    {
        IsComplete = true;

        foreach (ReportQuestion question in questions)
        {
            if (question.answerSlot != null)
            {
                question.answerSlot.Locked = true;
            }
        }

        if (resultText != null)
        {
            resultText.color = correctColour;
            resultText.text = allCorrectMessage;
        }

        PlaySound(successClip);

        if (MessageUI.Instance != null)
        {
            MessageUI.Instance.Show(completionMessage, 4f);
        }

        if (!string.IsNullOrEmpty(checklistId))
        {
            ClueTrigger.DiscoverClue(checklistId);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }
}