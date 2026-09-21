using UnityEngine;
//Akhona Khoali
// This belongs to the epmty game object (acts as a collider) Frank's computer with a Collider set to "Is Trigger"
// (a box on top of the monitor, or around the USB port).
// When the player DROPS the USB inside this box, the hidden "Case Studies" button unlocks.
// (This is the one place we use a trigger: the player drops the item, it triggers the collider so it doesnt use a raycast)
public class ComputerUsbSlot : MonoBehaviour
{
    public ComputerInteractable computer;
    public string requiredItemId = "USB";           // Must match the USB's Item Id.
    public string categoryToUnlock = "Case Studies";
    public Transform snapPoint;                     //the snap point will be ised once we figure out how to make a proper asset with that slot open

    private void OnTriggerEnter(Collider other)
    {
        // TEMPORARY DEBUG LINE: delete once the USB works.
        Debug.Log("USB zone touched by: " + other.name);

        PickupInteractable item = other.GetComponentInParent<PickupInteractable>();

        if (item == null || item.itemId != requiredItemId)
        {
            // TEMPORARY DEBUG LINE: delete once the USB works.
            string found = item == null ? "no pickup" : "Item Id '" + item.itemId + "'";
            Debug.Log("USB zone ignored it: " + found + ", but the zone needs '" + requiredItemId + "'");
            return;
        }

        if (snapPoint != null)
        {
            item.SnapTo(snapPoint);
        }

        bool newlyUnlocked = computer.UnlockCategory(categoryToUnlock);

        // TEMPORARY DEBUG LINE: delete once the USB works.
        Debug.Log("Unlock '" + categoryToUnlock + "' on " + computer.computerName + ": " + newlyUnlocked);

        if (newlyUnlocked && MessageUI.Instance != null)
        {
            MessageUI.Instance.Show("USB detected. New files are available on this computer.");
        }
    }
}