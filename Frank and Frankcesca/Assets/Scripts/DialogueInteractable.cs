using UnityEngine;
//Akhona Khoali

// Belongs to anything the player can talk to NPC,Radio,Phone
// Like every other interactable (PickupInteractable, OpenableInteractable, ComputerInteractable),
// it implements IInteractable, so PlayerInteractor treats it exactly the same way:
// it shows GetPrompt() and calls Interact() when E is pressed.
// The specifications Minnah gave me is that there are the two speakers and the conversation 
public class DialogueInteractable : MonoBehaviour, IInteractable
{
    [Header("The Two Speakers")]
    public string npcName = "Stranger";
    public Sprite npcPortrait;              // Shown on the RIGHT.
    public string playerName = "Detective";
    public Sprite playerPortrait;           // Shown on the LEFT.

    [Header("Conversation (alternate Player / NPC)")]
    public DialogueLine[] lines;

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