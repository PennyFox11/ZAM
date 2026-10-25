using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
//Akhona Khoali

// add it on each blank on the report form (one per question). The player drags a clue chip
// onto it; clicking a filled slot empties it again.
// The slot needs an Image (its background) with Raycast Target ticked, so it can receive the drop.
//   Inside it: an optional TMP text for the clue's name and an optional Image for its picture.
// ReportExamUI colours the background green (right) or red (wrong) when the player submits.
// Changing the answer afterwards clears that colour until the next submit.
public class ReportAnswerSlot : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Parts")]
    public Image background;
    public TMP_Text titleText;          // Shows the dropped clue's name.
    public Image iconImage;             // Optional: the dropped clue's picture.

    [Header("Look")]
    public string emptyText = "Drag a clue here";
    public Color emptyColour = new Color(1f, 1f, 1f, 0.5f);
    public Color filledColour = new Color(1f, 1f, 1f, 1f);

    public string ClueId { get; private set; }
    public bool IsEmpty { get { return string.IsNullOrEmpty(ClueId); } }

    // ReportExamUI locks the slots once the report is complete.
    public bool Locked { get; set; }

    private void Awake()
    {
        Clear();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (Locked || eventData.pointerDrag == null) return;

        ReportClueChip chip = eventData.pointerDrag.GetComponent<ReportClueChip>();
        if (chip == null) return;

        Fill(chip.ClueId, chip.Title, chip.Icon);
    }

    // Hovering a filled slot shows that clue's description from the notebook.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsEmpty || ReportExamUI.Instance == null) return;

        ReportExamUI.Instance.ShowClueInfo(ClueId);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ReportExamUI.Instance != null)
        {
            ReportExamUI.Instance.HideClueInfo();
        }
    }

    // Click a filled slot to take the clue back out.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (Locked || IsEmpty) return;

        Clear();
    }

    public void Fill(string clueId, string title, Sprite icon)
    {
        ClueId = clueId;

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(icon != null);
            iconImage.preserveAspect = true;
        }

        if (background != null)
        {
            background.color = filledColour;
        }
    }

    public void Clear()
    {
        ClueId = null;

        if (titleText != null)
        {
            titleText.text = emptyText;
        }

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
        }

        if (background != null)
        {
            background.color = emptyColour;
        }
    }

    // Right / wrong colour after a submit.
    public void SetResultColour(Color colour)
    {
        if (background != null)
        {
            background.color = colour;
        }
    }
}