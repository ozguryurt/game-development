using UnityEngine;
using System.Collections;
using TMPro;

public class WizardProjectile : MonoBehaviour
{
    public float speed = 5f;
    public int damage = 20;
    public GameObject explosionEffect;
    public GameObject damageText;

    private float direction;

    public void Initialize(float dir)
    {
        direction = dir;
        // Gerekirse scale ayarla
        Vector3 scale = transform.localScale;
        scale.x *= dir > 0 ? 1 : -1;
        transform.localScale = scale;
    }

    void Update()
    {
        transform.Translate(Vector2.right * direction * speed * Time.deltaTime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            HealthManager hm = collision.gameObject.GetComponent<HealthManager>();
            if (hm != null)
            {
                hm.TakeDamage(damage);
                // Hasar yazısı
                GameObject textObj = Instantiate(damageText, collision.transform.position + Vector3.up * 1.5f, Quaternion.identity);
                TextMeshProUGUI text = textObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = "-" + damage.ToString();
                    text.color = Color.red;
                }
                Destroy(textObj, 1f);
            }
            Explode();
        }
        else if (!collision.gameObject.CompareTag("Wizard_Projectile"))
        {
            Explode();
        }
    }

    void Explode()
    {
        if (explosionEffect != null)
        {
            GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            StartCoroutine(MoveExplosionAfterDelay(explosion, 1f));
        }

        Destroy(gameObject);
    }

    IEnumerator MoveExplosionAfterDelay(GameObject explosion, float delay)
    {
        yield return new WaitForSeconds(delay);
        explosion.transform.position = new Vector3(-10f, -7f, 0f);
    }
}