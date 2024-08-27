using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("�ړ����x")]
    [SerializeField] float moveSpeed = 0.1f;
    [Header("�W�����v��")]
    [SerializeField] float jumpPower = 0.2f;

    [Header("�v���C���[�̍ő�HP")]
    [SerializeField] int maxPlayerHP = 10;
    int PlayerCurrentHP;

    [Header("���̍����ȉ��ɗ��������烊�X�|�[��")]
    [SerializeField] float fallThreshold = -10f;
    private Vector3 initialSpawnPosition;

    private Rigidbody rig;

    //private float verticalInput;

    [SerializeField] DrawCircle drawCircle;
    [SerializeField] CameraControll cameraControll;

    public VariableJoystick variableJoystick;
    private Vector3 inputVector;

    #region �Q�[���N�����K���Ă΂��
    void Start()
    {
        //variableJoystick = FindAnyObjectByType<VariableJoystick>();
        rig = GetComponent<Rigidbody>();
        initialSpawnPosition = transform.position;
        PlayerCurrentHP = maxPlayerHP;
    }
    #endregion

    private void FixedUpdate()
    {
        //verticalInput = Input.GetAxis("Vertical"); // W/S �܂��� �㉺���L�[

        // �ړ�
        MovePlayerJoystick(inputVector);

        // �W�����v
        if (Input.GetKey(KeyCode.Space)) Jump();

        // �������̃��X�|�[��
        if (transform.position.y < fallThreshold) PlayerRespawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Tresure")) drawCircle.enabled = true;
    }

    #region �v���C���[�̑���Ɨ���
    private void MovePlayerJoystick(Vector3 input)
    {
        // �ړ�
        input = transform.forward * variableJoystick.Vertical + transform.right * variableJoystick.Horizontal;
        transform.position += moveSpeed * Time.deltaTime * input;
    }

    //void Move(float vertical)
    //{
    //    // WASD�L�[�̓��͂Ɋ�Â��ړ�
    //    Vector3 moveDirection = new Vector3(0, 0, vertical).normalized;
    //    moveDirection = transform.TransformDirection(moveDirection);

    //    // AddForce �ňړ�
    //    rig.AddForce(moveDirection * moveSpeed, ForceMode.VelocityChange);
    //}

    private void Jump()
    {
        if (Mathf.Abs(rig.velocity.y) < 0.01f) // �n�ʂɂ���Ƃ������W�����v
        {
            rig.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
        }
    }
    #endregion

    #region �_���[�W���󂯂�
    public void CmdTakeDamage(int attackPoint)
    {
        PlayerCurrentHP -= attackPoint;
        if (PlayerCurrentHP <= 0)
        {
            PlayerRespawn();
            PlayerCurrentHP = maxPlayerHP;
        }
    }
    #endregion

    #region ���X�|�[��
    void PlayerRespawn()
    {
        transform.position = initialSpawnPosition;
        Debug.Log("RpcRespawn called on client");
    }
    #endregion
}