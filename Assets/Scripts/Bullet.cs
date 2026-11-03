using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Vector3 direction = new Vector3(0, 0, 0);
    public float speed = 100f;
    public float timeAlive = 5f;
    public float damage = 0f;
    public GameObject particleEffect;

    private float destroyTime;

    // Start is called before the first frame update
    void Start()
    {
        destroyTime = Time.time + timeAlive;
    }

    // Update is called once per frame
    void Update()
    {
        while (Time.time < destroyTime)
        {
            transform.Translate(direction * speed * Time.deltaTime);
            return;
        }

        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            return;
        }

        if (particleEffect != null)
        {
            Instantiate(particleEffect, transform.position, Quaternion.identity);
        }

        if (other.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
            enemyHealth.TakeDamage(damage);
        }
        Destroy(gameObject);
    }
}
