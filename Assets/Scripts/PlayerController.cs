using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;
    public float attackRange = 1.5f;
    public Transform bot;
    public GameObject damageText;

    private Animator animator;
    private Rigidbody2D rb;
    private Vector2 movement;
    private bool facingRight = true;
    private bool isGrounded = true;
    public bool isAttacking = false;
    private HealthManager playerHealth;
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;

    public GameObject wizardProjectilePrefab;
    public GameObject wizardStunObjectPrefab;

    AudioManager audioManager;

    public float dashSpeed = 10f;
    public float dashDuration = 0.5f;
    private bool isDashing = false;
    private float dashTimeLeft;

    public bool isStunning = false;
    public bool isStunned = false;
    public bool isDefending = false;

    private void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<HealthManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isDead) return;

        if (isStunned)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Hareket animasyonu sadece saldırı ve savunma yokken güncellensin
        if (!isAttacking && !isDefending)
        {
            animator.SetFloat("Speed", Mathf.Abs(movement.x));
            animator.SetFloat("VerticalSpeed", Mathf.Abs(movement.y));
        }

        // Zıplama (sadece yere değdiğinde)
        if (Input.GetKeyDown(KeyCode.W) && isGrounded)
        {
            audioManager.PlaySFX(audioManager.jump);
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
            audioManager.PlaySFX(audioManager.walk);
        }

        // Oyuncu canını kontrol et
        if(playerHealth.currentHealth <= 0 && !isDead)
        {
            isDead = true;
            animator.SetTrigger("DeathAnim");
            StartCoroutine(DeathSequence());
            audioManager.PlaySFX(audioManager.ninjaDeath);
        }

        // Dash, Stun ve Savunma mekanizmaları
        if (CharacterSelection.Instance.selectedCharacter == "Ninja_Player" && Input.GetKeyDown(KeyCode.X) && isGrounded && !isDashing)
        {
            StartDash();
        }
        else if (CharacterSelection.Instance.selectedCharacter == "Wizard_Player" && Input.GetKeyDown(KeyCode.X))
        {
            StartStun();
        }
        else if (CharacterSelection.Instance.selectedCharacter == "Warrior_Player" && Input.GetKeyDown(KeyCode.X))
        {
            StartDefense();
        }

        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0)
            {
                EndDash();
            }
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
        yield return new WaitForSeconds(deathAnimDuration + 1f);

        // Sahneyi yükle
        GameResult.playerWon = false;
        SceneManager.LoadScene("EndGameScene");
    }

    void FixedUpdate()
    {
        if (isStunned)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Dash sırasında normal hareketi devre dışı bırak
        if (!isDashing)
        {
            rb.linearVelocity = new Vector2(movement.x * speed, rb.linearVelocity.y);
        }
    }

    void Jump()
    {      
        animator.SetBool("isJumping", true);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        isGrounded = false;
    }

    void Attack()
    {
        if (isAttacking) return;

        string selected = CharacterSelection.Instance.selectedCharacter;
        if (selected == "Wizard_Player" && wizardProjectilePrefab != null)
        {
            if (!isAttacking)
            {
                audioManager.PlaySFX(audioManager.wizardAttack); // Wizard ses efekti
                isAttacking = true;
                animator.SetBool("isAttacking", true);

                float attackDuration = GetAttackAnimationLength();
                Invoke("ResetWizardAttack", attackDuration);

                Invoke("SpawnProjectile", 0.5f);
            }
        }
        else if (selected == "Ninja_Player")
        {
            if (!isAttacking)
            {
                audioManager.PlaySFX(audioManager.ninjaAttack); // Ninja ses efekti
                isAttacking = true;
                animator.SetBool("isAttacking", true);
                float attackDuration = GetAttackAnimationLength();
                Invoke("ResetAttack", attackDuration);
            }
        }
        else if (selected == "Warrior_Player")
        {
            if (!isAttacking)
            {
                audioManager.PlaySFX(audioManager.warriorAttack); // Warrior ses efekti
                isAttacking = true;
                animator.SetBool("isAttacking", true);
                float attackDuration = GetAttackAnimationLength();
                Invoke("ResetAttack", attackDuration);
            }
        }
    }

    void SpawnProjectile()
    {
        float direction = transform.localScale.x > 0 ? 1f : -1f;
        Vector3 spawnOffset = new Vector3(direction * 0.75f, 0.5f, 0f);
        Vector3 spawnPos = transform.position + spawnOffset;

        GameObject projectile = Instantiate(wizardProjectilePrefab, spawnPos, Quaternion.identity);
        projectile.GetComponent<WizardProjectile>().Initialize(direction);
    }

    void ResetWizardAttack()
    {
        isAttacking = false;
        animator.SetBool("isAttacking", false);
    }

    void ResetAttack()
    {
        // Oyuncuya mesafe kontrolü
        float distanceToBot = Vector2.Distance(transform.position, bot.position);
        if (distanceToBot <= attackRange)
        {
            if(bot.GetComponent<BotAI>().isDefending) {
                GameObject textObj = Instantiate(damageText, bot.position + Vector3.up * 1.5f, Quaternion.identity);
                TextMeshProUGUI text = textObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = "Blocked";
                    text.color = Color.red;
                }
                Destroy(textObj, 1f);
            } else {
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
            if (clip.name == "AttackAnim1" || clip.name == "Wizard_AttackAnim" || clip.name == "Warrior_Attack1")
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

    void StartDash()
    {
        isDashing = true;
        dashTimeLeft = dashDuration;
        rb.linearVelocity = new Vector2((facingRight ? 1 : -1) * dashSpeed, rb.linearVelocity.y);
        rb.gravityScale = 0f; // Dash sırasında yerçekimini devre dışı bırak
        audioManager.PlaySFX(audioManager.dash);

        // Dash efektleri
        Color dashColor = spriteRenderer.color;
        dashColor.a = 0.5f; // Yarı şeffaf
        spriteRenderer.color = dashColor;

        // Collision'ları devre dışı bırak
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        GetComponent<Collider2D>().isTrigger = true;
    }

    void EndDash()
    {
        isDashing = false;
        rb.gravityScale = 1f; // Yerçekimini geri aç
        rb.linearVelocity = new Vector2(movement.x * speed, rb.linearVelocity.y); // Normal hıza dön
        
        // Dash efektlerini geri al
        Color normalColor = spriteRenderer.color;
        normalColor.a = 1f; // Tam opak
        spriteRenderer.color = normalColor;

        // Collision'ları geri aç
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        GetComponent<Collider2D>().isTrigger = false;
    }

    void StartStun()
    {
        if (isStunning) return;

        float distanceToBot = Vector2.Distance(transform.position, bot.position);
        if (distanceToBot <= 10f)
        {
            isStunning = true;
            
            GameObject stunObject = Instantiate(wizardStunObjectPrefab, bot.position, Quaternion.identity);
            
            BotAI botAI = bot.GetComponent<BotAI>();
            if (botAI != null)
            {
                float stunDuration = 2f; // Varsayılan süre
                Animator stunAnimator = stunObject.GetComponent<Animator>();
                if (stunAnimator != null)
                {
                    AnimationClip[] clips = stunAnimator.runtimeAnimatorController.animationClips;
                    foreach (AnimationClip clip in clips)
                    {
                        if (clip.name == "Wizard_StunAnim")
                        {
                            stunDuration = clip.length;
                            break;
                        }
                    }
                }

                botAI.ApplyStun(stunDuration);
                Destroy(stunObject, stunDuration);
                StartCoroutine(ResetStunFlag(stunDuration));
            }
        }
    }

    private IEnumerator ResetStunFlag(float duration)
    {
        yield return new WaitForSeconds(duration);
        isStunning = false;
    }

    public void ApplyStun(float duration)
    {
        if (!isStunned)
        {
            isStunned = true;
            rb.linearVelocity = Vector2.zero;
            StartCoroutine(EndStun(duration));
        }
    }

    private IEnumerator EndStun(float duration)
    {
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    void StartDefense()
    {
        Debug.Log("StartDefense");
        if (isDefending) return;

        isDefending = true;
        animator.SetBool("isDefending", true);
        animator.SetFloat("Speed", 0); // Hareket animasyonunu durdur
        audioManager.PlaySFX(audioManager.defence);
        // Animasyon süresi kadar bekle ve savunmayı bitir
        float defenseDuration = GetDefenseAnimationLength();
        Invoke("EndDefense", defenseDuration);
    }

    void EndDefense()
    {
        Debug.Log("EndDefense");
        if (!isDefending) return;

        isDefending = false;
        animator.SetBool("isDefending", false);
        
        // Normal hareket animasyonuna geri dön
        animator.SetFloat("Speed", Mathf.Abs(movement.x));
    }

    float GetDefenseAnimationLength()
    {
        // Animator içindeki Warrior_Defense animasyonunun süresini al
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip.name == "Warrior_Defense")
            {
                Debug.Log("GetDefenseAnimationLength");
                return clip.length;
            }
        }
        return 1f; // Eğer animasyon bulunamazsa varsayılan süre
    }
}