using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// Installed for both editor Play and standalone builds. No scene wiring is required.
public class AdvancedGame : MonoBehaviour
{
    public static AdvancedGame Instance { get; private set; }
    public PlayerAmmo Ammo { get; private set; }
    public Material AmmoMaterial { get; private set; }
    public bool Paused { get; private set; }
    public static bool CanControl => Instance == null || (!Instance.Paused &&
        !(SceneTransition.Instance != null && SceneTransition.Instance.Busy) && GameManager.IsActive);
    public bool InStage => SceneManager.GetActiveScene().name.StartsWith("Stage ");
    public static string SavePath => Path.Combine(Application.persistentDataPath, "hw2-progress.json");
    public bool HasSave => ReadSave() != null;
    private SaveData pending;
    private SaveData menuSave;
    private readonly Dictionary<string, EnemyHealth> enemies = new Dictionary<string, EnemyHealth>();
    private readonly HashSet<string> brokenCrates = new HashSet<string>();
    private readonly Dictionary<string, Vector3> supplies = new Dictionary<string, Vector3>();
    private Material wood;
    private string message = "";
    private float messageUntil;
    private GUIStyle textStyle, headingStyle, buttonStyle;

    [Serializable] public class EnemyState { public string id; public float health; public bool dead; }
    [Serializable] public class SupplyState { public string id; public Vector3 position; }
    [Serializable] public class SaveData
    {
        public int version = 1;
        public string scene, savedAt;
        public float health, elapsed, yaw;
        public Vector3 position;
        public int loaded, reserve;
        public List<EnemyState> enemies = new List<EnemyState>();
        public List<string> crates = new List<string>();
        public List<SupplyState> supplies = new List<SupplyState>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (Instance == null) new GameObject("Advanced Game Features").AddComponent<AdvancedGame>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Ammo = gameObject.AddComponent<PlayerAmmo>();
        gameObject.AddComponent<SceneTransition>();
        wood = MakeMaterial(new Color(0.48f, 0.24f, 0.075f));
        AmmoMaterial = MakeMaterial(new Color(0.15f, 0.8f, 0.95f));
    }

    private void OnEnable()
    {
        if (Instance == null) Instance = this;
        Ammo = GetComponent<PlayerAmmo>();
        if (wood == null) wood = MakeMaterial(new Color(0.48f, 0.24f, 0.075f));
        if (AmmoMaterial == null) AmmoMaterial = MakeMaterial(new Color(0.15f, 0.8f, 0.95f));
        SceneManager.sceneLoaded += OnLoaded;
    }

    private void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; }

    private Material MakeMaterial(Color color)
    {
        // Use the material of an existing primitive's pipeline-compatible shader where available.
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        return new Material(shader) { color = color };
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        if (Instance != this) return;
        Time.timeScale = 1;
        if (wood != null) Destroy(wood);
        if (AmmoMaterial != null) Destroy(AmmoMaterial);
        Instance = null;
    }

    private void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        Paused = false;
        Time.timeScale = 1;
        enemies.Clear();
        brokenCrates.Clear();
        supplies.Clear();
        if (!InStage) { menuSave = ReadSave(); SetCursor(false); return; }
        foreach (var enemy in FindObjectsOfType<EnemyHealth>())
            enemies[ObjectId(enemy.transform)] = enemy;
        StartCoroutine(PrepareStage());
    }

    private IEnumerator PrepareStage()
    {
        yield return null; // EnemyHealth.Start and GameManager.Start initialize first.
        var player = FindObjectOfType<PlayerController>();
        if (player == null) yield break;
        var data = pending;
        pending = null;
        if (data != null)
        {
            foreach (var state in data.enemies)
            {
                var enemy = FindSavedEnemy(state.id);
                if (enemy == null) continue;
                if (state.dead) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
                else enemy.RestoreHealth(state.health);
            }
            foreach (string id in data.crates) brokenCrates.Add(id);
            Ammo.Restore(data.loaded, data.reserve);
            GameManager.RestoreProgress(data.health, data.elapsed);
        }
        // Generate before restoring the player position: layout is tied to the stage spawn.
        SpawnCrates(player.transform);
        if (data != null)
        {
            foreach (var supply in data.supplies) RestoreSupply(supply);
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.SetPositionAndRotation(data.position, Quaternion.Euler(0, data.yaw, 0));
            controller.enabled = true;
            Notify("Progress loaded");
        }
        GameManager.RefreshEnemies();
        if (data != null && FindObjectsOfType<EnemyHealth>().Length == 0) GameManager.CompleteStage();
        SetCursor(true);
    }

    private EnemyHealth FindSavedEnemy(string id)
    {
        if (enemies.TryGetValue(id, out var exact)) return exact;
        // Scene updates can reorder roots. Migrate older saves only for a unique name path.
        string path = System.Text.RegularExpressions.Regex.Replace(id, @"\[\d+\]", "");
        EnemyHealth match = null;
        foreach (var pair in enemies)
        {
            if (System.Text.RegularExpressions.Regex.Replace(pair.Key, @"\[\d+\]", "") != path) continue;
            if (match != null) return null;
            match = pair.Value;
        }
        return match;
    }

    private static string ObjectId(Transform item)
    {
        string id = "";
        while (item != null)
        {
            id = item.name + "[" + item.GetSiblingIndex() + "]/" + id;
            item = item.parent;
        }
        return id;
    }

    private void SpawnCrates(Transform player)
    {
        var points = CratePlacement.Layout(player.position);
        int placed = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            var center = points[i] + Vector3.up * 0.55f;
            string id = "supply-crate-v2-" + i;
            if (brokenCrates.Contains(id)) continue;
            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Breakable Supply Crate " + i;
            crate.transform.position = center;
            crate.transform.localScale = Vector3.one;
            crate.GetComponent<Renderer>().sharedMaterial = wood;
            crate.AddComponent<DestructibleCrate>().Id = id;
            for (int band = 0; band < 2; band++)
            {
                var slat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slat.name = "Wood brace";
                slat.transform.SetParent(crate.transform, false);
                slat.transform.localPosition = new Vector3(0, band == 0 ? -0.3f : 0.3f, 0);
                slat.transform.localScale = new Vector3(1.04f, 0.12f, 1.04f);
                slat.GetComponent<Renderer>().sharedMaterial = wood;
                Destroy(slat.GetComponent<Collider>());
            }
        }
        if (placed == 0) Debug.LogWarning("No free ground for supply crates in this stage.");
    }

    public void RememberCrate(string id) { brokenCrates.Add(id); }
    public void RegisterSupply(string id, Vector3 position) { supplies[id] = position; }
    public void CollectSupply(string id) { supplies.Remove(id); }
    private void RestoreSupply(SupplyState supply)
    {
        var pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pickup.name = "Ammo supply (+24)";
        pickup.transform.position = supply.position;
        pickup.transform.localScale = Vector3.one * 0.4f;
        pickup.GetComponent<Renderer>().sharedMaterial = AmmoMaterial;
        pickup.GetComponent<Collider>().isTrigger = true;
        pickup.AddComponent<Rigidbody>().isKinematic = true;
        pickup.AddComponent<AmmoPickup>().Id = supply.id;
        RegisterSupply(supply.id, supply.position);
    }

    private void Update()
    {
        if (!InStage || !GameManager.IsActive || SceneTransition.Instance.Busy) return;
        if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(!Paused);
        if (!Paused && Input.GetKeyDown(KeyCode.R)) Ammo.Reload();
        if (Input.GetKeyDown(KeyCode.F5)) Save();
    }

    public void SetPaused(bool value)
    {
        Paused = value;
        Time.timeScale = value ? 0 : 1;
        SetCursor(!value);
    }

    private void SetCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    public void Notify(string value) { message = value; messageUntil = Time.unscaledTime + 3; }

    public void Save()
    {
        if (!InStage || !GameManager.IsActive || SceneTransition.Instance.Busy) return;
        var player = FindObjectOfType<PlayerController>();
        if (player == null) return;
        var data = new SaveData { scene = SceneManager.GetActiveScene().name,
            health = GameManager.CurrentHealth, elapsed = GameManager.ElapsedTime,
            position = player.transform.position, yaw = player.transform.eulerAngles.y,
            loaded = Ammo.Loaded, reserve = Ammo.Reserve, savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm") };
        foreach (var pair in enemies)
            data.enemies.Add(new EnemyState { id = pair.Key, dead = pair.Value == null || pair.Value.IsDead,
                health = pair.Value == null ? 0 : pair.Value.CurrentHealth });
        data.crates.AddRange(brokenCrates);
        foreach (var pair in supplies) data.supplies.Add(new SupplyState { id = pair.Key, position = pair.Value });
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            string temporary = SavePath + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
            if (File.Exists(SavePath)) File.Copy(SavePath, SavePath + ".bak", true);
            File.Copy(temporary, SavePath, true);
            File.Delete(temporary);
            Notify("Progress saved");
        }
        catch (Exception e) { Debug.LogWarning(e.Message); Notify("Could not save progress"); }
    }

    public SaveData ReadSave()
    {
        try
        {
            if (!File.Exists(SavePath)) return null;
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data == null || data.version != 1 || !ValidStage(data.scene) ||
                !Finite(data.health) || data.health <= 0 || !Finite(data.elapsed) || data.elapsed < 0 ||
                !Finite(data.position.x) || !Finite(data.position.y) || !Finite(data.position.z) ||
                !Finite(data.yaw) || data.enemies == null || data.crates == null || data.supplies == null) return null;
            return data;
        }
        catch { return null; }
    }
    private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    private static bool ValidStage(string scene)
    {
        return (scene == "Stage 1" || scene == "Stage 2" || scene == "Stage 3") && Application.CanStreamedLevelBeLoaded(scene);
    }

    public void ContinueGame()
    {
        if (SceneTransition.Instance.Busy) return;
        var data = ReadSave();
        if (data == null) { Notify("No valid saved progress"); return; }
        pending = data;
        GameManager.ResetRun();
        Ammo.Restore(12, 60);
        SceneTransition.Load(data.scene);
    }

    public void NewGame()
    {
        if (SceneTransition.Instance.Busy) return;
        pending = null;
        GameManager.ResetRun();
        Ammo.Restore(12, 60);
        SceneTransition.Load("Stage 1");
    }

    private void OnGUI()
    {
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true };
            headingStyle = new GUIStyle(textStyle) { fontSize = 25, fontStyle = FontStyle.Bold };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };
        }
        GUI.depth = -200;
        Matrix4x4 oldMatrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
        float width = Screen.width / scale, height = Screen.height / scale;
        if (InStage)
        {
            GUI.Box(new Rect(width - 325, height - 120, 305, 100), "");
            GUI.Label(new Rect(width - 310, height - 112, 280, 32),
                Ammo.Reloading ? "RELOADING..." : $"AMMO  {Ammo.Loaded:00} / {Ammo.Reserve:000}", headingStyle);
            GUI.Label(new Rect(width - 310, height - 75, 290, 50), "R  Reload    Esc  Menu\nF5  Save progress", textStyle);
            GUI.Label(new Rect(width / 2 - 6, height / 2 - 18, 24, 30), "+", headingStyle);
            if (Paused)
            {
                Rect panel = new Rect(width / 2 - 200, height / 2 - 170, 400, 340);
                GUI.Box(panel, "");
                GUI.Label(new Rect(panel.x + 25, panel.y + 20, 350, 40), "PAUSED", headingStyle);
                if (GUI.Button(new Rect(panel.x + 25, panel.y + 80, 350, 45), "Resume", buttonStyle)) SetPaused(false);
                if (GUI.Button(new Rect(panel.x + 25, panel.y + 140, 350, 45), "Save progress", buttonStyle)) Save();
                if (GUI.Button(new Rect(panel.x + 25, panel.y + 200, 350, 45), "Return to title", buttonStyle)) GameManager.ReturnToTitle();
                GUI.Label(new Rect(panel.x + 25, panel.y + 260, 350, 60), "Save before leaving to keep your latest progress.", textStyle);
            }
        }
        else if (SceneManager.GetActiveScene().name == "Start")
        {
            // A separate corner panel leaves the team's existing start/rules pages usable.
            float x = width - 380;
            GUI.Box(new Rect(x, height - 250, 355, 225), "");
            GUI.Label(new Rect(x + 18, height - 240, 320, 35), "SAVED PROGRESS", headingStyle);
            var data = menuSave;
            GUI.Label(new Rect(x + 18, height - 200, 320, 60), data == null ? "No saved game yet" :
                $"{data.scene}   HP {data.health:0}\n{data.savedAt}   Time {TimeSpan.FromSeconds(data.elapsed):mm\\:ss}", textStyle);
            GUI.enabled = data != null && !SceneTransition.Instance.Busy;
            if (GUI.Button(new Rect(x + 18, height - 130, 320, 42), "Continue saved game", buttonStyle)) ContinueGame();
            GUI.enabled = !SceneTransition.Instance.Busy;
            if (GUI.Button(new Rect(x + 18, height - 78, 320, 38), "New game", buttonStyle)) NewGame();
            GUI.enabled = true;
        }
        if (Time.unscaledTime < messageUntil)
        {
            GUI.Box(new Rect(width / 2 - 180, height - 175, 360, 40), "");
            GUI.Label(new Rect(width / 2 - 165, height - 170, 340, 35), message, textStyle);
        }
        GUI.matrix = oldMatrix;
    }
}
