using UnityEngine;

public class Coin : MonoBehaviour
{
    void Update()
    {
        // Fake a spin by squeezing the sprite horizontally.
        transform.localScale = new Vector3(Mathf.Max(0.15f, Mathf.Abs(Mathf.Cos(Time.time * 3f + transform.position.x))), 1f, 1f);
    }

    public void Collect()
    {
        Game.Instance.AddCoin();
        Destroy(gameObject);
    }
}
