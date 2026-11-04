using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;

    private float currentHealth;
    private bool isDead = false;

    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;
    public void RestoreHealth(float value) { currentHealth = Mathf.Clamp(value, 1, maxHealth); }

    private Animator animator;
    private NavMeshAgent agent;
    private EnemyAI enemyAI;

    void Start()
    {
        currentHealth = maxHealth;

        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        enemyAI = GetComponent<EnemyAI>();
    }

    // 敵人受到傷害
    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0f)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        Debug.Log(gameObject.name + " HP: " + currentHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            // 播放受傷動畫
            animator.SetTrigger("Hit");
        }
    }

    // 敵人死亡
    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log(gameObject.name + " died!");

        GameManager.OnEnemyKilled();

        // 停止 AI
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }

        // 停止尋路
        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }

        // 播放死亡動畫
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);

            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Hit");

            animator.SetTrigger("Die");
        }
        Destroy(gameObject, 1.5f);
    }

    // 測試用：每次造成 25 傷害
    [ContextMenu("Test Damage (25)")]
    void TestDamage()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Please enter Play Mode first.");
            return;
        }

        TakeDamage(25f);
    }
}