using UnityEngine;
using UnityEngine.InputSystem;
//Akhona Khoali
// Under the Player.
public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast Settings")]
    public Transform cameraTransform;
    public float interactRange = 3f;
    public LayerMask interactMask = ~0;     // ~0 means "everything". Later, exclude the Player layer here.

    [Header("Hold Settings")]
    public Transform holdPoint;             // Child of the camera, offset to the RIGHT (rotating pickups).
    public Transform noteHoldPoint;         // Child of the camera, in the CENTRE (notes).

    [Header("UI")]
    public InteractionPromptUI promptUI;    // Shows "[E] Pick up Toothbrush".
    public PickupInfoPanel infoPanel;       // Shows the item name and description on the left.

    [Header("Player")]
    public FPController playerController;   // Frozen while a screen (dialogue, computer, safe) is open.

    // True while a dialogue / safe / computer screen is open.
    public bool InputLocked => activeScreen != null;

    // True while the player is carrying a pickup or a note.
    public bool IsHolding => heldItem != null;

    private IInteractable currentTarget;        // What the ray is hitting right now (or null).
    private PickupInteractable heldItem;        // What the player is carrying (or null).
    private IInteractionScreen activeScreen;    // The screen currently open (or null).

    private void Awake()
    {
        // works with FPController on the same GameObject, but can also be assigned manually in the Inspector.
        if (playerController == null)
        {
            playerController = GetComponent<FPController>();
        }
    }

    private void Update()
    {
        // A screen is open, so hide the world prompt and do nothing else.
        if (InputLocked)
        {
            HidePrompt();
            return;
        }

        // Always look at what the ray is hitting, even while carrying something.
        FindTarget();

        // While carrying something, E drops it, except at things that can be used
        // while holding (the teleport doors), where E uses them.
        if (heldItem != null)
        {
            if (TargetWorksWhileHolding())
            {
                ShowPrompt(currentTarget.GetPrompt());
            }
            else
            {
                ShowPrompt("Drop");
            }

            return;
        }

        if (currentTarget != null)
        {
            ShowPrompt(currentTarget.GetPrompt());
        }
        else
        {
            HidePrompt();
        }
    }

    // Fires a ray from the centre of the camera and checks what it hits.
    private void FindTarget()
    {
        currentTarget = null;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // QueryTriggerInteraction.Ignore makes the ray pass through trigger colliders.
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactMask, QueryTriggerInteraction.Ignore))
        {
            // GetComponentInParent means the collider can be on a child object
            // (for example, a door model) while the script sits on the parent (the hinge).
            currentTarget = hit.collider.GetComponentInParent<IInteractable>();
        }
    }

    
    // Works the same way as FPController's OnMove / OnLook.
    public void OnInteract(InputAction.CallbackContext context)
    {
        // Only react once, at the moment the key is pressed.
        if (!context.performed) return;

        // A screen is open, so E belongs to that screen.
        if (activeScreen != null)
        {
            activeScreen.OnInteractPressed();
            return;
        }

        // If the player is carrying something, E drops it...
        if (heldItem != null)
        {
            // ...unless they are looking at something that works while holding (the doors).
            if (TargetWorksWhileHolding())
            {
                currentTarget.Interact(this);
                return;
            }

            DropHeldItem();
            return;
        }

        // Otherwise, interact with whatever the ray found.
        if (currentTarget != null)
        {
            currentTarget.Interact(this);
        }
    }

    // True if the target can be used while the player is carrying an item (see IUsableWhileHolding).
    private bool TargetWorksWhileHolding()
    {
        IUsableWhileHolding usable = currentTarget as IUsableWhileHolding;
        return usable != null && usable.UsableWhileHolding;
    }

    // Screens (dialogue, safe keypad, computer

    // Freezes the player and hands E over to the screen.
    // showCursor = true for screens with clickable buttons (the computer).
    public void LockPlayer(IInteractionScreen screen, bool showCursor)
    {
        activeScreen = screen;

        // Disabling FPController stops its Update, so movement AND looking are frozen.
        // (Unity Events still call OnMove/OnLook, so the input stays up to date.)
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        if (showCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        HidePrompt();
    }

    // Gives control back to the player.
    public void UnlockPlayer()
    {
        activeScreen = null;

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Holding items 

    // Called by PickupInteractable when the player presses E on it.
    public void Hold(PickupInteractable item)
    {
        heldItem = item;

        // Notes use the centre hold point, everything else uses the right-hand one.
        Transform point = holdPoint;
        if (item.useCentreHoldPoint && noteHoldPoint != null)
        {
            point = noteHoldPoint;
        }

        item.PickUp(point);

        if (infoPanel != null && item.showInfoPanel)
        {
            infoPanel.Show(item.itemName, item.description);
        }
    }

    private void DropHeldItem()
    {
        heldItem.Drop();
        heldItem = null;

        if (infoPanel != null)
        {
            infoPanel.Hide();
        }
    }

    private void ShowPrompt(string message)
    {
        if (promptUI != null) promptUI.Show(message);
    }

    private void HidePrompt()
    {
        if (promptUI != null) promptUI.Hide();
    }
}