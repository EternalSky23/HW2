using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealItem : MonoBehaviour
{
    [SerializeField] private float healValue = 20f;

    private float initY;
    private float cycleTime = 2f;
    private float magnitude = 0.1f;
    // Start is called before the first frame update
    void Start()
    {
        initY = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 cur = transform.position;
        transform.position = new Vector3(cur.x, 
            initY + magnitude * (float)Math.Sin(2 * Math.PI * (Time.time % cycleTime) / cycleTime), cur.z);
    }

    private void ActivateItem()
    {
        GameManager.Heal(healValue);
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Heal(healValue);
            Destroy(gameObject);
        }
    }
}
