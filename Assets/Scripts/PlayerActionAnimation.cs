using System;
using UnityEngine;

// Original upper-body skeletal animation, layered after Mixamo locomotion.
// Direction keyframes are in player space, independent of the imported bone axes.
[DefaultExecutionOrder(100)]
public class PlayerActionAnimation : MonoBehaviour
{
    public const float ThrowDuration = .75f;
    public const float ReleaseTime = .32f;
    public const float ReloadDuration = 1.5f;
    public event Action PoseApplied;
    public bool Busy => action != ActionKind.None;
    public bool IsReloading => action == ActionKind.Reload;
    public bool HasHumanoidRig { get; private set; }
    public Transform ThrowHand { get; private set; }
    public float Progress => Busy ? elapsed / duration : 0;
    private enum ActionKind { None, Throw, Reload }
    private ActionKind action;
    private float elapsed, duration;
    private bool released;
    private Action release;
    private Animator animator;
    private Transform rightArm, rightForearm, leftArm, leftForearm, leftHand, chest;
    private GameObject heldStone;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        HasHumanoidRig = animator != null && animator.isHuman && animator.avatar != null && animator.avatar.isValid;
        if (!HasHumanoidRig) return;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        foreach (var skin in animator.GetComponentsInChildren<SkinnedMeshRenderer>()) skin.updateWhenOffscreen = true;
        rightArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        rightForearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        ThrowHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        leftArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        leftForearm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        HasHumanoidRig = rightArm != null && rightForearm != null && ThrowHand != null && leftArm != null && leftForearm != null && leftHand != null;
    }

    public bool Throw(Action onRelease)
    {
        if (Busy) return false;
        Begin(ActionKind.Throw, ThrowDuration);
        release = onRelease;
        return true;
    }
    public bool Reload()
    {
        if (Busy) return false;
        Begin(ActionKind.Reload, ReloadDuration);
        return true;
    }
    private void Begin(ActionKind kind, float seconds)
    {
        action = kind; elapsed = 0; duration = seconds; released = false;
        if (HasHumanoidRig)
        {
            heldStone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            heldStone.name = "Held throwing stone";
            var collider = heldStone.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            heldStone.transform.SetParent(ThrowHand, false);
            heldStone.transform.localPosition = new Vector3(.07f, 0, 0);
            heldStone.transform.localScale = Vector3.one * (.13f / Mathf.Max(.001f, Mathf.Abs(ThrowHand.lossyScale.x)));
            var player = GetComponent<PlayerController>();
            var stoneRenderer = player != null && player.projectilePrefab != null ? player.projectilePrefab.GetComponentInChildren<Renderer>() : null;
            if (stoneRenderer != null) heldStone.GetComponent<Renderer>().sharedMaterial = stoneRenderer.sharedMaterial;
        }
    }
    private void LateUpdate()
    {
        if (!Busy) return;
        if (!GameManager.IsActive || (SceneTransition.Instance != null && SceneTransition.Instance.Busy))
        { Cancel(); return; }
        if (!AdvancedGame.CanControl) return;
        elapsed = Mathf.Min(elapsed + Time.deltaTime, duration);
        float t = elapsed / duration;
        float weight = Mathf.Min(Mathf.SmoothStep(0, 1, t / .14f), Mathf.SmoothStep(0, 1, (1 - t) / .2f));
        if (HasHumanoidRig)
        {
            if (chest != null)
            {
                float twist = action == ActionKind.Throw ? Mathf.Lerp(-20, 15, Mathf.Clamp01((t - .3f) / .25f)) : -8;
                chest.rotation = Quaternion.AngleAxis(twist * weight, transform.up) * chest.rotation;
            }
            Vector3 upper, lower;
            if (action == ActionKind.Throw)
            {
                float swing = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .28f) / .2f));
                upper = Vector3.Lerp(new Vector3(.8f, .7f, -.55f), new Vector3(.15f, -.1f, 1), swing);
                lower = Vector3.Lerp(new Vector3(-.2f, .7f, -.7f), new Vector3(-.1f, -.25f, 1), swing);
                PointArm(rightArm, rightForearm, ThrowHand, upper, lower, weight);
                PointArm(leftArm, leftForearm, leftHand, new Vector3(-.65f, -.6f, .2f), new Vector3(-.1f, -.4f, .8f), weight);
            }
            else
            {
                float scoop = Mathf.Sin(Mathf.Clamp01((t - .2f) / .65f) * Mathf.PI);
                upper = new Vector3(.45f, -.8f + scoop * .45f, .3f);
                lower = Vector3.Lerp(new Vector3(-.5f, -.5f, -.3f), new Vector3(-.6f, .4f, .8f), scoop);
                PointArm(rightArm, rightForearm, ThrowHand, upper, lower, weight);
                PointArm(leftArm, leftForearm, leftHand, new Vector3(-.5f, -.6f, .35f), new Vector3(.65f, .1f, .65f), weight);
            }
            if (heldStone != null)
            {
                heldStone.SetActive(action == ActionKind.Throw ? !released : t > .35f && t < .85f);
            }
        }
        // Release after posing the arm, so the projectile begins at the animated hand.
        if (action == ActionKind.Throw && !released && elapsed >= ReleaseTime)
        {
            released = true;
            if (heldStone != null) heldStone.SetActive(false);
            release?.Invoke();
            release = null;
        }
        PoseApplied?.Invoke();
        if (elapsed >= duration) Cancel();
    }
    private void PointArm(Transform upper, Transform forearm, Transform hand, Vector3 upperDirection, Vector3 lowerDirection, float weight)
    {
        Aim(upper, forearm, transform.TransformDirection(upperDirection), weight);
        Aim(forearm, hand, transform.TransformDirection(lowerDirection), weight);
    }
    private static void Aim(Transform bone, Transform child, Vector3 direction, float weight)
    {
        Quaternion target = Quaternion.FromToRotation(child.position - bone.position, direction.normalized) * bone.rotation;
        bone.rotation = Quaternion.Slerp(bone.rotation, target, weight);
    }
    private void Cancel()
    {
        action = ActionKind.None;
        release = null;
        if (heldStone != null) Destroy(heldStone);
    }
    private void OnDisable() { Cancel(); }
}
