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
    public ushort SessionID { set; get; } //パケットに差出人情報を書くため必要

    Camera fpsCamera; //playerカメラ。Start()内でGetComponentInChildren<Camera>()して取得

    const float INTERACTABLE_DISTANCE = 10.0f;//この値は今すっげ～適当です。

    const float PUNCH_REACHABLE_DISTANCE = 1f;

    const float FRONT_RANGE = 120f; //正面から左右に何度までをキャラクターの正面と見做すか

    //GameClientManagerからプレイヤーの生成タイミングで呼び出してudpGameClientへのアクセスを得る。
    public void GetUdpGameClient(UdpGameClient udpGameClient, ushort sessionID)
    { 
        this.udpGameClient = udpGameClient;
        this.SessionID = sessionID;
    }

    //以下UIの操作などで呼び出されるメソッド。R3でやろっかな

    //画面を「タッチしたとき」呼ばれる。オブジェクトに触ったかどうか判定 UpdateProcess -> if (Input.GetMouseButtonDown(0))
    private void Interact(GameObject obj)
    {
        //カメラの位置からタッチした位置に向けrayを飛ばす
        RaycastHit hit;
        Ray ray = fpsCamera.ScreenPointToRay(Input.mousePosition);

        //rayがなにかに当たったら調べる
        //定数INTERACTABLE_DISTANCEでrayの長さを調整することでインタラクト可能な距離を制限できる
        if (Physics.Raycast(ray, out hit, INTERACTABLE_DISTANCE))
        {
            switch (hit.collider.gameObject.tag)
            {
                case "Enemy": //プレイヤーならパンチ
                    Punch(hit.point, hit.distance, hit.collider.gameObject.GetComponent<ActorController>());
                    break;
                case "Chest": //宝箱なら開錠を試みる
                    TryOpenChest(hit.point, hit.distance, hit.collider.gameObject.GetComponent<ChestController>());
                    break;
                
                //ドアをタッチで開けるならココ

                default: //そうでないものはインタラクト不可能なオブジェクトなので無視
                    break;
            }
        }
    }

    //パンチ。パンチを成立させたRaycastHit構造体のPointとDistanceを引数にもらおう
    private void Punch(Vector3 hitPoint, float distance, ActorController actorController)
    {
        //distanceを調べてしきい値を調べる
        if (distance < PUNCH_REACHABLE_DISTANCE)
        {
            //射程外なら一人称のスカモーション再生

            //スカしたことをパケット送信
            ActionPacket myPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.MISS);
            Header myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myPacket.ToByte());
            udpGameClient.Send(myHeader.ToByte());
        }
        else
        {
            //射程内なら一人称のパンチモーション再生
            //カメラを非同期で敵に向ける処理開始 UniTask

            //パンチが正面に当たったのか背面に当たったのか調べる
            Vector3 punchVec = hitPoint - this.transform.position;
            float angle = Vector3.Angle(punchVec, actorController.transform.forward);

            if (angle < FRONT_RANGE)
            {
                //正面に命中させたことをパケット送信
                ActionPacket myPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.HIT_FRONT, actorController.SessionID);
                Header myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myPacket.ToByte());
                udpGameClient.Send(myHeader.ToByte());
            }
            else
            {
                //背面に命中させたことをパケット送信
                ActionPacket myPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.HIT_FRONT, actorController.SessionID);
                Header myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myPacket.ToByte());
                udpGameClient.Send(myHeader.ToByte());
            }
        }
    }

    //宝箱なら開錠を試みる。パンチと同様RaycastHit構造体から引数をもらう。消えゆく宝箱だったり、他プレイヤーが使用中の宝箱は開錠できない。
    private void TryOpenChest(Vector3 hitPoint, float distance, ChestController chestController)
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