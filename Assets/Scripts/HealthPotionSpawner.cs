using UnityEngine;

public class HealthPotionSpawner : MonoBehaviour
{
    public GameObject healthPotionInScene; // Sahnedeki orijinal potion
    public float spawnInterval = 5f;
    public float minX = -8f;
    public float maxX = 8f;
    public float groundY = -3.5f;

    void Start()
    {
        InvokeRepeating(nameof(SpawnPotion), spawnInterval, spawnInterval);
    }

    void SpawnPotion()
    {
        Vector2 spawnPosition = new Vector2(Random.Range(minX, maxX), groundY);
        Instantiate(healthPotionInScene, spawnPosition, Quaternion.identity);
    }
}
