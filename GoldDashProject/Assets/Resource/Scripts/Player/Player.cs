using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("移動速度")]
    [SerializeField] float moveSpeed = 0.1f;
    [SerializeField] float rotationSpeed = 200f;

    [Header("ジャンプ力")]
    [SerializeField] float jumpPower = 0.2f;

    [Header("プレイヤーの最大HP")]
    [SerializeField] int maxPlayerHP = 10;
    int PlayerCurrentHP;

    [Header("この高さ以下に落下したらリスポーン")]
    [SerializeField] float fallThreshold = -10f;
    private Vector3 initialSpawnPosition;

    [SerializeField] DrawCircle drawCircle;
    [SerializeField] CameraControll cameraControll;

    [SerializeField] VariableJoystick variableJoystick;
    private Vector3 inputVector;

    #region ゲーム起動時必ず呼ばれる
    void Start()
    {
        //variableJoystick = FindAnyObjectByType<VariableJoystick>();
        initialSpawnPosition = transform.position;
        PlayerCurrentHP = maxPlayerHP;
    }
    #endregion

    private void FixedUpdate()
    {    
        // 移動
        MovePlayerJoystick(inputVector);
        MoveKey();

        // 落下時のリスポーン
        if (transform.position.y < fallThreshold) PlayerRespawn();
    }

    #region プレイヤーの操作と落下
    private void MovePlayerJoystick(Vector3 input)
    {
        // 移動
        input = transform.forward * variableJoystick.Vertical + transform.right * variableJoystick.Horizontal;
        transform.position -= moveSpeed * Time.deltaTime * input;
    }

    void MoveKey()
    {
        float moveDirection = 0;
        if (Input.GetKey(KeyCode.W))
        {
            moveDirection = -1;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            moveDirection = 1;
        }

        // 前進後退の移動
        transform.Translate(Vector3.forward * moveDirection * moveSpeed * Time.deltaTime);
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Tresure")) drawCircle.enabled = true;
    }
}