using System.Collections;
using UnityEngine;

// A power-up that rises out of its block.
// The grow berry then slides along the ground, bouncing off walls and falling off ledges.
// The fire pepper stays where it appeared.
public class PowerUp : MonoBehaviour
{
    const float Speed = 3f;
    const float Gravity = 39f;
    const float RiseTime = 0.6f;

    public bool Fire;

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
        if (rising || Fire) return;
        float dt = Time.fixedDeltaTime;
        var pos = body.position;

        float dx = direction * Speed * dt;
        if (Mover.Blocked(col, Vector2.right * direction, Mathf.Abs(dx), out _)) direction = -direction;
        else pos.x += dx;

        fallSpeed += Gravity * dt;
        float dy = fallSpeed * dt;
        if (Mover.Blocked(col, Vector2.down, dy, out float distance))
        {
            pos.y -= distance;
            fallSpeed = 0f;
        }
        else pos.y -= dy;

        body.MovePosition(pos);
        if (pos.y < -3f) Destroy(gameObject);
    }

    public void Collect(PlayerController player)
    {
        if (rising) return;
        player.PowerUp(Fire);
        Destroy(gameObject);
    }
}
