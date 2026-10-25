using UnityEngine;
//Akhona Khoali

// Notes are pickups with a different hold style:
//  - no side panel (the note itself is the information)
//  - snaps to the CENTRE of the camera instead of the right-hand side
//  - faces the camera, and never spins by itself (hold the left mouse button and drag to turn it)
// The player can walk around while holding it, and E drops it anywhere.
//
// While held, holding Space shows the clean, readable version of the writing (via NoteReadUI),
// in case the note's own art is illustrated/stylised and hard to actually read.
//
// Tip for heldEulerAngles (Inspector):
//   Unity Quad  -> (0, 0, 0)
//   Unity Plane -> (-90, 0, 0)

public class NoteInteractable : PickupInteractable
{
    [Header("Legible Text")]
    [TextArea(3, 8)]
    public string legibleText = "";   // Shown while the player holds Space. Leave empty if not needed.

    // Reset() runs once when you add the component, and sets these defaults for you.
    private void Reset()
    {
        itemName = "Note";
        showInfoPanel = false;
        useCentreHoldPoint = true;
    }

    public override string GetPrompt()
    {
        return "Read " + itemName;
    }

    public override void Interact(PlayerInteractor interactor)
    {
        base.Interact(interactor);

        if (!string.IsNullOrEmpty(legibleText) && NoteReadUI.Instance != null)
        {
            NoteReadUI.Instance.SetCurrentNote(this);
        }
    }

    public override void Drop()
    {
        base.Drop();

        if (NoteReadUI.Instance != null)
        {
            NoteReadUI.Instance.ClearCurrentNote(this);
        }
    }
}