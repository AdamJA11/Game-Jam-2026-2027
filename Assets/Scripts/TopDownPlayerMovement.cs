using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class TopDownPlayerMovement : MonoBehaviour
{
    public enum ControlScheme { WASD, ArrowKeys, IJKL }

    [SerializeField] private ControlScheme controls = ControlScheme.WASD;
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField, Min(0.01f)] private float acceleration = 40f;
    [SerializeField, Min(0.01f)] private float deceleration = 60f;
    private Rigidbody2D body;
    private Vector2 direction;
    
    // Assurez-vous que la classe MovementSpeedModifier est bien définie dans votre projet
    private readonly MovementSpeedModifier speedModifier = new MovementSpeedModifier();
    private readonly MovementSpeedModifier itemSpeedModifier = new MovementSpeedModifier();
    
    public bool InputBlocked { get; private set; }
    public float SpeedMultiplier => speedModifier.Multiplier;
    public float EffectiveMoveSpeed => itemSpeedModifier.EffectiveSpeed(speedModifier.EffectiveSpeed(moveSpeed));
    public Vector2 MovementIntent => InputBlocked || !enabled ? Vector2.zero : direction;
    public float Acceleration => acceleration;
    public float Deceleration => deceleration;
    
    public void SetItemSpeedMultiplier(float multiplier) { itemSpeedModifier.Set(multiplier); }
    public void SetSpeedMultiplier(float multiplier) { speedModifier.Set(multiplier); }

    public void SetInputBlocked(bool blocked)
    {
        InputBlocked = blocked;
        if (blocked) speedModifier.Set(1f);
        direction = Vector2.zero;
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (body != null) { body.velocity = Vector2.zero; body.angularVelocity = 0f; }
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (InputBlocked) { direction = Vector2.zero; return; }
        
        // Separate key sets let local players share one keyboard.
        bool arrows = controls == ControlScheme.ArrowKeys;
        bool ijkl = controls == ControlScheme.IJKL;
        float x = (Input.GetKey(arrows ? KeyCode.RightArrow : ijkl ? KeyCode.L : KeyCode.D) ? 1f : 0f)
                - (Input.GetKey(arrows ? KeyCode.LeftArrow : ijkl ? KeyCode.J : KeyCode.A) ? 1f : 0f);
        float y = (Input.GetKey(arrows ? KeyCode.UpArrow : ijkl ? KeyCode.I : KeyCode.W) ? 1f : 0f)
                - (Input.GetKey(arrows ? KeyCode.DownArrow : ijkl ? KeyCode.K : KeyCode.S) ? 1f : 0f);
        
        direction = ReadDirection(x, y);

        // --- NOUVEAU : Rotation du joueur ---
        if (direction != Vector2.zero)
        {
            // Oriente le sommet du sprite vers la direction du mouvement
            transform.up = direction; 
        }
    }

    private static Vector2 ReadDirection(float x, float y)
    {
        return Vector2.ClampMagnitude(new Vector2(x, y), 1f); 
    }

    private void FixedUpdate()
    {
        if (InputBlocked) { body.velocity = Vector2.zero; body.angularVelocity = 0f; return; }
        Vector2 targetVelocity = direction * EffectiveMoveSpeed;
        float rate = direction.sqrMagnitude > 0f ? acceleration : deceleration;
        body.velocity = Vector2.MoveTowards(
            body.velocity, targetVelocity, rate * Time.fixedDeltaTime);
    }

    private void OnDisable()
    {
        speedModifier.Set(1f);
        itemSpeedModifier.Set(1f);
        direction = Vector2.zero;
        if (body != null) body.velocity = Vector2.zero;
    }
}