using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;
    public float attackRange = 1.5f;
    private Animator animator;
    private Rigidbody2D rb;
    private Vector2 movement;
    private bool facingRight = true;
    private bool isGrounded = true;
    private bool isAttacking = false;
    public Transform bot;
    public GameObject damageText;
    private HealthManager playerHealth;
    private bool isDead = false;
    private float lastNinjaJumpTime = 0f;
    private int ninjaJumpCount = 0;
    private const int MAX_NINJA_JUMPS = 1;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<HealthManager>();
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
                ninjaJumpCount = 0; // Yere değdiğinde zıplayış sayısını sıfırla
            }
            else if (!isGrounded && ninjaJumpCount < MAX_NINJA_JUMPS && Time.time - lastNinjaJumpTime >= 5f)
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

        // Oyuncu canını kontrol et
        if(playerHealth.currentHealth <= 0 && !isDead)
        {
            isDead = true;
            animator.SetTrigger("DeathAnim");
            StartCoroutine(DeathSequence());
        }
    }
    private IEnumerator DeathSequence()
    {
        //yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        //Debug.Log("Kaybettiniz!");

        // Aktif animasyon klibini al
        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        float deathAnimDuration = clips[0].clip.length;

        // Animasyon süresi kadar bekle
        yield return new WaitForSeconds(deathAnimDuration);

        // Sahneyi yükle
        GameResult.playerWon = false;
        SceneManager.LoadScene("EndGameScene");
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
        lastNinjaJumpTime = Time.time;
        ninjaJumpCount++;
        animator.SetBool("isJumping", true);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 1.5f);
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
                    text.color = Color.red;
                }
                Destroy(textObj, 1f);
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
            ninjaJumpCount = 0; // Yere değdiğinde zıplayış sayısını sıfırla
        }
    }
}