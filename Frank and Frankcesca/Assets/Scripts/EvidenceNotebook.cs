using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
//Akhona Khoali
// One clue in the notebook. Fill these in on the EvidenceNotebook in the Inspector.
[Serializable]
public class ClueEntry
{
    public string clueId;       // A short name other scripts use, e.g. "usb_stick". No spaces!
    public string title;        // Shown once the clue is found, e.g. "USB stick".
    public Sprite image;        // The picture. It is shown as a dark shape until the clue is found.
}

// Canvas
// Tab opens and closes the notebook. Every clue in the game has a row: the picture on the left
// (dark until found) and a box on the right where the player types their own notes.
// The rows stay alive while the notebook is closed, so the writing is saved.
//
// Tab is not a letter, so it can close the notebook even while the player is typing in a box.
public class EvidenceNotebook : MonoBehaviour, IInteractionScreen
{
    public static EvidenceNotebook Instance { get; private set; }

    // Other scripts can listen to this to know when a NEW clue is found (the checklist will).
    public event Action<string> ClueDiscovered;

    [Header("Clues (one per clue in the game)")]
    public ClueEntry[] clues;

    [Header("Notebook UI")]
    public GameObject notebookPanel;        // The whole notebook screen.
    public Transform rowContainer;          // The "Content" object of the Scroll View.
    public NotebookClueRow rowPrefab;       // One row: picture, title, notes box.
    public Button closeButton;              // Optional Close button.

    [Header("Locked / Found Look")]
    public Color lockedImageColour = new Color(0.05f, 0.05f, 0.05f, 1f);   // Dark shape.
    public Color foundImageColour = Color.white;
    public string lockedTitle = "???";
    public string lockedHint = "Not found yet...";
    public string foundHint = "Write your thoughts here...";

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

    private Dictionary<string, NotebookClueRow> rows = new Dictionary<string, NotebookClueRow>();

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

        interactor = FindFirstObjectByType<PlayerInteractor>();

        sfxAudioSource = gameObject.AddComponent<AudioSource>();
        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.pitch = 1f;

        BuildRows();
    }

    // Makes one row for every clue. They all start locked.
    private void BuildRows()
    {
        // Remove any leftover rows you may have placed by hand in the Editor.
        foreach (Transform child in rowContainer)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        foreach (ClueEntry clue in clues)
        {
            if (string.IsNullOrEmpty(clue.clueId))
            {
                Debug.LogWarning("EvidenceNotebook: a clue has an empty Clue Id, so it was skipped.");
                continue;
            }

            if (rows.ContainsKey(clue.clueId))
            {
                Debug.LogWarning("EvidenceNotebook: the Clue Id '" + clue.clueId + "' is used twice.");
                continue;
            }

            NotebookClueRow row = Instantiate(rowPrefab, rowContainer);
            row.SetLocked(clue.image, lockedImageColour, lockedTitle, lockedHint);
            rows.Add(clue.clueId, row);
        }
    }

    // Input 

    
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

    // Opening and closing

    private void Open()
    {
        isOpen = true;
        notebookPanel.SetActive(true);

        // Freeze the player and show the mouse cursor so the player can click and type.
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

    // Makes the notes box let go of the keyboard.
    private void StopTyping()
    {
        if (EventSystem.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected != null)
        {
            TMP_InputField field = selected.GetComponentInParent<TMP_InputField>();

            if (field != null)
            {
                // Pressing Tab can leave a tab space at the end of the notes, so remove it.
                field.text = field.text.TrimEnd('\t');
                field.DeactivateInputField();
            }
        }

        EventSystem.current.SetSelectedGameObject(null);
    }

    //  Clues 

    // Called by ClueTrigger when the player finds a clue.
    public void DiscoverClue(string clueId)
    {
        NotebookClueRow row;

        if (!rows.TryGetValue(clueId, out row))
        {
            Debug.LogWarning("EvidenceNotebook: there is no clue with the id '" + clueId + "'. Check the spelling.");
            return;
        }

        // Already found before.
        if (row.IsDiscovered) return;

        ClueEntry clue = FindClue(clueId);
        row.SetDiscovered(clue.image, foundImageColour, clue.title, foundHint);

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
        return rows.TryGetValue(clueId, out row) && row.IsDiscovered;
    }

    // What the player has written for a clue (for later use).
    public string GetNotes(string clueId)
    {
        NotebookClueRow row;
        return rows.TryGetValue(clueId, out row) ? row.GetNotes() : "";
    }

    private ClueEntry FindClue(string clueId)
    {
        foreach (ClueEntry clue in clues)
        {
            if (clue.clueId == clueId)
            {
                return clue;
            }
        }

        return null;
    }

    // Notification

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