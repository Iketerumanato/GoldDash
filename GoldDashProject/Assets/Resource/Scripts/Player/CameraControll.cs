using UnityEngine;
using Unity.VisualScripting;

public class CameraControll : MonoBehaviour
{
    [Header("カメラ操作用のジョイスティック")]
    [SerializeField] DynamicJoystick dynamicjoystick;

    [Header("プレイヤーのオブジェクト")]
    [SerializeField] GameObject playerBody;

    [Header("カメラの感度")]
    [Range(50f, 150f)]
    [SerializeField] float rotationSensitivity = 100f;
    [Header("カメラの位置調整")]
    [SerializeField] Vector3 cameraOffset = new(0f, 0.7f, 0.4f);

    [Header("縦回転の制御")]
    float xRotation = 0f;
    [Range(90f, 120f)]
    [SerializeField] float CamXMaxClanpRot = 90f;
    [Range(-90f, -120f)]
    [SerializeField] float CamXMinClanpRot = -90f;

    float yRotation = 0f;

    [Header("プレイヤーの回転速度")]
    [SerializeField] float rotateSpeed = 100f;

    Camera PlayerCamera;
    readonly float CamNeer = 0.1f;
    readonly float CamFar = 1000f;

    void SetClippingPlanes(float near, float far)
    {
        if (PlayerCamera != null)
        {
            PlayerCamera.nearClipPlane = near;
            PlayerCamera.farClipPlane = far;
        }
    }

    private void Start()
    {
        PlayerCamera = new GameObject("PlayerCamera").AddComponent<Camera>();
        PlayerCamera.AddComponent<CamTest>();
        SetClippingPlanes(CamNeer, CamFar);
        PlayerCamera.transform.SetParent(transform);
        PlayerCamera.transform.localPosition = cameraOffset;
    }

    #region カメラの毎フレーム処理
    void Update()
    {
        // Horizontal入力でプレイヤーを回転させる
        float horizontalInput = Input.GetAxis("Horizontal") * rotationSensitivity * Time.deltaTime;
        yRotation += horizontalInput;

        // カメラの縦回転を更新
        xRotation = Mathf.Clamp(xRotation, CamXMinClanpRot, CamXMaxClanpRot);
        PlayerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // プレイヤーの回転を更新
        playerBody.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
    }
    #endregion
}