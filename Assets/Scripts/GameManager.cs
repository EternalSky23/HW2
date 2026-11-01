using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;

    [Header("Value Setup")]
    public float maxHealth = 100f;
    public float health = 100f;

    [Header("UI")]
    [SerializeField] private RectTransform healthBar;
    
    private float healthBarMaxWidth = 220f;

    void Awake()
    {
        DontDestroyOnLoad(this);
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public static void Heal(float value)
    {
        instance.health = Math.Min(instance.maxHealth, instance.health + value);
        instance.UpdateHealthBar();
    }

    public static void TakeDamage(float value)
    {
        instance.health -= value;
        instance.UpdateHealthBar();

        if (instance.health <= 0) {
            // Game fail handle
        }
    }

    private void UpdateHealthBar()
    {
        healthBar.sizeDelta = new Vector2(healthBarMaxWidth * health / maxHealth, 
                                            healthBar.sizeDelta.y);
    }
}
