using UnityEngine;

public enum EnemyKind
{
    Slime, // walks; stomp it to squash it
    Spiky, // walks slowly; covered in spikes, so stomping it hurts you
    Bat,   // flies back and forth in a wave; stomp it to knock it down
}

// All enemies can also be defeated with fireballs or by hitting the block they stand on.
public class Enemy : MonoBehaviour
{
    const float WakeDistance = 15f;
    const float BatRange = 3f;

    public EnemyKind Kind;

    Rigidbody2D body;
    Collider2D col;
    SpriteRenderer sprite;
    Vector2 home;
    float direction = -1f;
    bool awake;

    public bool IsDead { get; private set; }
    public bool CanBeStomped => Kind != EnemyKind.Spiky;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        home = body.position;
        UpdateFacing();
    }

    void FixedUpdate()
    {
        if (IsDead) return;
        // Stay still until the camera gets close, so enemies don't wander off before you see them.
        if (!awake)
        {
            var cam = Camera.main;
            if (cam == null || Mathf.Abs(transform.position.x - cam.transform.position.x) > WakeDistance) return;
            awake = true;
        }

        if (Kind == EnemyKind.Bat) Fly();
        else Walk(Kind == EnemyKind.Spiky ? 1.5f : 2f);
    }

    void Walk(float speed)
    {
        var b = col.bounds;
        var ahead = new Vector2(direction > 0f ? b.max.x + 0.05f : b.min.x - 0.05f, b.center.y);
        bool wall = IsSolid(ahead);
        bool floor = IsSolid(ahead + Vector2.down * (b.extents.y + 0.2f));
        if (wall || !floor) Turn();
        body.linearVelocity = new Vector2(direction * speed, body.linearVelocity.y);
    }

    void Fly()
    {
        const float Speed = 2.5f;
        var b = col.bounds;
        var ahead = new Vector2(direction > 0f ? b.max.x + 0.05f : b.min.x - 0.05f, b.center.y);
        if (IsSolid(ahead)
            || (direction < 0f && body.position.x < home.x - BatRange)
            || (direction > 0f && body.position.x > home.x + BatRange))
            Turn();
        float targetY = home.y + Mathf.Sin(Time.time * 3f) * 0.7f;
        body.linearVelocity = new Vector2(direction * Speed, (targetY - body.position.y) * 8f);
    }

    void Turn()
    {
        direction = -direction;
        UpdateFacing();
    }

    // The slime and bat art faces left, the hedgehog faces right.
    void UpdateFacing() => sprite.flipX = Kind == EnemyKind.Spiky ? direction < 0f : direction > 0f;

    void Update()
    {
        if (IsDead) return;
        float wobble = Mathf.Sin(Time.time * (Kind == EnemyKind.Bat ? 20f : 10f)) * (Kind == EnemyKind.Bat ? 0.12f : 0.05f);
        transform.localScale = new Vector3(1f + wobble, 1f - wobble, 1f);
    }

    bool IsSolid(Vector2 point)
    {
        foreach (var hit in Physics2D.OverlapPointAll(point))
            if (hit != col && !hit.isTrigger && !hit.TryGetComponent<PlayerController>(out _)) return true;
        return false;
    }

    // Jumped on by the player. Slimes get squashed flat; bats get knocked out of the air.
    public void Stomp()
    {
        if (IsDead) return;
        if (Kind != EnemyKind.Slime)
        {
            Kill();
            return;
        }
        IsDead = true;
        sprite.sprite = PixelArt.SlimeFlat;
        transform.localScale = Vector3.one;
        col.enabled = false;
        body.simulated = false;
        Destroy(gameObject, 0.5f);
    }

    // Hit by a fireball or a bumped block: flips over and falls off the screen.
    public void Kill()
    {
        if (IsDead) return;
        IsDead = true;
        transform.localScale = Vector3.one;
        col.enabled = false;
        body.gravityScale = 4f;
        body.linearVelocity = new Vector2(direction * 2f, 10f);
        sprite.flipY = true;
        sprite.sortingOrder = 20;
        Destroy(gameObject, 2f);
    }
}
