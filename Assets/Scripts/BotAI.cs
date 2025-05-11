using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class BotAI : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 3f;
    public float attackRange = 2.5f;
    public float chaseRange = 6f;
    public float attackDelay = 1.5f;
    public float decisionInterval = 0.2f;
    public float retreatDistance = 3f;
    public int attackDamage = 15;
    public float stunDuration = 0.5f;

    private float lastAttackTime;
    private float decisionTimer;
    private bool isAttacking = false;
    private bool isStunned = false;
    private Animator animator;
    private bool facingRight = true;
    private Vector2 lastKnownPlayerPosition;
    private float lastPlayerSeenTime;
    private float playerLostTime = 2f;

    private enum BotState { Idle, Chase, Attack, Retreat }
    private BotState currentState = BotState.Idle;
    public GameObject damageText;
    private HealthManager botHealth;
    private bool isDead = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
        botHealth = GetComponent<HealthManager>();
        decisionTimer = decisionInterval;
        lastKnownPlayerPosition = transform.position;

        if (gameObject.name.Contains("Wizard_Player"))
        {
            attackRange = 4f;
            chaseRange = 8f;
            attackDamage = 12;
        }
    }

    private void Update()
    {
        if (isDead || isStunned) return;

        float distance = Vector2.Distance(transform.position, player.position);
        bool canSeePlayer = CanSeePlayer();

        if (canSeePlayer)
        {
            lastKnownPlayerPosition = player.position;
            lastPlayerSeenTime = Time.time;
        }

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f && !isAttacking)
        {
            DecideNextAction(distance);
            decisionTimer = decisionInterval;
        }

        HandleState(distance);

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
        if (isAttacking) return;

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

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        animator.SetBool("isAttacking", true);
        animator.SetFloat("Speed", 0);

        float animDuration = GetAttackAnimationLength("Wizard_AttackAnim");
        float impactTime = animDuration * 0.4f;

        yield return new WaitForSeconds(impactTime);

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange)
        {
            HealthManager playerHealth = player.GetComponent<HealthManager>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                ShowDamageText(attackDamage);
            }
        }

        yield return new WaitForSeconds(animDuration - impactTime);
        animator.SetBool("isAttacking", false);
        isAttacking = false;
    }

    private void ShowDamageText(int damage)
    {
        GameObject textObj = Instantiate(damageText, player.position + Vector3.up * 1.5f, Quaternion.identity);
        TextMeshProUGUI text = textObj.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = "-" + damage.ToString();
            text.color = Color.red;
        }
        Destroy(textObj, 1f);
    }

    public void Stun()
    {
        StartCoroutine(StunRoutine());
    }

    private IEnumerator StunRoutine()
    {
        isStunned = true;
        animator.SetFloat("Speed", 0);
        yield return new WaitForSeconds(stunDuration);
        isStunned = false;
    }

    private IEnumerator DeathSequence()
    {
        //yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        //Debug.Log("Kazandınız!");

        // Aktif animasyon klibini al
        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        float deathAnimDuration = clips[0].clip.length;

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
        facingRight = !facingRight;
        Vector3 newScale = transform.localScale;
        newScale.x *= -1;
        transform.localScale = newScale;
    }
}
