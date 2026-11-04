using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }
    public bool Busy { get; private set; }
    public float Opacity { get; private set; }
    public float Duration = 0.55f;

    public static void Load(string scene)
    {
        if (Instance == null) new GameObject("SceneTransition").AddComponent<SceneTransition>();
        if (!Instance.Busy) Instance.StartCoroutine(Instance.ChangeScene(scene));
    }

    private void OnEnable() { if (Instance == null) Instance = this; }
    private void OnDisable() { if (Instance == this) Instance = null; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator ChangeScene(string scene)
    {
        Busy = true;
        Time.timeScale = 1;
        yield return Fade(1);
        var operation = SceneManager.LoadSceneAsync(scene);
        while (!operation.isDone) yield return null;
        // Let scene initialization and the save restore finish behind the black screen.
        yield return null;
        if (scene.StartsWith("Stage ")) yield return RefreshEnvironment();
        yield return Fade(0);
        Busy = false;
    }

    private IEnumerator RefreshEnvironment()
    {
        // Unbaked scenes loaded from the title may have a zero ambient probe in Unity 2021.
        // Render the new sky environment and keep the fade black during GPU readback.
        DynamicGI.UpdateEnvironment();
        yield return null;
        yield return null;
        float deadline = Time.realtimeSinceStartup + 2f;
        while (!HasAmbientProbe() && Time.realtimeSinceStartup < deadline) yield return null;
        if (!HasAmbientProbe())
            Debug.LogWarning("Scene environment lighting is still empty after refresh; generate Lighting Data for this scene.");
    }

    private static bool HasAmbientProbe()
    {
        var probe = RenderSettings.ambientProbe;
        float energy = 0;
        for (int color = 0; color < 3; color++)
            for (int coefficient = 0; coefficient < 9; coefficient++) energy += Mathf.Abs(probe[color, coefficient]);
        return energy > .0001f;
    }

    private IEnumerator Fade(float target)
    {
        float initial = Opacity;
        float elapsed = 0;
        while (elapsed < Duration)
        {
            elapsed += Time.unscaledDeltaTime;
            Opacity = Mathf.Lerp(initial, target, Mathf.Clamp01(elapsed / Duration));
            yield return null;
        }
        Opacity = target;
    }

    private void OnGUI()
    {
        if (Opacity <= 0) return;
        GUI.depth = -10000;
        Color previous = GUI.color;
        GUI.color = new Color(0, 0, 0, Opacity);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
