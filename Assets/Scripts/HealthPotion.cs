using UnityEngine;
using TMPro;

public class HealthPotion : MonoBehaviour
{
    public int healAmount = 20;
    private Rigidbody2D rb;

    public float minX = -10f;
    public float maxX = 10f;
    public float spawnHeight = -0.5f;
    
    public GameObject healText;

    AudioManager audioManager;
    private void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0.5f; // Yerçekimi etkisini azalt
            rb.linearDamping = 1f; // Hava direncini artır
        }
        InvokeRepeating("Respawn", 10f, 10f);
    }

    void Respawn() {
        Debug.Log("HealthPotion Respawn");
        float randomX = Random.Range(minX, maxX);
        transform.position = new Vector3(randomX, spawnHeight, 0f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Player")) {
            HealthManager playerHealth = collision.gameObject.GetComponent<HealthManager>();
            playerHealth.Heal(10);
            audioManager.PlaySFX(audioManager.getHealth);
            // Heal yazısı
            GameObject textObj = Instantiate(healText, collision.transform.position + Vector3.up * 1.5f, Quaternion.identity);
            TextMeshProUGUI text = textObj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = "+10";
                text.color = Color.green;
            }
            Destroy(textObj, 1f);
            transform.position = new Vector3(-20f, 1f, 0f);
        }
    }
}