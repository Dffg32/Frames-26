using UnityEngine;

// A slime that patrols back and forth, turning at walls and ledges. Jump on it to squash it.
public class Enemy : MonoBehaviour
{
    const float Speed = 2f;
    const float WakeDistance = 15f;

    Rigidbody2D body;
    Collider2D col;
    SpriteRenderer sprite;
    float direction = -1f;
    bool awake, squashed;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    void FixedUpdate()
    {
        if (squashed) return;
        // Stay still until the camera gets close, so enemies don't wander off before you see them.
        if (!awake)
        {
            var cam = Camera.main;
            if (cam == null || Mathf.Abs(transform.position.x - cam.transform.position.x) > WakeDistance) return;
            awake = true;
        }

        var b = col.bounds;
        var ahead = new Vector2(direction > 0f ? b.max.x + 0.05f : b.min.x - 0.05f, b.center.y);
        bool wall = IsSolid(ahead);
        bool floor = IsSolid(ahead + Vector2.down * (b.extents.y + 0.2f));
        if (wall || !floor)
        {
            direction = -direction;
            sprite.flipX = direction > 0f;
        }
        body.linearVelocity = new Vector2(direction * Speed, body.linearVelocity.y);
    }

    void Update()
    {
        if (!squashed)
            transform.localScale = new Vector3(1f + Mathf.Sin(Time.time * 10f) * 0.05f, 1f - Mathf.Sin(Time.time * 10f) * 0.05f, 1f);
    }

    bool IsSolid(Vector2 point)
    {
        foreach (var hit in Physics2D.OverlapPointAll(point))
            if (hit != col && !hit.isTrigger && !hit.TryGetComponent<PlayerController>(out _)) return true;
        return false;
    }

    public void Squash()
    {
        if (squashed) return;
        squashed = true;
        sprite.sprite = PixelArt.SlimeFlat;
        transform.localScale = Vector3.one;
        col.enabled = false;
        body.simulated = false;
        Destroy(gameObject, 0.5f);
    }
}
