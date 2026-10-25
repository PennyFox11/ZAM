using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif
//Akhona Khoali
//Dymamic crosshair that expands when looking at something interactable.
//Can be a T shape or a dot, and can be rotated. The crosshair is hidden while an interaction is in progress (screen open or item held).

public class CrosshairController : MonoBehaviour
{
    [Header("Size & Position & Rotation")]
    public float CrosshairGap = 15f;
    public float CrosshairLength = 15f;
    public float CrosshairThickness = 5f;
    public float CrosshairRotation = 0f;

    [Header("Styles")]
    public bool CrosshairTStyle = false;
    public bool CrosshairDot = true;

    [Header("Color")]
    public Color CrosshairColor = Color.green;
    public float CrosshairAlpha = 1f;

    [Header("Interact Expand")]
    public PlayerInteractor interactor;    // Drag your Player here. If left empty it is found automatically.
    public float ExpandAmount = 10f;       // Extra gap added while looking at something interactable.
    public float ExpandSpeed = 12f;        // Higher = snappier expand/shrink.

    [Header("Rects")]
    [SerializeField] private Transform _crosshair;
    [SerializeField] private RectTransform _crosshairUp;
    [SerializeField] private RectTransform _crosshairDown;
    [SerializeField] private RectTransform _crosshairLeft;
    [SerializeField] private RectTransform _crosshairRight;
    [SerializeField] private RectTransform _crosshairCenter;

    [Header("Other")]
    public bool AutoRefresh = true;

    private Image _crosshairUpImage;
    private Image _crosshairDownImage;
    private Image _crosshairLeftImage;
    private Image _crosshairRightImage;
    private Image _crosshairCenterImage;

    private float _expand;                 // Current extra gap, smoothly moves between 0 and ExpandAmount.
    private bool _visible = true;          // False while an interaction is in progress.

    private void Awake()
    {
        _crosshairUpImage = _crosshairUp.GetComponent<Image>();
        _crosshairDownImage = _crosshairDown.GetComponent<Image>();
        _crosshairLeftImage = _crosshairLeft.GetComponent<Image>();
        _crosshairRightImage = _crosshairRight.GetComponent<Image>();
        _crosshairCenterImage = _crosshairCenter.GetComponent<Image>();

        if (interactor == null)
        {
            interactor = FindFirstObjectByType<PlayerInteractor>();
        }

        RefreshVisual();
    }

    private void Update()
    {
        // The crosshair is hidden for the whole interaction: while a screen is open
        // (dialogue, computer, safe) or while an item is held. It comes back once the
        // player presses E to close the screen or drop the item.
        bool visible = interactor == null || !(interactor.InputLocked || interactor.IsHolding);
        bool visibilityChanged = visible != _visible;
        _visible = visible;

        HandleInteractExpand();

        if (AutoRefresh || visibilityChanged) RefreshVisual();
    }

    // Checks whether the player is looking at something interactable and smoothly expands/shrinks the crosshair.
    private void HandleInteractExpand()
    {
        float target = (_visible && IsLookingAtInteractable()) ? ExpandAmount : 0f;
        _expand = Mathf.Lerp(_expand, target, ExpandSpeed * Time.deltaTime);

        // Refreshes the visual when AutoRefresh is off, so the expand still shows.
        if (!AutoRefresh) RefreshVisual();
    }

    // Uses the same ray settings as PlayerInteractor, so the crosshair expands exactly when E would work.
    private bool IsLookingAtInteractable()
    {
        if (interactor == null) return false;

        Ray ray = new Ray(interactor.cameraTransform.position, interactor.cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactor.interactRange, interactor.interactMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<IInteractable>() != null;
        }

        return false;
    }

    public void RefreshVisual()
    {
        _crosshairUp.gameObject.SetActive(!CrosshairTStyle);
        _crosshairCenter.gameObject.SetActive(CrosshairDot);

        Color color = new Color(CrosshairColor.r, CrosshairColor.g, CrosshairColor.b, _visible ? CrosshairAlpha : 0f);
        _crosshairUpImage.color = color;
        _crosshairDownImage.color = color;
        _crosshairLeftImage.color = color;
        _crosshairRightImage.color = color;
        _crosshairCenterImage.color = color;

        float gap = CrosshairGap + _expand;
        float halfLength = CrosshairLength / 2;
        _crosshairUp.anchoredPosition = new Vector2(0, gap + halfLength);
        _crosshairDown.anchoredPosition = new Vector2(0, -gap - halfLength);
        _crosshairLeft.anchoredPosition = new Vector2(-gap - halfLength, 0);
        _crosshairRight.anchoredPosition = new Vector2(gap + halfLength, 0);

        _crosshairUp.sizeDelta = new Vector2(CrosshairLength, CrosshairThickness);
        _crosshairDown.sizeDelta = new Vector2(CrosshairLength, CrosshairThickness);
        _crosshairLeft.sizeDelta = new Vector2(CrosshairLength, CrosshairThickness);
        _crosshairRight.sizeDelta = new Vector2(CrosshairLength, CrosshairThickness);
        _crosshairCenter.sizeDelta = new Vector2(CrosshairThickness, CrosshairThickness);

        _crosshair.localEulerAngles = new Vector3(0, 0, CrosshairRotation);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (this == null) return;
        if (_crosshairUp == null || _crosshairDown == null || _crosshairLeft == null || _crosshairRight == null || _crosshairCenter == null)
            return;

        _crosshairUpImage = _crosshairUp.GetComponent<Image>();
        _crosshairDownImage = _crosshairDown.GetComponent<Image>();
        _crosshairLeftImage = _crosshairLeft.GetComponent<Image>();
        _crosshairRightImage = _crosshairRight.GetComponent<Image>();
        _crosshairCenterImage = _crosshairCenter.GetComponent<Image>();

        EditorApplication.delayCall -= RefreshVisual;

        EditorApplication.delayCall += () =>
        {
            if (this != null && _crosshair != null)
                RefreshVisual();
        };

        EditorUtility.SetDirty(this);
    }
#endif
}