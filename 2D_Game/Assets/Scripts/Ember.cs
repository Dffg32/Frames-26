using UnityEngine;

// A spark that hides in a lava pit and leaps out every couple of seconds.
// It can't be defeated: wait for it to drop back before jumping over the pit.
public class Ember : MonoBehaviour
{
    const float Gravity = 30f;
    const float JumpSpeed = 13f;   // about 2.8 tiles high
    const float Rest = 2.2f;
    const float WakeDistance = 15f;

    Rigidbody2D body;
    SpriteRenderer sprite;
    float restY, verticalSpeed, wait;
    bool jumping;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        restY = body.position.y;
        // Sparks in different pits don't all jump at the same moment.
        wait = 0.5f + Mathf.Repeat(body.position.x * 0.37f, 1.5f);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        var pos = body.position;
        if (!jumping)
        {
            var cam = Camera.main;
            if (cam != null && Mathf.Abs(pos.x - cam.transform.position.x) > WakeDistance) return;
            wait -= dt;
            if (wait > 0f) return;
            jumping = true;
            verticalSpeed = JumpSpeed;
        }

        verticalSpeed -= Gravity * dt;
        pos.y += verticalSpeed * dt;
        sprite.flipY = verticalSpeed < 0f;
        if (pos.y <= restY)
        {
            pos.y = restY;
            jumping = false;
            wait = Rest;
        }
        body.MovePosition(pos);
    }
}
