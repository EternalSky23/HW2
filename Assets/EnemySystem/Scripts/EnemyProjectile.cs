using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 8f;
    public float lifetime = 4f;

    private Rigidbody rb;
    private Vector3 direction;
    private bool launched = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;

        GetComponent<SphereCollider>().isTrigger = true;
    }

    void Start()
    {
        // 避免沒打中玩家的魔法彈一直留在場景
        Destroy(gameObject, lifetime);
    }

    // 設定魔法彈的飛行方向
    public void Launch(Transform target)
    {
        if (target == null)
            return;

        direction = (target.position - transform.position).normalized;

        launched = true;
    }

    void FixedUpdate()
    {
        if (!launched)
            return;

        Vector3 nextPosition =
            rb.position + direction * speed * Time.fixedDeltaTime;

        rb.MovePosition(nextPosition);
    }

    // 魔法彈碰到玩家
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Magic projectile hit Player!");

            Destroy(gameObject);
        }
    }
}