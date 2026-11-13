using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// A book the player can pick up and carry, open and close, and flip through.
// It is a PickupInteractable (like NoteInteractable): E picks it up and drops it, the player can walk
// around while holding it, and holding the left mouse button and dragging turns it.
//
// You do NOT make quads or materials. Drag your page PNGs into "Page Images" and the book builds
// itself when the game starts: a back cover, a front cover that swings open, and real pages with a
// PNG on each side, turning around the spine.
//
// Controls while the book is held:
//   Right mouse button   open / close
//   Scroll wheel         flip pages (only while open). Replaces camera zoom while a book is held.
//
// Page Images are single SIDES in reading order:
//   [0] right-hand page when the book is first opened   [1] left-hand page after you turn it
//   [2] next right-hand page                            [3] its back ...and so on.
// A missing last side is filled with plain paper.
//
// This script hides the object's own mesh (the little placeholder cube) but keeps its collider,
// and fits that collider around the book for you.
public class BookInteractable : PickupInteractable
{
    // The book the player is holding right now (null when none). FPController reads this to skip zoom.
    public static BookInteractable Current { get; private set; }

    [Header("Pages (one PNG per page SIDE, in reading order)")]
    public Texture2D[] pageImages;
    [Tooltip("Size of one page in the book's own units. The book grows with Held Scale.")]
    public Vector2 pageSize = new Vector2(0.3f, 0.4f);
    public Color blankPageColour = new Color(0.93f, 0.9f, 0.82f, 1f);

    [Header("Covers (all optional - plain colour is used when empty)")]
    public Texture2D frontCoverImage;           // The outside of the front cover.
    public Texture2D insideCoverImage;          // The inside of the front cover (the left page of the first spread).
    public Texture2D backCoverImage;
    public Color coverColour = new Color(0.25f, 0.5f, 0.15f, 1f);
    public float coverMargin = 0.012f;          // How far the covers stick out past the pages.

    [Header("Size")]
    [Tooltip("The book grows by this much while it is open, so the two pages are easy to read.")]
    public float openScale = 2f;
    [Tooltip("Tick to resize this object's collider to the book's size. Untick if you set the collider yourself.")]
    public bool autoFitCollider = true;

    [Header("On The Ground")]
    [Tooltip("The good-looking 3D book that sits in the world. It is shown until the player presses E, then hidden while the pages take over, and shown again when the book is put down. It must be a CHILD of this object. Leave empty to use this object's own mesh.")]
    public GameObject worldModel;

    [Header("Animation")]
    public float openSeconds = 0.5f;
    public float flipSeconds = 0.45f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public bool scrollUpIsNext = false;

    [Header("Sounds (all optional)")]
    public AudioClip openClip;
    public AudioClip closeClip;
    public AudioClip[] flipClips;               // One is picked at random each flip.
    [Range(0f, 1f)]
    public float soundVolume = 0.8f;
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    private static Sprite whiteSprite;

    private Transform visual;                   // Everything the book builds lives under this.
    private Vector3 inverseScale = Vector3.one;
    private Leaf[] leaves;                      // [0] = the front cover, then the pages.
    private int pageIndex;                      // How many page leaves are already turned over.
    private bool isOpen;
    private bool isBusy;                        // A cover swing or a page flip is playing.
    private int pendingFlip;                    // One flip can wait in line.
    private float closedHeldScale = 1f;
    private AudioSource soundSource;

    // Sets defaults when the component is first added.
    private void Reset()
    {
        itemName = "Book";
        showInfoPanel = false;
        useCentreHoldPoint = true;
    }

    private void Start()
    {
        Build();
    }

    private void OnDisable()
    {
        if (Current == this)
        {
            Current = null;
        }
    }

    // ----- Pick up / put down -----

    public override string GetPrompt()
    {
        return "Read " + itemName;
    }

    public override void Interact(PlayerInteractor interactor)
    {
        base.Interact(interactor);
        Current = this;
        ShowHeldLook(true);
    }

    public override void Drop()
    {
        base.Drop();

        if (Current == this)
        {
            Current = null;
        }

        // Put the book down closed.
        StopAllCoroutines();
        isBusy = false;
        pendingFlip = 0;
        CloseImmediately();
        ShowHeldLook(false);
    }

    // On the ground: the nice 3D book. In the hands: the opening, flipping pages.
    private void ShowHeldLook(bool held)
    {
        if (visual != null)
        {
            visual.gameObject.SetActive(held);
        }

        if (worldModel != null && worldModel != gameObject)
        {
            worldModel.SetActive(!held);
        }
        else
        {
            MeshRenderer ownMesh = GetComponent<MeshRenderer>();
            if (ownMesh != null) ownMesh.enabled = !held;
        }
    }

    // ----- Controls -----

    private void Update()
    {
        if (Current != this || Mouse.current == null || leaves == null) return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            ToggleOpen();
        }

        if (!isOpen) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < 0.01f) return;

        int direction = scroll < 0f ? 1 : -1;       // Scroll down = next page.
        if (scrollUpIsNext) direction = -direction;

        RequestFlip(direction);
    }

    public void ToggleOpen()
    {
        if (isBusy) return;
        StartCoroutine(OpenCloseRoutine(!isOpen));
    }

    public void RequestFlip(int direction)
    {
        if (!isOpen) return;

        if (isBusy)
        {
            pendingFlip = direction;
            return;
        }

        StartCoroutine(FlipRoutine(direction));
    }

    // ----- Animation -----

    private IEnumerator OpenCloseRoutine(bool open)
    {
        isBusy = true;
        PlaySound(open ? openClip : closeClip);

        // Closing starts from the first page, so the cover never swings through turned pages.
        if (!open)
        {
            ResetPages();
        }

        float time = 0f;
        while (time < 1f)
        {
            time += Time.deltaTime / Mathf.Max(0.05f, openSeconds);
            float eased = curve.Evaluate(Mathf.Clamp01(time));
            float fraction = open ? eased : 1f - eased;

            leaves[0].SetAngle(180f * fraction);
            leaves[0].SetFlightOrders(180f * fraction);
            ApplyOpenLook(fraction);
            yield return null;
        }

        float end = open ? 1f : 0f;
        leaves[0].SetAngle(180f * end);
        leaves[0].SetRestOrders(open);
        ApplyOpenLook(end);

        isOpen = open;
        isBusy = false;
    }

    private IEnumerator FlipRoutine(int direction)
    {
        int leafNumber = direction > 0 ? pageIndex + 1 : pageIndex;     // leaves[0] is the cover.

        if (leafNumber < 1 || leafNumber >= leaves.Length)
        {
            pendingFlip = 0;
            yield break;
        }

        Leaf leaf = leaves[leafNumber];

        isBusy = true;
        PlaySound(flipClips != null && flipClips.Length > 0 ? flipClips[Random.Range(0, flipClips.Length)] : null);

        float from = direction > 0 ? 0f : 180f;
        float to = direction > 0 ? 180f : 0f;

        float time = 0f;
        while (time < 1f)
        {
            time += Time.deltaTime / Mathf.Max(0.05f, flipSeconds);
            float angle = Mathf.Lerp(from, to, curve.Evaluate(Mathf.Clamp01(time)));
            leaf.SetAngle(angle);
            leaf.SetFlightOrders(angle);
            yield return null;
        }

        leaf.SetAngle(to);
        leaf.SetRestOrders(direction > 0);
        pageIndex += direction;
        isBusy = false;

        if (pendingFlip != 0)
        {
            int next = pendingFlip;
            pendingFlip = 0;
            RequestFlip(next);
        }
    }

    private void ResetPages()
    {
        if (leaves == null) return;

        for (int i = 1; i < leaves.Length; i++)
        {
            leaves[i].SetAngle(0f);
            leaves[i].SetRestOrders(false);
        }

        pageIndex = 0;
    }

    private void CloseImmediately()
    {
        if (leaves == null) return;

        ResetPages();
        leaves[0].SetAngle(0f);
        leaves[0].SetRestOrders(false);
        ApplyOpenLook(0f);
        isOpen = false;
    }

    // Slides the book so the open spread is centred, and grows it so the pages are readable.
    private void ApplyOpenLook(float fraction)
    {
        float x = Mathf.Lerp(-pageSize.x * 0.5f, 0f, fraction);
        visual.localPosition = Vector3.Scale(new Vector3(x, 0f, 0f), inverseScale);

        heldScale = Mathf.Lerp(closedHeldScale, closedHeldScale * openScale, fraction);
    }

    // ----- Building the book from the PNGs -----

    private void Build()
    {
        closedHeldScale = heldScale;

        // The object may be a flat scaled cube. Undo that scale for everything we build, so the
        // pages come out exactly pageSize big and never stretched.
        Vector3 scale = transform.localScale;
        inverseScale = new Vector3(
            Mathf.Abs(scale.x) > 0.0001f ? 1f / scale.x : 1f,
            Mathf.Abs(scale.y) > 0.0001f ? 1f / scale.y : 1f,
            Mathf.Abs(scale.z) > 0.0001f ? 1f / scale.z : 1f);

        visual = new GameObject("BookVisual").transform;
        visual.SetParent(transform, false);
        visual.localScale = inverseScale;

        float width = pageSize.x;
        float height = pageSize.y;
        Vector2 coverSize = new Vector2(width + coverMargin, height + coverMargin * 2f);
        Vector3 coverCentre = new Vector3(coverSize.x * 0.5f, 0f, 0f);
        Vector3 pageCentre = new Vector3(width * 0.5f, 0f, 0f);

        int pageSides = pageImages != null ? pageImages.Length : 0;
        int pageLeaves = (pageSides + 1) / 2;
        int total = 1 + pageLeaves;
        leaves = new Leaf[total];

        // The back cover never moves. It is the lowest thing in the pile.
        SpriteRenderer backCover = MakeFace("BackCover", visual, backCoverImage, coverColour, coverSize, coverCentre, false);
        backCover.sortingOrder = 0;

        // Leaf 0: the front cover. Outside faces the reader; its inside shows as the first left-hand page.
        leaves[0] = MakeLeaf("FrontCover", 0, total,
            frontCoverImage, coverColour, insideCoverImage, blankPageColour, coverSize, coverCentre);

        for (int i = 0; i < pageLeaves; i++)
        {
            Texture2D frontImage = pageImages[i * 2];
            Texture2D backImage = (i * 2 + 1 < pageSides) ? pageImages[i * 2 + 1] : null;

            leaves[i + 1] = MakeLeaf("Page" + (i + 1), i + 1, total,
                frontImage, blankPageColour, backImage, blankPageColour, pageSize, pageCentre);
        }

        for (int i = 0; i < total; i++)
        {
            leaves[i].SetRestOrders(false);
        }

        ApplyOpenLook(0f);

        // Starts on the ground: the pages stay hidden until the player picks the book up.
        ShowHeldLook(false);

        if (autoFitCollider)
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box != null)
            {
                box.center = Vector3.zero;
                box.size = new Vector3(
                    coverSize.x * inverseScale.x,
                    coverSize.y * inverseScale.y,
                    0.06f * inverseScale.z);
            }
        }
    }

    private Leaf MakeLeaf(string leafName, int number, int total,
        Texture2D frontImage, Color frontColour, Texture2D backImage, Color backColour,
        Vector2 size, Vector3 centre)
    {
        // The leaf's own origin sits ON the spine, so turning it needs no maths about edges.
        Transform pivot = new GameObject(leafName).transform;
        pivot.SetParent(visual, false);

        Leaf leaf = new Leaf();
        leaf.pivot = pivot;
        leaf.front = MakeFace("Front", pivot, frontImage, frontColour, size, centre, false);
        leaf.back = MakeFace("Back", pivot, backImage, backColour, size, centre, true);

        // Stacking order: the top of the right-hand pile is the highest number; the left-hand pile
        // grows the other way, so the page you just turned is on top.
        leaf.rightBase = 10 + (total - 1 - number) * 4;
        leaf.leftBase = 10 + number * 4;
        return leaf;
    }

    // One flat picture. facesAway = it is the BACK of a leaf (seen once the leaf has turned over).
    private SpriteRenderer MakeFace(string faceName, Transform parent, Texture2D image, Color colour,
        Vector2 size, Vector3 centre, bool facesAway)
    {
        Sprite sprite;
        Color tint;

        if (image != null)
        {
            image.wrapMode = TextureWrapMode.Clamp;     // Stops dark fringes from the opposite edge.
            sprite = Sprite.Create(image, new Rect(0f, 0f, image.width, image.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            tint = Color.white;
        }
        else
        {
            sprite = GetWhiteSprite();
            tint = colour;
        }

        GameObject go = new GameObject(faceName);
        go.transform.SetParent(parent, false);

        Vector2 nativeSize = sprite.bounds.size;
        go.transform.localScale = new Vector3(size.x / nativeSize.x, size.y / nativeSize.y, 1f);
        go.transform.localRotation = facesAway ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        go.transform.localPosition = centre;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = tint;
        return sr;
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite == null)
        {
            Texture2D white = Texture2D.whiteTexture;
            whiteSprite = Sprite.Create(white, new Rect(0f, 0f, white.width, white.height),
                new Vector2(0.5f, 0.5f), 100f);
        }

        return whiteSprite;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (soundSource == null)
        {
            soundSource = gameObject.AddComponent<AudioSource>();
            soundSource.playOnAwake = false;
            soundSource.spatialBlend = 0f;
        }

        soundSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        soundSource.PlayOneShot(clip, soundVolume);
    }

    // One sheet: a picture on each side, hinged on the spine.
    private class Leaf
    {
        public Transform pivot;
        public SpriteRenderer front;
        public SpriteRenderer back;
        public int rightBase;
        public int leftBase;

        // 0 = lying on the right, 180 = turned over onto the left.
        public void SetAngle(float angle)
        {
            pivot.localRotation = Quaternion.Euler(0f, angle, 0f);
        }

        // At rest: which side is on top, and which pile it belongs to.
        public void SetRestOrders(bool turnedOver)
        {
            if (turnedOver)
            {
                back.sortingOrder = leftBase + 1;
                front.sortingOrder = leftBase;
            }
            else
            {
                front.sortingOrder = rightBase + 1;
                back.sortingOrder = rightBase;
            }
        }

        // While turning, the leaf is above everything, and the side facing the reader is on top.
        public void SetFlightOrders(float angle)
        {
            if (angle < 90f)
            {
                front.sortingOrder = 1001;
                back.sortingOrder = 1000;
            }
            else
            {
                back.sortingOrder = 1001;
                front.sortingOrder = 1000;
            }
        }
    }
}