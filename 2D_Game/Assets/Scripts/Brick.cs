using System.Collections;
using UnityEngine;

// A stone block. Small players just bump it; big players smash it.
public class Brick : MonoBehaviour
{
    public void Hit(bool canBreak)
    {
        SquashEnemiesOnTop(transform.position);
        if (!canBreak)
        {
            StartCoroutine(Bump(transform));
            return;
        }

        Game.Instance.AddScore(50);
        for (int i = 0; i < 4; i++)
        {
            var bit = new GameObject("BrickBit");
            bit.transform.SetParent(transform.parent);
            bit.transform.position = transform.position + new Vector3(i % 2 == 0 ? -0.25f : 0.25f, i < 2 ? 0.25f : -0.25f);
            PixelArt.AddRenderer(bit, PixelArt.BrickBit, 15);
            var body = bit.AddComponent<Rigidbody2D>();
            body.gravityScale = 4f;
            body.linearVelocity = new Vector2(i % 2 == 0 ? -3f : 3f, i < 2 ? 14f : 10f);
            body.angularVelocity = i % 2 == 0 ? 360f : -360f;
            Destroy(bit, 1.5f);
        }
        Destroy(gameObject);
    }

    // Hitting a block from below defeats any enemy standing on it.
    public static void SquashEnemiesOnTop(Vector2 blockPosition)
    {
        foreach (var hit in Physics2D.OverlapBoxAll(blockPosition + Vector2.up * 0.6f, new Vector2(0.9f, 0.3f), 0f))
            if (hit.TryGetComponent(out Enemy enemy))
            {
                enemy.Squash();
                Game.Instance.AddScore(100);
            }
    }

    public static IEnumerator Bump(Transform block)
    {
        var start = block.position;
        for (float t = 0f; t < 0.15f; t += Time.deltaTime)
        {
            block.position = start + Vector3.up * Mathf.Sin(t / 0.15f * Mathf.PI) * 0.25f;
            yield return null;
        }
        block.position = start;
    }
}
