using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Vector3 direction;
    public float speed = 100f;
    public float timeAlive = 5f;
    public float damage;
    public GameObject particleEffect;
    public AudioClip hitSFX;
    private float destroyTime;
    private bool hit;
    private void Start() { destroyTime = Time.time + timeAlive; }
    private void Update()
    {
        if (hit) return;
        if (Time.time >= destroyTime) { Destroy(gameObject); return; }
        Vector3 displacement = direction * speed * Time.deltaTime;
        // Sweep the movement to avoid tunnelling through a crate at high speed.
        Collider closest = null;
        float distance = float.PositiveInfinity;
        foreach (var collision in Physics.RaycastAll(transform.position, direction, displacement.magnitude,
            ~0, QueryTriggerInteraction.Ignore))
        {
            if (collision.collider.GetComponentInParent<PlayerController>() != null || collision.distance >= distance) continue;
            closest = collision.collider;
            distance = collision.distance;
        }
        if (closest != null)
        {
            transform.position += direction * distance;
            Impact(closest);
            return;
        }
        transform.position += displacement;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger || other.GetComponentInParent<PlayerController>() != null) return;
        Impact(other);
    }
    private void Impact(Collider other)
    {
        if (hit) return;
        hit = true;
        if (particleEffect != null) Instantiate(particleEffect, transform.position, Quaternion.identity);
        var crate = other.GetComponentInParent<DestructibleCrate>();
        if (crate != null) crate.TakeDamage(damage);
        var enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy != null) enemy.TakeDamage(damage);
        if (hitSFX != null) AudioSource.PlayClipAtPoint(hitSFX, transform.position);
        Destroy(gameObject);
    }
}
