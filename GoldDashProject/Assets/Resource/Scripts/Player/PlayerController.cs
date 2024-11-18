using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //パラメータ
    [Header("インタラクト可能な距離")]
    [SerializeField] float interactableDistance = 10.0f;

    [Header("パンチの射程")]
    [SerializeField] float punchReachableDistance = 1f;

    [Header("正面から左右に何度までをキャラクターの正面と見做すか")]
    [Range(0f, 360f)]
    [SerializeField] float flontRange = 120f;
    //例えばこの値が一時的に0になれば、敵をどの角度からパンチしても金を奪える状態になる

    [Header("移動速度（マス／毎秒）")]
    [SerializeField] private float playerMoveSpeed = 1f;

    [Header("カメラ回転速度（度／毎秒）")]
    [SerializeField] private float cameraMoveSpeed = 1f;

    [Header("カメラの縦方向（X軸中心）回転の角度制限")]
    [Range(0f, 90f)]
    [SerializeField] private float camRotateLimitX = 90f;

    [Header("この高さ以下に落下したらリスポーン")]
    [SerializeField] private float fallThreshold = -10f;
    private Vector3 initialSpawnPosition; //リスポーン地点

    [Header("魔法を最大何個持てるか")]
    [SerializeField] private int magicSlot = 3;

    //UI関連
    //プレイヤーを移動させる左ジョイスティック
    private VariableJoystick leftJoystick;
    //カメラを操作する右ジョイスティック
    private DynamicJoystick rightJoystick;

    //カメラ関連
    //右スティックの操作対象になるカメラ
    private Camera playerCam;
    //カメラがX軸中心に何度回転しているか
    private float rotationX;
    //カメラがY軸中心に何度回転しているか
    private float rotationY;

    //パケット関連
    UdpGameClient udpGameClient = null; //パケット送信用。
    public ushort sessionID; //パケットに差出人情報を書くため必要

    //アニメーション関連
    private Animator playerAnimator;
    private readonly string strPlayerAnimSpeed = "ArmAnimationSpeed";
    private readonly string strPunchTrigger = "ArmPunchTrigger";
    [SerializeField] float smoothSpeed = 10f;

    //魔法関連
    //所持している魔法。可変長である必要がないため配列で
    private MagicData[] magicDataArray;

    private void Start()
    {
        //リスポーン地点の記録
        initialSpawnPosition = transform.position;

        //参照
        leftJoystick = GetComponentInChildren<VariableJoystick>(); //プレイヤープレハブの子のキャンバスにある
        rightJoystick = GetComponentInChildren<DynamicJoystick>(); //同上
        playerCam = Camera.main; //プレイヤーカメラにはMainCameraのタグがついている
        playerAnimator = GetComponent<Animator>();

        //コレクションのインスタンス作成
        magicDataArray = new MagicData[magicSlot];
    }

    private void LateUpdate()
    {
        #region 左スティックでプレイヤーを移動させる
        //WASDの入力をベクトルにする
        Vector3 playerMoveVec = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

        //ジョイスティックの入力があればそれで上書きする
        if (!Mathf.Approximately(leftJoystick.Horizontal, 0) || !Mathf.Approximately(leftJoystick.Vertical, 0)) //左スティックの水平垂直どちらの入力も"ほぼ0"でないなら
        playerMoveVec = new Vector3(leftJoystick.Horizontal, 0f, leftJoystick.Vertical); //上書き

        this.transform.Translate(playerMoveVec * playerMoveSpeed * Time.deltaTime); //求めたベクトルに移動速度とdeltaTimeをかけて座標書き換え
        playerAnimator.SetFloat(strPlayerAnimSpeed, playerMoveVec.magnitude); //走りモーション（仮）
        #endregion

        #region 右スティックでカメラを操作しつつ、プレイヤーを左右に回転させる
        //カメラ操作の入力がないなら回転しない
        if (!Mathf.Approximately(rightJoystick.Horizontal, 0) || !Mathf.Approximately(rightJoystick.Vertical, 0)) //右スティックの水平垂直どちらの入力も"ほぼ0"でないなら
        {
            //ジョイスティックの入力をオイラー角（〇軸を中心に△度回転、という書き方）にする
            //前提：カメラはZ軸の正の方向を向いている
            //水平の入力はY軸中心、垂直の入力はX軸中心になる。Z軸中心の回転はペテルギウス・ロマネコンティになってしまうため行わない。
            rotationX -= rightJoystick.Vertical * cameraMoveSpeed * Time.deltaTime; //Unityは左手座標系なので、上下の回転角度（X軸中心）にはマイナスをかけなければならない
            rotationX = Mathf.Clamp(rotationX, -camRotateLimitX, camRotateLimitX); //縦方向(X軸中心)回転には角度制限をつけないと宙返りしてしまう
            rotationY += rightJoystick.Horizontal * cameraMoveSpeed * Time.deltaTime; //Unityは左手座標系なので、左右の回転角度（Y軸中心）は加算でいい
            Vector3 cameraMoveEulers = new Vector3(rotationX, rotationY, 0f); //X軸だけマイナスをかけています

            //オイラー角をtransform.rotationに代入するため、クォータニオンに変換する
            playerCam.transform.rotation = Quaternion.Euler(cameraMoveEulers);

            //プレイヤーの正面方向を、カメラの正面方向（注視点の方向）と（XZ平面について）合わせる。カメラのX軸回転によって注視点のY座標（高さ）が変化するが、これは無視して0fを代入。
            this.transform.forward = new Vector3(playerCam.transform.forward.x, 0f, playerCam.transform.forward.z);
        }
        #endregion

        //タッチ（クリック）したものにインタラクト
        if (Input.GetMouseButtonDown(0)) Interact();

        //落下していたらリスポーン
        if (transform.position.y < fallThreshold) transform.position = initialSpawnPosition;
    }

    //エンティティに接触したとき
    private void OnTriggerEnter(Collider other)
    {
        //送信用クラスを外側のスコープで宣言しておく
        ActionPacket myActionPacket;
        Header myHeader;

        switch (other.tag)
        {
            case "GoldPile":
                //金貨の山に触れたというリクエスト送信。他のプレイヤーが先に触れていた場合、お金は入手できない。早い者勝ち。
                myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.GET_GOLDPILE, other.GetComponent<Entity>().EntityID);
                myHeader = new Header(this.sessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                udpGameClient.Send(myHeader.ToByte());
                break;
            default:
                break;
        }
    }

    //GameClientManagerからプレイヤーの生成タイミングで呼び出してudpGameClientへのアクセスを得る。
    public void GetUdpGameClient(UdpGameClient udpGameClient, ushort sessionID)
    {
        this.udpGameClient = udpGameClient;
        this.sessionID = sessionID;
    }

    //画面を「タッチしたとき」呼ばれる。オブジェクトに触ったかどうか判定 UpdateProcess -> if (Input.GetMouseButtonDown(0))
    public void Interact()
    {
        Debug.Log("レイ飛ばします");

        //カメラの位置からタッチした位置に向けrayを飛ばす
        RaycastHit hit;
        Ray ray = playerCam.ScreenPointToRay(Input.mousePosition);

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
    private void Punch(Vector3 hitPoint, float distance, ActorController actorController)
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
            myHeader = new Header(this.sessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
            udpGameClient.Send(myHeader.ToByte());

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
                myHeader = new Header(this.sessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                udpGameClient.Send(myHeader.ToByte());


                Debug.Log("正面送信");
            }
            else
            {
                //背面に命中させたことをパケット送信
                myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.HIT_BACK, actorController.SessionID, default, punchVec);
                myHeader = new Header(this.sessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                udpGameClient.Send(myHeader.ToByte());


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
        myHeader = new Header(this.sessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
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
