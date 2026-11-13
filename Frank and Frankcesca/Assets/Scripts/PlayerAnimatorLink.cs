using UnityEngine;

public class PlayerAnimatorLink : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float smoothing = 10f;

    private Transform player;
    private Vector3 lastPosition;
    private float currentSpeed;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Reset()
    {
        animator = GetComponent<Animator>();
    }

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();

        CharacterController controller = GetComponentInParent<CharacterController>();
        player = controller != null ? controller.transform : transform.root;
        lastPosition = player.position;
    }

    void Update()
    {
        // Measure how far the player moved sideways this frame (ignores jumping/falling).
        Vector3 delta = player.position - lastPosition;
        delta.y = 0f;
        lastPosition = player.position;

        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;

        currentSpeed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * smoothing);
        animator.SetFloat(SpeedHash, currentSpeed);
    }
}