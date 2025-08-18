using UnityEngine;

public class FloatingPowerUp : MonoBehaviour
{
    public float floatAmplitude = 0.3f;  // How far it floats up/down
    public float floatFrequency = 1.2f;  // How fast it floats

    private Vector3 startPos;

    void Awake()
    {
        // Try 3D collider
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
        else
        {
            // Try 2D collider
            Collider2D collider2d = GetComponent<Collider2D>();
            if (collider2d == null)
            {
                collider2d = gameObject.AddComponent<BoxCollider2D>();
            }
            collider2d.isTrigger = true;
        }
    }

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float yOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = startPos + new Vector3(0, yOffset, 0);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            Debug.Log("bullet collected");
            Destroy(gameObject);
        }
        else if (other.CompareTag("Player"))
        {
            Debug.Log("player collected");
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Bullet"))
        {
            Debug.Log("bullet collected");
            Destroy(gameObject);
        }
        else if (other.CompareTag("Player"))
        {
            Debug.Log("player collected");
            Destroy(gameObject);
        }
    }
}