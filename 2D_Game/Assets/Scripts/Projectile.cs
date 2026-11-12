using UnityEngine;

// Flies in a straight horizontal line and hurts the player on touch.
// Burns out when it hits a wall or block, or after a few seconds.
public class Projectile : MonoBehaviour
{
    const float Lifetime = 5f;

    public Vector2 Velocity;
    public bool Spin;

    Rigidbody2D body;
    Collider2D col;

    public static Projectile Spawn(Transform parent, Sprite sprite, Vector2 position, Vector2 velocity,
        Vector2 colliderSize, float scale = 1f, bool spin = false)
    {
        var go = new GameObject("Projectile");
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * scale;
        PixelArt.AddRenderer(go, sprite, 12).flipX = velocity.x > 0f;
        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = colliderSize;
        box.isTrigger = true;
        go.AddComponent<Hazard>();
        var projectile = go.AddComponent<Projectile>();
        projectile.Velocity = velocity;
        projectile.Spin = spin;
        return projectile;
    }

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        Destroy(gameObject, Lifetime);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        float dx = Velocity.x * dt;
        if (Mover.Blocked(col, Vector2.right * Mathf.Sign(dx), Mathf.Abs(dx), out _))
        {
            Destroy(gameObject);
            return;
        }
        body.MovePosition(body.position + Velocity * dt);
        if (Spin) transform.Rotate(0f, 0f, -Mathf.Sign(Velocity.x) * 540f * dt);
    }
}
