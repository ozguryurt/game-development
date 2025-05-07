using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 12f;
    public float attackRange = 1.5f;
    private Animator animator;
    private Rigidbody2D rb;
    private Vector2 movement;
    private bool facingRight = true;
    private bool isGrounded = true;
    private bool isAttacking = false;
    public Transform bot;
    public GameObject damageText;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Hareket animasyonu sadece saldırı yokken güncellensin
        if (!isAttacking)
        {
            animator.SetFloat("Speed", Mathf.Abs(movement.x));
            animator.SetFloat("VerticalSpeed", Mathf.Abs(movement.y));
        }

        // Zıplama (sadece yere değdiğinde)
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (isGrounded)
            {
                Jump();
            }
            else if (!isGrounded)
            {
                JumpNinja();
            }
        }

        // Saldırı (sadece yere değdiğinde çalışsın)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isAttacking)
        {
            Attack();
        }

        // Yön değiştirme
        if ((movement.x > 0 && !facingRight) || (movement.x < 0 && facingRight))
        {
            Flip();
        }
    }

    void FixedUpdate()
    {
        // Saldırı sırasında bile karakter hareket edebilsin
        rb.linearVelocity = new Vector2(movement.x * speed, rb.linearVelocity.y);
    }

    void Jump()
    {
        animator.SetBool("isJumping", true);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        isGrounded = false;
    }

    void JumpNinja()
    {
        // Burada JumpWarrior fonksiyonunun içeriği olacak
        animator.SetBool("isJumping", true);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 1.5f); // Normal zıplamadan biraz daha güçlü yapabiliriz
        isGrounded = false;
    }

    void Attack()
    {
        if(!isAttacking) {
            isAttacking = true;
            animator.SetBool("isAttacking", true);

            // AttackAnim1 animasyon süresini al
            float attackDuration = GetAttackAnimationLength();
            
            // Saldırı bitince durumu sıfırla
            Invoke("ResetAttack", attackDuration);
        }
    }

    void ResetAttack()
    {
        // Oyuncuya mesafe kontrolü
        float distanceToBot = Vector2.Distance(transform.position, bot.position);
        if (distanceToBot <= attackRange)
        {
            HealthManager botHealth = bot.GetComponent<HealthManager>();
            if (botHealth != null)
            {
                botHealth.TakeDamage(20);

                // Hasar yazısı
                GameObject textObj = Instantiate(damageText, bot.position + Vector3.up * 1.5f, Quaternion.identity);
                TextMeshProUGUI text = textObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = "-20";
                }
                Destroy(textObj, 1f);

                // Botun canını kontrol et
                if (botHealth.currentHealth <= 0)
                {
                    Animator botAnimator = bot.GetComponent<Animator>();
                    if (botAnimator != null)
                    {
                        botAnimator.SetTrigger("Wizard_DeathAnim");
                    }
                }
            }
        }

        isAttacking = false;
        animator.SetBool("isAttacking", false);
    }

    float GetAttackAnimationLength()
    {
        // Animator içindeki AttackAnim1'in süresini al
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip.name == "AttackAnim1" || clip.name == "Wizard_AttackAnim" || clip.name == "Warrior_Attack")
            {
                return clip.length;
            }
        }
        return 0.5f; // Eğer animasyon bulunamazsa varsayılan süre
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 newScale = transform.localScale;
        newScale.x *= -1;
        transform.localScale = newScale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            animator.SetBool("isJumping", false);
        }
    }
}