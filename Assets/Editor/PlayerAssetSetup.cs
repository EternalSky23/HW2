using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Run after importing the Bodyguards package and the three Mixamo FBX files.
public static class PlayerAssetSetup
{
    private const string PlayerPath = "Assets/Prefabs/Player.prefab";
    private const string ModelPath = "Assets/BodyGuards/Meshes/SkelMesh_Bodyguard_01.fbx";
    private const string MaterialPath = "Assets/BodyGuards/Meshes/Materials/Boduguard_01_D.mat";
    private const string ControllerPath = "Assets/Player/BodyguardPlayer.controller";

    [MenuItem("Tools/HW2/Rebind Player Assets")]
    public static void Rebind()
    {
        var modelImporter = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (modelImporter != null && modelImporter.animationType != ModelImporterAnimationType.Human)
        {
            modelImporter.animationType = ModelImporterAnimationType.Human;
            modelImporter.SaveAndReimport();
        }

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (model == null || prefab == null || material == null || controller == null)
        {
            Debug.LogError("Player setup: import Bodyguards before running Rebind Player Assets.");
            return;
        }

        var idle = LoadMotion("UnarmedIdle", true);
        var run = LoadMotion("UnarmedRun", true);
        var jump = LoadMotion("UnarmedJump", false);
        if (idle == null || run == null || jump == null)
        {
            Debug.LogError("Player setup: add all three Mixamo FBX files to Assets/Player/Animations first.");
            return;
        }

        ConvertMaterial(material);
        foreach (var child in controller.layers[0].stateMachine.states)
        {
            switch (child.state.name)
            {
                case "Idle": child.state.motion = idle; break;
                case "Run": child.state.motion = run; break;
                case "Jump": child.state.motion = jump; child.state.speed = 2f; break;
            }
            EditorUtility.SetDirty(child.state);
        }
        EditorUtility.SetDirty(controller);

        var player = PrefabUtility.LoadPrefabContents(PlayerPath);
        try
        {
            var visual = player.transform.Find("BodyguardVisual");
            bool missingMesh = visual == null;
            if (visual != null)
            {
                var meshes = visual.GetComponentsInChildren<SkinnedMeshRenderer>();
                missingMesh = meshes.Length == 0;
                foreach (var mesh in meshes)
                    if (mesh.sharedMesh == null) missingMesh = true;
            }

            if (missingMesh)
            {
                if (visual != null) Object.DestroyImmediate(visual.gameObject);
                var instance = Object.Instantiate(model, player.transform);
                instance.name = "BodyguardVisual";
                visual = instance.transform;
                PositionVisual(player.transform, visual);
            }

            var animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Player setup complete: Bodyguards model and three Mixamo motions are bound.");
    }

    private static AnimationClip LoadMotion(string name, bool loop)
    {
        string path = $"Assets/Player/Animations/{name}.fbx";
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return null;
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.SaveAndReimport();
        }

        var clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
        if (clips != null && clips.Length > 0 && clips[0].loopTime != loop)
        {
            clips[0].loopTime = loop;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            var clip = asset as AnimationClip;
            if (clip != null && !clip.name.StartsWith("__preview__")) return clip;
        }
        return null;
    }

    private static void ConvertMaterial(Material material)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null || material.shader == shader) return;
        var baseMap = material.GetTexture("_MainTex");
        var normalMap = material.GetTexture("_BumpMap");
        var occlusionMap = material.GetTexture("_OcclusionMap");
        material.shader = shader;
        material.SetTexture("_BaseMap", baseMap);
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BumpMap", normalMap);
        material.SetTexture("_OcclusionMap", occlusionMap);
        if (normalMap != null) material.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(material);
    }

    private static void PositionVisual(Transform player, Transform visual)
    {
        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
        var renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        visual.localScale *= 1.8f / bounds.size.y;
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        visual.position += Vector3.up * (player.position.y - 1f - bounds.min.y);
    }
}
