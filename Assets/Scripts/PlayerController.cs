using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("移動設定")]
    public float moveSpeed = 6f;
    public float turnSmoothTime = 0.1f; // 轉向平滑時間
    public float gravity = -9.81f;

    [Header("相機參考")]
    public Transform cameraTransform;

    private CharacterController controller;
    private Vector3 velocity;
    private float turnSmoothVelocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // 自動抓取 Main Camera
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // 1. 地面偵測與下壓微力
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 2. 讀取鍵盤輸入
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        // 3. 移動與轉向運算
        if (direction.magnitude >= 0.1f)
        {
            // 計算相機面向水平方向的旋轉角度 (Yaw)
            float camAngle = cameraTransform != null ? cameraTransform.eulerAngles.y : 0f;

            // 計算玩家應該面向的目標角度：輸入方向角 + 相機水平角
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + camAngle;

            // 平滑插值旋轉角，避免 180 度反向自轉死循環
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // 將角度轉為前進向量並移動
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * moveSpeed * Time.deltaTime);
        }

        // 4. 施加重力
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
