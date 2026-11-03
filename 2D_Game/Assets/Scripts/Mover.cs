using UnityEngine;

// Shared tile collision for things that move themselves (power-ups, fireballs).
public static class Mover
{
    const float Skin = 0.02f;

    // Casts the collider's box and reports the distance to the nearest solid tile in that direction.
    // The box is trimmed across the direction of travel, so the floor it rests on never counts
    // as a wall and a wall it slides along never counts as floor.
    public static bool Blocked(Collider2D col, Vector2 direction, float distance, out float hitDistance)
    {
        var b = col.bounds;
        bool horizontal = direction.y == 0f;
        var size = horizontal
            ? new Vector2(b.size.x - Skin * 2f, b.size.y * 0.6f)
            : new Vector2(b.size.x * 0.6f, b.size.y - Skin * 2f);

        hitDistance = distance;
        bool blocked = false;
        foreach (var hit in Physics2D.BoxCastAll(b.center, size, 0f, direction, distance + Skin))
        {
            var other = hit.collider;
            if (other == col || other.isTrigger || other.GetComponent<PlayerController>() || other.GetComponent<Enemy>()) continue;
            blocked = true;
            hitDistance = Mathf.Min(hitDistance, Mathf.Max(0f, hit.distance - Skin));
        }
        return blocked;
    }
}
