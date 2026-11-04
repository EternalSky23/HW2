using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Integration checks against the actual three scenes; no test-only objects enter a build.
[InitializeOnLoad]
public static class AdvancedFeatureChecks
{
    private static IEnumerator sequence;
    private static readonly List<string> results = new List<string>();
    private static double deadline;
    private static string backup;
    private static bool hadSave;
    private const string Request = "Temp/RunAdvancedChecks";
    private const string Report = "Temp/AdvancedChecks.txt";
    static AdvancedFeatureChecks() { EditorApplication.update += Tick; }

    [MenuItem("Tools/HW2/Run Advanced Feature Checks")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying)
        {
            File.WriteAllText(Request, "run");
            EditorApplication.isPlaying = true;
        }
        else Begin();
    }
    private static void Begin()
    {
        results.Clear();
        hadSave = File.Exists(AdvancedGame.SavePath);
        backup = hadSave ? File.ReadAllText(AdvancedGame.SavePath) : null;
        sequence = Checks();
        deadline = EditorApplication.timeSinceStartup + 100;
    }
    private static void Tick()
    {
        if (File.Exists("Temp/StartAdvancedChecks") && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling)
        {
            File.Delete("Temp/StartAdvancedChecks");
            File.WriteAllText(Request, "run");
            EditorApplication.isPlaying = true;
        }
        if (EditorApplication.isPlaying && !EditorApplication.isCompiling && sequence == null && File.Exists(Request))
        { File.Delete(Request); Begin(); }
        if (sequence == null) return;
        try
        {
            if (!EditorApplication.isPlaying) throw new Exception("Play Mode stopped during checks");
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Integration check timed out");
            if (!sequence.MoveNext()) Finish();
        }
        catch (Exception e) { results.Add("FAIL: " + e); Finish(); }
    }
    private static void Finish()
    {
        sequence = null;
        if (hadSave) File.WriteAllText(AdvancedGame.SavePath, backup);
        else if (File.Exists(AdvancedGame.SavePath)) File.Delete(AdvancedGame.SavePath);
        // Tests restore the original user save and leave Stage 1 available for manual review.
        File.WriteAllLines(Report, results);
        Debug.Log(string.Join("\n", results));
        if (AdvancedGame.Instance != null) AdvancedGame.Instance.SetPaused(true);
        if (Application.isBatchMode) EditorApplication.Exit(results.Exists(line => line.StartsWith("FAIL")) ? 1 : 0);
    }
    private static void Check(bool pass, string name)
    {
        if (!pass) throw new Exception(name);
        results.Add("PASS: " + name);
    }
    private static void CheckMusic(string scene)
    {
        var managers = UnityEngine.Object.FindObjectsOfType<AudioManager>();
        Check(managers.Length == 1, "Exactly one BGM manager in " + scene);
        var source = managers[0].GetComponent<AudioSource>();
        Check(source.clip != null && source.loop && source.spatialBlend == 0,
            "BGM assigned and looping in " + scene);
        // Batch mode may have no audio output device/listener; check playback in the editor.
        if (!Application.isBatchMode) Check(source.isPlaying, "BGM playing in " + scene);
    }
    private static void CaptureAction(PlayerController player, string path)
    {
        string folder = Path.Combine("Temp", "ActionPreviews");
        Directory.CreateDirectory(folder);
        path = Path.Combine(folder, Path.GetFileName(path));
        Action capture = null;
        capture = () =>
        {
            player.Actions.PoseApplied -= capture;
            RenderAction(player, path);
        };
        player.Actions.PoseApplied += capture;
    }
    private static void RenderAction(PlayerController player, string path)
    {
        var cameraObject = new GameObject("Action preview camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.CopyFrom(Camera.main);
        camera.enabled = false;
        camera.transform.position = player.transform.position + player.transform.right * 2.4f + player.transform.forward * 2.4f + Vector3.up * .6f;
        camera.transform.LookAt(player.transform.position + Vector3.up * .1f);
        var target = new RenderTexture(800, 800, 24);
        var previous = RenderTexture.active;
        camera.targetTexture = target;
        // Bake the just-posed skeleton for an immediate offscreen Camera.Render;
        // the normal renderer's skinning job otherwise runs later in the frame.
        var previews = new List<GameObject>();
        var skins = player.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var skin in skins)
        {
            var mesh = new Mesh();
            skin.BakeMesh(mesh);
            var preview = new GameObject("Baked pose preview");
            preview.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
            preview.transform.localScale = skin.transform.lossyScale;
            preview.AddComponent<MeshFilter>().sharedMesh = mesh;
            preview.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
            skin.enabled = false;
            previews.Add(preview);
        }
        camera.Render();
        foreach (var skin in skins) skin.enabled = true;
        foreach (var preview in previews)
        {
            UnityEngine.Object.Destroy(preview.GetComponent<MeshFilter>().sharedMesh);
            UnityEngine.Object.Destroy(preview);
        }
        RenderTexture.active = target;
        var pixels = new Texture2D(800, 800, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect(0, 0, 800, 800), 0, 0);
        pixels.Apply();
        File.WriteAllBytes(path, pixels.EncodeToPNG());
        RenderTexture.active = previous;
        camera.targetTexture = null;
        target.Release();
        UnityEngine.Object.Destroy(target);
        UnityEngine.Object.Destroy(pixels);
        UnityEngine.Object.Destroy(cameraObject);
    }

    private static void CheckPlacement()
    {
        var player = UnityEngine.Object.FindObjectOfType<PlayerController>();
        Check(player.moveSpeed == 6, "Player speed is 6 in " + SceneManager.GetActiveScene().name);
        var crates = UnityEngine.Object.FindObjectsOfType<DestructibleCrate>();
        Check(crates.Length == 6, "Six crates on open reachable ground in " + SceneManager.GetActiveScene().name);
        foreach (var crate in crates)
        {
            Vector3 floor = crate.transform.position - Vector3.up * .55f;
            Check(CratePlacement.Reachable(player.transform.position, floor) && CratePlacement.OpenGround(floor),
                "Reachable with wall/edge clearance: " + crate.Id + " at " + floor);
        }
    }
    private static IEnumerator Checks()
    {
        while (AdvancedGame.Instance == null) yield return null;
        var game = AdvancedGame.Instance;
        game.NewGame();
        float maxOpacity = 0;
        while (SceneTransition.Instance.Busy)
        { maxOpacity = Mathf.Max(maxOpacity, SceneTransition.Instance.Opacity); yield return null; }
        Check(maxOpacity > 0.99f && SceneTransition.Instance.Opacity == 0, "New game fades fully out and in");
        CheckMusic("Stage 1");
        CheckPlacement();
        var ammo = game.Ammo;
        var actionPlayer = UnityEngine.Object.FindObjectOfType<PlayerController>();
        Check(actionPlayer.Actions.HasHumanoidRig, "Original action animations bind to the humanoid arms");
        int releasedBefore = actionPlayer.ProjectilesReleased;
        Check(actionPlayer.TryThrow() && ammo.Loaded == 11, "Throw animation begins and consumes one round");
        Check(actionPlayer.ProjectilesReleased == releasedBefore && !actionPlayer.TryThrow(), "Windup delays release and blocks duplicate throws");
        while (actionPlayer.Actions.Progress < .32f) yield return null;
        CaptureAction(actionPlayer, "hw2-throw-windup.png");
        while (actionPlayer.Actions.Progress < .58f) yield return null;
        Check(actionPlayer.ProjectilesReleased == releasedBefore + 1, "One stone releases at the animated hand after windup");
        CaptureAction(actionPlayer, "hw2-throw-release.png");
        while (actionPlayer.Actions.Busy) yield return null;
        ammo.Restore(6, 20);
        ammo.Reload();
        while (actionPlayer.Actions.Progress < .5f) yield return null;
        Check(actionPlayer.Actions.IsReloading && ammo.Reloading && ammo.Loaded == 6 && !actionPlayer.TryThrow(),
            "Reload animates the arms, blocks throwing and waits before transferring ammo");
        CaptureAction(actionPlayer, "hw2-reload.png");
        while (ammo.Reloading || actionPlayer.Actions.Busy) yield return null;
        Check(ammo.Loaded == 12 && ammo.Reserve == 14, "Reload animation finishes with the correct ammo transfer");
        ammo.Restore(0, 5);
        Check(!ammo.TryFire() && ammo.Reloading, "Empty magazine blocks attack and starts reload");
        while (ammo.Reloading) yield return null;
        Check(ammo.Loaded == 5 && ammo.Reserve == 0, "Reload transfers finite reserve after delay");
        Check(ammo.TryFire() && ammo.Loaded == 4 && !ammo.TryFire(), "One shot consumes ammo; cooldown blocks duplicate shots");
        ammo.Restore(0, 0);
        Check(!ammo.TryFire() && !ammo.Reloading, "No attack or reload when all ammo is exhausted");
        ammo.Restore(7, 35);
        var crates = UnityEngine.Object.FindObjectsOfType<DestructibleCrate>();
        Check(crates.Length >= 4, "Stage 1 contains accessible supply crates: " + crates.Length);
        string brokenId = crates[0].Id;
        var projectile = UnityEngine.Object.Instantiate(UnityEngine.Object.FindObjectOfType<PlayerController>().projectilePrefab,
            crates[0].transform.position + Vector3.up * 2, Quaternion.identity).GetComponent<Bullet>();
        projectile.direction = Vector3.down;
        projectile.damage = 10;
        double hitDeadline = EditorApplication.timeSinceStartup + 3;
        while (crates[0].Health == 20 && EditorApplication.timeSinceStartup < hitDeadline) yield return null;
        Check(crates[0].Health == 10, "Actual fast projectile damages crate without tunnelling");
        crates[0].TakeDamage(10);
        yield return null;
        Check(UnityEngine.Object.FindObjectsOfType<AmmoPickup>().Length > 0, "Second shot breaks crate and drops ammo supply");
        var enemies = UnityEngine.Object.FindObjectsOfType<EnemyHealth>();
        Check(enemies.Length > 1, "Imported enemies exist in Stage 1");
        enemies[0].TakeDamage(10000);
        GameManager.TakeDamage(17);
        float expectedHealth = GameManager.CurrentHealth;
        game.SetPaused(true);
        Check(Time.timeScale == 0 && !AdvancedGame.CanControl, "Pause stops simulation and player controls");
        game.Save();
        var saved = game.ReadSave();
        Check(saved != null && saved.loaded == 7 && saved.reserve == 35 && saved.health == expectedHealth,
            "Save contains health, ammunition, time, position and stage");
        GameManager.ReturnToTitle();
        while (SceneTransition.Instance.Busy) yield return null;
        Check(SceneManager.GetActiveScene().name == "Start" && game.HasSave, "Title exposes valid saved progress");
        CheckMusic("Start");
        game.ContinueGame();
        while (SceneTransition.Instance.Busy) yield return null;
        Check(SceneManager.GetActiveScene().name == "Stage 1" && Mathf.Abs(GameManager.CurrentHealth - saved.health) < .01f &&
            Mathf.Abs(GameManager.ElapsedTime - saved.elapsed) < .3f && ammo.Loaded == 7 && ammo.Reserve == 35,
            "Continue restores stage, health, timer and ammunition");
        Check(Vector3.Distance(UnityEngine.Object.FindObjectOfType<PlayerController>().transform.position, saved.position) < 0.5f,
            "Continue restores player position");
        Check(UnityEngine.Object.FindObjectsOfType<EnemyHealth>().Length == enemies.Length - 1,
            "Killed enemies remain removed after loading");
        bool missing = true;
        foreach (var crate in UnityEngine.Object.FindObjectsOfType<DestructibleCrate>()) if (crate.Id == brokenId) missing = false;
        Check(missing && UnityEngine.Object.FindObjectsOfType<AmmoPickup>().Length > 0, "Broken crate and uncollected drop survive save/load");
        foreach (string stage in new[] { "Stage 2", "Stage 3" })
        {
            foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyHealth>()) enemy.TakeDamage(10000);
            while (SceneManager.GetActiveScene().name != stage || SceneTransition.Instance.Busy) yield return null;
            results.Add("PASS: Clearing enemies transitions to " + stage);
            CheckMusic(stage);
            CheckPlacement();
            Check(UnityEngine.Object.FindObjectsOfType<DestructibleCrate>().Length >= 4, stage + " contains supply crates");
            Check(ammo.Loaded == 7 && ammo.Reserve == 35, "Ammo persists into " + stage);
        }
        var supplyPickup = UnityEngine.Object.FindObjectsOfType<DestructibleCrate>()[0];
        supplyPickup.TakeDamage(20);
        yield return null;
        var drop = UnityEngine.Object.FindObjectOfType<AmmoPickup>();
        var character = UnityEngine.Object.FindObjectOfType<PlayerController>();
        character.GetComponent<CharacterController>().enabled = false;
        character.transform.position = drop.transform.position;
        character.GetComponent<CharacterController>().enabled = true;
        double pickupDeadline = EditorApplication.timeSinceStartup + 3;
        while (ammo.Reserve == 35 && EditorApplication.timeSinceStartup < pickupDeadline) yield return null;
        Check(ammo.Reserve == 59, "Walking into supply pickup adds exactly 24 reserve rounds");
        game.NewGame();
        while (SceneTransition.Instance.Busy) yield return null;
        Check(GameManager.CurrentHealth == 100 && ammo.Loaded == 12 && ammo.Reserve == 60, "New game resets run state");
        results.Add("ALL ADVANCED FEATURE CHECKS PASSED");
    }
}
