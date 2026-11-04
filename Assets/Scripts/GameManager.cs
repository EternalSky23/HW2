using System;
using System.Collections;
using UnityEngine;
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
    [SerializeField] private GameObject gameEndForeground;
    private float healthBarMaxWidth = 220f;
    [Header("Level & Enemy Tracking")]
    public int remainingEnemies;
    private float elapsedTime;
    private bool isGameActive = true;
    private bool clearing;
    public static bool IsActive => instance == null || instance.isGameActive;
    public static float CurrentHealth => instance != null ? instance.health : 100;
    public static float ElapsedTime => instance != null ? instance.elapsedTime : 0;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void OnEnable() { if (instance == null) instance = this; SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    private void Start()
    {
        if (instance != this) return;
        RefreshUIReferences(); InitStage(); UpdateHealthBar();
    }
    private void Update()
    {
        if (instance != this || !isGameActive || SceneManager.GetActiveScene().name == "Start") return;
        if (SceneTransition.Instance != null && SceneTransition.Instance.Busy) return;
        elapsedTime += Time.deltaTime;
        UpdateTimerUI();
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (instance != this) return;
        clearing = false;
        RefreshUIReferences(); InitStage(); UpdateHealthBar(); UpdateTimerUI();
    }
    private void RefreshUIReferences()
    {
        timerText = GameObject.Find("Timer")?.GetComponent<TextMeshProUGUI>();
        enemyCounterText = GameObject.Find("EnemyCounter")?.GetComponent<TextMeshProUGUI>();
        healthBar = GameObject.Find("Health")?.GetComponent<RectTransform>();
        var canvas = GameObject.Find("Canvas");
        gameEndForeground = canvas != null ? canvas.transform.Find("Foreground")?.gameObject : null;
    }
    public void InitStage()
    {
        remainingEnemies = 0;
        foreach (var enemy in FindObjectsOfType<EnemyHealth>())
            if (!enemy.IsDead) remainingEnemies++;
        UpdateEnemyCounterUI();
    }
    public static void RefreshEnemies() { if (instance != null) instance.InitStage(); }
    public static void RestoreProgress(float savedHealth, float savedElapsed)
    {
        if (instance == null) return;
        instance.health = Mathf.Clamp(savedHealth, 1, instance.maxHealth);
        instance.elapsedTime = Mathf.Max(0, savedElapsed);
        instance.UpdateHealthBar(); instance.UpdateTimerUI();
    }
    private void UpdateTimerUI()
    {
        if (timerText != null) timerText.text = string.Format("Time: {0:00}:{1:00}",
            Mathf.FloorToInt(elapsedTime / 60), Mathf.FloorToInt(elapsedTime % 60));
    }
    private void UpdateEnemyCounterUI()
    {
        if (enemyCounterText != null) enemyCounterText.text = $"Enemies Left: {remainingEnemies}";
    }
    public static void Heal(float value)
    {
        if (instance == null || !instance.isGameActive) return;
        instance.health = Math.Min(instance.maxHealth, instance.health + value);
        instance.UpdateHealthBar();
    }
    public static void TakeDamage(float value)
    {
        if (instance == null || !instance.isGameActive || !AdvancedGame.CanControl) return;
        instance.health = Mathf.Max(0, instance.health - value);
        instance.UpdateHealthBar();
        if (instance.health <= 0) instance.GameOverHandle(false);
    }
    private void UpdateHealthBar()
    {
        if (healthBar != null) healthBar.sizeDelta = new Vector2(healthBarMaxWidth * (health / maxHealth), healthBar.sizeDelta.y);
    }
    public static void OnEnemyKilled()
    {
        if (instance == null || !instance.isGameActive) return;
        instance.remainingEnemies = Mathf.Max(0, instance.remainingEnemies - 1);
        instance.UpdateEnemyCounterUI();
        if (instance.remainingEnemies == 0) CompleteStage();
    }
    public static void CompleteStage()
    {
        if (instance == null || instance.clearing) return;
        instance.clearing = true;
        instance.StartCoroutine(instance.StageClearRoutine());
    }
    private IEnumerator StageClearRoutine()
    {
        yield return new WaitForSeconds(2);
        if (!isGameActive) yield break;
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(next);
            SceneTransition.Load(System.IO.Path.GetFileNameWithoutExtension(path));
        }
        else GameOverHandle(true);
    }
    private void GameOverHandle(bool win)
    {
        isGameActive = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (gameEndForeground == null) return;
        gameEndForeground.SetActive(true);
        var panel = gameEndForeground.transform.Find(win ? "Win" : "Lose");
        if (panel != null) panel.gameObject.SetActive(true);
    }
    public static void ResetRun()
    {
        if (instance == null) return;
        // Disable now, because Destroy is deferred until the end of the frame.
        instance.gameObject.SetActive(false);
        Destroy(instance.gameObject);
        instance = null;
    }
    public static void ReturnToTitle()
    {
        if (SceneTransition.Instance != null && SceneTransition.Instance.Busy) return;
        if (AdvancedGame.Instance != null) AdvancedGame.Instance.SetPaused(false);
        ResetRun();
        SceneTransition.Load("Start");
    }
}
