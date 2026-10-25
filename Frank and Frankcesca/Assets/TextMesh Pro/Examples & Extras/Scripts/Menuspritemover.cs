using UnityEngine;

// Put this on the 2D sprite that wanders around the main menu. It works on a UI Image inside the
// Canvas or on a SpriteRenderer in the world - it just moves the object it is on.
// "Draw" the path by placing empty objects (targets) where the sprite should walk to, and drag them
// into the Targets list in order. The sprite walks to the first target, waits, walks to the next,
// and so on. For a UI sprite, make the targets empty UI objects under the same Canvas, so they
// share its space.
//
// Speed is in the units of that space: a Canvas sprite moves in pixels (try 100 - 300), a world
// sprite moves in world units (try 1 - 3).
public class MenuSpriteMover : MonoBehaviour
{
    public enum PathMode
    {
        Once,       // 1 -> 2 -> 3, then it stops there for good. For "point A to point B", use this with 2 targets.
        Loop,       // 1 -> 2 -> 3 -> 1 -> 2 ...
        PingPong,   // 1 -> 2 -> 3 -> 2 -> 1 ...
        Random      // Any target, never the same one twice in a row.
    }

    [Header("Targets (placed in the order it should visit them)")]
    public Transform[] targets;
    public PathMode pathMode = PathMode.Once;
    public bool startAtFirstTarget = true;

    [Header("Movement")]
    public float speed = 150f;
    public float waitAtTarget = 1.5f;       // Seconds it stands still on arrival.

    [Header("Looks")]
    public bool flipToFaceDirection = true; // Turns left / right to face where it is walking.
    public bool facesRightByDefault = true; // Tick if the drawing looks to the RIGHT.
    public float bobHeight = 0f;            // A little hop while walking. 0 = none. Same units as Speed (e.g. 10 for a Canvas sprite).
    public float bobSpeed = 10f;

    private Vector3 basePosition;           // Where it is, without the hop.
    private int targetIndex;
    private int direction = 1;              // Used by PingPong.
    private bool waiting;
    private bool finished;                  // Used by Once: it reached the last target and stays there.
    private float waitTimer;

    private void Start()
    {
        if (targets == null || targets.Length == 0)
        {
            Debug.LogWarning("MenuSpriteMover: drag at least one target into the Targets list, or the sprite has nowhere to go.", this);
            enabled = false;
            return;
        }

        if (startAtFirstTarget)
        {
            transform.position = targets[0].position;
        }

        basePosition = transform.position;

        // Starting on the first target, so head for the next one.
        targetIndex = 0;

        if (startAtFirstTarget)
        {
            PickNextTarget();
        }
    }

    // Unscaled time, so it still moves if the menu is ever shown while the game is paused.
    private void Update()
    {
        if (finished) return;

        float deltaTime = Time.unscaledDeltaTime;

        if (waiting)
        {
            waitTimer -= deltaTime;

            if (waitTimer <= 0f)
            {
                waiting = false;
                PickNextTarget();
            }

            transform.position = basePosition;
            return;
        }

        Vector3 goal = targets[targetIndex].position;

        FaceDirection(goal.x - basePosition.x);

        basePosition = Vector3.MoveTowards(basePosition, goal, speed * deltaTime);

        // The little hop only happens while walking.
        float hop = bobHeight > 0f ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * bobSpeed)) * bobHeight : 0f;
        transform.position = basePosition + Vector3.up * hop;

        // Arrived.
        if ((goal - basePosition).sqrMagnitude < 0.0001f)
        {
            waiting = true;
            waitTimer = waitAtTarget;
        }
    }

    private void PickNextTarget()
    {
        if (targets.Length <= 1) return;

        switch (pathMode)
        {
            case PathMode.Once:
                if (targetIndex >= targets.Length - 1)
                {
                    finished = true;    // Already at the last target: stay put.
                }
                else
                {
                    targetIndex++;
                }
                break;

            case PathMode.Loop:
                targetIndex = (targetIndex + 1) % targets.Length;
                break;

            case PathMode.PingPong:
                if (targetIndex + direction >= targets.Length || targetIndex + direction < 0)
                {
                    direction = -direction;
                }
                targetIndex += direction;
                break;

            case PathMode.Random:
                int next = targetIndex;
                while (next == targetIndex)
                {
                    next = Random.Range(0, targets.Length);
                }
                targetIndex = next;
                break;
        }
    }

    private void FaceDirection(float horizontalDistance)
    {
        if (!flipToFaceDirection || Mathf.Abs(horizontalDistance) < 0.01f) return;

        bool movingRight = horizontalDistance > 0f;
        float sign = (movingRight == facesRightByDefault) ? 1f : -1f;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * sign;
        transform.localScale = scale;
    }
}