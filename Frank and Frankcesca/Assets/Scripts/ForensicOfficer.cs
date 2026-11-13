using UnityEngine;

// Put this on the Forensic Officer (the same object that has the DialogueInteractable).
// His controller: Wipe is the default state, Walk plays when he moves,
// and Talk plays during his dialogue.
public class ForensicOfficer : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float smoothing = 10f;

    [Header("Talking")]
    [SerializeField] private float turnSpeed = 5f;

    private DialogueInteractable myDialogue;
    private Transform player;
    private Vector3 lastPosition;
    private float currentSpeed;
    private bool isTalking;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int TalkHash = Animator.StringToHash("IsTalking");

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        animator.applyRootMotion = false;

        myDialogue = GetComponentInParent<DialogueInteractable>();
        if (myDialogue == null) myDialogue = GetComponentInChildren<DialogueInteractable>();
        if (myDialogue == null)
            Debug.LogWarning("ForensicOfficer: no DialogueInteractable found on " + name + ". The talk animation will not play.", this);

        lastPosition = transform.position;
    }

    void OnEnable()
    {
        DialogueUI.DialogueStarted += OnDialogueStarted;
        DialogueUI.DialogueEnded += OnDialogueEnded;
    }

    void OnDisable()
    {
        DialogueUI.DialogueStarted -= OnDialogueStarted;
        DialogueUI.DialogueEnded -= OnDialogueEnded;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        // Walk animation: based on how far he actually moved this frame.
        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        lastPosition = transform.position;

        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        currentSpeed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * smoothing);
        animator.SetFloat(SpeedHash, currentSpeed);

        // Turn to face the player while talking.
        if (isTalking && player != null)
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * turnSpeed);
            }
        }
    }

    private void OnDialogueStarted(DialogueInteractable npc)
    {
        if (npc != myDialogue) return;
        isTalking = true;
        animator.SetBool(TalkHash, true);
    }

    private void OnDialogueEnded(DialogueInteractable npc)
    {
        if (npc != myDialogue) return;
        isTalking = false;
        animator.SetBool(TalkHash, false);
    }
}