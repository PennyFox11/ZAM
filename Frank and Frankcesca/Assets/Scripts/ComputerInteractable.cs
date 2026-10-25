using System;
using UnityEngine;
//Akhona Khoali

// One item inside a category. Fill in whichever parts you need:
//   Case file / report / newspaper article -> title + image
//   Audio recording                        -> title + audio
//   Employee information                   -> title + text
[Serializable]
public class DatabaseEntry
{
    public string title;
    public Sprite image;
    public AudioClip audio;
    [TextArea(3, 8)]
    public string text;

    [Tooltip("Optional. Matches a Clue Id in the EvidenceNotebook - reported the moment the player opens this entry.")]
    public string clueId = "";
}

// One button on the computer's home screen, e.g. "Case Files" or "Audio Files".
[Serializable]
public class DatabaseCategory
{
    public string categoryName;
    public bool isUnlocked = true;      // Untick for "Case Studies" (unlocked by the USB).
    public DatabaseEntry[] entries;
}

// Put this on each computer (with a Collider). Every computer has its own categories,
// so all three computers use this same script with different Inspector data.
public class ComputerInteractable : MonoBehaviour, IInteractable
{
    [Header("Computer Info")]
    public string computerName = "Computer";
    public DatabaseCategory[] categories;

    [Header("Boot Screen")]
    [TextArea(2, 6)]
    public string[] bootLines;          // The lines typed when this computer starts. One per element.
                                        // Leave empty to use the default lines from the Database UI.
                                        // {computer} is replaced with the computer name.

    [Header("Code Lock (optional - tick for the one computer that needs a code)")]
    public bool requiresCode = false;
    public string code = "1234";            // Digits only. The length sets how many digits the player types.
    public string lockedPrompt = "Enter password";
    public string unlockedChecklistId = "";     // Optional. Ticks this checklist task when the right code is typed.

    // Becomes true once the right code has been typed, so it only asks the first time.
    private bool codeAccepted;

    private bool IsCodeLocked
    {
        get { return requiresCode && !codeAccepted; }
    }

    public string GetPrompt()
    {
        return IsCodeLocked ? lockedPrompt : "Use " + computerName;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (ComputerDatabaseUI.Instance == null) return;

        if (IsCodeLocked)
        {
            if (SafeCodeUI.Instance == null)
            {
                Debug.LogWarning(name + ": there is no SafeCodeUI in the scene, so the code screen can't open.");
                return;
            }

            // Reuses the safe's keypad. The right code opens the computer straight away.
            SafeCodeUI.Instance.OpenForCode(code, interactor, () =>
            {
                codeAccepted = true;

                if (!string.IsNullOrEmpty(unlockedChecklistId))
                {
                    ClueTrigger.DiscoverClue(unlockedChecklistId);
                }

                ComputerDatabaseUI.Instance.Open(this, interactor);
            }, "Enter password");

            return;
        }

        ComputerDatabaseUI.Instance.Open(this, interactor);
    }

    // Unlocks a hidden category by name. Returns true only the first time.
    public bool UnlockCategory(string categoryName)
    {
        foreach (DatabaseCategory category in categories)
        {
            if (category.categoryName == categoryName && !category.isUnlocked)
            {
                category.isUnlocked = true;
                return true;
            }
        }

        return false;
    }
}