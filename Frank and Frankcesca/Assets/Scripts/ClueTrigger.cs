using UnityEngine;
//Akhona Khoali

// To be put on any object that is a clue. 
// the Clue Id must match a clue in the EvidenceNotebook's list. If the clue is also on the
// InvestigationChecklist (the ones that unlock the door), use the same id there too.
//
// With Auto Connect ticked, it finds the moment the clue is found on its own:
//   a pickup or a note     -> when the player picks it up
//   a RevealScannable      -> when the scanner beam first finds it

public class ClueTrigger : MonoBehaviour
{
    public string clueId = "";          // Must match a Clue Id in the notebook Even scacing is important.
    public bool autoConnect = true;

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

    // Tells the notebook and the checklist that this clue has been found.
    public void Discover()
    {
        bool reported = false;

        if (EvidenceNotebook.Instance != null)
        {
            EvidenceNotebook.Instance.DiscoverClue(clueId);
            reported = true;
        }

        if (InvestigationChecklist.Instance != null)
        {
            InvestigationChecklist.Instance.CompleteItem(clueId);
            reported = true;
        }

        if (!reported)
        {
            Debug.LogWarning(name + ": there is no EvidenceNotebook or InvestigationChecklist in the scene.");
        }
    }
}