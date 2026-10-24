using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class TopDownPlayerMovement : MonoBehaviour
{
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
        float x = Input.GetAxisRaw("Horizontal"); // if a+d -> 0 // Edit / Project Settings / Input Manager/ axes you can see binding
        float y = Input.GetAxisRaw("Vertical");
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
