using Cysharp.Threading.Tasks;
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
    [SerializeField] Animator playerAnimator;
    readonly string RunAnimation = "IsRun";

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
        float inputMagnitude = input.magnitude;

        if (inputMagnitude > 0.5f)
            playerAnimator.SetBool(RunAnimation, true);
        else
            playerAnimator.SetBool(RunAnimation, false); // 走るアニメーション解除
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

    //以下手動マージ予定
    UdpGameClient udpGameClient = null; //パケット送信用。

    //GameClientManagerからプレイヤーの生成タイミングで呼び出してudpGameClientへのアクセスを得る。
    public void GetUdpGameClient(UdpGameClient udpGameClient)
    { 
        this.udpGameClient = udpGameClient;
    }

    //以下UIの操作などで呼び出されるメソッド。R3でやろっかな
    //画面を「タッチしたとき」呼ばれる。オブジェクトに触ったかどうか判定
    private void Interact()
    {
        //カメラの位置からレイを飛ばす
        //RaycastHit hit

        //インタラクト対象でないならreturn。tagで判断　...A
        //インタラクト対象なら距離を調べる。しきい値より遠いならreturn. ...B
        //AとB,多分Aの方が処理高速だと思うのでAを先に実行しています

        //プレイヤーならパンチ
        //Punch(hit.point, hit.distance)
        //宝箱なら開錠を試みる。消えゆく宝箱だったり、他プレイヤーが使用中の宝箱は開錠できない。
        //TryOpenChest(hit.point, hit.distance hit.collider.gameObject.GetComponent<ChestController>())
    }

    //パンチ。パンチを成立させたRaycastHit構造体のPointとDistanceを引数にもらおう
    private void Punch(Vector3 hitPoint, float distance)
    { 
        //distanceを調べてしきい値を調べる

        //射程外なら一人称のスカモーション再生
        //スカしたことをパケット送信

        //射程内なら一人称のパンチモーション再生
        //カメラを非同期で敵に向ける処理開始 UniTask

        //パケット送信
    }

    //宝箱なら開錠を試みる。パンチと同様RaycastHit構造体から引数をもらう。消えゆく宝箱だったり、他プレイヤーが使用中の宝箱は開錠できない。
    private void TryOpenChest(Vector3 hitPoint, float distance /*ChestController chestController*/)
    {
        //既に空いていないたらなにもしない

        //距離が遠すぎる場合、宝箱の付近までオートラン開始。スティック入力があれば即時解除
        //オートランの目標地点算出

        //宝箱のID取得。サーバー側で開錠中ステータスにする
        //ACTIONパケット送信

        //キャンバス表示（まわせ）

        //非同期回転処理開始
        //以下非同期ローカル関数で

        //OpenChest

        //async UniTask OpenChest()
        //{
        //    //いつでも中断できるようにする

        //    //成功したらパケット送信準備
        //    //宝箱のID取得

        //    //パケット送信
        //}
    }
}