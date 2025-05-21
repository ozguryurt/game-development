using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class BotAI : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 3f;
    public float attackRange = 1.5f;
    public float chaseRange = 6f;
    public float attackDelay = 1.5f;
    public float decisionInterval = 0.2f;
    public float retreatDistance = 3f;
    public int attackDamage = 15;
    private float lastDefenseTime; // Son savunma zamanını takip etmek için
    private float defenseCooldown = 3f; // Savunma arasındaki bekleme süresi

    public GameObject wizardProjectilePrefab;
    public GameObject wizardStunObjectPrefab;

    private float lastAttackTime;
    private float decisionTimer;
    private bool isAttacking = false;
    private bool isStunned = false;
    public bool isDefending = false;
    private bool isSlowed = false;
    private float originalMoveSpeed;
    private float slowEffectMultiplier = 0.5f; // Hızın yarıya düşmesi
    private Animator animator;
    private bool facingRight = true;
    private Vector2 lastKnownPlayerPosition;
    private float lastPlayerSeenTime;
    private float playerLostTime = 2f;
    private float lastStunTime; // Son stun zamanını takip etmek için
    private float stunCooldown = 8f; // Stun arasındaki bekleme süresi

    // Dash değişkenleri
    public float dashSpeed = 10f;
    public float dashDuration = 0.5f;
    private bool isDashing = false;
    private float dashTimeLeft;
    private SpriteRenderer spriteRenderer;
    private float lastDashTime; // Son dash zamanını takip etmek için
    private float dashCooldown = 3f; // Dash arasındaki bekleme süresi

    private enum BotState { Idle, Chase, Attack, Retreat, Stunned, Dashing }
    private BotState currentState = BotState.Idle;
    public GameObject damageText;
    private HealthManager botHealth;
    private bool isDead = false;
    private Rigidbody2D rb;

    AudioManager audioManager;

    private void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        botHealth = GetComponent<HealthManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        decisionTimer = decisionInterval;
        lastKnownPlayerPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        originalMoveSpeed = moveSpeed;

        if (gameObject.name.Contains("Wizard_Player"))
        {
            attackRange = 4f;
            chaseRange = 8f;
            attackDamage = 12;
        }
    }

    private void Update()
    {
        if (isDead) return;

        if (isStunned)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0)
            {
                EndDash();
            }
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        bool canSeePlayer = CanSeePlayer();

        if (canSeePlayer)
        {
            lastKnownPlayerPosition = player.position;
            lastPlayerSeenTime = Time.time;
        }

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f && !isAttacking && !isDefending)
        {
            DecideNextAction(distance);
            decisionTimer = decisionInterval;
        }

        HandleState(distance);

        // Dash, Slow ve Savunma yeteneklerini kullanma kararı
        if (CharacterSelection.Instance.botCharacter == "Ninja_Player" && !isDashing && !isStunned)
        {
            // Dash kullanma kararı - cooldown kontrolü ve daha düşük ihtimal
            if (distance <= 5f && Time.time - lastDashTime >= dashCooldown && Random.value < 0.05f)
            {
                StartDash();
                lastDashTime = Time.time;
            }
        }
        else if (CharacterSelection.Instance.botCharacter == "Wizard_Player" && !isStunned)
        {
            // Slow kullanma kararı - daha dengeli ve cooldown ile
            if (distance <= 10f && Time.time - lastStunTime >= stunCooldown)
            {
                float slowChance = 0.03f;
                PlayerController playerController = player.GetComponent<PlayerController>();
                if (playerController != null && playerController.isAttacking)
                {
                    slowChance = 0.08f;
                }
                else if (distance <= 5f)
                {
                    slowChance = 0.05f;
                }

                if (Random.value < slowChance)
                {
                    StartSlow();
                    lastStunTime = Time.time;
                }
            }
        }
        else if (CharacterSelection.Instance.botCharacter == "Warrior_Player" && !isDefending)
        {
            // Savunma kullanma kararı - cooldown ve daha düşük ihtimal
            if (distance <= 5f && Time.time - lastDefenseTime >= defenseCooldown && Random.value < 0.05f)
            {
                StartDefense();
                lastDefenseTime = Time.time;
            }
        }

        if (botHealth.currentHealth <= 0 && !isDead)
        {
            isDead = true;
            animator.SetTrigger("DeathAnim");
            StartCoroutine(DeathSequence());
        }
    }

    private bool CanSeePlayer()
    {
        Vector2 directionToPlayer = player.position - transform.position;
        float distance = directionToPlayer.magnitude;
        
        if (distance > chaseRange) return false;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer.normalized, distance);
        return hit.collider != null && hit.collider.transform == player;
    }

    private void DecideNextAction(float distance)
    {
        if (botHealth.currentHealth < botHealth.maxHealth * 0.3f && distance < attackRange * 1.5f)
        {
            currentState = BotState.Retreat;
            return;
        }

        if (distance <= attackRange)
        {
            if (Time.time - lastAttackTime >= attackDelay)
            {

                currentState = BotState.Attack;
                string selectedCharacter = CharacterSelection.Instance.selectedCharacter;

                if (selectedCharacter == "Wizard_Player")
                {
                    audioManager.PlaySFX(audioManager.wizardAttack);
                }
                else if (selectedCharacter == "Ninja_Player")
                {
                    audioManager.PlaySFX(audioManager.ninjaAttack);
                }
                else if (selectedCharacter == "Warrior_Player")
                {
                    audioManager.PlaySFX(audioManager.warriorAttack);
                }
            }
            else
            {
                int rand = Random.Range(0, 100);
                if (rand < 60)
                    currentState = BotState.Retreat;
                else
                    currentState = BotState.Chase;
            }
        }
        else if (distance <= chaseRange)
        {
            currentState = BotState.Chase;
        }
        else
        {
            currentState = BotState.Idle;
        }
    }

    private void HandleState(float distance)
    {
        if (isAttacking || isStunned || isDashing) return;

        switch (currentState)
        {
            case BotState.Idle:
                animator.SetFloat("Speed", 0);
                break;

            case BotState.Chase:
                MoveTowardsPlayer(1);
                break;

            case BotState.Retreat:
                if (distance < retreatDistance)
                    MoveTowardsPlayer(-1);
                else
                    currentState = BotState.Idle;
                break;

            case BotState.Attack:
                if (distance <= attackRange && Time.time - lastAttackTime >= attackDelay)
                {
                    StartCoroutine(AttackRoutine());
                    lastAttackTime = Time.time;
                }
                else
                {
                    animator.SetFloat("Speed", 0);
                }
                break;

            case BotState.Stunned:
                animator.SetFloat("Speed", 0);
                break;

            case BotState.Dashing:
                animator.SetFloat("Speed", 0);
                break;
        }
    }

    private void MoveTowardsPlayer(int directionMultiplier)
    {
        Vector2 dir = (player.position - transform.position).normalized * directionMultiplier;
        
        float slowDownFactor = Mathf.Clamp01(Vector2.Distance(transform.position, player.position) / chaseRange);
        Vector2 movement = dir * moveSpeed * slowDownFactor;

        transform.Translate(movement * Time.deltaTime);
        animator.SetFloat("Speed", Mathf.Abs(movement.x));

        if (dir.x < 0 && facingRight)
            Flip();
        else if (dir.x > 0 && !facingRight)
            Flip();
    }

    private IEnumerator AttackRoutine() {
        string selected = CharacterSelection.Instance.botCharacter;
        if (selected == "Wizard_Player" && wizardProjectilePrefab != null)
        {
            if(!isAttacking) {
                float animDuration = GetAttackAnimationLength("Wizard_AttackAnim");
                float impactTime = animDuration * 0.4f;
                yield return new WaitForSeconds(animDuration - impactTime);
                isAttacking = true;
                animator.SetBool("isAttacking", true);

                float attackDuration = GetAttackAnimationLength();
                Invoke("ResetWizardAttack", attackDuration);

                Invoke("SpawnProjectile", 0.5f);
            }
        } else {
            if(!isAttacking) {
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
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange)
        {
            if(player.GetComponent<PlayerController>().isDefending) {
                GameObject textObj = Instantiate(damageText, player.position + Vector3.up * 1.5f, Quaternion.identity);
                TextMeshProUGUI text = textObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = "Blocked";
                    text.color = Color.red;
                }
                Destroy(textObj, 1f);
            } else {
                HealthManager playerHealth = player.GetComponent<HealthManager>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(20);

                    // Hasar yazısı
                    GameObject textObj = Instantiate(damageText, player.position + Vector3.up * 1.5f, Quaternion.identity);
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

    private IEnumerator DeathSequence()
    {
        //yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        //Debug.Log("Kazandınız!");

        // Aktif animasyon klibini al
        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        float deathAnimDuration = clips[0].clip.length;
        audioManager.PlaySFX(audioManager.ninjaDeath);
        // Animasyon süresi kadar bekle
        yield return new WaitForSeconds(deathAnimDuration + 1f);

        // Sahneyi yükle
        GameResult.playerWon = true;
        SceneManager.LoadScene("EndGameScene");
    }

    private float GetAttackAnimationLength(string animName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 0.6f;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == animName)
                return clip.length;
        }

        return 0.6f;
    }

    private void Flip()
    {
        audioManager.PlaySFX(audioManager.walk);
        facingRight = !facingRight;
        Vector3 newScale = transform.localScale;
        newScale.x *= -1;
        transform.localScale = newScale;
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

    void StartSlow()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= 10f)
        {
            GameObject stunObject = Instantiate(wizardStunObjectPrefab, player.position, Quaternion.identity);
            
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                float slowDuration = 3f;
                Animator stunAnimator = stunObject.GetComponent<Animator>();
                if (stunAnimator != null)
                {
                    AnimationClip[] clips = stunAnimator.runtimeAnimatorController.animationClips;
                    foreach (AnimationClip clip in clips)
                    {
                        if (clip.name == "Wizard_StunAnim")
                        {
                            slowDuration = clip.length;
                            break;
                        }
                    }
                }

                playerController.ApplySlow(slowDuration, slowEffectMultiplier);
                Destroy(stunObject, slowDuration);
            }
        }
    }

    public void ApplySlow(float duration, float slowMultiplier)
    {
        if (!isSlowed)
        {
            isSlowed = true;
            moveSpeed = originalMoveSpeed * slowMultiplier;
            StartCoroutine(EndSlow(duration));
        }
    }

    private IEnumerator EndSlow(float duration)
    {
        yield return new WaitForSeconds(duration);
        isSlowed = false;
        moveSpeed = originalMoveSpeed;
    }

    void StartDash()
    {
        if (isDashing || isStunned) return; // Stun durumunda dash atamaz

        isDashing = true;
        currentState = BotState.Dashing;
        dashTimeLeft = dashDuration;
        
        // Dash efektleri
        Color dashColor = spriteRenderer.color;
        dashColor.a = 0.5f;
        spriteRenderer.color = dashColor;
        spriteRenderer.sortingOrder = 5;

        // Dash hareketi ve çarpışmaları devre dışı bırak
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        Collider2D col = GetComponent<Collider2D>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector2((facingRight ? 1 : -1) * dashSpeed, rb.linearVelocity.y);
            rb.gravityScale = 0f;
            audioManager.PlaySFX(audioManager.dash);
            if (col != null)
            {
                col.isTrigger = true;
            }
        }
    }

    void EndDash()
    {
        if (!isDashing) return; // Eğer zaten dash yapmıyorsa çık

        isDashing = false;
        currentState = BotState.Idle;
        
        // Dash efektlerini geri al
        Color normalColor = spriteRenderer.color;
        normalColor.a = 1f;
        spriteRenderer.color = normalColor;
        spriteRenderer.sortingOrder = 2;

        // Fizik ayarlarını ve çarpışmaları geri al
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        Collider2D col = GetComponent<Collider2D>();
        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.linearVelocity = Vector2.zero;
            if (col != null)
            {
                col.isTrigger = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Dash sırasında duvarlarla çarpışma kontrolü
        if (isDashing && (collision.gameObject.name == "SagDuvar" || collision.gameObject.name == "SolDuvar"))
        {
            EndDash();
        }
    }

    void StartDefense()
    {
        if (isDefending) return;

        isDefending = true;
        animator.SetBool("isDefending", true);
        animator.SetFloat("Speed", 0);
        audioManager.PlaySFX(audioManager.defence);

        // Animasyon süresi kadar bekle ve savunmayı bitir
        float defenseDuration = GetDefenseAnimationLength();
        Invoke("EndDefense", defenseDuration);
    }

    void EndDefense()
    {
        if (!isDefending) return;

        isDefending = false;
        animator.SetBool("isDefending", false);
        
        // Normal hareket animasyonuna geri dön
        animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
    }

    float GetDefenseAnimationLength()
    {
        // Animator içindeki Warrior_Defense animasyonunun süresini al
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip.name == "Warrior_Defense")
            {
                return clip.length;
            }
        }
        return 1f; // Eğer animasyon bulunamazsa varsayılan süre
    }

    private void FixedUpdate()
    {
        if (isStunned)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isDashing)
        {
            return;
        }

        // Normal hareket
        if (currentState == BotState.Chase)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
        }
        else if (currentState == BotState.Retreat)
        {
            Vector2 direction = (transform.position - player.position).normalized;
            rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }
}
