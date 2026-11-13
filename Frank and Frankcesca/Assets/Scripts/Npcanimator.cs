using UnityEngine;
using UnityEngine.AI;

// Put this on each Franklin NPC (the same object that has the DialogueInteractable).
// It plays Walk when the NPC moves, plays Talk during that NPC's dialogue,
// and turns the NPC to face the player while talking.
// Patrolling is optional: it only happens if there is a NavMeshAgent and waypoints.
public class NPCAnimator : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float smoothing = 10f;
    [SerializeField] private bool randomiseIdleStart = true;

    [Header("Talking")]
    [SerializeField] private float turnSpeed = 5f;

    [Header("Optional patrol (needs a NavMeshAgent on this object)")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waitTime = 2f;

    private NavMeshAgent agent;
    private DialogueInteractable myDialogue;
    private Transform player;
    private Vector3 lastPosition;
    private float currentSpeed;
    private bool isTalking;
    private int waypointIndex;
    private float waitTimer;

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

        TryGetComponent(out agent);

        myDialogue = GetComponentInParent<DialogueInteractable>();
        if (myDialogue == null) myDialogue = GetComponentInChildren<DialogueInteractable>();
        if (myDialogue == null)
            Debug.LogWarning("NPCAnimator: no DialogueInteractable found on " + name + ". The talk animation will not play.", this);

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

        // Start the idle at a random point so the NPCs are not perfectly in sync.
        if (randomiseIdleStart)
        {
            animator.Update(0f);
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            animator.Play(state.fullPathHash, 0, Random.value);
        }

        if (CanPatrol()) agent.SetDestination(waypoints[0].position);
    }

    void Update()
    {
        UpdateSpeed();

        if (isTalking)
        {
            FacePlayer();
            return;
        }

        if (CanPatrol()) Patrol();
    }

    // ---------- Animation ----------

    private void UpdateSpeed()
    {
        // Measure how far we actually moved this frame (works for NavMeshAgent or anything else).
        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        lastPosition = transform.position;

        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        currentSpeed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * smoothing);
        animator.SetFloat(SpeedHash, currentSpeed);
    }

    private void FacePlayer()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * turnSpeed);
    }

    // ---------- Dialogue events ----------

    private void OnDialogueStarted(DialogueInteractable npc)
    {
        if (npc != myDialogue) return;
        SetTalking(true);
    }

    private void OnDialogueEnded(DialogueInteractable npc)
    {
        if (npc != myDialogue) return;
        SetTalking(false);
    }

    private void SetTalking(bool talking)
    {
        isTalking = talking;
        animator.SetBool(TalkHash, talking);

        if (agent != null && agent.isOnNavMesh) agent.isStopped = talking;
    }

    // ---------- Optional patrol ----------

    private bool CanPatrol()
    {
        return agent != null && agent.isOnNavMesh && waypoints != null && waypoints.Length > 0;
    }

    private void Patrol()
    {
        if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance) return;

        waitTimer += Time.deltaTime;
        if (waitTimer >= waitTime)
        {
            waitTimer = 0f;
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            agent.SetDestination(waypoints[waypointIndex].position);
        }
    }
}