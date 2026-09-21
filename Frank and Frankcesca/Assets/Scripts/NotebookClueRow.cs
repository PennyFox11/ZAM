using TMPro;
using UnityEngine;
using UnityEngine.UI;
//Akhona Khoali
// This script is for the row in the notebook that shows a clue. It has three parts:


public class NotebookClueRow : MonoBehaviour
{
    [Header("Row Parts")]
    public Image clueImage;
    public TMP_Text titleText;
    public TMP_InputField notesInput;

    public bool IsDiscovered { get; private set; }

    // The clue has not been found: dark picture, no title, the player cannot type.
    public void SetLocked(Sprite image, Color imageColour, string title, string hint)
    {
        IsDiscovered = false;
        Apply(image, imageColour, title, hint, false);
    }

    // The clue was found: normal picture, its name, and the player can type notes.
    public void SetDiscovered(Sprite image, Color imageColour, string title, string hint)
    {
        IsDiscovered = true;
        Apply(image, imageColour, title, hint, true);
    }

    public string GetNotes()
    {
        return notesInput.text;
    }

    private void Apply(Sprite image, Color imageColour, string title, string hint, bool canType)
    {
        clueImage.sprite = image;
        clueImage.color = imageColour;
        clueImage.preserveAspect = true;

        titleText.text = title;

        // The box only accepts typing once the clue has been found.
        notesInput.interactable = canType;

        // Enter starts a new line, so the player can write more than one line.
        notesInput.lineType = TMP_InputField.LineType.MultiLineNewline;

        // The faint text shown while the box is empty.
        TMP_Text placeholderText = notesInput.placeholder as TMP_Text;
        if (placeholderText != null)
        {
            placeholderText.text = hint;
        }
    }
}