using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;

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
        if (Input.GetMouseButtonDown(0))
        {
            player.Interact();
        }
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
    //パケット関連
    UdpGameClient udpGameClient = null; //パケット送信用。
    public ushort SessionID { set; get; } //パケットに差出人情報を書くため必要

    //playerの一人称カメラ
    Camera fpsCamera;

    //パラメータ
    [Header("インタラクト可能な距離")]
    [SerializeField] float interactableDistance = 10.0f;

    [Header("パンチの射程")]
    [SerializeField] float punchReachableDistance = 1f;

    [Header("正面から左右に何度までをキャラクターの正面と見做すか")]
    [SerializeField] float flontRange = 120f; //例えばこの値が一時的に0になれば、敵をどの角度からパンチしても金を奪える状態になる

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
    readonly string strPlayerAnimSpeed = "ArmAnimationSpeed";
    readonly string strPunchTrigger = "ArmPunchTrigger";

    #region 殴られた判定で使うstring型のTrigger
    readonly string strHitedFrontTrigger = "HitedFrontArmTrigger";
    readonly string strHitedBackTrigger = "HitedBackArmTrigger";
    #endregion

    [SerializeField] float smoothSpeed = 10f;

    [SerializeField] ActorController myActor;
    [SerializeField] ShakeEffect shakeEffect;

    private VibrationManager vibrationManager;

    void Start()
    {
        ChangePlayerState(new NormalState());
        //variableJoystick = FindAnyObjectByType<VariableJoystick>();
        initialSpawnPosition = transform.position;
        PlayerCurrentHP = maxPlayerHP;

        //カメラ取得
        fpsCamera = GetComponentInChildren<Camera>();

        vibrationManager = new();
    }

    private void Update()
    {
        //if (myActor.Gold != oldGold)
        //{
        //    currentGoldText.text = $"Gold: { myActor.Gold}";
        //    oldGold = myActor.Gold;
        //}
    }

    private void FixedUpdate()
    {
        _playerCurrentState.UpdateProcess(this);
        // 落下時のリスポーン
        if (transform.position.y < fallThreshold) PlayerRespawn();
    }

    #region プレイヤーの操作と落下
    public void MovePlayerJoystick(Vector3 input)
    {
        input = transform.forward * variableJoystick.Vertical + transform.right * variableJoystick.Horizontal;

        if (input.magnitude > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(input);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);

            // プレイヤーの移動
            transform.position -= moveSpeed * Time.deltaTime * input;

            // アニメーションの遷移 (BlendSpeedの補間)
            float inputMagnitude = Mathf.Sqrt(input.sqrMagnitude); ;
            playerAnimator.SetFloat(strPlayerAnimSpeed, inputMagnitude);
        }
        else playerAnimator.SetFloat(strPlayerAnimSpeed, 0f);//動きが止まった時はアニメーションの停止
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

    //エンティティに接触したとき
    private void OnTriggerEnter(Collider other)
    {
        //送信用クラスを外側のスコープで宣言しておく
        ActionPacket myActionPacket;
        Header myHeader;

        //if (other.gameObject.CompareTag("Tresure")) drawCircle.enabled = true;

        switch (other.tag)
        {
            case "GoldPile":
                //金貨の山に触れたというリクエスト送信。（他のプレイヤーが先に触れていた場合、お金は入手できない。早い者勝ち。）
                myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.GET_GOLDPILE, other.GetComponent<Entity>().EntityID);
                myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                udpGameClient.Send(myHeader.ToByte());
                Debug.Log("金貨Getリクエスト。");
                break;
            default:
                break;
        }
    }

    //以下手動マージ予定

    //GameClientManagerからプレイヤーの生成タイミングで呼び出してudpGameClientへのアクセスを得る。
    public void GetUdpGameClient(UdpGameClient udpGameClient, ushort sessionID)
    {
        this.udpGameClient = udpGameClient;
        this.SessionID = sessionID;
    }

    //以下UIの操作などで呼び出されるメソッド。R3でやろっかな

    //画面を「タッチしたとき」呼ばれる。オブジェクトに触ったかどうか判定 UpdateProcess -> if (Input.GetMouseButtonDown(0))
    public void Interact()
    {
        Debug.Log("レイ飛ばします");

        //カメラの位置からタッチした位置に向けrayを飛ばす
        RaycastHit hit;
        Ray ray = fpsCamera.ScreenPointToRay(Input.mousePosition);

        //rayがなにかに当たったら調べる
        //定数INTERACTABLE_DISTANCEでrayの長さを調整することでインタラクト可能な距離を制限できる
        if (Physics.Raycast(ray, out hit, interactableDistance))
        {

            Debug.Log(hit.collider.gameObject.name);

            switch (hit.collider.gameObject.tag)
            {
                case "Enemy": //プレイヤーならパンチ
                    Debug.Log("Punch入りたい");
                    Punch(hit.point, hit.distance, hit.collider.gameObject.GetComponent<ActorController>());
                    break;
                case "Chest": //宝箱なら開錠を試みる
                    TryOpenChest(hit.point, hit.distance, hit.collider.gameObject.GetComponent<Chest>());
                    break;

                //ドアをタッチで開けるならココ

                default: //そうでないものはインタラクト不可能なオブジェクトなので無視
                    break;
            }
        }
    }

    //パンチ。パンチを成立させたRaycastHit構造体のPointとDistanceを引数にもらおう
    async UniTaskVoid Punch(Vector3 hitPoint, float distance, ActorController actorController)
    {
        Debug.Log("Punch入った");

        //送信用クラスを外側のスコープで宣言しておく
        ActionPacket myActionPacket;
        Header myHeader;

        //distanceを調べてしきい値を調べる
        if (distance > punchReachableDistance)
        {
            //射程外なら一人称のスカモーション再生(現在通常のパンチのモーションを再生)
            playerAnimator.SetTrigger(strPunchTrigger);
            //スカしたことをパケット送信
            myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.MISS);
            myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
            udpGameClient.Send(myHeader.ToByte());
            await UniTask.Delay(400);
            shakeEffect.ShakeCameraEffect(ShakeEffect.ShakeType.Small);
            vibrationManager.VibrateWithAmplitude(125, 35);
            Debug.Log("スカ送信");
        }
        else
        {
            //射程内なら一人称のパンチモーション再生
            playerAnimator.SetTrigger(strPunchTrigger);
            //カメラを非同期で敵に向ける処理開始 UniTask

            //パンチが正面に当たったのか背面に当たったのか調べる
            Vector3 punchVec = hitPoint - this.transform.position;
            float angle = Vector3.Angle(punchVec, actorController.transform.forward);

            if (angle < flontRange)
            {
                //正面に命中させたことをパケット送信
                myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.HIT_FRONT, actorController.SessionID);
                myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                udpGameClient.Send(myHeader.ToByte());
                await UniTask.Delay(200);
                shakeEffect.ShakeCameraEffect(ShakeEffect.ShakeType.Medium);
                vibrationManager.VibrateWithAmplitude(250,75);
                Debug.Log("正面送信");
            }
            else
            {
                //背面に命中させたことをパケット送信
                myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.HIT_BACK, actorController.SessionID, default, punchVec);
                myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                udpGameClient.Send(myHeader.ToByte());
                await UniTask.Delay(200);
                shakeEffect.ShakeCameraEffect(ShakeEffect.ShakeType.Large);
                vibrationManager.VibrateWithAmplitude(500,150);
                Debug.Log("背面送信");
            }
        }
    }

    //宝箱なら開錠を試みる。パンチと同様RaycastHit構造体から引数をもらう。消えゆく宝箱だったり、他プレイヤーが使用中の宝箱は開錠できない。
    private void TryOpenChest(Vector3 hitPoint, float distance, Chest chestController)
    {
        //送信用クラスを宣言しておく
        ActionPacket myActionPacket;
        Header myHeader;

        //仮！！！！！！
        //背面に命中させたことをパケット送信
        myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.OPEN_CHEST_SUCCEED, chestController.EntityID);
        myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
        udpGameClient.Send(myHeader.ToByte());

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