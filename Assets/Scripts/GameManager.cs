using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;

    [Header("Value Setup")]
    public float maxHealth = 100f;
    public float health = 100f;

    [Header("UI")]
    [SerializeField] private RectTransform healthBar;
    [SerializeField] private TextMeshProUGUI timerText;    
    [SerializeField] private TextMeshProUGUI enemyCounterText; 

    private float healthBarMaxWidth = 220f;

    [Header("Level & Enemy Tracking")]
    public int remainingEnemies = 0;
    private float elapsedTime = 0f;
    private bool isGameActive = true;

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

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Start is called before the first frame update
    void Start()
    {
        RefreshUIReferences();
        InitStage();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isGameActive) return;
        elapsedTime += Time.deltaTime;
        UpdateTimerUI();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshUIReferences();
        InitStage();
        UpdateHealthBar();
    }

    private void RefreshUIReferences()
    {
        if (timerText == null)
        {
            GameObject tObj = GameObject.Find("Timer");
            if (tObj != null) timerText = tObj.GetComponent<TextMeshProUGUI>();
        }

        if (enemyCounterText == null)
        {
            GameObject eObj = GameObject.Find("EnemyCounter");
            if (eObj != null) enemyCounterText = eObj.GetComponent<TextMeshProUGUI>();
        }

        if (healthBar == null)
        {
            GameObject hpObj = GameObject.Find("HealthBarFill");
            if (hpObj != null) healthBar = hpObj.GetComponent<RectTransform>();
        }
    }

    public void InitStage()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        remainingEnemies = enemies.Length;
        UpdateEnemyCounterUI();
        Debug.Log($"場上找到的敵人數量: {enemies.Length}");
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60F);
            int seconds = Mathf.FloorToInt(elapsedTime % 60F);
            timerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
        }
    }

    private void UpdateEnemyCounterUI()
    {
        if (enemyCounterText != null)
        {
            enemyCounterText.text = $"Enemies Left: {remainingEnemies}";
        }
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
            instance.GameOver();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.sizeDelta = new Vector2(healthBarMaxWidth * (health / maxHealth), healthBar.sizeDelta.y);
        }
    }

    public static void OnEnemyKilled()
    {
        if (instance == null) return;

        instance.remainingEnemies--;
        if (instance.remainingEnemies < 0) instance.remainingEnemies = 0;
        instance.UpdateEnemyCounterUI();

        if (instance.remainingEnemies <= 0)
        {
            instance.StartCoroutine(instance.StageClearRoutine());
        }
    }

    private IEnumerator StageClearRoutine()
    {
        Debug.Log("本關所有敵人已消滅！準備前往下一關...");
        yield return new WaitForSeconds(2.0f);

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("恭喜通關所有關卡！(Game Win)");
            //Game Win UI
        }
    }

    private void GameOver()
    {
        isGameActive = false;
        Debug.Log("玩家死亡！Game Over");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        //Game Over UI
    }
}
