using UnityEngine;
//Akhona Khoali

// The safe is just a locked door that asks for a code instead of showing a message.
// belongs under safe's DOOR hinge object (the same setup as any OpenableInteractable),
// tick "Is Locked", and type the code 
// 'OpenableInteractable' is its parent class, so it still opens and closes like a door
// once the code has been entered.
public class SafeInteractable : OpenableInteractable
{
    [Header("Safe Settings")]
    public string code = "1234";        // Digits only. The length also sets how many digits the player types.

    public override string GetPrompt()
    {
        if (isLocked)
        {
            return "Enter code";
        }

        // Unlocked: behave like a normal door ("Open safe" / "Close safe").
        return base.GetPrompt();
    }

    public override void Interact(PlayerInteractor interactor)
    {
        if (isLocked)
        {
            if (SafeCodeUI.Instance != null)
            {
                SafeCodeUI.Instance.Open(this, interactor);
            }
            return;
        }

        // Unlocked: behave like a normal door.
        base.Interact(interactor);
    }
}
