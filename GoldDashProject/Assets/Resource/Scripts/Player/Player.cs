using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("移動速度")]
    [SerializeField] float moveSpeed = 0.1f;
    [Header("ジャンプ力")]
    [SerializeField] float jumpPower = 0.2f;

    [Header("プレイヤーの最大HP")]
    [SerializeField] int maxPlayerHP = 10;
    int PlayerCurrentHP;

    [Header("この高さ以下に落下したらリスポーン")]
    [SerializeField] float fallThreshold = -10f;
    private Vector3 initialSpawnPosition;

    private Rigidbody rig;

    private float verticalInput;

    [SerializeField] DrawCircle drawCircle;

    //public VariableJoystick variableJoystick;
    //private Vector3 inputVector;

    #region ゲーム起動時必ず呼ばれる
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

        verticalInput = Input.GetAxis("Vertical"); // W/S または 上下矢印キー

        // ローカルプレイヤーの場合のみジョイスティックの入力を取得
        //float horizontal = variableJoystick.Horizontal;
        //float vertical = variableJoystick.Vertical;
        //// 入力ベクトルを更新
        //inputVector = new Vector3(horizontal, 0, vertical);
        //// サーバーに入力値を送信
        //CmdSendInput(inputVector);

        // ジャンプ
        if (Input.GetKey(KeyCode.Space)) Jump();
    }

    private void FixedUpdate()
    {
        // プレイヤーを移動
        Move(verticalInput);

        // 落下時のリスポーン
        if (transform.position.y < fallThreshold) PlayerRespawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Tresure")) drawCircle.enabled = true;
    }

    #region プレイヤーの操作と落下
    //[Command]
    //void CmdSendInput(Vector3 input)
    //{
    //    // サーバー側で入力ベクトルを更新
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
        // WASDキーの入力に基づく移動
        Vector3 moveDirection = new Vector3(0, 0, vertical).normalized;
        moveDirection = transform.TransformDirection(moveDirection);

        // AddForce で移動
        rig.AddForce(moveDirection * moveSpeed, ForceMode.VelocityChange);
    }

    private void Jump()
    {
        if (Mathf.Abs(rig.velocity.y) < 0.01f) // 地面にいるときだけジャンプ
        {
            rig.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
        }
    }
    #endregion

    #region ダメージを受ける
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

    #region リスポーン
    void PlayerRespawn()
    {
        transform.position = initialSpawnPosition;
        Debug.Log("RpcRespawn called on client");
    }
    #endregion

    #region キャンバスの生成
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