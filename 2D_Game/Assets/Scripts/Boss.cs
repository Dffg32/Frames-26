using System.Collections;
using UnityEngine;

// The final boss, waiting in a closed room at the end of the castle.
// It cycles through three attacks, each announced by a short wind-up:
//   - Fire: throws fireballs that alternate low (jump over them) and high (stay on the ground);
//     once it has been hurt, the last one flies straight at the player's height.
//   - Slam: leaps at the player and sends a wave of fire both ways along the floor when it lands.
//   - Charge: shakes in place, then runs until it crashes into a wall. It charges at least
//     every third attack.
// Crashing leaves it dazed for a moment. That's the only time it can be hurt: stomp on it
// (or hit it with fireballs). Three hits defeat it, and each one makes it faster and shortens
// the dazed time. Landing on it while it isn't dazed just bounces you off; touching it from the
// side hurts.
public class Boss : MonoBehaviour
{
    public const int MaxHealth = 3;
    public const float Scale = 1.25f;
    public const string Name = "REY GÓLEM";

    public int Health { get; private set; } = MaxHealth;
    public bool Defeated { get; private set; }
    public bool Dazed { get; private set; }

    Rigidbody2D body;
    BoxCollider2D col;
    SpriteRenderer sprite;
    Transform dizzy;
    Vector2 pos;
    float left, right, floorY, facing = -1f, invulnerableUntil;
    int attacksSinceCharge;
    bool hitWhileDazed;

    // 1 at full health, faster after each hit.
    float Speed => 1f + (MaxHealth - Health) * 0.25f;
    float HalfWidth => col.size.x * Scale / 2f;
    PlayerController Player => Game.Instance.Player;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        sprite = GetComponent<SpriteRenderer>();

        // Stars circling its head while it's dazed.
        dizzy = new GameObject("Dizzy").transform;
        dizzy.SetParent(transform, false);
        dizzy.localPosition = new Vector3(0f, 2.15f);
        for (int i = 0; i < 3; i++)
        {
            var star = new GameObject("DizzyStar");
            star.transform.SetParent(dizzy, false);
            star.transform.localScale = Vector3.one * 0.35f;
            PixelArt.AddRenderer(star, PixelArt.Star, 7);
        }
        dizzy.gameObject.SetActive(false);
    }

    // Called when the player walks into the arena and the gate closes behind them.
    // The arena edges are the inner faces of its walls.
    public void Begin(float arenaLeft, float arenaRight)
    {
        left = arenaLeft;
        right = arenaRight;
        pos = body.position;
        floorY = pos.y;
        StartCoroutine(Fight());
    }

    IEnumerator Fight()
    {
        yield return new WaitForSeconds(1.5f);
        while (true)
        {
            FacePlayer();
            yield return new WaitForSeconds(1.1f / Speed);
            int attack = attacksSinceCharge >= 2 ? 2 : Random.Range(0, 3);
            if (attack == 0) yield return StartCoroutine(Fire());
            else if (attack == 1) yield return StartCoroutine(Slam());
            else yield return StartCoroutine(Charge());
        }
    }

    IEnumerator Fire()
    {
        attacksSinceCharge++;
        yield return StartCoroutine(WindUp(0.6f, new Color(1f, 0.6f, 0.3f), 0f));
        int count = 3 + (MaxHealth - Health);
        for (int i = 0; i < count; i++)
        {
            FacePlayer();
            bool aimed = i == count - 1 && Health < MaxHealth;
            float y = aimed ? Player.Bounds.center.y : floorY + (i % 2 == 0 ? 0.45f : 1.95f);
            Projectile.Spawn(transform.parent, PixelArt.Fireball, new Vector2(pos.x + facing * (HalfWidth + 0.3f), y),
                new Vector2(facing * 6.5f * Speed, 0f), new Vector2(0.4f, 0.4f), 1.4f, spin: true);
            yield return new WaitForSeconds(0.7f / Speed);
        }
    }

    IEnumerator Slam()
    {
        attacksSinceCharge++;
        // Crouches before the leap.
        transform.localScale = new Vector3(Scale * 1.1f, Scale * 0.85f, 1f);
        yield return new WaitForSeconds(0.45f / Speed);
        transform.localScale = Vector3.one * Scale;

        float startX = pos.x;
        float targetX = Mathf.Clamp(Player.transform.position.x, left + HalfWidth, right - HalfWidth);
        float duration = 1f / Mathf.Sqrt(Speed);
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            pos = new Vector2(Mathf.Lerp(startX, targetX, k), floorY + 18f * k * (1f - k));   // 4.5 tiles high
            body.MovePosition(pos);
            yield return null;
        }
        pos = new Vector2(targetX, floorY);
        body.MovePosition(pos);

        Game.Instance.Shake(0.35f, 0.25f);
        foreach (float dir in new[] { -1f, 1f })
        {
            var wave = Projectile.Spawn(transform.parent, PixelArt.Shockwave, new Vector2(pos.x + dir * (HalfWidth - 0.2f), floorY + 0.5f),
                new Vector2(dir * 6f * Speed, 0f), new Vector2(0.8f, 0.45f));
            // The flames are drawn in the bottom half of the sprite.
            wave.GetComponent<BoxCollider2D>().offset = new Vector2(0f, -0.27f);
        }
        yield return new WaitForSeconds(0.6f / Speed);
    }

    IEnumerator Charge()
    {
        attacksSinceCharge = 0;
        FacePlayer();
        // Already against the wall it faces (the player is on a plank above it): charge the other way,
        // so it can't end up dazed without having run.
        if ((Wall() - pos.x) * facing < 2f) facing = -facing;
        yield return StartCoroutine(WindUp(0.8f, new Color(1f, 0.4f, 0.4f), 0.08f));

        float speed = 8f * Mathf.Sqrt(Speed);
        float wall = Wall();
        while ((wall - pos.x) * facing > 0f)
        {
            pos.x = Mathf.MoveTowards(pos.x, wall, speed * Time.deltaTime);
            body.MovePosition(pos);
            yield return null;
        }

        // Crashed into the wall: dazed, and open to attack.
        Game.Instance.Shake(0.5f, 0.4f);
        Dazed = true;
        hitWhileDazed = false;
        dizzy.gameObject.SetActive(true);
        float dazedTime = 2.6f - 0.5f * (MaxHealth - Health);   // 2.6, 2.1, 1.6 seconds
        for (float t = 0f; t < dazedTime && !hitWhileDazed; t += Time.deltaTime) yield return null;
        Dazed = false;
        dizzy.gameObject.SetActive(false);
        if (hitWhileDazed) yield return new WaitForSeconds(0.8f);
    }

    // Flickers in a color (and optionally shakes in place) so the player can see what's coming.
    IEnumerator WindUp(float time, Color color, float shake)
    {
        for (float t = 0f; t < time / Speed; t += Time.deltaTime)
        {
            sprite.color = Mathf.Repeat(t * 8f, 1f) < 0.5f ? color : Color.white;
            body.MovePosition(pos + Vector2.right * (shake * Mathf.Sin(t * 60f)));
            yield return null;
        }
        sprite.color = Color.white;
        body.MovePosition(pos);
    }

    IEnumerator Flash(float time)
    {
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            sprite.color = Mathf.Repeat(t * 10f, 1f) < 0.5f ? new Color(1f, 0.3f, 0.3f) : Color.white;
            yield return null;
        }
        sprite.color = Color.white;
    }

    // Where its center stops when it runs into the wall it's facing.
    float Wall() => facing > 0f ? right - HalfWidth : left + HalfWidth;

    void FacePlayer()
    {
        if (Player != null) facing = Player.transform.position.x < pos.x ? -1f : 1f;
    }

    void Update()
    {
        if (!Dazed) return;
        for (int i = 0; i < dizzy.childCount; i++)
        {
            float angle = Time.time * 5f + i * Mathf.PI * 2f / 3f;
            dizzy.GetChild(i).localPosition = new Vector3(Mathf.Cos(angle) * 0.6f, Mathf.Sin(angle) * 0.18f);
        }
    }

    // The player is touching the boss.
    public void Touch(PlayerController player)
    {
        if (Defeated) return;
        bool fromAbove = player.Bounds.min.y > col.bounds.center.y + col.bounds.extents.y * 0.4f;
        if (fromAbove)
        {
            player.Bounce();
            if (Dazed) TakeHit();
        }
        else if (!Dazed) player.Hurt();
    }

    // Fireballs bounce off its stone skin unless it's dazed.
    public void HitByFireball()
    {
        if (Dazed) TakeHit();
    }

    void TakeHit()
    {
        if (Defeated || Time.time < invulnerableUntil) return;
        invulnerableUntil = Time.time + 1f;
        hitWhileDazed = true;
        Health--;
        Game.Instance.AddScore(1000);
        Game.Instance.Shake(0.3f, 0.3f);
        if (Health > 0)
        {
            StartCoroutine(Flash(0.8f));
            return;
        }

        Defeated = true;
        Dazed = false;
        StopAllCoroutines();
        dizzy.gameObject.SetActive(false);
        col.enabled = false;
        StartCoroutine(Fall());
    }

    // Flashes, flips over and falls through the floor; then the gem appears.
    IEnumerator Fall()
    {
        Game.Instance.Shake(1f, 0.3f);
        yield return StartCoroutine(Flash(1f));
        sprite.flipY = true;
        sprite.sortingOrder = 20;
        // The sprite is anchored at its feet, so flipping it would draw it under the floor.
        pos.y += 2f * Scale;
        float verticalSpeed = 8f;
        while (pos.y > floorY - 12f)
        {
            verticalSpeed -= 30f * Time.deltaTime;
            pos.y += verticalSpeed * Time.deltaTime;
            body.MovePosition(pos);
            yield return null;
        }
        Game.Instance.BossDefeated(new Vector2((left + right) / 2f, floorY));
        Destroy(gameObject);
    }
}
