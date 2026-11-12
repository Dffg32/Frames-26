using UnityEngine;

public enum EnemyKind
{
    Slime,  // walks; stomp it to squash it
    Spiky,  // walks slowly; covered in spikes, so stomping it hurts you
    Bat,    // flies back and forth in a wave; stomp it to knock it down
    Frog,   // sits, then hops toward you; won't jump into a pit
    Cactus, // stays put and spits seeds at you when you're close (jump over them)
    Ghost,  // floats slowly after you through walls; only fire or a star can beat it
}

// All enemies can also be defeated with fireballs or by hitting the block they stand on.
public class Enemy : MonoBehaviour
{
    const float WakeDistance = 15f;
    const float BatRange = 3f;
    const float HopEvery = 1.3f;
    const float ShootEvery = 2.6f;
    const float ShootRange = 11f;
    const float HauntRange = 12f;

    public EnemyKind Kind;

    Rigidbody2D body;
    Collider2D col;
    SpriteRenderer sprite;
    Vector2 home;
    float direction = -1f, timer = 1f;
    bool awake;

    public bool IsDead { get; private set; }
    public bool CanBeStomped => Kind != EnemyKind.Spiky && Kind != EnemyKind.Ghost;

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

        switch (Kind)
        {
            case EnemyKind.Bat: Fly(); break;
            case EnemyKind.Frog: Hop(); break;
            case EnemyKind.Cactus: Shoot(); break;
            case EnemyKind.Ghost: Haunt(); break;
            default: Walk(Kind == EnemyKind.Spiky ? 1.5f : 2f); break;
        }
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

    void Hop()
    {
        var b = col.bounds;
        bool grounded = body.linearVelocity.y <= 0.01f && IsSolid(new Vector2(b.center.x, b.min.y - 0.1f));
        if (!grounded) return;
        body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        sprite.sprite = PixelArt.Frog;

        timer -= Time.fixedDeltaTime;
        if (timer > 0f) return;
        timer = HopEvery;
        FacePlayer();
        // A hop covers about 1.7 tiles: only jump if there's floor to land on.
        if (!IsSolid(new Vector2(b.center.x + direction * 1.7f, b.min.y - 0.3f))) return;
        body.linearVelocity = new Vector2(direction * 3f, 11f);
        sprite.sprite = PixelArt.FrogJump;
    }

    void Shoot()
    {
        body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        var player = Game.Instance.Player;
        if (player == null || Mathf.Abs(player.transform.position.x - body.position.x) > ShootRange) return;
        FacePlayer();
        timer -= Time.fixedDeltaTime;
        if (timer > 0f) return;
        timer = ShootEvery;
        Projectile.Spawn(transform.parent, PixelArt.Seed, body.position + new Vector2(direction * 0.5f, 0.05f),
            new Vector2(direction * 6f, 0f), new Vector2(0.3f, 0.3f));
    }

    void Haunt()
    {
        var player = Game.Instance.Player;
        Vector2 target = home;
        if (player != null && Mathf.Abs(player.transform.position.x - body.position.x) < HauntRange)
            target = (Vector2)player.transform.position + Vector2.up * 0.6f;
        var toTarget = target - body.position;
        body.linearVelocity = toTarget.magnitude > 0.1f ? toTarget.normalized * 1.5f : Vector2.zero;
        if (Mathf.Abs(toTarget.x) > 0.1f && Mathf.Sign(toTarget.x) != direction) Turn();
        sprite.color = new Color(1f, 1f, 1f, 0.75f + Mathf.Sin(Time.time * 4f) * 0.15f);
    }

    void FacePlayer()
    {
        var player = Game.Instance.Player;
        if (player == null) return;
        float wanted = player.transform.position.x < body.position.x ? -1f : 1f;
        if (wanted != direction) Turn();
    }

    void Turn()
    {
        direction = -direction;
        UpdateFacing();
    }

    // The hedgehog art faces right; the others face left (the cactus looks the same both ways).
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

    // Jumped on by the player. Slimes get squashed flat; the others get knocked over.
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
