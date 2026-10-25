using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Belongs to the inspector of the clue row prefab, used in the interrogation room's bottom carousel.
// InterrogationRoomUI makes one row per clue on the suspect's list, then looks up its picture
// and title from the EvidenceNotebook by Clue Id:
//   not found yet >> dark picture, "???" title, can't be clicked (locked, same look as a notebook row)
//   found         >> normal picture and title, clickable
// While the carousel is scrolled, the row closest to the centre marker grows on its own.
// The outline shows what's going on with this clue:
//   clear  >> nothing has happened with this one yet
//   green  >> this is the clue currently being asked about
//   red    >> this clue has already been asked (and the answer heard)
public class InterrogationClueRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Row Parts")]
    public Image clueImage;
    public TMP_Text titleText;
    public Button selectButton;
    public Image outline;              // A frame/border Image around the row. Starts invisible.

    [Header("Carousel Grow")]
    public float baseScale = 1f;
    public float centreScale = 1.3f;
    public float scaleSpeed = 8f;
    public float growDistance = 220f;  // Pixel distance from the centre marker where it's back to base size.

    [Header("Outline Colours")]
    public Color noOutline = new Color(1f, 1f, 1f, 0f);
    public Color selectedOutline = new Color(0.3f, 1f, 0.3f, 1f);
    public Color answeredOutline = new Color(0.9f, 0.2f, 0.2f, 1f);

    [Header("Locked Look")]
    public Color lockedImageColour = new Color(0.05f, 0.05f, 0.05f, 1f);

    public InterrogationClue Clue { get; private set; }
    public bool IsAvailable { get; private set; }     // Found, so it can be picked.
    public bool HasBeenAsked { get; private set; }

    private RectTransform rect;
    private RectTransform centreMarker;
    private float targetScale = 1f;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();

        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnClicked);
        }

        if (outline != null)
        {
            outline.color = noOutline;
        }
    }

    // Grows the row as it nears the centre of the carousel. Unscaled, so it still grows
    // smoothly even though the player is frozen while this screen is open.
    private void Update()
    {
        if (centreMarker != null)
        {
            float distance = Mathf.Abs(rect.position.x - centreMarker.position.x);
            float proximity = 1f - Mathf.Clamp01(distance / growDistance);
            targetScale = Mathf.Lerp(baseScale, centreScale, proximity);
        }
        else
        {
            targetScale = baseScale;
        }

        rect.localScale = Vector3.Lerp(rect.localScale, Vector3.one * targetScale, scaleSpeed * Time.unscaledDeltaTime);
    }

    // Called once by InterrogationRoomUI when it builds the carousel.
    public void Setup(InterrogationClue clue, RectTransform carouselCentre)
    {
        Clue = clue;
        centreMarker = carouselCentre;
        HasBeenAsked = false;

        if (outline != null)
        {
            outline.color = noOutline;
        }
    }

    // Not found yet: dark picture, no title shown, can't be clicked.
    public void SetLocked()
    {
        IsAvailable = false;

        if (clueImage != null)
        {
            clueImage.color = lockedImageColour;
        }

        if (titleText != null)
        {
            titleText.text = "???";
        }

        if (selectButton != null)
        {
            selectButton.interactable = false;
        }
    }

    // Found: real picture and title, can be clicked.
    public void SetAvailable(Sprite image, string title)
    {
        IsAvailable = true;

        if (clueImage != null)
        {
            clueImage.sprite = image;
            clueImage.color = Color.white;
            clueImage.preserveAspect = true;
        }

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (selectButton != null)
        {
            selectButton.interactable = true;
        }
    }

    // Green while this clue is the one currently being asked about. Turning it off falls back
    // to the red "already asked" outline if MarkAsked() has been called, otherwise no outline.
    public void SetSelected(bool selected)
    {
        if (outline == null) return;

        if (selected)
        {
            outline.color = selectedOutline;
        }
        else
        {
            outline.color = HasBeenAsked ? answeredOutline : noOutline;
        }
    }

    public void MarkAsked()
    {
        HasBeenAsked = true;
    }

    // Hovering a found clue shows the question it would ask, so the player knows what they're picking.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsAvailable && InterrogationRoomUI.Instance != null)
        {
            InterrogationRoomUI.Instance.ShowQuestionPreview(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (InterrogationRoomUI.Instance != null)
        {
            InterrogationRoomUI.Instance.HideQuestionPreview();
        }
    }

    private void OnClicked()
    {
        if (!IsAvailable) return;

        if (InterrogationRoomUI.Instance != null)
        {
            InterrogationRoomUI.Instance.SelectClue(this);
        }
    }
}