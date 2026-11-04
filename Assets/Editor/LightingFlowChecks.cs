using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class LightingFlowChecks
{
    private const string Request = "Temp/RunLightingComparison";
    private const string Folder = "Logs/LightingComparison";
    private static IEnumerator run;
    private static double deadline;
    private static Vector3 cameraPosition;
    private static Quaternion cameraRotation;
    private static Color32[] baseline;
    private static Report report;
    private static double startTitlePlayAt;
    [Serializable] private class Snapshot
    {
        public string label, ambientMode, skybox, scene;
        public float[] probe = new float[27];
        public float lowerImageLuminance, darkPixelFraction, differenceFromDirect;
        public float sunIntensity, shadowStrength, fadeOpacity;
    }
    [Serializable] private class Report { public string unityVersion; public List<Snapshot> snapshots = new List<Snapshot>(); public string error; }

    static LightingFlowChecks() { EditorApplication.update += Tick; }
    [MenuItem("Tools/HW2/Compare Title Lighting")]
    public static void Start()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play Mode before starting lighting comparison."); return; }
        Directory.CreateDirectory(Folder);
        SessionState.SetBool("HW2LightingComparison", true);
        EditorApplication.isPlaying = true;
    }
    [MenuItem("Tools/HW2/Compare Title First Lighting")]
    public static void StartFromTitle()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play Mode before starting lighting comparison."); return; }
        File.WriteAllText("Temp/RunLightingTitleFirst", "run");
    }
    private static void Tick()
    {
        if (File.Exists("Temp/RunLightingTitleFirst") && !EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            File.Delete("Temp/RunLightingTitleFirst");
            EditorSceneManager.OpenScene("Assets/Scenes/Start.unity");
            startTitlePlayAt = EditorApplication.timeSinceStartup + 3;
        }
        if (startTitlePlayAt > 0 && EditorApplication.timeSinceStartup >= startTitlePlayAt)
        {
            startTitlePlayAt = 0;
            SessionState.SetBool("HW2LightingComparison", true);
            EditorApplication.isPlaying = true;
        }
        if (File.Exists(Request) && !EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode)
        { File.Delete(Request); Start(); }
        if (run == null && EditorApplication.isPlaying && SessionState.GetBool("HW2LightingComparison", false))
        {
            SessionState.SetBool("HW2LightingComparison", false);
            report = new Report { unityVersion = Application.unityVersion };
            deadline = EditorApplication.timeSinceStartup + 50;
            run = Compare();
        }
        if (run == null) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Lighting comparison timeout");
            if (!run.MoveNext()) Finish();
        }
        catch (Exception e) { report.error = e.ToString(); Finish(); }
    }
    private static void Finish()
    {
        File.WriteAllText(Path.Combine(Folder, "report.json"), JsonUtility.ToJson(report, true));
        Debug.Log("Lighting comparison complete: " + Path.GetFullPath(Folder));
        run = null;
        foreach (var actor in UnityEngine.Object.FindObjectsOfType<PlayerController>()) actor.enabled = true;
        foreach (var actor in UnityEngine.Object.FindObjectsOfType<CameraLook>()) actor.enabled = true;
        foreach (var actor in UnityEngine.Object.FindObjectsOfType<EnemyAI>()) actor.enabled = true;
        if (AdvancedGame.Instance != null) AdvancedGame.Instance.SetPaused(true);
    }
    private static IEnumerator Compare()
    {
        // Start from an editor-opened Stage 1 so its editor environment cache is exercised.
        bool fromTitle = SceneManager.GetActiveScene().name == "Start";
        if (fromTitle)
        {
            // Capture the scene entered from an editor-opened title, before visiting Stage 1.
            yield return null;
            AdvancedGame.Instance.NewGame();
            while (SceneTransition.Instance.Busy) yield return null;
        }
        double ready = EditorApplication.timeSinceStartup + 1;
        while (EditorApplication.timeSinceStartup < ready) yield return null;
        FreezeActors();
        cameraPosition = Camera.main.transform.position;
        cameraRotation = Camera.main.transform.rotation;
        if (fromTitle && File.Exists(Path.Combine(Folder, "direct-stage1.png")))
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(Path.Combine(Folder, "direct-stage1.png")));
            baseline = texture.GetPixels32();
            UnityEngine.Object.DestroyImmediate(texture);
        }
        SnapshotImage(fromTitle ? "title-first-stage1" : "direct-stage1");
        GameManager.ReturnToTitle();
        while (SceneTransition.Instance.Busy) yield return null;
        ready = EditorApplication.timeSinceStartup + .5;
        while (EditorApplication.timeSinceStartup < ready) yield return null;
        AdvancedGame.Instance.NewGame();
        while (SceneTransition.Instance.Busy) yield return null;
        FreezeActors();
        ready = EditorApplication.timeSinceStartup + 1;
        while (EditorApplication.timeSinceStartup < ready) yield return null;
        SnapshotImage("title-to-stage1");
        DynamicGI.UpdateEnvironment();
        ready = EditorApplication.timeSinceStartup + 3;
        while (EditorApplication.timeSinceStartup < ready) yield return null;
        SnapshotImage("title-to-stage1-after-gi-refresh");
    }
    private static void FreezeActors()
    {
        foreach (var actor in UnityEngine.Object.FindObjectsOfType<PlayerController>()) actor.enabled = false;
        foreach (var actor in UnityEngine.Object.FindObjectsOfType<CameraLook>()) actor.enabled = false;
        foreach (var actor in UnityEngine.Object.FindObjectsOfType<EnemyAI>()) actor.enabled = false;
    }
    private static void SnapshotImage(string label)
    {
        var data = new Snapshot { label = label, scene = SceneManager.GetActiveScene().name,
            ambientMode = RenderSettings.ambientMode.ToString(), skybox = RenderSettings.skybox != null ? RenderSettings.skybox.name : "none",
            fadeOpacity = SceneTransition.Instance.Opacity };
        for (int channel = 0; channel < 3; channel++)
            for (int coefficient = 0; coefficient < 9; coefficient++) data.probe[channel * 9 + coefficient] = RenderSettings.ambientProbe[channel, coefficient];
        foreach (var light in UnityEngine.Object.FindObjectsOfType<Light>())
            if (light.type == LightType.Directional) { data.sunIntensity = light.intensity; data.shadowStrength = light.shadowStrength; break; }
        var hidden = new List<Renderer>();
        foreach (var player in UnityEngine.Object.FindObjectsOfType<PlayerController>())
            foreach (var renderer in player.GetComponentsInChildren<Renderer>()) if (renderer.enabled) { hidden.Add(renderer); renderer.enabled = false; }
        foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyHealth>())
            foreach (var renderer in enemy.GetComponentsInChildren<Renderer>()) if (renderer.enabled) { hidden.Add(renderer); renderer.enabled = false; }
        var obj = new GameObject("Lighting comparison camera");
        var camera = obj.AddComponent<Camera>();
        camera.CopyFrom(Camera.main);
        camera.enabled = false;
        camera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        var target = new RenderTexture(960, 540, 24);
        var previous = RenderTexture.active;
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        var pixels = new Texture2D(960, 540, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
        pixels.Apply();
        var colors = pixels.GetPixels32();
        double total = 0, difference = 0; int dark = 0, count = 960 * 360;
        for (int i = 0; i < count; i++)
        {
            float luminance = (colors[i].r * .2126f + colors[i].g * .7152f + colors[i].b * .0722f) / 255;
            total += luminance;
            if (luminance < .05f) dark++;
            if (baseline != null) difference += (Math.Abs(colors[i].r - baseline[i].r) + Math.Abs(colors[i].g - baseline[i].g) + Math.Abs(colors[i].b - baseline[i].b)) / (3f * 255);
        }
        data.lowerImageLuminance = (float)(total / count);
        data.darkPixelFraction = (float)dark / count;
        data.differenceFromDirect = (float)(difference / count);
        if (baseline == null) baseline = colors;
        File.WriteAllBytes(Path.Combine(Folder, label + ".png"), pixels.EncodeToPNG());
        report.snapshots.Add(data);
        RenderTexture.active = previous;
        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(pixels);
        UnityEngine.Object.DestroyImmediate(obj);
        foreach (var renderer in hidden) if (renderer != null) renderer.enabled = true;
    }
}
