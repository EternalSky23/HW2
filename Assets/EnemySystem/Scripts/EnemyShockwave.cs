using UnityEngine;

public class EnemyShockwave : MonoBehaviour
{
    [Header("Shockwave Settings")]
    public float radius = 3f;
    public float duration = 0.8f;
    public float damage = 20f;

    private float elapsedTime = 0f;

    void Start()
    {
        // 偵測範圍內的玩家
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            radius
        );

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Debug.Log(
                    "Shockwave hit Player! Damage: " + damage
                );

                // 之後可以在這裡加入玩家扣血程式
                break;
            }
        }

        // 播放完後自動消失
        Destroy(gameObject, Mathf.Max(duration, 0.01f));
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        // 計算擴散進度（0 到 1）
        float progress = Mathf.Clamp01(
            elapsedTime / Mathf.Max(duration, 0.01f)
        );

        // 從小圓盤擴大成半徑 radius 的圓盤
        float size = Mathf.Lerp(
            0.1f,
            radius * 2f,
            progress
        );

        transform.localScale = new Vector3(
            size,
            0.02f,
            size
        );
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            radius
        );
    }
}