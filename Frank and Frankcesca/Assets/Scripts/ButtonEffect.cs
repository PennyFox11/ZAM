using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//Akhona Khoali

//Belongs to buttons under the inspector
// Grows slightly while the mouse is over it, and swaps to a different colour while actually

[RequireComponent(typeof(RectTransform))]
public class MenuButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Hover Scale")]
    public float hoverScale = 1.08f;      // How big it gets under the mouse. 1 = no change.
    public float scaleSpeed = 10f;        // Higher = snaps to size faster.

    [Header("Click Colour")]
    [Tooltip("The button's background Image. Left empty, it finds the first Graphic on this object.")]
    public Graphic targetGraphic;
    public Color pressedColour = new Color(0.8f, 0.8f, 0.4f, 1f);

    private RectTransform rect;
    private Vector3 baseScale;
    private float targetScale = 1f;
    private Color normalColour;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        baseScale = rect.localScale;

        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Graphic>();
        }

        // Remembers whatever colour the button already had
        if (targetGraphic != null)
        {
            normalColour = targetGraphic.color;
        }
    }

    //the hover grow still works even if the game is paused behind the menu.
    private void Update()
    {
        rect.localScale = Vector3.Lerp(rect.localScale, baseScale * targetScale, scaleSpeed * Time.unscaledDeltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = hoverScale;

        // Plays the hover sound if there is a MenuAudio in the scene.
        if (MenuAudio.Instance != null)
        {
            MenuAudio.Instance.PlayHover();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = 1f;

        if (targetGraphic != null)
        {
            targetGraphic.color = normalColour;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Plays the click sound if there is a MenuAudio in the scene 
        if (MenuAudio.Instance != null)
        {
            MenuAudio.Instance.PlayClick();
        }

        if (targetGraphic != null)
        {
            targetGraphic.color = pressedColour;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (targetGraphic != null)
        {
            targetGraphic.color = normalColour;
        }
    }
}