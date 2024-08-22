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

    private float verticalInput;

    [SerializeField] DrawCircle drawCircle;

    //public VariableJoystick variableJoystick;
    //private Vector3 inputVector;

    #region �Q�[���N�����K���Ă΂��
    void Start()
    {
        //variableJoystick = FindAnyObjectByType<VariableJoystick>();
        rig = GetComponent<Rigidbody>();
        initialSpawnPosition = transform.position;
        PlayerCurrentHP = maxPlayerHP;
    }
    #endregion

    private void Update()
    {

        verticalInput = Input.GetAxis("Vertical"); // W/S �܂��� �㉺���L�[

        // ���[�J���v���C���[�̏ꍇ�̂݃W���C�X�e�B�b�N�̓��͂��擾
        //float horizontal = variableJoystick.Horizontal;
        //float vertical = variableJoystick.Vertical;
        //// ���̓x�N�g�����X�V
        //inputVector = new Vector3(horizontal, 0, vertical);
        //// �T�[�o�[�ɓ��͒l�𑗐M
        //CmdSendInput(inputVector);

        // �W�����v
        if (Input.GetKey(KeyCode.Space)) Jump();
    }

    private void FixedUpdate()
    {
        // �v���C���[���ړ�
        Move(verticalInput);

        // �������̃��X�|�[��
        if (transform.position.y < fallThreshold) PlayerRespawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Tresure")) drawCircle.enabled = true;
    }

    #region �v���C���[�̑���Ɨ���
    //[Command]
    //void CmdSendInput(Vector3 input)
    //{
    //    // �T�[�o�[���œ��̓x�N�g�����X�V
    //    RPCMovePlayer(input);
    //    Debug.Log("Player is Moving : " + input);
    //}
    //[ClientRpc]
    //private void RPCMovePlayer(Vector3 input)
    //{
    //    Vector3 move = input * moveSpeed * Time.deltaTime;
    //    transform.Translate(move, Space.World);
    //}

    void Move(float vertical)
    {
        // WASD�L�[�̓��͂Ɋ�Â��ړ�
        Vector3 moveDirection = new Vector3(0, 0, vertical).normalized;
        moveDirection = transform.TransformDirection(moveDirection);

        // AddForce �ňړ�
        rig.AddForce(moveDirection * moveSpeed, ForceMode.VelocityChange);
    }

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

    #region �L�����o�X�̐���
    //[Command]
    //void CmdCanvasIns()
    //{
    //    RPCCanvusIns();
    //}
    //[ClientRpc]
    //void RPCCanvusIns()
    //{
    //    Instantiate(PlayerCanvas);
    //}
    #endregion
}