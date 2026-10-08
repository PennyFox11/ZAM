using TMPro;
using UnityEngine;
using UnityEngine.UI;
//Akhona Khoali


public class NotebookClueRow : MonoBehaviour
{
    [Header("Row Parts")]
    public Image clueImage;
    public TMP_Text titleText;
    public TMP_Text descriptionText;

    public bool IsDiscovered { get; private set; }

    // The clue has not been found: dark/hidden picture, "???" title, a locked hint instead of the description.
    public void SetLocked(Sprite image, Color imageColour, string title, string hint)
    {
        IsDiscovered = false;
        Apply(image, imageColour, title, hint);
    }

    // The clue was found: normal picture, its name, and its real description.
    public void SetDiscovered(Sprite image, Color imageColour, string title, string description)
    {
        IsDiscovered = true;
        Apply(image, imageColour, title, description);
    }

    private void Apply(Sprite image, Color imageColour, string title, string bodyText)
    {
        // A clear message instead of a crash if the prefab's slots were not filled in.
        if (clueImage == null || titleText == null || descriptionText == null)
        {
            Debug.LogError("NotebookClueRow: open the ClueRow prefab and drag ClueImage, TitleText and DescriptionText into the three slots.", this);
            return;
        }

        clueImage.sprite = image;
        clueImage.color = imageColour;
        clueImage.preserveAspect = true;

        titleText.text = title;
        descriptionText.text = bodyText;
    }
}