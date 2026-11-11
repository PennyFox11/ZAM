using UnityEngine;
//Akhona khoali

// Belongs to the/every clue object. Objects without it (like the toothbrush) are
// not clues.the Clue Id to match a clue in the EvidenceNotebook's list. If the clue is also
// on the InvestigationChecklist (the ones that unlock the door), the same id there too.
//
// With Auto Connect ticked, it finds the moment the clue is found on its own:
//   a pickup or a note     -> when the player picks it up
//   a RevealScannable      -> when the scanner beam first finds it
// Tick "Discover On Enable" instead for an object that starts OFF in the scene and gets switched
// ON by something else (a cutscene, a switch, a trigger volume) the moment it's found e.g.
// SetActive(true) on a hidden piece of evidence. Leave the object disabled in the Inspector/at
// the start, or it will count as "found" the instant the scene loads.
// For a one-off case with its own logic (the safe), call Discover() from a UnityEvent instead.
//
// A dialogue line and a computer database entry aren't their own GameObject, so those report
// straight to the notebook without this script: put the clue id in the line's or entry's own
// "Clue Id" field (DialogueLine / DatabaseEntry) and DialogueUI / ComputerDatabaseUI report it
// the moment the line is shown / the entry is opened.
public class ClueTrigger : MonoBehaviour
{
    public string clueId = "";              // Must match a Clue Id in the notebook, e.g. "usb_stick".
    public bool autoConnect = true;
    public bool discoverOnEnable = false;   // For a hidden object that gets switched on when found.

    private void Awake()
    {
        if (!autoConnect) return;

        PickupInteractable pickup = GetComponent<PickupInteractable>();
        if (pickup != null)
        {
            pickup.onPickedUp.AddListener(Discover);
        }

        RevealScannable scannable = GetComponent<RevealScannable>();
        if (scannable != null)
        {
            scannable.onFirstScanned.AddListener(Discover);
        }
    }

    // Fires every time this object becomes active - harmless to call more than once,
    // DiscoverClue() below quietly ignores a clue that was already found.
    private void OnEnable()
    {
        if (discoverOnEnable)
        {
            Discover();
        }
    }

    // Tells the notebook and the checklist that this clue has been found.
    public void Discover()
    {
        DiscoverClue(clueId);
    }

    // Static so anything can report a clue by id directly, not just a GameObject with this
    // script on it - DialogueUI and ComputerDatabaseUI call this for clues tied to one specific
    // line or database entry.
    public static void DiscoverClue(string clueId)
    {
        bool reported = false;

        if (EvidenceNotebook.Instance != null)
        {
            // Some ids are checklist tasks only (e.g. "report_submitted") and aren't notebook
            // clues. Skip those quietly instead of warning about an unknown notebook clue.
            if (EvidenceNotebook.Instance.GetClue(clueId) != null)
            {
                EvidenceNotebook.Instance.DiscoverClue(clueId);
            }

            reported = true;
        }

        if (InvestigationChecklist.Instance != null)
        {
            InvestigationChecklist.Instance.CompleteItem(clueId);
            reported = true;
        }

        if (!reported)
        {
            Debug.LogWarning("ClueTrigger: there is no EvidenceNotebook or InvestigationChecklist in the scene to report '" + clueId + "' to.");
        }
    }
}