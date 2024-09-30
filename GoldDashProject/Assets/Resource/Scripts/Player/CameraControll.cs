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
    [Header("カメラの位置調整")]
    [SerializeField] Vector3 cameraOffset = new(0f, 0.3f, -0.07f);
    Vector3 cameraRata = new(0f, 0f, 0f);
    [Header("縦回転の制御")]
    float xRotation = 0f;
    [Range(90f, 120f)]
    [SerializeField] float CamXMaxClanpRot = 90f;
    [Range(-90f, -120f)]
    [SerializeField] float CamXMinClanpRot = -90f;
    float yRotation = 0f;

    [SerializeField] float rotateSpeed = 100f;

    Camera PlayerCamera;
    readonly float CamNeer = 0.1f;
    readonly float CamFar = 1000f;

    private void Start()
    {
        CreateCamera();
    }

    #region カメラの毎フレーム処理
    void Update()
    {
        float horizontalInput = cameramoveJoystick.Horizontal * joystickSensitivity * Time.deltaTime;
        float verticalInput = cameramoveJoystick.Vertical * joystickSensitivity * Time.deltaTime;

        yRotation += horizontalInput;
        //yRotation = Mathf.Clamp(yRotation, CamYMinClanpRot, CamYMaxClanpRot);

        xRotation -= verticalInput;
        xRotation = Mathf.Clamp(xRotation, CamXMinClanpRot, CamXMaxClanpRot);
        // カメラの縦回転を更新
        PlayerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // プレイヤーの回転を更新(横回転も一緒に)
        Vector3 direction = new(xRotation, yRotation, 0f);
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
            playerBody.transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotateSpeed * Time.deltaTime);
            playerBody.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        }
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
        //PlayerCamera.transform.localPosition = cameraOffset;
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