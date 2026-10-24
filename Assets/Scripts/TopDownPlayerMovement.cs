using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class TopDownPlayerMovement : MonoBehaviour
{
    public enum ControlScheme { WASD, ArrowKeys }

    [SerializeField] private ControlScheme controls = ControlScheme.WASD;
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField, Min(0.01f)] private float acceleration = 40f;
    [SerializeField, Min(0.01f)] private float deceleration = 60f;
    private Rigidbody2D body;
    private Vector2 direction;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Separate key sets let two local players share one keyboard.
        bool arrows = controls == ControlScheme.ArrowKeys;
        float x = (Input.GetKey(arrows ? KeyCode.RightArrow : KeyCode.D) ? 1f : 0f)
                - (Input.GetKey(arrows ? KeyCode.LeftArrow : KeyCode.A) ? 1f : 0f);
        float y = (Input.GetKey(arrows ? KeyCode.UpArrow : KeyCode.W) ? 1f : 0f)
                - (Input.GetKey(arrows ? KeyCode.DownArrow : KeyCode.S) ? 1f : 0f);
        direction = ReadDirection(x, y);
    }

    private static Vector2 ReadDirection(float x, float y)
    // deal with w+d d+w
    {
        return Vector2.ClampMagnitude(new Vector2(x, y), 1f); // ClampMagnitude make suqareroot (1+1) = 1.4 -> to 1
    }

    private void FixedUpdate()
    {
        Vector2 targetVelocity = direction * moveSpeed;
        float rate = direction.sqrMagnitude > 0f ? acceleration : deceleration;
        body.velocity = Vector2.MoveTowards(
            body.velocity, targetVelocity, rate * Time.fixedDeltaTime);
    }

    private void OnDisable()
    {
        direction = Vector2.zero;
        if (body != null) body.velocity = Vector2.zero;
    }
}
