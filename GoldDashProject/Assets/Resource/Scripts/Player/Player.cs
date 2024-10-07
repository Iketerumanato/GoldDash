using UnityEngine;

public interface IPlayerState
{
    void EnterState(Player player);
    void UpdateProcess(Player player);
    void ExitState(Player player);
}

//通常の状態
public class NormalState : IPlayerState
{
    private Vector3 inputVector;

    public void EnterState(Player player)
    {
        Debug.Log("Player操作中");
    }

    public void UpdateProcess(Player player)
    {
        player.MovePlayerJoystick(inputVector);
        player.MoveKey();
    }

    public void ExitState(Player player)
    {
        Debug.Log("Playerの状態変更");
    }
}

//プレイヤーが動けなくなった時
public class IncapacitatedState : IPlayerState
{
    public void EnterState(Player player)
    {
        Debug.Log("Playerに対して何かしらのアクション");
    }

    public void UpdateProcess(Player player)
    {
        Debug.Log("Player行動不能中");
    }

    public void ExitState(Player player)
    {
        Debug.Log("Playerの気絶解除");
    }
}

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

    private IPlayerState _playerCurrentState;
    public Animator playerAnimator;

    #region ゲーム起動時必ず呼ばれる
    void Start()
    {
        ChangePlayerState(new NormalState());
       //variableJoystick = FindAnyObjectByType<VariableJoystick>();
       initialSpawnPosition = transform.position;
        PlayerCurrentHP = maxPlayerHP;
    }
    #endregion

    private void FixedUpdate()
    {
        _playerCurrentState.UpdateProcess(this);
        // 落下時のリスポーン
        if (transform.position.y < fallThreshold) PlayerRespawn();
    }

    #region プレイヤーの操作と落下
    public void MovePlayerJoystick(Vector3 input)
    {
        // 移動
        input = transform.forward * variableJoystick.Vertical + transform.right * variableJoystick.Horizontal;
        transform.position -= moveSpeed * Time.deltaTime * input;
        playerAnimator.SetFloat("BlendSpeed", Mathf.Max(Mathf.Abs(input.x), Mathf.Abs(input.z)));
    }

    public void MoveKey()
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

    //Stateの切り替え
    public void ChangePlayerState(IPlayerState newState)
    {
        if (_playerCurrentState != null) _playerCurrentState.ExitState(this);
        _playerCurrentState = newState;
        _playerCurrentState.EnterState(this);
    }

    //宝箱に接触したとき
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Tresure")) drawCircle.enabled = true;
    }
}