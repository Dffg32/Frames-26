using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PowerLevel { Small, Big, Fire }

// Arrows / A-D to move, Space / W / Up to jump (hold for a higher jump), X / J / Shift to throw fire.
// Gamepad works too (A to jump, X to throw).
// A berry makes the player big: big players can smash stone blocks and survive one hit
// (they shrink back and blink for a moment instead of dying). A pepper adds fireballs.
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
    const float InvincibleTime = 2f;
    const int MaxFireballs = 2;
    // Draws the player (and its collider) a bit smaller than a tile.
    const float Scale = 0.8f;

    Rigidbody2D body;
    CapsuleCollider2D col;
    SpriteRenderer sprite;
    float lastGroundedTime = -1f, lastJumpPressTime = -1f, walkTimer, invincibleUntil;
    bool dead, frozen, transforming;
    PowerLevel level;

    public bool IsBig => level != PowerLevel.Small;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();
        sprite = GetComponent<SpriteRenderer>();
        transform.localScale = Vector3.one * Scale;
        SetLevel(PowerLevel.Small);
    }

    void Update()
    {
        if (dead || frozen || transforming) return;

        float input = 0f;
        bool jumpPressed = false, jumpHeld = false, firePressed = false;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) input -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) input += 1f;
            jumpPressed = kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame;
            jumpHeld = kb.spaceKey.isPressed || kb.upArrowKey.isPressed || kb.wKey.isPressed;
            firePressed = kb.xKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame;
        }
        var pad = Gamepad.current;
        if (pad != null)
        {
            input += pad.leftStick.x.ReadValue() + pad.dpad.x.ReadValue();
            jumpPressed |= pad.buttonSouth.wasPressedThisFrame;
            jumpHeld |= pad.buttonSouth.isPressed;
            firePressed |= pad.buttonWest.wasPressedThisFrame;
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
        sprite.sprite = SpriteFor(level, !grounded || (int)(walkTimer * 10f) % 2 == 1);

        if (firePressed && level == PowerLevel.Fire && Fireball.Alive < MaxFireballs) ThrowFireball();
        if (transform.position.y < -2f) Die();
    }

    static Sprite SpriteFor(PowerLevel level, bool walkFrame) => level switch
    {
        PowerLevel.Fire => walkFrame ? PixelArt.HeroFireWalk : PixelArt.HeroFire,
        PowerLevel.Big => walkFrame ? PixelArt.HeroBigWalk : PixelArt.HeroBig,
        _ => walkFrame ? PixelArt.HeroWalk : PixelArt.Hero,
    };

    void ThrowFireball()
    {
        float direction = sprite.flipX ? -1f : 1f;
        var go = new GameObject("Fireball");
        go.transform.SetParent(transform.parent);
        go.transform.position = transform.position + new Vector3(direction * 0.5f, 0.9f);
        PixelArt.AddRenderer(go, PixelArt.Fireball, 12);
        var fireBody = go.AddComponent<Rigidbody2D>();
        fireBody.bodyType = RigidbodyType2D.Kinematic;
        fireBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.4f, 0.4f);
        box.isTrigger = true;
        go.AddComponent<Fireball>().Direction = direction;
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
            if (enemy.IsDead) return;
            if (mine.min.y > other.bounds.center.y && enemy.CanBeStomped)
            {
                enemy.Stomp();
                body.linearVelocity = new Vector2(body.linearVelocity.x, StompBounce);
                Game.Instance.AddScore(100);
            }
            else Hurt();
        }
        else if (mine.max.y <= other.bounds.min.y + 0.15f)
        {
            if (other.TryGetComponent(out BonusBlock block)) block.Hit(this);
            else if (other.TryGetComponent(out Brick brick)) brick.Hit(IsBig);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (dead || frozen) return;
        if (other.TryGetComponent(out Coin coin)) coin.Collect();
        else if (other.TryGetComponent(out PowerUp powerUp)) powerUp.Collect(this);
        else if (other.TryGetComponent(out Goal _)) Game.Instance.Win();
    }

    public void PowerUp(bool fire)
    {
        Game.Instance.AddScore(1000);
        var target = fire ? PowerLevel.Fire : PowerLevel.Big;
        if (target > level && !transforming) StartCoroutine(ChangeLevel(target));
    }

    void Hurt()
    {
        if (transforming || Time.time < invincibleUntil) return;
        if (level == PowerLevel.Small)
        {
            Die();
            return;
        }
        StartCoroutine(ChangeLevel(PowerLevel.Small));
        StartCoroutine(Invincibility());
    }

    // Pauses the game briefly while the player flickers between the old and new look.
    IEnumerator ChangeLevel(PowerLevel target)
    {
        transforming = true;
        Time.timeScale = 0f;
        var from = level;
        for (int i = 0; i < 9; i++)
        {
            sprite.sprite = SpriteFor(i % 2 == 0 ? target : from, false);
            yield return new WaitForSecondsRealtime(0.08f);
        }
        SetLevel(target);
        Time.timeScale = 1f;
        transforming = false;
    }

    // After shrinking, enemies pass through the blinking player for a moment.
    IEnumerator Invincibility()
    {
        invincibleUntil = Time.time + InvincibleTime;
        var enemies = FindObjectsByType<Enemy>();
        foreach (var e in enemies) Physics2D.IgnoreCollision(col, e.GetComponent<Collider2D>(), true);

        while (Time.time < invincibleUntil && !dead)
        {
            sprite.enabled = !sprite.enabled;
            yield return new WaitForSeconds(0.08f);
        }
        sprite.enabled = true;
        foreach (var e in enemies)
            if (e != null) Physics2D.IgnoreCollision(col, e.GetComponent<Collider2D>(), false);
    }

    void SetLevel(PowerLevel value)
    {
        level = value;
        sprite.sprite = SpriteFor(level, false);
        // The transform sits at the player's feet, so only the collider's height changes.
        col.size = IsBig ? new Vector2(0.8f, 1.9f) : new Vector2(0.7f, 0.94f);
        col.offset = new Vector2(0f, col.size.y / 2f);
    }

    public void Freeze()
    {
        frozen = true;
        body.linearVelocity = Vector2.zero;
        body.bodyType = RigidbodyType2D.Kinematic;
        sprite.sprite = SpriteFor(level, false);
        sprite.enabled = true;
    }

    void Die()
    {
        dead = true;
        Time.timeScale = 1f;
        col.enabled = false;
        body.gravityScale = NormalGravity;
        body.linearVelocity = new Vector2(0f, 14f);
        sprite.flipY = true;
        sprite.sortingOrder = 20;
        sprite.enabled = true;
        Game.Instance.PlayerDied();
    }
}
