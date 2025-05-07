using UnityEngine;

public class HealthPotion : MonoBehaviour
{
    public int healAmount = 20;

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Potion'a biri çarptý: " + other.name);

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player potion'a çarptý.");
            HealthManager healthManager = other.GetComponent<HealthManager>();
            if (healthManager != null)
            {
                int newHealth = Mathf.Min(healthManager.currentHealth + healAmount, healthManager.maxHealth);
                healthManager.currentHealth = newHealth;
                healthManager.healthbar.SetCurrentHealth(newHealth);
            }

            Destroy(gameObject);
        }
    }

}
