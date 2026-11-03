using System.Collections;
using UnityEngine;

public enum BlockContents { Coin, Berry, Pepper }

// Hit it from below to get what it holds. Works once.
// A pepper block gives a berry instead if the player is still small.
public class BonusBlock : MonoBehaviour
{
    public BlockContents Contents;

    bool used;

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
                SpawnPowerUp(false);
                break;
            case BlockContents.Pepper:
                SpawnPowerUp(player.IsBig);
                break;
        }
    }

    void SpawnPowerUp(bool fire)
    {
        var go = new GameObject(fire ? "FirePepper" : "Berry");
        go.transform.SetParent(transform.parent);
        go.transform.position = transform.position;
        // Drawn behind the block so it looks like it comes out of it.
        PixelArt.AddRenderer(go, fire ? PixelArt.Pepper : PixelArt.Berry, -1);
        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.8f, 0.8f);
        box.isTrigger = true;
        go.AddComponent<PowerUp>().Fire = fire;
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
