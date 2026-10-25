using UnityEngine;
//Akhona Khoali
// This script
// beongs on anything the player can talk to (an NPC, a radio, a phone...) with a Collider.
// Like every other interactable (PickupInteractable, OpenableInteractable, ComputerInteractable),
// it implements IInteractable, so PlayerInteractor treats it exactly the same way:
// it shows GetPrompt() and calls Interact() when E is pressed.
// Its own specifications are the two speakers and the conversation lines below.
public class DialogueInteractable : MonoBehaviour, IInteractable
{
    [Header("The Two Speakers")]
    public string npcName = "Stranger";
    public Sprite npcPortrait;              // Shown on the RIGHT.
    public string playerName = "Detective";
    public Sprite playerPortrait;           // Shown on the LEFT.

    [Header("Conversation (alternate Player / NPC)")]
    public DialogueLine[] lines;

    [Header("Ending Choice (optional - tick for the one character who ends the game)")]
    public bool endsWithChoice = false;     // After the last line, the player must pick A or B, then the game ends.
    [TextArea(1, 3)]
    public string choicePrompt = "What do you do?";
    public DialogueChoice optionA;
    public DialogueChoice optionB;

    // IInteractable 

    public string GetPrompt()
    {
        return "Talk to " + npcName;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (lines == null || lines.Length == 0) return;

        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.StartDialogue(this, interactor);
        }
    }
}