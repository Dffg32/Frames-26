using UnityEngine;

// The gem at the end of the level. Touch it to win.
public class Goal : MonoBehaviour
{
    Vector3 start;

    void Start() => start = transform.position;

    void Update() => transform.position = start + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.2f;
}
