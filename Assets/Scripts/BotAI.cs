using UnityEngine;
using System.Collections;
using TMPro;

public class BotAI : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 3f;
    public float attackRange = 3f;
    public float chaseRange = 6f;
    public float attackDelay = 2f;
    public float decisionInterval = 1.5f;
    public float retreatDistance = 3f;

    private float lastAttackTime;
    private float decisionTimer;
    private bool isAttacking = false;
    private Animator animator;
    private bool facingRight = true;

    private enum BotState { Idle, Chase, Attack, Retreat }
    private BotState currentState = BotState.Idle;
    public GameObject damageText;

    private void Start()
    {
        animator = GetComponent<Animator>();
        decisionTimer = decisionInterval;

        // E�er bu bot bir b�y�c� ise (Wizard_Player), menzil daha fazla olsun
        if (gameObject.name.Contains("Wizard_Player"))
        {
            attackRange = 6f;   // normalde 3f idi
            chaseRange = 8f;    // normalde 6f idi
        }
    }

    private void Update()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        // Karar verme s�resi dolunca yeni davran�� belirle
        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f && !isAttacking)
        {
            DecideNextAction(distance);
            decisionTimer = decisionInterval;
        }

        HandleState(distance);
    }

    private void DecideNextAction(float distance)
    {
        if (distance <= attackRange)
        {
            int rand = Random.Range(0, 100);
            if (rand < 70)
                currentState = BotState.Attack;
            else
                currentState = BotState.Retreat;
        }
        else if (distance <= chaseRange)
        {
            int rand = Random.Range(0, 100);
            if (rand < 80)
                currentState = BotState.Chase;
            else
                currentState = BotState.Idle;
        }
        else
        {
            currentState = BotState.Idle;
        }
    }

    private void HandleState(float distance)
    {
        if (isAttacking)
            return;

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

        // Yak�nsa yava�lat
        float slowDownFactor = Mathf.Clamp01(Vector2.Distance(transform.position, player.position) / chaseRange);
        Vector2 movement = dir * moveSpeed * slowDownFactor;

        transform.Translate(movement * Time.deltaTime);

        animator.SetFloat("Speed", Mathf.Abs(movement.x));

        if (dir.x < 0 && facingRight)
            Flip();
        else if (dir.x > 0 && !facingRight)
            Flip();
    }

    private float GetAttackAnimationLength(string animName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 0.6f; // Varsay�lan

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == animName)
                return clip.length;
        }

        return 0.6f; // Animasyon bulunamazsa varsay�lan
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        animator.SetBool("isAttacking", true);
        animator.SetFloat("Speed", 0);

        float animDuration = GetAttackAnimationLength("Wizard_AttackAnim");
        float impactTime = animDuration * 0.4f; // Darbenin ortas� gibi bir zamanlama

        yield return new WaitForSeconds(impactTime);

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange)
        {
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
                }
                Destroy(textObj, 1f);

                // Oyuncunun canını kontrol et
                if (playerHealth.currentHealth <= 0)
                {
                    Animator playerAnimator = player.GetComponent<Animator>();
                    if (playerAnimator != null)
                    {
                        playerAnimator.SetTrigger("DeathAnim");
                    }
                }
            }
        }

        yield return new WaitForSeconds(animDuration - impactTime);
        animator.SetBool("isAttacking", false);
        isAttacking = false;
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 newScale = transform.localScale;
        newScale.x *= -1;
        transform.localScale = newScale;
    }
}
