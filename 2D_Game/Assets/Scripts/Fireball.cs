using UnityEngine;

// Thrown by the fire hero. Bounces along the ground and defeats any enemy it touches.
// It burns out when it hits a wall, falls off the level or after a few seconds.
public class Fireball : MonoBehaviour
{
    const float Speed = 11f;
    const float Gravity = 50f;
    const float BounceSpeed = 8f;
    const float Lifetime = 3f;

    // How many fireballs are flying right now; the player can only have a couple at once.
    public static int Alive { get; private set; }

    public float Direction = 1f;

    Rigidbody2D body;
    Collider2D col;
    float verticalSpeed = -4f;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        Alive++;
        Destroy(gameObject, Lifetime);
    }

    void OnDestroy() => Alive--;

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        var pos = body.position;

        float dx = Direction * Speed * dt;
        if (Mover.Blocked(col, Vector2.right * Direction, Mathf.Abs(dx), out _))
        {
            Destroy(gameObject);
            return;
        }
        pos.x += dx;

        verticalSpeed -= Gravity * dt;
        float dy = verticalSpeed * dt;
        if (dy < 0f && Mover.Blocked(col, Vector2.down, -dy, out float distance))
        {
            pos.y -= distance;
            verticalSpeed = BounceSpeed;
        }
        else if (dy > 0f && Mover.Blocked(col, Vector2.up, dy, out _)) verticalSpeed = 0f;
        else pos.y += dy;

        body.MovePosition(pos);
        transform.Rotate(0f, 0f, -Direction * 720f * dt);
        if (pos.y < -3f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out Enemy enemy) || enemy.IsDead) return;
        enemy.Kill();
        Game.Instance.AddScore(200);
        Destroy(gameObject);
    }
}
