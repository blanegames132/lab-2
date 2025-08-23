using UnityEngine;

public class EnemyFlipXOnTimer : MonoBehaviour
{
    [Header("Flip Timing")]
    public float minFlipInterval = 2f;
    public float maxFlipInterval = 4f;

    [Header("Optional: assign the visual child to flip (if mesh/sprite is not on root)")]
    public Transform visualChild;

    [Header("Debug")]
    public bool isFlipped = false; // True if facing left, false if facing right

    private float flipTimer = 0f;
    private float nextFlipTime = 0f;

    void Start()
    {
        SetNextFlipTime();
    }

    void Update()
    {
        flipTimer += Time.deltaTime;
        if (flipTimer >= nextFlipTime)
        {
            FlipX();
            SetNextFlipTime();
        }
    }

    void SetNextFlipTime()
    {
        flipTimer = 0f;
        nextFlipTime = Random.Range(minFlipInterval, maxFlipInterval);
    }

    void FlipX()
    {
        isFlipped = !isFlipped;
        if (visualChild)
        {
            Vector3 scale = visualChild.localScale;
            scale.x = Mathf.Abs(scale.x) * (isFlipped ? -1f : 1f);
            visualChild.localScale = scale;
        }
        else
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (isFlipped ? -1f : 1f);
            transform.localScale = scale;
        }
    }
}