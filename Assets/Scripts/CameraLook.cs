using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLook : MonoBehaviour
{
    [Header("滑鼠靈敏度")]
    public float mouseSensitivity = 300f;

    [Header("垂直視角限制")]
    public float minPitch = -20f; // 往上看限制
    public float maxPitch = 60f;  // 往下看限制

    private float pitch = 0f;
    private float yaw = 0f;

    void Start()
    {
        // 鎖定滑鼠游標在遊戲視窗中央並隱藏
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    void LateUpdate()
    {
        if (!AdvancedGame.CanControl) return;
        // 讀取滑鼠滑動
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY; // 滑鼠向上推，視角往上看
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // 旋轉 CameraTarget，VCam 會隨之改變注視方向與環繞位置
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
