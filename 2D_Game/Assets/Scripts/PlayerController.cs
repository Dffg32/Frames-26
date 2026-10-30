using UnityEngine;
using UnityEngine.InputSystem;

// Arrows / A-D to move, Space / W / Up to jump (hold for a higher jump). Gamepad works too.
public class PlayerController : MonoBehaviour
{
    const float RunSpeed = 7f;
    const float Acceleration = 60f;
    const float JumpSpeed = 18.5f;
    const float StompBounce = 12f;
    const float CoyoteTime = 0.1f;
    const float JumpBuffer = 0.12f;
    const float NormalGravity = 4f;
    const float ShortJumpGravity = 9f;

    Rigidbody2D body;
    Collider2D col;
    SpriteRenderer sprite;
    float lastGroundedTime = -1f, lastJumpPressTime = -1f, walkTimer;
    bool dead, frozen;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (dead || frozen) return;

        float input = 0f;
        bool jumpPressed = false, jumpHeld = false;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) input -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) input += 1f;
            jumpPressed = kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame;
            jumpHeld = kb.spaceKey.isPressed || kb.upArrowKey.isPressed || kb.wKey.isPressed;
        }
        var pad = Gamepad.current;
        if (pad != null)
        {
            input += pad.leftStick.x.ReadValue() + pad.dpad.x.ReadValue();
            jumpPressed |= pad.buttonSouth.wasPressedThisFrame;
            jumpHeld |= pad.buttonSouth.isPressed;
        }
        input = Mathf.Clamp(input, -1f, 1f);

        bool grounded = IsGrounded();
        if (grounded) lastGroundedTime = Time.time;
        if (jumpPressed) lastJumpPressTime = Time.time;

        var v = body.linearVelocity;
        v.x = Mathf.MoveTowards(v.x, input * RunSpeed, Acceleration * Time.deltaTime);
        if (Time.time - lastJumpPressTime < JumpBuffer && Time.time - lastGroundedTime < CoyoteTime)
        {
            v.y = JumpSpeed;
            lastJumpPressTime = lastGroundedTime = -1f;
        }
        body.linearVelocity = v;
        // Releasing jump early pulls the player down faster, giving a shorter hop.
        body.gravityScale = !jumpHeld && v.y > 0f ? ShortJumpGravity : NormalGravity;

        if (input != 0f) sprite.flipX = input < 0f;
        walkTimer = grounded && Mathf.Abs(v.x) > 0.5f ? walkTimer + Time.deltaTime : 0f;
        sprite.sprite = !grounded || (int)(walkTimer * 10f) % 2 == 1 ? PixelArt.HeroWalk : PixelArt.Hero;

        if (transform.position.y < -2f) Die();
    }

    bool IsGrounded()
    {
        if (body.linearVelocity.y > 0.1f) return false;
        var b = col.bounds;
        var hits = Physics2D.OverlapBoxAll(new Vector2(b.center.x, b.min.y - 0.05f), new Vector2(b.size.x * 0.8f, 0.1f), 0f);
        foreach (var hit in hits)
            if (hit != col && !hit.isTrigger) return true;
        return false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (dead || frozen) return;
        var mine = col.bounds;
        var other = collision.collider;

        if (other.TryGetComponent(out Enemy enemy))
        {
            if (mine.min.y > other.bounds.center.y)
            {
                enemy.Squash();
                body.linearVelocity = new Vector2(body.linearVelocity.x, StompBounce);
                Game.Instance.AddScore(100);
            }
            else Die();
        }
        else if (other.TryGetComponent(out BonusBlock block) && mine.max.y <= other.bounds.min.y + 0.15f)
        {
            block.Hit();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (dead || frozen) return;
        if (other.TryGetComponent(out Coin coin)) coin.Collect();
        else if (other.TryGetComponent(out Goal _)) Game.Instance.Win();
    }

    public void Freeze()
    {
        frozen = true;
        body.linearVelocity = Vector2.zero;
        body.bodyType = RigidbodyType2D.Kinematic;
        sprite.sprite = PixelArt.Hero;
    }

    void Die()
    {
        dead = true;
        col.enabled = false;
        body.gravityScale = NormalGravity;
        body.linearVelocity = new Vector2(0f, 14f);
        sprite.flipY = true;
        sprite.sortingOrder = 20;
        Game.Instance.PlayerDied();
    }
}
