using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public float minSize = 0.5f;
    public float maxSize = 2f;
    public float minSpeed = 50f;
    public float maxSpeed = 150f;
    public float maxSpinSpeed = 10f;
    
    [Header("Difficulty Settings")]
    public float maxObstacleSpeed = 10f; // Il limite massimo di velocità consentito

    Rigidbody2D rb;

    void Start()
    {
        float randomSize = Random.Range(minSize, maxSize);
        transform.localScale = new Vector3(randomSize, randomSize, 1);

        rb = GetComponent<Rigidbody2D>();

        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomSpeed = Random.Range(minSpeed, maxSpeed) / randomSize;
        rb.AddForce(randomDirection * randomSpeed);

        float randomTorque = Random.Range(-maxSpinSpeed, maxSpinSpeed);
        rb.AddTorque(randomTorque);

        Color obstacleColor = PaletteManager.Instance != null
            ? PaletteManager.Instance.ObstacleColor
            : Color.white;

        ProceduralShape ps = gameObject.GetComponent<ProceduralShape>()
                          ?? gameObject.AddComponent<ProceduralShape>();
        ps.Initialize(ProceduralShape.RandomPolygon(), obstacleColor);
    }

    // Usiamo FixedUpdate perché lavoriamo con la fisica (Rigidbody2D)
    void FixedUpdate()
    {
        // 4. Controllo della velocità massima (Clamping)
        // Utile se usi un Physics Material con Bounciness > 1
        if (rb != null && rb.linearVelocity.magnitude > maxObstacleSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxObstacleSpeed;
        }
    }
}