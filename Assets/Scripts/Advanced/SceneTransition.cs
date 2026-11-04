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
        yield return Fade(0);
        Busy = false;
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
