using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 12f;
    private Animator animator;
    private Rigidbody2D rb;
    private Vector2 movement;
    private bool facingRight = true;
    private bool isGrounded = true;
    private bool isAttacking = false;

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
        if (Input.GetKeyDown(KeyCode.W) && isGrounded)
        {
            Jump();
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