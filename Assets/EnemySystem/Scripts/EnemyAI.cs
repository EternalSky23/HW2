using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("AI Settings")]
    public float detectionRange = 10f;
    public float attackRange = 1.8f;

    [Header("Attack Settings")]
    public float attackCooldown = 2f;
    public float attackDuration = 1f;
    public float attackDamage = 5f;
    public AudioClip hitSFX;

    [Header("Ranged Attack")]
    public bool isRanged = false;
    public GameObject projectilePrefab;

    [Header("Heavy Attack")]
    public bool isHeavy = false;
    public GameObject shockwavePrefab;
    public AudioClip heavyHitSFX;


    private NavMeshAgent agent;
    private Animator animator;

    private float nextAttackTime;
    private float attackEndTime;
    private float nextSearchTime;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // 設定停止追蹤距離
        agent.stoppingDistance = attackRange;
    }

    void Update()
    {
        if (agent == null || animator == null)
            return;

        // 自動尋找玩家
        if (player == null && Time.time >= nextSearchTime)
        {
            GameObject target =
                GameObject.FindGameObjectWithTag("Player");

            if (target != null)
            {
                player = target.transform;
            }

            nextSearchTime = Time.time + 1f;
        }

        // 確認敵人在 NavMesh 上
        if (!agent.isOnNavMesh)
        {
            animator.SetFloat("Speed", 0f);
            return;
        }

        // 沒有玩家時停止移動
        if (player == null)
        {
            StopMoving();
            return;
        }

        // 計算玩家水平距離
        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        float distance = direction.magnitude;

        // 攻擊動畫播放期間停止移動
        if (Time.time < attackEndTime)
        {
            StopMoving();
            FacePlayer(direction);
            return;
        }

        // 超出偵測範圍
        if (distance > detectionRange)
        {
            StopMoving();
            return;
        }

        // 玩家進入攻擊範圍
        if (distance <= attackRange)
        {
            StopMoving();
            FacePlayer(direction);

            if (Time.time >= nextAttackTime)
            {
                // 播放攻擊動畫
                animator.SetTrigger("Attack");

                // 重型敵人：產生衝擊波
                if (isHeavy)
                {
                    CreateShockwave();
                }
                // 遠程敵人：發射魔法彈
                else if (isRanged)
                {
                    FireProjectile(direction);
                }

                attackEndTime =
                    Time.time + attackDuration;

                nextAttackTime =
                    Time.time + attackCooldown;

                if (!isRanged && !isHeavy)
                {
                    GameManager.TakeDamage(attackDamage);
                    if (hitSFX != null)
                    {
                        AudioSource.PlayClipAtPoint(hitSFX, transform.position);
                    }
                }
            }

            return;
        }

        // 使用 NavMeshAgent 追蹤玩家
        agent.isStopped = false;
        agent.SetDestination(player.position);

        // 根據速度切換動畫
        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            animator.SetFloat("Speed", 1f);
        }
        else
        {
            animator.SetFloat("Speed", 0f);
        }
    }

    // 停止移動
    void StopMoving()
    {
        agent.isStopped = true;
        animator.SetFloat("Speed", 0f);
    }

    // 面向玩家
    void FacePlayer(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            8f * Time.deltaTime
        );
    }

    // 遠程攻擊：發射魔法彈
    void FireProjectile(Vector3 direction)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning(
                gameObject.name +
                ": Projectile Prefab is not assigned!"
            );

            return;
        }

        Vector3 spawnPosition =
            transform.position
            + Vector3.up * 1.2f
            + direction.normalized * 0.6f;

        GameObject bullet = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

        EnemyProjectile projectile =
            bullet.GetComponent<EnemyProjectile>();

        if (projectile != null)
        {
            projectile.Launch(player);
            projectile.damage = attackDamage;
        }
    }

    // 重型攻擊：產生地面衝擊波
    void CreateShockwave()
    {
        if (shockwavePrefab == null)
        {
            Debug.LogWarning(
                gameObject.name +
                ": Shockwave Prefab is not assigned!"
            );

            return;
        }

        Vector3 spawnPosition =
            transform.position + Vector3.up * 0.05f;

        if (heavyHitSFX != null)
        {
            AudioSource.PlayClipAtPoint(heavyHitSFX, spawnPosition);
        }

        Instantiate(
            shockwavePrefab,
            spawnPosition,
            Quaternion.identity
        );
    }
}