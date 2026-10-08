using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// One clue in the notebook. Fill these in on the EvidenceNotebook in the Inspector.
[Serializable]
public class ClueEntry
{
    public string clueId;       // A short name other scripts use, e.g. "usb_stick". No spaces!
    public string title;        // Shown once the clue is found, e.g. "USB stick".
    [TextArea(2, 5)]
    public string description;  // Shown once the clue is found, in the scrollable list on the left.
    public Sprite image;        // The picture. It is shown as a dark shape until the clue is found.
}

// Put this on your UI manager object (an active object, NOT the panel).
// Tab opens and closes the notebook. The notebook has two halves:
//   LEFT  -> a scrollable, read-only list: every clue in the game gets a row with its picture,
//            title and description. Locked clues show as "???" until the player finds them.
//   RIGHT -> ONE single sheet where the player writes their own thoughts, free-form. It is not
//            tied to any one clue, so there is only one notes field for the whole notebook.
// The moment the player picks up (or scans) a tracked clue, its row updates immediately and
// the notification flickers, whether or not the notebook is open.
//
// Tab is not a letter, so it can close the notebook even while the player is typing on the sheet.
public class EvidenceNotebook : MonoBehaviour, IInteractionScreen
{
    public static EvidenceNotebook Instance { get; private set; }

    // Other scripts can listen to this to know when a NEW clue is found (the checklist will).
    public event Action<string> ClueDiscovered;

    [Header("Clues (one per clue in the game)")]
    public ClueEntry[] clues;

    [Header("Notebook UI")]
    public GameObject notebookPanel;        // The whole notebook screen.
    public Transform rowContainer;          // The left list's Scroll View "Content" object.
    public NotebookClueRow rowPrefab;       // One row: picture, title, description.
    public Button closeButton;              // Optional Close button.

    [Header("The Player's Own Sheet (right side)")]
    public TMP_InputField notesInput;       // ONE shared, free-form notes field. Not per-clue.

    [Header("Locked / Found Look")]
    public Color lockedImageColour = new Color(0.05f, 0.05f, 0.05f, 1f);   // Dark shape.
    public Color foundImageColour = Color.white;
    public string lockedDescription = "Not found yet...";

    [Header("New Clue Notification")]
    public GameObject notificationObject;   // e.g. "(Tab) Notebook" in a corner. Put it OUTSIDE the notebook panel.
    public float flickerOnTime = 0.5f;      // Seconds it stays bright.
    public float flickerOffTime = 0.1f;     // Seconds it dips.
    [Range(0f, 1f)]
    public float flickerDimAlpha = 0.2f;    // How faint it gets when it dips.

    [Header("Sounds (all optional)")]
    public AudioClip openClip;
    public AudioClip closeClip;
    public AudioClip newClueClip;

    private PlayerInteractor interactor;
    private AudioSource sfxAudioSource;
    private CanvasGroup notificationGroup;
    private Coroutine flickerRoutine;
    private bool isOpen;

    // Clue Ids are matched ignoring capital letters and spaces at the ends.
    private Dictionary<string, NotebookClueRow> rows =
        new Dictionary<string, NotebookClueRow>(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        Instance = this;

        notebookPanel.SetActive(false);

        if (notificationObject != null)
        {
            notificationObject.SetActive(false);

            // The flicker fades a CanvasGroup, so add one if the object doesn't have it.
            notificationGroup = notificationObject.GetComponent<CanvasGroup>();
            if (notificationGroup == null)
            {
                notificationGroup = notificationObject.AddComponent<CanvasGroup>();
            }
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        if (notesInput != null)
        {
            // Enter starts a new line, so the player can write more than one line on the sheet.
            notesInput.lineType = TMP_InputField.LineType.MultiLineNewline;
        }
        else
        {
            Debug.LogWarning("EvidenceNotebook: the Notes Input slot is empty, so the player has nowhere to write.");
        }

        interactor = FindFirstObjectByType<PlayerInteractor>();

        sfxAudioSource = gameObject.AddComponent<AudioSource>();
        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.pitch = 1f;

        BuildRows();
    }

    // Makes one row for every clue in the left-side list. They all start locked.
    private void BuildRows()
    {
        // A clear message instead of a confusing crash if a slot was left empty.
        if (rowContainer == null || rowPrefab == null)
        {
            Debug.LogError("EvidenceNotebook: drag the Scroll View's Content object into Row Container, and the ClueRow prefab into Row Prefab.", this);
            return;
        }

        // Remove any leftover rows you may have placed by hand in the Editor.
        foreach (Transform child in rowContainer)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        foreach (ClueEntry clue in clues)
        {
            string id = CleanId(clue.clueId);

            if (id.Length == 0)
            {
                Debug.LogWarning("EvidenceNotebook: a clue has an empty Clue Id, so it was skipped.");
                continue;
            }

            if (rows.ContainsKey(id))
            {
                Debug.LogWarning("EvidenceNotebook: the Clue Id '" + id + "' is used twice.");
                continue;
            }

            NotebookClueRow row = Instantiate(rowPrefab, rowContainer);
            row.SetLocked(clue.image, lockedImageColour, clue.title, lockedDescription);
            rows.Add(id, row);
        }
    }

    // Removes spaces at the start and end, which are easy to type by accident.
    private string CleanId(string id)
    {
        return id == null ? "" : id.Trim();
    }

    // ----- Input -----

    // Hook this up in the Player Input component: Notebook action -> EvidenceNotebook.OnNotebook.
    public void OnNotebook(InputAction.CallbackContext context)
    {
        // Only react once, at the moment the key is pressed.
        if (!context.performed) return;

        if (isOpen)
        {
            Close();
        }
        else
        {
            // Don't open over a dialogue, safe or computer screen.
            if (interactor != null && interactor.InputLocked) return;

            Open();
        }
    }

    // E is a letter the player can type too, so E does nothing while the notebook is open.
    public void OnInteractPressed()
    {
    }

    // ----- Opening and closing -----

    private void Open()
    {
        isOpen = true;
        notebookPanel.SetActive(true);

        // Freeze the player and show the mouse cursor so the player can click, scroll and type.
        if (interactor != null)
        {
            interactor.LockPlayer(this, true);
        }

        PlaySound(openClip);
    }

    private void Close()
    {
        if (!isOpen) return;

        isOpen = false;

        StopTyping();

        notebookPanel.SetActive(false);

        // The notification disappears once the player has looked at the notebook.
        HideNotification();

        if (interactor != null)
        {
            interactor.UnlockPlayer();
        }

        PlaySound(closeClip);
    }

    // Makes the sheet let go of the keyboard.
    private void StopTyping()
    {
        if (EventSystem.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected != null && notesInput != null)
        {
            TMP_InputField field = selected.GetComponentInParent<TMP_InputField>();

            if (field != null)
            {
                // Pressing Tab can leave a tab space at the end of the writing, so remove it.
                field.text = field.text.TrimEnd('\t');
                field.DeactivateInputField();
            }
        }

        EventSystem.current.SetSelectedGameObject(null);
    }

    // ----- Clues -----

    // Called by ClueTrigger when the player finds a clue. Updates its row immediately,
    // whether or not the notebook is currently open.
    public void DiscoverClue(string clueId)
    {
        clueId = CleanId(clueId);

        NotebookClueRow row;

        if (!rows.TryGetValue(clueId, out row))
        {
            string known = rows.Count > 0 ? string.Join(", ", rows.Keys) : "none (the Clues list is empty or the rows failed to build)";
            Debug.LogWarning("EvidenceNotebook: there is no clue with the id '" + clueId + "'. Ids in the notebook: " + known);
            return;
        }

        // Already found before.
        if (row.IsDiscovered) return;

        ClueEntry clue = FindClue(clueId);
        row.SetDiscovered(clue.image, foundImageColour, clue.title, clue.description);

        ShowNotification();
        PlaySound(newClueClip);

        if (ClueDiscovered != null)
        {
            ClueDiscovered(clueId);
        }
    }

    public bool IsClueDiscovered(string clueId)
    {
        NotebookClueRow row;
        return rows.TryGetValue(CleanId(clueId), out row) && row.IsDiscovered;
    }

    // What the player has written on their one shared sheet.
    public string GetNotes()
    {
        return notesInput != null ? notesInput.text : "";
    }

    private ClueEntry FindClue(string clueId)
    {
        foreach (ClueEntry clue in clues)
        {
            if (CleanId(clue.clueId).Equals(clueId, StringComparison.OrdinalIgnoreCase))
            {
                return clue;
            }
        }

        return null;
    }

    // ----- Notification -----

    private void ShowNotification()
    {
        if (notificationObject == null) return;

        notificationObject.SetActive(true);

        if (flickerRoutine != null)
        {
            StopCoroutine(flickerRoutine);
        }
        flickerRoutine = StartCoroutine(Flicker());
    }

    private void HideNotification()
    {
        if (flickerRoutine != null)
        {
            StopCoroutine(flickerRoutine);
            flickerRoutine = null;
        }

        if (notificationObject != null)
        {
            notificationObject.SetActive(false);
        }
    }

    // Bright for a moment, dims for a moment, over and over, until the player opens the notebook.
    private IEnumerator Flicker()
    {
        while (true)
        {
            notificationGroup.alpha = 1f;
            yield return new WaitForSeconds(flickerOnTime * UnityEngine.Random.Range(0.6f, 1.4f));

            notificationGroup.alpha = flickerDimAlpha;
            yield return new WaitForSeconds(flickerOffTime * UnityEngine.Random.Range(0.6f, 1.4f));
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