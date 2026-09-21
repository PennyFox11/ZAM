using System;
using UnityEngine;
//Akhona Khoali
//This script is attached to each computer in the game. It contains the data for the computer's database, including categories and entries. 
//The ComputerDatabaseUI script uses this data to display the database UI when the player interacts with the computer.

// One item inside a category and is customisable 
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
}

// One button on the computer's home screen, e.g. "Case Files" or "Audio Files".
[Serializable]
public class DatabaseCategory
{
    public string categoryName;
    public bool isUnlocked = true;      // Untick for "Case Studies" (unlocked by the USB).
    public DatabaseEntry[] entries;
}

// Put this on each computer 
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

    public string GetPrompt()
    {
        return "Use " + computerName;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (ComputerDatabaseUI.Instance != null)
        {
            ComputerDatabaseUI.Instance.Open(this, interactor);
        }
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