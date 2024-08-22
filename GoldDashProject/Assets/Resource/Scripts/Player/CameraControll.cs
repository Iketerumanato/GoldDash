using UnityEngine;
using Unity.VisualScripting;

public class CameraControll : MonoBehaviour
{
    [Header("�J��������p�̃W���C�X�e�B�b�N")]
    [SerializeField] DynamicJoystick dynamicjoystick;

    [Header("�v���C���[�̃I�u�W�F�N�g")]
    [SerializeField] GameObject playerBody;

    [Header("�J�����̊��x")]
    [Range(50f, 150f)]
    [SerializeField] float rotationSensitivity = 100f;
    [Header("�J�����̈ʒu����")]
    [SerializeField] Vector3 cameraOffset = new(0f, 0.7f, 0.4f);

    [Header("�c��]�̐���")]
    float xRotation = 0f;
    [Range(90f, 120f)]
    [SerializeField] float CamXMaxClanpRot = 90f;
    [Range(-90f, -120f)]
    [SerializeField] float CamXMinClanpRot = -90f;

    float yRotation = 0f;

    [Header("�v���C���[�̉�]���x")]
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

    #region �J�����̖��t���[������
    void Update()
    {
        // Horizontal���͂Ńv���C���[����]������
        float horizontalInput = Input.GetAxis("Horizontal") * rotationSensitivity * Time.deltaTime;
        yRotation += horizontalInput;

        // �J�����̏c��]���X�V
        xRotation = Mathf.Clamp(xRotation, CamXMinClanpRot, CamXMaxClanpRot);
        PlayerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // �v���C���[�̉�]���X�V
        playerBody.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
    }
    #endregion
}