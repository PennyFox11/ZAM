using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
//Akhona Khoali

// Add to the inspector of the clue "chip" prefab shown in the report's tray.
// ReportExamUI makes one chip for every clue the player has found in the EvidenceNotebook.
// The player drags a chip onto an answer slot (ReportAnswerSlot). The chip itself stays in the
// tray - a copy follows the mouse - so the same clue can be used for more than one answer.
// The chip needs an Image (so the mouse can hit it) with Raycast Target ticked.
public class ReportClueChip : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Chip Parts")]
    public Image iconImage;         // Optional: the clue's picture.
    public TMP_Text titleText;      // The clue's name.

    public string ClueId { get; private set; }
    public string Title { get; private set; }
    public Sprite Icon { get; private set; }

    private RectTransform ghost;
    private RectTransform canvasRect;

    public void Setup(string clueId, string title, Sprite icon)
    {
        ClueId = clueId;
        Title = title;
        Icon = icon;

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
    }

    // Hovering a chip shows the clue's description from the notebook (if the report has a description box).
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ReportExamUI.Instance != null)
        {
            ReportExamUI.Instance.ShowClueInfo(ClueId);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ReportExamUI.Instance != null)
        {
            ReportExamUI.Instance.HideClueInfo();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (ReportExamUI.Instance != null)
        {
            ReportExamUI.Instance.HideClueInfo();
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Canvas rootCanvas = canvas.rootCanvas;
        canvasRect = rootCanvas.transform as RectTransform;

        // A copy of this chip follows the mouse, drawn on top of everything.
        GameObject copy = Instantiate(gameObject, rootCanvas.transform);
        Destroy(copy.GetComponent<ReportClueChip>());

        // The copy must not block the mouse, or the slot under it never receives the drop.
        CanvasGroup group = copy.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = copy.AddComponent<CanvasGroup>();
        }
        group.blocksRaycasts = false;
        group.alpha = 0.85f;

        ghost = copy.GetComponent<RectTransform>();
        Vector2 size = ((RectTransform)transform).rect.size;
        ghost.anchorMin = new Vector2(0.5f, 0.5f);
        ghost.anchorMax = new Vector2(0.5f, 0.5f);
        ghost.pivot = new Vector2(0.5f, 0.5f);
        ghost.sizeDelta = size;
        ghost.SetAsLastSibling();

        MoveGhost(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        MoveGhost(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghost != null)
        {
            Destroy(ghost.gameObject);
            ghost = null;
        }
    }

    private void MoveGhost(PointerEventData eventData)
    {
        if (ghost == null || canvasRect == null) return;

        Vector3 world;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out world))
        {
            ghost.position = world;
        }
    }

    private void OnDisable()
    {
        if (ghost != null)
        {
            Destroy(ghost.gameObject);
            ghost = null;
        }
    }
}