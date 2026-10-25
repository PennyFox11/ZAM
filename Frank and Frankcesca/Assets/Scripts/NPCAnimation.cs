using UnityEngine;
using UnityEngine.AI;
//Akhona khoali

// Goes on each NPC (the object that has the Animator, or its parent).
// It picks which animation plays by STATE NAME, so the Animator Controller needs no transitions
// and no parameters - just the states ( Idle, Walk, Shrug) sitting in it.
//
//   Idle State   what plays while the NPC just stands there
//   Walk State   plays while the NPC is moving (can be left empty for an NPC that never walks)
//   Talk State   plays while the player is talking to this NPC (can be left empty to keep the idle)
//
// "Talking" = a screen/dialogue is open (the player is frozen) and the player is within Talk Distance.
public class NpcAnimation : MonoBehaviour
{
    [Header("Who to react to")]
    [Tooltip("Drag the Player here (the object with PlayerInteractor).")]
    public PlayerInteractor player;
    public float talkDistance = 4f;

    [Header("Animator")]
    public Animator animator;               // Found in this object or its children if empty.

    [Header("Animation State Names (exactly as they appear in the Animator window)")]
    public string idleState = "Idle";
    public string walkState = "";
    public string talkState = "";
    public float crossFadeSeconds = 0.2f;
    public float walkSpeedThreshold = 0.1f;

    private NavMeshAgent agent;
    private string currentState = "";

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        Switch(idleState, true);
    }

    private void Update()
    {
        if (animator == null) return;

        string wanted = idleState;

        bool talking = player != null
            && player.InputLocked
            && Vector3.Distance(transform.position, player.transform.position) < talkDistance;

        if (talking && !string.IsNullOrEmpty(talkState))
        {
            wanted = talkState;
        }
        else if (!string.IsNullOrEmpty(walkState) && agent != null && agent.velocity.magnitude > walkSpeedThreshold)
        {
            wanted = walkState;
        }

        Switch(wanted, false);
    }

    private void Switch(string state, bool instant)
    {
        if (string.IsNullOrEmpty(state) || state == currentState) return;

        currentState = state;

        if (instant)
        {
            animator.Play(state);
        }
        else
        {
            animator.CrossFadeInFixedTime(state, crossFadeSeconds);
        }
    }
}
