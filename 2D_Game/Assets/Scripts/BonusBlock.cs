using System.Collections;
using UnityEngine;

public enum BlockContents { Coin, Berry, Pepper, Star }

// Hit it from below to get what it holds. Works once.
// A pepper block gives a berry instead if the player is still small.
// A hidden block is invisible and can be walked or jumped through, until the player
// hits it with their head from below: then it appears and works like a normal block.
public class BonusBlock : MonoBehaviour
{
    public BlockContents Contents;
    public bool Hidden;

    bool used;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!Hidden || !other.TryGetComponent(out PlayerController player)) return;
        var col = GetComponent<Collider2D>();
        var block = col.bounds;
        if (!player.IsJumpingUnder(block)) return;

        Hidden = false;
        col.isTrigger = false;
        GetComponent<SpriteRenderer>().enabled = true;
        player.BumpHead(block.min.y);
        Hit(player);
    }

    public void Hit(PlayerController player)
    {
        if (used) return;
        used = true;
        GetComponent<SpriteRenderer>().sprite = PixelArt.UsedBonus;
        Brick.DefeatEnemiesOnTop(transform.position);
        StartCoroutine(Brick.Bump(transform));
        switch (Contents)
        {
            case BlockContents.Coin:
                Game.Instance.AddCoin();
                StartCoroutine(PopCoin());
                break;
            case BlockContents.Berry:
                SpawnPowerUp(PowerUpKind.Berry);
                break;
            case BlockContents.Pepper:
                SpawnPowerUp(player.IsBig ? PowerUpKind.Pepper : PowerUpKind.Berry);
                break;
            case BlockContents.Star:
                SpawnPowerUp(PowerUpKind.Star);
                break;
        }
    }

    void SpawnPowerUp(PowerUpKind kind)
    {
        var go = new GameObject(kind.ToString());
        go.transform.SetParent(transform.parent);
        go.transform.position = transform.position;
        var sprite = kind switch
        {
            PowerUpKind.Pepper => PixelArt.Pepper,
            PowerUpKind.Star => PixelArt.Star,
            _ => PixelArt.Berry,
        };
        // Drawn behind the block so it looks like it comes out of it.
        PixelArt.AddRenderer(go, sprite, -1);
        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.8f, 0.8f);
        box.isTrigger = true;
        go.AddComponent<PowerUp>().Kind = kind;
    }

    IEnumerator PopCoin()
    {
        var coin = new GameObject("CoinPop");
        coin.transform.SetParent(transform.parent);
        PixelArt.AddRenderer(coin, PixelArt.Coin, 3);
        var start = transform.position + Vector3.up;
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            coin.transform.position = start + Vector3.up * Mathf.Sin(t / 0.4f * Mathf.PI) * 1.5f;
            coin.transform.localScale = new Vector3(Mathf.Abs(Mathf.Cos(t * 20f)), 1f, 1f);
            yield return null;
        }
        Destroy(coin);
    }
}
