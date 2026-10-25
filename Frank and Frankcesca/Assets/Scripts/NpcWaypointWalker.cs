using UnityEngine;
using UnityEngine.AI;
//Akhona Khoali

// Also belongs on the NPC (the object with the NavMeshAgent). The NPC walks between the waypoints we
// give it, waits at each one, and feeds its Animator a "Speed" float so Idle / Walk play by themselves.
// It stops and turns to face the player when the player comes close (to talk), and carries on after.
[RequireComponent(typeof(NavMeshAgent))]
public class NpcWaypointWalker : MonoBehaviour
{
    public enum PathMode { Loop, PingPong, Once }

    [Header("Path")]
    public Transform[] waypoints;
    public PathMode pathMode = PathMode.Loop;
    public float waitAtWaypoint = 2f;
    public float walkSpeed = 1.4f;

    [Header("Talking")]
    [Tooltip("Drag the Player here. The NPC stops and faces them while they are talking (dialogue open).")]
    public PlayerInteractor player;
    public float stopDistance = 4f;         // Only counts as "talking to me" if the player is this close.
    public float turnSpeed = 5f;

    [Header("Animation")]
    public Animator animator;               // The NPC model's Animator (found in children if empty).
    [Tooltip("Leave EMPTY if you use NpcAnimation (it picks the animations by state name instead).")]
    public string speedParameter = "";      // Float in the Animator Controller.
    public float speedDamping = 0.1f;

    private NavMeshAgent agent;
    private int index;
    private int direction = 1;
    private float waitTimer;
    private bool waiting;
    private bool finished;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = walkSpeed;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        GoToCurrent();
    }

    private void Update()
    {
        UpdateAnimator();

        if (waypoints == null || waypoints.Length == 0 || finished) return;

        // The player is close AND a screen (dialogue) is open: stand still and face them.
        // Walking past does not stop the NPC, only talking to them does.
        if (player != null && player.InputLocked && Vector3.Distance(transform.position, player.transform.position) < stopDistance)
        {
            agent.isStopped = true;
            Vector3 look = player.transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), turnSpeed * Time.deltaTime);
            }
            return;
        }

        agent.isStopped = false;

        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                waiting = false;
                Advance();
            }
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            waiting = true;
            waitTimer = waitAtWaypoint;
        }
    }

    private void Advance()
    {
        if (waypoints.Length <= 1) return;

        switch (pathMode)
        {
            case PathMode.Loop:
                index = (index + 1) % waypoints.Length;
                break;
            case PathMode.PingPong:
                if (index + direction >= waypoints.Length || index + direction < 0) direction = -direction;
                index += direction;
                break;
            case PathMode.Once:
                if (index >= waypoints.Length - 1) { finished = true; return; }
                index++;
                break;
        }

        GoToCurrent();
    }

    private void GoToCurrent()
    {
        if (waypoints == null || waypoints.Length == 0 || waypoints[index] == null) return;
        agent.SetDestination(waypoints[index].position);
    }

    private void UpdateAnimator()
    {
        if (animator == null || string.IsNullOrEmpty(speedParameter)) return;
        animator.SetFloat(speedParameter, agent.velocity.magnitude, speedDamping, Time.deltaTime);
    }
}
