using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject obstaclePrefab; 
    public float spawnRadius = 18f;   
    public float spawnInterval = 1.5f; 
    public float minSpawnInterval = 0.4f; 
    public float difficultyIncrease = 0.01f; 

    [Header("Obstacle Physics")]
    public float minSpeed = 3f;
    public float maxSpeed = 7f;
    public float maxRotationSpeed = 200f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        // Difficoltà progressiva
        if (spawnInterval > minSpawnInterval)
        {
            spawnInterval -= difficultyIncrease * Time.deltaTime;
        }

        if (timer >= spawnInterval)
        {
            SpawnNewObstacle();
            timer = 0;
        }
    }

    void SpawnNewObstacle()
    {
        if (obstaclePrefab == null) return;

        Vector2 spawnPos = Random.insideUnitCircle.normalized * spawnRadius;
        GameObject obs = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);

        Rigidbody2D rbObstacle = obs.GetComponent<Rigidbody2D>();
        if (rbObstacle != null)
        {
            Vector2 direction = -spawnPos.normalized; 
            rbObstacle.linearVelocity = direction * Random.Range(minSpeed, maxSpeed); 
            rbObstacle.angularVelocity = Random.Range(-maxRotationSpeed, maxRotationSpeed);
        }

        Destroy(obs, 12f);
    }
}