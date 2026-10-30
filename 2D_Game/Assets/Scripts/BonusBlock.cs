using System.Collections;
using UnityEngine;

// Hit it from below to pop out a coin. Works once.
public class BonusBlock : MonoBehaviour
{
    bool used;

    public void Hit()
    {
        if (used) return;
        used = true;
        GetComponent<SpriteRenderer>().sprite = PixelArt.UsedBonus;
        Game.Instance.AddCoin();
        StartCoroutine(Bump());
        StartCoroutine(PopCoin());
    }

    IEnumerator Bump()
    {
        var start = transform.position;
        for (float t = 0f; t < 0.15f; t += Time.deltaTime)
        {
            transform.position = start + Vector3.up * Mathf.Sin(t / 0.15f * Mathf.PI) * 0.25f;
            yield return null;
        }
        transform.position = start;
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
