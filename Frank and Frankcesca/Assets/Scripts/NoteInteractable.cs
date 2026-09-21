using UnityEngine;
//Akhona Khoali
//this script is for the note interactable, which is a pickup that can be read. 
//It inherits from PickupInteractable, but overrides the GetPrompt() method to show "Read" instead of "Pick up".

// Notes are pickups with a different hold style:

public class NoteInteractable : PickupInteractable
{
    // Reset() runs once when you add the component, and sets these defaults for you.
    private void Reset()
    {
        itemName = "Note";
        showInfoPanel = false;
        useCentreHoldPoint = true;
        spinWhileHeld = false;
    }

    public override string GetPrompt()
    {
        return "Read " + itemName;
    }
}