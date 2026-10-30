using UnityEngine;

/// <summary>
/// The writing hand. Its pivot is the pen tip. Snaps toward the target
/// position with a small hop, so every word feels like a pen stroke.
/// </summary>
public class Hand : MonoBehaviour
{
    public float followSpeed = 30f;
    public float hopHeight = 0.015f;
    public float hopSettleSpeed = 25f;

    private Vector3 target;
    private Vector3 position;
    private float hop = 0f;

    void Start()
    {
        position = transform.position;
        target = position;
    }

    public void MoveTo(Vector3 point)
    {
        target = point;
        hop = 1f;
    }

    void Update()
    {
        position = Vector3.Lerp(position, target, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
        hop = Mathf.Lerp(hop, 0f, 1f - Mathf.Exp(-hopSettleSpeed * Time.deltaTime));
        transform.position = position + Vector3.up * hop * hopHeight;
    }
}
