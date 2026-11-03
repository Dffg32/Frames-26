using System.Collections;
using UnityEngine;

public enum PowerUpKind
{
    Berry,  // makes the player big; slides along the ground
    Pepper, // gives fireballs; stays where it appeared
    Star,   // a few seconds of invincibility; bounces around
}

// A power-up that rises out of its block, then moves according to its kind,
// turning around at walls and falling off ledges.
public class PowerUp : MonoBehaviour
{
    const float Gravity = 39f;
    const float RiseTime = 0.6f;
    const float StarBounce = 13f;

    public PowerUpKind Kind;

    Rigidbody2D body;
    Collider2D col;
    float direction = 1f, verticalSpeed;
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
        if (rising || Kind == PowerUpKind.Pepper) return;
        float dt = Time.fixedDeltaTime;
        var pos = body.position;

        float dx = direction * (Kind == PowerUpKind.Star ? 4f : 3f) * dt;
        if (Mover.Blocked(col, Vector2.right * direction, Mathf.Abs(dx), out _)) direction = -direction;
        else pos.x += dx;

        verticalSpeed -= Gravity * dt;
        float dy = verticalSpeed * dt;
        if (dy < 0f && Mover.Blocked(col, Vector2.down, -dy, out float distance))
        {
            pos.y -= distance;
            // The star keeps bouncing; the berry just lands.
            verticalSpeed = Kind == PowerUpKind.Star ? StarBounce : 0f;
        }
        else if (dy > 0f && Mover.Blocked(col, Vector2.up, dy, out _)) verticalSpeed = 0f;
        else pos.y += dy;

        body.MovePosition(pos);
        if (pos.y < -3f) Destroy(gameObject);
    }

    public void Collect(PlayerController player)
    {
        if (rising) return;
        player.PowerUp(Kind);
        Destroy(gameObject);
    }
}
