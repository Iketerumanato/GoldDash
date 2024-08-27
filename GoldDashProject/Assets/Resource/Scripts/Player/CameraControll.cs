using Unity.VisualScripting;
using UnityEngine;

public class CameraControll : MonoBehaviour
{
    [Header("�v���C���[�̃I�u�W�F�N�g")]
    [SerializeField] GameObject playerBody;
    [Header("�J��������p�̃W���C�X�e�B�b�N")]
    [SerializeField] DynamicJoystick cameramoveJoystick;
    [Header("�J�����̊��x")]
    [Range(50f, 150f)]
    [SerializeField] float joystickSensitivity = 100f;
    [Header("�J�����̈ʒu����")]
    [SerializeField] Vector3 cameraOffset = new(0f, 0.7f, 0.4f);
    [Header("�c��]�̐���")]
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

    public static CameraControll _cameracontrollIns { get; private set; }
    private void Awake()
    {
        if (_cameracontrollIns != null && _cameracontrollIns != this) Destroy(gameObject);
        else
        {
            _cameracontrollIns = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    #region �Q�[���N�����K���Ă΂��
    private void Start()
    {
        cameramoveJoystick = FindObjectOfType<DynamicJoystick>();
        PlayerCamera = new GameObject("PlayerCamera").AddComponent<Camera>();
        PlayerCamera.tag = "MainCamera";
        PlayerCamera.AddComponent<CamTest>();
        SetClippingPlanes(CamNeer, CamFar);
        PlayerCamera.transform.SetParent(transform);
        PlayerCamera.transform.localPosition = cameraOffset;
    }
    #endregion

    void SetClippingPlanes(float near, float far)
    {
        if (PlayerCamera != null)
        {
            PlayerCamera.nearClipPlane = near;
            PlayerCamera.farClipPlane = far;
        }
    }

    #region �J�����̖��t���[������
    void Update()
    {
        float horizontalInput = cameramoveJoystick.Horizontal * joystickSensitivity * Time.deltaTime;
        float verticalInput = cameramoveJoystick.Vertical * joystickSensitivity * Time.deltaTime;

        yRotation += horizontalInput;
        //yRotation = Mathf.Clamp(yRotation, CamYMinClanpRot, CamYMaxClanpRot);

        xRotation -= verticalInput;
        xRotation = Mathf.Clamp(xRotation, CamXMinClanpRot, CamXMaxClanpRot);
        // �J�����̏c��]���X�V
        PlayerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // �v���C���[�̉�]���X�V(����]���ꏏ��)
        Vector3 direction = new(xRotation, yRotation, 0f);
        Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
        playerBody.transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotateSpeed * Time.deltaTime);
        playerBody.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
    }
    #endregion

    public void OffCamera()
    {
        this.enabled = false;
        cameramoveJoystick.enabled = false;
    }

    public void ActiveCamera()
    {
        this.enabled = true;
        cameramoveJoystick.enabled = true;
    }
}