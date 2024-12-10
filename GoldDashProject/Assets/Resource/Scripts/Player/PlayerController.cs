using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

#region プレイヤーのState(通常,状態異常)
public interface IPlayerState
{
    //そのステートに入る
    void EnterState(PlayerController playerController);

    //そのステートで毎フレーム呼び出される処理群
    void UpdateProcess(PlayerController playerController);

    //そのステートから出る
    void ExitState(PlayerController playerController);
}

//通常の状態
public class NormalState : IPlayerState
{
    public void EnterState(PlayerController playerController)
    {
        Debug.Log("Player通常状態に移行");
    }

    public void UpdateProcess(PlayerController playerController)
    {
        playerController.ControllPlayerLeftJoystick();

        if (!playerController.isTouchUI &&
            playerController.isControllCam) playerController.ControllPlayerRightJoystick();

        playerController.UIInteract();

        //1点以上のタッチまたはクリックが確認されたらインタラクト
        if (Input.touchCount > 0 || Input.GetMouseButtonDown(0))
        {
            playerController.Interact();
        }

        playerController.PlayerRespawn();
    }

    public void ExitState(PlayerController playerController)
    {
        Debug.Log("Playerの状態変更");
    }
}

//プレイヤーが動けなくなった時(雷や罠などで)
public class IncapacitatedState : IPlayerState
{
    public void EnterState(PlayerController playerController)
    {
        Debug.Log("Playerに対して何かしらのアクション");
    }

    public void UpdateProcess(PlayerController playerController)
    {
        Debug.Log("Player行動不能中");
    }

    public void ExitState(PlayerController playerController)
    {
        Debug.Log("Playerの気絶解除");
    }
}
#endregion

public class PlayerController : MonoBehaviour
{
    //パラメータ
    [Header("インタラクト可能な距離")]
    [SerializeField] float interactableDistance = 10.0f;

    [Header("パンチの射程")]
    [SerializeField] float punchReachableDistance = 1f;

    [Header("パンチのクールダウン時間（ミリ秒）")]
    [SerializeField] int punchCooldownTime = 1000;

    [Header("正面から左右に何度までをキャラクターの正面と見做すか")]
    [Range(0f, 360f)]
    [SerializeField] float flontRange = 120f;
    //例えばこの値が一時的に0になれば、敵をどの角度からパンチしても金を奪える状態になる

    [Header("背面を殴られたときの水平方向への吹っ飛び倍率")]
    [SerializeField] float blownPowerHorizontal = 1f;

    [Header("背面を殴られたときの垂直方向への吹っ飛び倍率")]
    [SerializeField] float blownPowerVertical = 1f;

    [Header("背面を殴られてから金貨を拾えるようになるまでの時間（ミリ秒）")]
    [SerializeField] int forbidPickTime = 1000;

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

    //プレイヤーの現在のState
    private IPlayerState _playerControllState;

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

    //カメラを振動させるためのコンポーネント
    private ShakeEffect shakeEffect;

    //パケット関連
    //GameClientManagerからプレイヤーの生成タイミングでsetterを呼び出し
    public UdpGameClient UdpGameClient { set; get; } //パケット送信用。
    public ushort SessionID { set; get; } //パケットに差出人情報を書くため必要

    [Header("以下演出関連")]
    [SerializeField] PlayerAnimator playerAnimator;

    //パンチのクールダウン管理用
    private bool isPunchable = true; //punch + ableなので単に「パンチ可能」という意味だけど、英語圏のスラングでは「殴りたくなる」みたいな意味になるそうですよ。（例：punchable face）

    //吹っ飛び関連
    private Rigidbody _rigidbody; //吹っ飛ぶためのrigidbody
    private bool isPickable = true; //吹っ飛んでいる間金貨を拾えないようにする
    CancellationTokenSource forbidPickCts; //短時間で何度も吹っ飛ばしを受けた時に、発生中の金貨獲得禁止時間を延長するためにunitaskを停止させる必要がある

    [Header("UI関連")]
    [SerializeField] Camera MagicButtonCam;
    [SerializeField] LayerMask MagicButtonLayer;
    [SerializeField] UIFade uiFade;
    const string EnemyTag = "Enemy";
    const string ChestTag = "Chest";
    const string MagicButtonTag = "MagicButton";
    const string MagicButtonBackTag = "MagicButtonBack";
    public bool isTouchUI = false;
    public bool isControllCam = true;
    private Vector3 dragStartPos;
    private MagicButton currentMagicButton;

    private void Start()
    {
        //stateをノーマルにする
        ChangePlayerState(new NormalState());

        //リスポーン地点の記録
        initialSpawnPosition = transform.position;

        //参照
        leftJoystick = GetComponentInChildren<VariableJoystick>(); //プレイヤープレハブの子のキャンバスにある
        rightJoystick = GetComponentInChildren<DynamicJoystick>(); //同上
        playerCam = Camera.main; //プレイヤーカメラにはMainCameraのタグがついている
        shakeEffect = GetComponent<ShakeEffect>();
        _rigidbody = GetComponent<Rigidbody>();

        //インスタンス作成
        forbidPickCts = new CancellationTokenSource();

        //振動させたいカメラを指定してtransformをshakeEffectに渡す
        shakeEffect.shakeCameraTransform = playerCam.transform;

    }

    private void Update()
    {
        _playerControllState.UpdateProcess(this);
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
                if (!isPickable) break; //金貨を拾えない状態にされているならbreakする。
                //金貨の山に触れたというリクエスト送信。他のプレイヤーが先に触れていた場合、お金は入手できない。早い者勝ち。
                myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.GET_GOLDPILE, other.GetComponent<Entity>().EntityID);
                myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                UdpGameClient.Send(myHeader.ToByte());
                break;
            default:
                break;
        }
    }

    #region ジョイスティックによるキャラクターとカメラの操作
    public void ControllPlayerLeftJoystick()
    {
        //WASDの入力をベクトルにする
        Vector3 playerMoveVec = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

        //ジョイスティックの入力があればそれで上書きする
        if (!Mathf.Approximately(leftJoystick.Horizontal, 0) || !Mathf.Approximately(leftJoystick.Vertical, 0)) //左スティックの水平垂直どちらの入力も"ほぼ0"でないなら
            playerMoveVec = new Vector3(leftJoystick.Horizontal, 0f, leftJoystick.Vertical); //上書き
        playerAnimator.PlayFPSRunAnimation(playerMoveVec);

        //注意！プレイヤーオブジェクトの腕やカメラは、オブジェクトのforwardとは逆を向いているので移動方向にマイナスをかける。Mayaの座標系がすべての元凶
        this.transform.Translate(-playerMoveVec * playerMoveSpeed * Time.deltaTime); //求めたベクトルに移動速度とdeltaTimeをかけて座標書き換え
    }

    public void ControllPlayerRightJoystick()
    {
        //カメラ操作の入力がないなら回転しない
        if (!Mathf.Approximately(rightJoystick.Horizontal, 0) || !Mathf.Approximately(rightJoystick.Vertical, 0)) //右スティックの水平垂直どちらの入力も"ほぼ0"でないなら
        {
            //ジョイスティックの入力をオイラー角（〇軸を中心に△度回転、という書き方）にする
            //前提：カメラはZ軸の負の方向を向いている
            //まずプレイヤーを回転させる
            rotationY += rightJoystick.Horizontal * cameraMoveSpeed * Time.deltaTime; //Unityは左手座標系なので、左右の回転角度（Y軸中心）は加算でいい
            this.transform.eulerAngles = new Vector3(0f, rotationY, 0f);

            //垂直の入力を使って、カメラのみ、X軸中心回転を行う。
            rotationX -= rightJoystick.Vertical * cameraMoveSpeed * Time.deltaTime; //Unityは左手座標系なので、上下の回転角度（X軸中心）にはマイナスをかけなければならない
            rotationX = Mathf.Clamp(rotationX, -camRotateLimitX, camRotateLimitX); //縦方向(X軸中心)回転には角度制限をつけないと宙返りしてしまう
            playerCam.transform.eulerAngles = new Vector3(rotationX, playerCam.transform.eulerAngles.y, 0f); //playerCam.transform.eulerAngles.yは親の回転に委ねているので弄らない
        }
    }
    #endregion

    //画面を「タッチしたとき」呼ばれる。オブジェクトに触ったかどうか判定 UpdateProcess -> if (Input.GetMouseButtonDown(0))
    public void Interact()
    {
        //どこかしらタッチされているなら（＝タッチ対応デバイスを使っているなら）
        if (Input.touchCount > 0)
        {
            foreach (Touch t in Input.touches)
            {
                //カメラの位置からタッチした位置に向けrayを飛ばす
                RaycastHit hit;
                Ray ray = playerCam.ScreenPointToRay(t.position);

                //タッチし始めたフレームでないなら処理しない
                if (t.phase != TouchPhase.Began) continue;

                //rayがなにかに当たったら調べる
                //定数INTERACTABLE_DISTANCEでrayの長さを調整することでインタラクト可能な距離を制限できる
                if (Physics.Raycast(ray, out hit, interactableDistance))
                {
                    switch (hit.collider.gameObject.tag)
                    {
                        case EnemyTag: //プレイヤーならパンチ
                            Debug.Log("Punch入りたい");
                            Punch(hit.point, hit.distance, hit.collider.gameObject.GetComponent<ActorController>());
                            break;
                        case ChestTag: //宝箱なら開錠を試みる
                            TryOpenChest(hit.point, hit.distance, hit.collider.gameObject.GetComponent<Chest>());
                            break;

                        //ドアをタッチで開けるならココ

                        default: //そうでないものはインタラクト不可能なオブジェクトなので無視
                            break;
                    }
                }
            }
        }
        else
        {
            //カメラの位置からタッチした位置に向けrayを飛ばす
            RaycastHit hit;
            Ray ray = playerCam.ScreenPointToRay(Input.mousePosition);

            //rayがなにかに当たったら調べる
            //定数INTERACTABLE_DISTANCEでrayの長さを調整することでインタラクト可能な距離を制限できる
            if (Physics.Raycast(ray, out hit, interactableDistance))
            {
                switch (hit.collider.gameObject.tag)
                {
                    case EnemyTag: //プレイヤーならパンチ
                        Debug.Log("Punch入りたい");
                        Punch(hit.point, hit.distance, hit.collider.gameObject.GetComponent<ActorController>());
                        break;
                    case ChestTag: //宝箱なら開錠を試みる
                        TryOpenChest(hit.point, hit.distance, hit.collider.gameObject.GetComponent<Chest>());
                        break;

                    //ドアをタッチで開けるならココ

                    default: //そうでないものはインタラクト不可能なオブジェクトなので無視
                        break;
                }
            }
        }
    }

    public void UIInteract()
    {
        //カメラの位置からタッチした(クリックした)位置に向けrayを飛ばす
        RaycastHit UIhit;

        #region タブレット版のインタラクト
        //どこかしらタッチされているなら（＝タッチ対応デバイスを使っているなら）
        if (Input.touchCount > 0)
        {
            //タブレットでのタッチ操作を行うための宣言
            Touch UiTouch = Input.GetTouch(0);
            //同時に魔法ボタンを映しているカメラからもRayを飛ばす
            Ray UIrayTouch = MagicButtonCam.ScreenPointToRay(UiTouch.position);

            switch (UiTouch.phase)
            {
                case TouchPhase.Began:
                    if (Physics.Raycast(UIrayTouch, out UIhit, Mathf.Infinity, MagicButtonLayer))
                    {
                        if (UIhit.collider.CompareTag(MagicButtonTag)) //タグを見て
                        {
                            if (!isTouchUI) //このフレームに触れ始めたなら
                            {
                                currentMagicButton = UIhit.collider.gameObject.GetComponent<MagicButton>(); //コンポーネント取得
                                isTouchUI = true; //フラグtrue
                                dragStartPos = UiTouch.position;
                            }
                            currentMagicButton.FollowFingerPosY(UIhit.point); //マウスのy軸の位置とボタンの位置を同じよう\
                        }
                        else if (UIhit.collider.CompareTag(MagicButtonBackTag)) //背景にrayが当たっていたら
                        {
                            if (isTouchUI) currentMagicButton.FollowFingerPosY(UIhit.point); //UI操作中なら引き続き追従処理を行う
                        }
                    }
                    break;

                case TouchPhase.Ended:
                    if(isTouchUI)
                    {
                        Vector3 dragEndPos = UiTouch.position;
                        Vector3 dragVector = dragEndPos - dragStartPos;
                        currentMagicButton.FrickUpper(dragVector);
                        isTouchUI = false;
                        currentMagicButton = null;
                    }
                    break;
            }
        }
        #endregion

        #region クリック版のインタラクト
        if (Input.GetMouseButton(0))
        {
            Ray UIrayClick = MagicButtonCam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(UIrayClick, out UIhit, Mathf.Infinity, MagicButtonLayer))
            {
                if (UIhit.collider.CompareTag(MagicButtonTag)) //タグを見て
                {
                    if (!isTouchUI) //このフレームに触れ始めたなら
                    {
                        currentMagicButton = UIhit.collider.gameObject.GetComponent<MagicButton>(); //コンポーネント取得
                        isTouchUI = true; //フラグtrue
                        dragStartPos = Input.mousePosition;
                    }
                    currentMagicButton.FollowFingerPosY(UIhit.point); //マウスのy軸の位置とボタンの位置を同じよう\
                }
                else if (UIhit.collider.CompareTag(MagicButtonBackTag)) //背景にrayが当たっていたら
                {
                    if (isTouchUI) currentMagicButton.FollowFingerPosY(UIhit.point); //UI操作中なら引き続き追従処理を行う
                }

                #region 旧UI操作
                //Debug.Log(diff_y);
                //if (diff_y > 0.01f) currentMagicButton.OnFlickUpper();


                //    isTouchUI = true;
                //    MagicButton magicButton = UIhit.collider.gameObject.GetComponent<MagicButton>();

                //    if (Input.GetMouseButton(0))
                //    {
                //        //UIが触れていることを検知
                //        isControllUI = true;
                //        //前フレームに放ったのRayのy座標を保存
                //        oldHitRayHeightY.y = UIhit.point.y;
                //        //マウスのy軸の位置とボタンの位置を同じように
                //        magicButton.TouchingMagic(UIhit.point);
                //    }
                //    else isControllUI = false;
                //}
                ////MagicButtonTagが当たっていないということでフリックされたとみなす
                //if (Physics.Raycast(UIray, out UIhit, Mathf.Infinity, MagicButtonLayer)
                //        && UIhit.collider.CompareTag(MagicButtonBackTag))
                //{
                //    isTouchUI = false;
                //    isControllUI = false;
                //    //前フレームに放たれたRayのHitした高さよりも新しくHitしたRayの高さが高かった時にフリックの判定となる
                //    if(UIhit.point.y > oldHitRayHeightY.y) Debug.Log("ボタンをフリック");
                #endregion
            }
        }
        //クリックが終了時のフリック判定
        else if (isTouchUI && Input.GetMouseButtonUp(0))
        {

            Vector3 dragEndPos = Input.mousePosition;
            Vector3 dragVector = dragEndPos - dragStartPos;
            currentMagicButton.FrickUpper(dragVector);
            isTouchUI = false;
            currentMagicButton = null;
            #region　旧UI操作終了処理
            //Ray UIray = MagicButtonCam.ScreenPointToRay(Input.mousePosition);

            //if (Physics.Raycast(UIray, out UIhit, Mathf.Infinity, MagicButtonLayer))
            //{
            //    float diff_y = 0f;

            //    if (UIhit.collider.CompareTag(MagicButtonTag)) //タグを見て
            //    {
            //        if (!isTouchUI) //このフレームに触れ始めたなら
            //        {
            //            currentMagicButton = UIhit.collider.gameObject.GetComponent<MagicButton>(); //コンポーネント取得
            //            isTouchUI = true; //フラグtrue
            //        }
            //        diff_y = currentMagicButton.FollowFingerPosY(UIhit.point); //マウスのy軸の位置とボタンの位置を同じよう
            //    }
            //    else if (UIhit.collider.CompareTag(MagicButtonBackTag)) //背景にrayが当たっていたら
            //    {
            //        if (isTouchUI) diff_y = currentMagicButton.FollowFingerPosY(UIhit.point); //UI操作中なら引き続き追従処理を行う
            //    }

            //    if (diff_y > 0.008f) currentMagicButton.OnFlickAnimation();
            //    else currentMagicButton.ReturnToOriginPos();


            //    isTouchUI = false;
            //    currentMagicButton = null;
            //}
            #endregion
        }
        #endregion
    }

    #region パンチ関連
    //パンチ。パンチを成立させたRaycastHit構造体のPointとDistanceを引数にもらおう
    async private void Punch(Vector3 hitPoint, float distance, ActorController actorController)
    {
        //パンチのクールダウンが上がってなければreturn
        if (!isPunchable) return;
        Debug.Log("Punch入った");

        //送信用クラスを外側のスコープで宣言しておく
        ActionPacket myActionPacket;
        Header myHeader;

        //distanceを調べてしきい値を調べる
        if (distance > punchReachableDistance)
        {
            //射程外なら一人称のスカモーション再生(現在通常のパンチのモーションを再生)
            playerAnimator.PlayFPSPunchAnimation();
            UniTask u = UniTask.RunOnThreadPool(() => PunchCoolDown()); //クールダウン開始
            //画面揺れ小
            await UniTask.Delay(400);
            shakeEffect.ShakeCameraEffect(ShakeEffect.ShakeType.Small);
            //スカしたことをパケット送信
            myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.MISS);
            myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
            UdpGameClient.Send(myHeader.ToByte());

            Debug.Log("スカ送信");
        }
        else
        {
            //射程内なら一人称のパンチモーション再生
            playerAnimator.PlayFPSPunchAnimation();
            UniTask u = UniTask.RunOnThreadPool(() => PunchCoolDown()); //クールダウン開始

            //パンチが正面に当たったのか背面に当たったのか調べる
            Vector3 punchVec = hitPoint - this.transform.position;
            float angle = Vector3.Angle(punchVec, actorController.transform.forward);

            if (angle < flontRange)
            {
                //画面揺れ小
                await UniTask.Delay(400);
                shakeEffect.ShakeCameraEffect(ShakeEffect.ShakeType.Small);

                //正面に命中させたことをパケット送信
                myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.HIT_FRONT, actorController.SessionID);
                myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                UdpGameClient.Send(myHeader.ToByte());


                Debug.Log("正面送信");
            }
            else
            {
                //画面揺れ中
                await UniTask.Delay(400);
                shakeEffect.ShakeCameraEffect(ShakeEffect.ShakeType.Medium);

                //背面に命中させたことをパケット送信
                myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.HIT_BACK, actorController.SessionID, default, punchVec);
                myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                UdpGameClient.Send(myHeader.ToByte());


                Debug.Log("背面送信");
            }
        }

        //一定時間パンチができなくなるローカル関数
        async void PunchCoolDown()
        {
            isPunchable = false; //クールダウン開始
            await UniTask.Delay(punchCooldownTime); //指定された秒数待ったら
            isPunchable = true; //クールダウン終了
        }
    }
    #endregion

    #region 宝箱関連
    //宝箱なら開錠を試みる。パンチと同様RaycastHit構造体から引数をもらう。消えゆく宝箱だったり、他プレイヤーが使用中の宝箱は開錠できない。
    private void TryOpenChest(Vector3 hitPoint, float distance, Chest chestController)
    {
        //送信用クラスを宣言しておく
        ActionPacket myActionPacket;
        Header myHeader;
        isControllCam = false;
        chestController.ActivateEntity();

        //仮！！！！！！
        //宝箱を開錠したことをパケット送信
        myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.OPEN_CHEST_SUCCEED, chestController.EntityID);
        myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
        UdpGameClient.Send(myHeader.ToByte());
    }
    #endregion

    #region 殴られたときのリアクション
    //正面から殴られたときの処理。GameClientManagerから呼ばれる
    public void GetPunchFront()
    {
        //一人称モーションの再生
        playerAnimator.PlayFPSHitedFrontAnimation();

        //カメラ演出
        shakeEffect.ShakeCameraEffect(ShakeEffect.ShakeType.Medium); //振動中
    }

    //背面から殴られたときの処理。GameClientManagerから呼ばれる
    public void GetPunchBack()
    {
        //一人称モーションの再生
        playerAnimator.PlayFPSHitedBackAnimation();

        //カメラ演出
        shakeEffect.ShakeCameraEffect(ShakeEffect.ShakeType.Large); //振動大

        //金貨を拾えない状態にする
        if (!isPickable) forbidPickCts.Cancel(); //既に拾えない状態であれば実行中のForbidPickタスクが存在するはずなので、キャンセルする
        UniTask.RunOnThreadPool(() => ForbidPick(), default, forbidPickCts.Token);

        //前に吹っ飛ぶ
        _rigidbody.AddForce(this.transform.forward * blownPowerHorizontal + Vector3.up * blownPowerVertical, ForceMode.Impulse);

        //金貨を一定時間拾えないようにするローカル関数
        async void ForbidPick()
        {
            isPickable = false; //金貨を拾えない状態にする
            await UniTask.Delay(forbidPickTime); //指定された時間待つ
            isPickable = true; //金貨を拾えるようにする
        }
    }
    #endregion

    //落下していたらリスポーン
    public void PlayerRespawn()
    {
        if (transform.position.y < fallThreshold) transform.position = initialSpawnPosition;
    }

    //Stateの切り替え(雷に打たれた時や罠にかかった時に呼び出される)
    public void ChangePlayerState(IPlayerState newState)
    {
        if (_playerControllState != null) _playerControllState.ExitState(this);
        _playerControllState = newState;
        _playerControllState.EnterState(this);
    }
}