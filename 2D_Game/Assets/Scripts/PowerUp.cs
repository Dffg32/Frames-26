using System.Collections;
using UnityEngine;

// The grow berry. It rises out of its block, then slides along the ground,
// bouncing off walls and falling off ledges. Touch it to become big.
public class PowerUp : MonoBehaviour
{
    const float Speed = 3f;
    const float Gravity = 39f;
    const float RiseTime = 0.6f;

    Rigidbody2D body;
    Collider2D col;
    float direction = 1f, fallSpeed;
    bool rising = true;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    IEnumerator Start()
    {
        var start = transform.position;
        var end = start + Vector3.up;
        for (float t = 0f; t < RiseTime; t += Time.deltaTime)
        {
            body.position = Vector3.Lerp(start, end, t / RiseTime);
            yield return null;
        }
        body.position = end;
        rising = false;
    }

    void FixedUpdate()
    {
        if (rising) return;
        float dt = Time.fixedDeltaTime;
        var pos = body.position;

        float dx = direction * Speed * dt;
        if (Blocked(Vector2.right * direction, Mathf.Abs(dx), out _)) direction = -direction;
        else pos.x += dx;

        fallSpeed += Gravity * dt;
        float dy = fallSpeed * dt;
        if (Blocked(Vector2.down, dy, out float distance))
        {
            pos.y -= distance;
            fallSpeed = 0f;
        }
        else pos.y -= dy;

        body.MovePosition(pos);
        if (pos.y < -3f) Destroy(gameObject);
    }

    // Casts the berry's box and reports the distance to the nearest solid tile in that direction.
    bool Blocked(Vector2 direction, float distance, out float hitDistance)
    {
        var b = col.bounds;
        hitDistance = distance;
        bool blocked = false;
        foreach (var hit in Physics2D.BoxCastAll(b.center, b.size * 0.95f, 0f, direction, distance))
        {
            var other = hit.collider;
            if (other == col || other.isTrigger || other.GetComponent<PlayerController>() || other.GetComponent<Enemy>()) continue;
            blocked = true;
            hitDistance = Mathf.Min(hitDistance, hit.distance);
        }
        return blocked;
    }

    public void Collect(PlayerController player)
    {
        if (rising) return;
        player.PowerUp();
        Destroy(gameObject);
    }
}
