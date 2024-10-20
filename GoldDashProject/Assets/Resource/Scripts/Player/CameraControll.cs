using Unity.VisualScripting;
using UnityEngine;

public class CameraControll : MonoBehaviour
{
    [Header("プレイヤーのオブジェクト")]
    [SerializeField] GameObject playerBody;
    [Header("カメラ操作用のジョイスティック")]
    [SerializeField] DynamicJoystick cameramoveJoystick;
    [Header("カメラの感度")]
    [Range(50f, 150f)]
    [SerializeField] float joystickSensitivity = 100f;
    //[Header("カメラの位置調整")]
    //[SerializeField] Vector3 cameraOffset = new(0f, 0.3f, -0.07f);
    //Vector3 cameraRata = new(0f, 0f, 0f);
    [Header("縦回転の制御")]
    float xRotation = 0f;
    [Range(50f, 90f)]
    [SerializeField] float CamXMaxClanpRot = 90f;
    [Range(-90f, -50f)]
    [SerializeField] float CamXMinClanpRot = -90f;
    float yRotation = 0f;

    [SerializeField] float rotateSpeed = 100f;

    [SerializeField] Camera PlayerCamera;
    readonly float CamNeer = 0.1f;
    readonly float CamFar = 1000f;

    // 初期回転を保持する変数
    Quaternion initialCameraRotation;

    private void Start()
    {
        //CreateCamera();
        // カメラの初期回転を保持 (0, 180, 0)
        initialCameraRotation = Quaternion.Euler(0, 180, 0);
        PlayerCamera.transform.localRotation = initialCameraRotation;
        //PlayerCamera.AddComponent<CamTest>();
        //SetClippingPlanes(CamNeer, CamFar);
    }

    #region カメラの毎フレーム処理
    void Update()
    {
        float horizontalInput = cameramoveJoystick.Horizontal * joystickSensitivity * Time.deltaTime;
        float verticalInput = cameramoveJoystick.Vertical * joystickSensitivity * Time.deltaTime;

        yRotation += horizontalInput;

        xRotation -= verticalInput;
        xRotation = Mathf.Clamp(xRotation, CamXMinClanpRot, CamXMaxClanpRot);

        PlayerCamera.transform.localRotation = initialCameraRotation * Quaternion.Euler(xRotation, 0f, 0f);

        playerBody.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
    }
    #endregion

    #region カメラの生成と設定
    void CreateCamera()
    {
        cameramoveJoystick = FindObjectOfType<DynamicJoystick>();
        PlayerCamera = new GameObject("PlayerCamera").AddComponent<Camera>();
        PlayerCamera.tag = "MainCamera";
        PlayerCamera.AddComponent<CamTest>();
        SetClippingPlanes(CamNeer, CamFar);
        PlayerCamera.transform.SetParent(transform);
        //// カメラの位置と回転を設定
        //PlayerCamera.transform.localPosition = cameraOffset;  // カメラ位置を設定
        //PlayerCamera.transform.localRotation = Quaternion.Euler(cameraRata);
    }

    void SetClippingPlanes(float near, float far)
    {
        if (PlayerCamera != null)
        {
            PlayerCamera.nearClipPlane = near;
            PlayerCamera.farClipPlane = far;
        }
    }
    #endregion

    #region CameraOn/Off
    public void ActiveCameraControll()
    {
        this.enabled = true;
        cameramoveJoystick.enabled = true;
    }

    public void OffCameraControll()
    {
        this.enabled = false;
        cameramoveJoystick.enabled = false;
    }
    #endregion
}