using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

public enum PLAYER_STATE : int //enumの型はデフォルトでintだが、int型であることを期待しているスクリプト（PlayerMoverなど）があるので明示的にintにしておく
{
    NORMAL = 0, //通常
    DASH, //ダッシュ魔法発動中
    OPENING_CHEST, //宝箱を開いている間
    USING_SCROLL, //巻物を開いている間
    WAITING_MAP_ACTION, //魔法を使用したのち、地図による座標決定を待っている間
    KNOCKED, //殴られたリアクションを取っている間
    STUNNED, //スタンしている間
    UNCONTROLLABLE, //リザルト画面など、操作不能にしたい間
}

public enum INTERACT_TYPE : int
{ 
    NONE,
    ENEMY_MISS,
    ENEMY_FRONT,
    ENEMY_BACK,
    CHEST,
    MAGIC_ICON,
    MAGIC_USE,
    MAGIC_CANCEL,
}

public class PlayerControllerV2 : MonoBehaviour
{
    [Header("WASD移動を有効化する。ただしMathf.Maxが毎フレーム呼ばれるようになるので注意。")]
    [SerializeField] private bool m_WASD_Available;

    [Header("プレイヤーを操作するジョイスティック")]
    [SerializeField] private VariableJoystick m_variableJoystick;
    [SerializeField] private DynamicJoystick m_dynamicJoystick;

    [Header("背面を殴られたときの水平方向への吹っ飛び倍率")]
    [SerializeField] private float m_blownPowerHorizontal = 4f;

    [Header("背面を殴られたときの垂直方向への吹っ飛び倍率")]
    [SerializeField] private float m_blownPowerVertical = 2f;

    [Header("背面を殴られてから金貨を拾えるようになるまでの時間（ミリ秒）")]
    [SerializeField] private int m_forbidPickTime = 1000;

    [Header("背面を殴られてからNormalStateに戻るまでの時間（ミリ秒）")]
    [SerializeField] private int m_lockStateTimeKnocked = 1500;

    [Header("スタンしてからNormalStateに戻るまでの時間（ミリ秒）")]
    [SerializeField] private int m_lockStateTimeStunned = 3000;

    [Header("ダッシュ状態からNormalStateに戻るまでの時間（ミリ秒）")]
    [SerializeField] private int m_dashableTime = 10000;

    [Header("ダッシュ中、この時間おきに金貨の山をドロップする（ミリ秒）")]
    [SerializeField] private int m_dashDropInterval = 1500;

    [Header("Y座標がこれ以下になったら落下したとみなしリスポーンする")]
    [SerializeField] private float m_fallThreshold = -3f;
    private Vector3 m_RespawnPosition;

    //入力取得用プロパティ
    private float V_InputHorizontal
    {
        get
        {
            if (m_WASD_Available) //WASDが有効化されているなら、WASD入力とスティック入力について、絶対値がより大きい方を採用して返却する
            {
                if(Mathf.Abs(m_variableJoystick.Horizontal) < Mathf.Abs(Input.GetAxis("Horizontal"))) return Input.GetAxis("Horizontal");
            }
            return m_variableJoystick.Horizontal;
        }
    }
    private float V_InputVertical
    {
        get
        {
            if (m_WASD_Available) //WASDが有効化されているなら、WASD入力とスティック入力について、絶対値がより大きい方を採用して返却する
            {
                if (Mathf.Abs(m_variableJoystick.Horizontal) < Mathf.Abs(Input.GetAxis("Vertical"))) return Input.GetAxis("Vertical");
            }
            return m_variableJoystick.Vertical;
        }
    }
    private float D_InputHorizontal
    {
        get
        {
            return m_dynamicJoystick.Horizontal;
        }
    }
    private float D_InputVertical
    {
        get
        {
            return m_dynamicJoystick.Vertical;
        }
    }

    //stateプロパティ
    private PLAYER_STATE m_state;
    //そのstateに入った最初のフレームか
    private bool m_isFirstFrameOfState = true;
    private PLAYER_STATE State
    {
        set
        {
            m_state = value;
            m_isFirstFrameOfState = true; //stateに入った時のモーションを再生するためboolをfalseに
        }
        get { return m_state; }
    }
    [SerializeField] private bool m_allowedUnlockState = true; //NormalStateに戻る条件(ステートロックの解除条件)を満たしているか
    private CancellationTokenSource m_stateLockCts; //ステートロックの非同期処理を中心するcts
    private CancellationToken StateLockCt //同ct
    {
        get
        {
            m_stateLockCts = new CancellationTokenSource();
            return m_stateLockCts.Token;
        }
    }

    //巻物を開いているとき、使おうとしている魔法のID
    private Definer.MID m_currentMagicID;
    //巻物を開いているとき、使おうとしている魔法のホットバースロット番号
    private int m_currentMagicIndex;

    //現在の開けようとしている宝箱のEntityID
    private ushort m_currentChestID;
    //現在の開けようとしている宝箱のティア
    private int m_currentChestTier;

    //プレイヤー制御用コンポーネント
    private PlayerCameraController m_playerCameraController;
    private PlayerMover m_playerMover;
    private PlayerInteractor m_playerInteractor;
    private PlayerAnimationController m_playerAnimationController;
    private UIDisplayer m_UIDisplayer;
    private HotbarManager m_hotbarManager;
    private ChestUnlocker m_chestUnlocker;
    private Rigidbody m_Rigidbody;

    //金貨を拾うことを禁止する処理
    [SerializeField] private bool m_isAbleToPickUpGold = true; //金貨を拾うことができるか
    private CancellationTokenSource m_forbidPickUpGoldCts; //金貨を拾うことを禁止する非同期処理を中心するcts
    private CancellationToken ForbidPickUpGoldCt //同ct
    {
        get 
        {
            m_forbidPickUpGoldCts = new CancellationTokenSource();
            return m_forbidPickUpGoldCts.Token;
        }
    }

    //ダッシュ状態の制限時間を記録する処理
    [SerializeField] private bool m_isDashable = false; //ダッシュすることができるか
    private CancellationTokenSource m_dashableTimeCountCts; //金貨を拾うことを禁止する非同期処理を中心するcts
    private CancellationToken DashableTimeCountCt //同ct
    {
        get
        {
            m_dashableTimeCountCts.Dispose();
            m_dashableTimeCountCts = new CancellationTokenSource();
            return m_dashableTimeCountCts.Token;
        }
    }

    //金貨を一定時間おきに落とす処理
    [SerializeField] private bool m_isDropable; //金貨を落とすべきか
    private CancellationTokenSource m_dropableTimeCountCts; //金貨を落とさなくてもいい時間をカウントする非同期処理を中心するcts
    private CancellationToken DropableTimeCountCt
    {
        get
        {
            m_dropableTimeCountCts.Dispose();
            m_dropableTimeCountCts = new CancellationTokenSource();
            return m_dropableTimeCountCts.Token;
        }
    }

    //パケット関連
    //GameClientManagerからプレイヤーの生成タイミングでsetterを呼び出し
    public UdpGameClient UdpGameClient { set; get; } //パケット送信用。
    public ushort SessionID { set; get; } //パケットに差出人情報を書くため必要

    private void Start()
    {
        //Ctsインスタンス生成
        m_stateLockCts = new CancellationTokenSource();
        m_forbidPickUpGoldCts = new CancellationTokenSource();
        m_dashableTimeCountCts = new CancellationTokenSource();
        m_dropableTimeCountCts = new CancellationTokenSource();

        //コンポーネントの取得
        m_playerCameraController = this.gameObject.GetComponent<PlayerCameraController>();
        m_playerMover = this.gameObject.GetComponent<PlayerMover>();
        m_playerInteractor = this.gameObject.GetComponent<PlayerInteractor>();
        m_playerAnimationController = this.gameObject.GetComponent<PlayerAnimationController>();
        m_UIDisplayer = this.gameObject.GetComponent<UIDisplayer>();
        m_hotbarManager = this.gameObject.GetComponent<HotbarManager>();
        m_chestUnlocker = this.gameObject.GetComponent<ChestUnlocker>();
        m_Rigidbody = this.gameObject.GetComponent<Rigidbody>();

        //リスポーン地点の記憶
        m_RespawnPosition = this.transform.position;
    }

    //デバッグ用
    [SerializeField] TextMeshProUGUI stateTxt;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetMagicToHotbar(Definer.MID.THUNDER);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetMagicToHotbar(Definer.MID.DASH);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetMagicToHotbar(Definer.MID.TELEPORT);
        if (Input.GetKeyDown(KeyCode.Alpha4)) m_hotbarManager.RemoveMagicFromHotbar(0);
        if (Input.GetKeyDown(KeyCode.Alpha5)) m_hotbarManager.RemoveMagicFromHotbar(1);
        if (Input.GetKeyDown(KeyCode.Alpha6)) m_hotbarManager.RemoveMagicFromHotbar(2);


        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            GetPunchBack();
            PlayLostCoinAnimation();
        }

        switch (this.State) //Stateによって実行するUpdate関数を変える
        { 
            case PLAYER_STATE.NORMAL:
            case PLAYER_STATE.DASH:
                NormalUpdate();
                break;
            case PLAYER_STATE.OPENING_CHEST:
                ChestUpdate();
                break;
            case PLAYER_STATE.USING_SCROLL:
                ScrollUpdate();
                break;
            case PLAYER_STATE.WAITING_MAP_ACTION:
                WaitingMapActionUpdate();
                break;
            case PLAYER_STATE.KNOCKED:
                KnockedUpdate();
                break;
            case PLAYER_STATE.STUNNED:
                StunedUpdate();
                break;
            case PLAYER_STATE.UNCONTROLLABLE:
                break;
        }

        //落下していたらリスポーン
        CheckPositionRespawn();

        stateTxt.text = this.State.ToString(); //デバッグ用
    }

    //y座標をチェックしてリスポーンを行う
    private void CheckPositionRespawn()
    { 
        if(this.transform.position.y < m_fallThreshold) this.transform.position = m_RespawnPosition;
    }

    private void NormalUpdate()
    {
        if (m_isFirstFrameOfState) //このstateに入った最初のフレームなら
        {
            //STEP_A ダッシュ可能ならダッシュ状態になろう
            if (m_isDashable)
            {
                this.State = PLAYER_STATE.DASH;
                m_dropableTimeCountCts.Cancel(); //dashStateから出るときに金貨ドロップのインターバルカウントを止める
                m_isDropable = false; //クールダウンリセット
                UniTask.RunOnThreadPool(()=>CountDropableTime(m_dashDropInterval, DropableTimeCountCt),cancellationToken: DropableTimeCountCt); //クールダウン開始

                SEPlayer.instance.PlaySEDash(); //SE再生
            }

            //STEP_B UI表示を切り替えよう
            m_UIDisplayer.ActivateUIFromState(this.State);

            //STEP_C モーションを切り替えよう
            m_playerAnimationController.SetAnimationFromState(this.State);

            //STEP_D 最初のフレームではなくなるのでフラグを書き変えよう
            m_isFirstFrameOfState = false;
        }

        //STEP1 ダッシュ可能状態でないなら通常stateになろう
        if (!m_isDashable)
        {
            m_dropableTimeCountCts.Cancel();
            this.State = PLAYER_STATE.NORMAL;
        }

        //STEP2 カメラを動かそう
        m_playerCameraController.RotateCamara(D_InputVertical);

        //STEP3 移動・旋回を実行しよう
        float runSpeed = m_playerMover.MovePlayer(this.State, V_InputHorizontal, V_InputVertical, D_InputHorizontal);

        //STEP4 インタラクトを実行しよう
        (INTERACT_TYPE interactType, ushort targetID, int value, Definer.MID magicID, Vector3 punchHitVec) interactInfo = m_playerInteractor.Interact();

        //STEP5 パケット送信が必要なら送ろう
        this.MakePacketFromInteract(interactInfo);

        //STEP6 インタラクト結果をメンバ変数に格納する必要があればそうしよう
        this.SetParameterFromInteract(interactInfo);

        //STEP7 カメラを揺らす必要があれば揺らそう
        m_playerCameraController.InvokeShakeEffectFromInteract(interactInfo.interactType);

        //STEP8 モーションを決めよう
        m_playerAnimationController.SetAnimationFromInteract(interactInfo.interactType, runSpeed); //インタラクト結果に応じてモーションを再生

        //STEP9 ダッシュ中で、かつ金貨ドロップのクールダウンが回っていたら金貨を落とそう
        if (m_isDashable && m_isDropable)
        {
            Debug.Log("金貨ドロップリクエスト送信");
            ActionPacket myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.DROP_GOLD, default, default, this.transform.position　+ (this.transform.forward * 0.4f));
            Header myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
            UdpGameClient.Send(myHeader.ToByte());
            m_isDropable = false;
        }

        //STEP10 次フレームのStateを決めよう
        PLAYER_STATE nextState = GetNextStateFromInteract(interactInfo.interactType, interactInfo.magicID); //インタラクト結果に応じて次のState決定
        if (this.State != nextState)
        {
            this.State = nextState; //nextStateと現在のStateが異なるならStateプロパティのセッター呼び出し
        }
    }

    private void ChestUpdate()
    {
        if (m_isFirstFrameOfState) //このstateに入った最初のフレームなら
        {
            //STEP_A 無期限にステートロックしよう
            m_allowedUnlockState = false;

            //STEP_B UI表示を切り替えよう
            m_UIDisplayer.ActivateUIFromState(this.State, m_currentMagicID);

            //STEP_C モーションを切り替えよう
            m_playerAnimationController.SetAnimationFromState(this.State);

            //STEP_D 宝箱を開錠するために必要な回転数をサーバーから取得してプロパティに書き込もう
            Debug.Log("宝箱のTierは" + m_currentChestTier);
            m_chestUnlocker.MaxDrawCount = 5 * m_currentChestTier;
            Debug.Log(m_chestUnlocker.MaxDrawCount + "回 回せ");

            //STEP_E 鍵の状態をリセットしよう
            m_chestUnlocker.ResetCircleDraw();

            //STEP_F 最初のフレームではなくなるのでフラグを書き変えよう
            m_isFirstFrameOfState = false;
        }

        //STEP1 宝箱を開錠できたかどうかのフラグを宣言しておこう
        bool isUnlocked = false;

        //STEP2 タッチ・クリックされている座標を使って宝箱を開錠しよう
        if (Input.GetMouseButtonDown(0))
        {
            m_chestUnlocker.StartDrawCircle();
        }
        else if (Input.GetMouseButton(0))
        {
            isUnlocked = m_chestUnlocker.DrawingCircle(Input.mousePosition); //開錠できたかどうか変数で受け取る
        }

        //STEP3 開錠できたらパケットを送信しよう
        if (isUnlocked)
        {
            ActionPacket myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.OPEN_CHEST_SUCCEED, m_currentChestID);
            Header myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
            UdpGameClient.Send(myHeader.ToByte());
        }

        //STEPX SEを再生しよう
        if(isUnlocked) SEPlayer.instance.PlaySEOpenChest();

        //STEP4 開錠できたら少し待ってステートロックを解除しよう
        if (isUnlocked)
        {
            UniTask.RunOnThreadPool(() => CountStateLockTime(1200, StateLockCt), cancellationToken: StateLockCt);
        }

        //STEP5 通常stateに戻ることができるなら戻ろう
        if (m_allowedUnlockState)
        {
            this.State = PLAYER_STATE.NORMAL;
        }
    }

    private void ScrollUpdate()
    {
        if (m_isFirstFrameOfState) //このstateに入った最初のフレームなら
        {
            //STEP_A UI表示を切り替えよう
            m_UIDisplayer.ActivateUIFromState(this.State, m_currentMagicID);

            //STEP_B モーションを切り替えよう
            m_playerAnimationController.SetAnimationFromState(this.State);

            //STEP_C 最初のフレームではなくなるのでフラグを書き変えよう
            m_isFirstFrameOfState = false;
        }

        //STEP1 カメラを動かそう
        m_playerCameraController.RotateCamara(D_InputVertical);

        //STEP2 移動・旋回を実行しよう
        float runSpeed = m_playerMover.MovePlayer(this.State, V_InputHorizontal, V_InputVertical, D_InputHorizontal);

        //STEP3 インタラクトを実行しよう
        (INTERACT_TYPE interactType, ushort targetID, int value, Definer.MID magicID, Vector3 punchHitVec) interactInfo = m_playerInteractor.Interact();

        //STEP4 パケット送信が必要なら送ろう
        this.MakePacketFromInteract(interactInfo);

        //STEP5 巻物をキャンセルしたなら元のStateに戻ろう
        if(interactInfo.interactType == INTERACT_TYPE.MAGIC_CANCEL)
        {
            this.State = PLAYER_STATE.NORMAL;
        }
    }

    private void WaitingMapActionUpdate()
    {
        if (m_isFirstFrameOfState) //このstateに入った最初のフレームなら
        {
            //STEP_A 無期限にステートロックしよう
            m_allowedUnlockState = false;

            //STEP_A UI表示を切り替えよう
            m_UIDisplayer.ActivateUIFromState(this.State);

            //STEP_B モーションを切り替えよう
            m_playerAnimationController.SetAnimationFromState(this.State);

            //STEP_C 最初のフレームではなくなるのでフラグを書き変えよう
            m_isFirstFrameOfState = false;
        }

        //サーバー側でマップアクションが終わったらm_allowedUnlockStateがtrueになる

        //STEP1 通常stateに戻ることができるなら戻ろう。このときホットバーから使っていた魔法を取り除こう
        if (m_allowedUnlockState)
        {
            m_hotbarManager.RemoveMagicFromHotbar(m_currentMagicIndex); //サーバーからm_allowedUnlockStateがtrueになったときのみ魔法が消費されるので、殴られたときは消費されない
            this.State = PLAYER_STATE.NORMAL;
        }
    }

    private void KnockedUpdate()
    {
        if (m_isFirstFrameOfState) //このstateに入った最初のフレームなら
        {
            //もし既にステートロック中なら、実行中の非同期処理があるはずなのでキャンセルする
            if (!m_allowedUnlockState) m_stateLockCts.Cancel();
            //一定時間ステートロックする
            m_allowedUnlockState = false;
            UniTask.RunOnThreadPool(() => CountStateLockTime(1500, StateLockCt), cancellationToken: StateLockCt);

            //STEP_A 吹き飛ぼう
            //金貨を拾えない状態にする
            if (!m_isAbleToPickUpGold) m_forbidPickUpGoldCts.Cancel(); //既に拾えない状態であれば実行中のForbidPickタスクが存在するはずなので、キャンセルする
            //一定時間金貨を拾えない状態にする
            m_isAbleToPickUpGold = false;
            UniTask.RunOnThreadPool(() => CountForbidPickTime(1000, ForbidPickUpGoldCt), cancellationToken: ForbidPickUpGoldCt);

            //前に吹っ飛ぶ
            //transform.forwardと実際の前方は（カメラの向きに合わせた関係で）逆なのでマイナスをかける
            m_Rigidbody.AddForce(-this.transform.forward * m_blownPowerHorizontal + Vector3.up * m_blownPowerVertical, ForceMode.Impulse);
            
            //STEP_B カメラを揺らそう
            m_playerCameraController.InvokeShakeEffectFromState(this.State);

            //STEP_C モーションを再生しよう
            m_playerAnimationController.SetAnimationFromState(this.State);

            //STEP_D 最初のフレームではなくなるのでフラグを書き変えよう
            m_isFirstFrameOfState = false;
        }

        //STEP1 カメラを動かそう
        m_playerCameraController.RotateCamara(D_InputVertical);
        
        //STEP2 移動・旋回を実行しよう
        float runSpeed = m_playerMover.MovePlayer(this.State, V_InputHorizontal, V_InputVertical, D_InputHorizontal);

        //STEP3 通常stateに戻ることができるなら戻ろう
        if (m_allowedUnlockState) this.State = PLAYER_STATE.NORMAL;
    }

    private void StunedUpdate()
    {
        if (m_isFirstFrameOfState) //このstateに入った最初のフレームなら
        {
            //もし既にステートロック中なら、実行中の非同期処理があるはずなのでキャンセルする
            if (!m_allowedUnlockState) m_stateLockCts.Cancel();
            //一定時間ステートロックする
            m_allowedUnlockState = false;
            UniTask.RunOnThreadPool(() => CountStateLockTime(3000, StateLockCt), cancellationToken: StateLockCt);

            //STEP_A カメラを揺らそう
            m_playerCameraController.InvokeShakeEffectFromState(this.State);

            //STEP_B モーションを再生しよう
            m_playerAnimationController.SetAnimationFromState(this.State);

            //STEP_C 最初のフレームではなくなるのでフラグを書き変えよう
            m_isFirstFrameOfState = false;
        }

        //STEP1 通常stateに戻ることができるなら戻ろう
        if (m_allowedUnlockState) this.State = PLAYER_STATE.NORMAL;
    }

    private PLAYER_STATE GetNextStateFromInteract(INTERACT_TYPE interactType, Definer.MID magicID)
    {
        switch (interactType)
        {
            case INTERACT_TYPE.CHEST: //宝箱を開ける
                return PLAYER_STATE.OPENING_CHEST;
            case INTERACT_TYPE.MAGIC_ICON: //巻物を開く
                return PLAYER_STATE.USING_SCROLL;
            case INTERACT_TYPE.MAGIC_USE: //マップアクションを待機するか、ダッシュ状態になる
                if (magicID == Definer.MID.DASH) return PLAYER_STATE.DASH;
                else return PLAYER_STATE.WAITING_MAP_ACTION;
            case INTERACT_TYPE.MAGIC_CANCEL: //通常状態になる
                return PLAYER_STATE.NORMAL;
            default: //上記に当てはまらないなら現在のStateを維持
                return this.State;
        }
    }

    //ステートロック用フラグを一定時間後に解除する
    private async void CountStateLockTime(int cooldownTimeMilliSec, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Delay(cooldownTimeMilliSec, cancellationToken: cancellationToken); //指定された時間待つ
            m_allowedUnlockState = true; //ステートロック解除
        }
        catch (OperationCanceledException)
        {
            Debug.Log("ステートロックの制限時間カウントをキャンセルします");
        }
    }

    //金貨を拾うためのフラグを一定時間後にtrueにする
    private async void CountForbidPickTime(int cooldownTimeMilliSec, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Delay(cooldownTimeMilliSec, cancellationToken: cancellationToken); //指定された時間待つ
            m_isAbleToPickUpGold = true; //金貨を拾えるようにする
        }
        catch (OperationCanceledException)
        {
            Debug.Log("金貨拾得禁止の制限時間カウントをキャンセルします");
        }
    }

    private async void CountDashableTime(int cooldownTimeMilliSec, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Delay(cooldownTimeMilliSec, cancellationToken: cancellationToken); //指定された時間待つ
            m_isDashable = false; //ダッシュできない状態にする
        }
        catch (OperationCanceledException)
        {
            Debug.Log("ダッシュの制限時間カウントをキャンセルします");
        }
    }

    private async void CountDropableTime(int cooldownTimeMilliSec, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Delay(cooldownTimeMilliSec, cancellationToken: cancellationToken); //指定された時間待つ
                m_isDropable = true; //金貨を落とさせる
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("金貨ドロップのインターバルカウントをキャンセルします");
        }
    }

    //インタラクト結果から、必要があればメンバ変数を編集する
    private void SetParameterFromInteract((INTERACT_TYPE interactType, ushort targetID, int value, Definer.MID magicID, Vector3 punchHitVec) interactInfo)
    {
        switch (interactInfo.interactType)
        {
            case INTERACT_TYPE.CHEST:
                m_currentChestID = interactInfo.targetID; //アクセスする宝物のEntityIDを書き込み
                m_currentChestTier = interactInfo.value; //アクセスする宝箱のTierを書き込み
                break;
            case INTERACT_TYPE.MAGIC_ICON:
                m_currentMagicID = interactInfo.magicID; //使う魔法のIDを書き込み
                m_currentMagicIndex = interactInfo.value; //使うホットバーのスロット番号を書き込み
                break;
            default:
                break;
        }
    }

    private void MakePacketFromInteract((INTERACT_TYPE interactType, ushort targetID, int value, Definer.MID magicID, Vector3 punchHitVec) interactInfo)
    {
        if (UdpGameClient == null)
        {
            if(interactInfo.interactType != INTERACT_TYPE.NONE)
            Debug.LogWarning("UdpGameClientの参照が無いため、パケット送信をキャンセルしました。");
            return;
        }

        //送信用クラスを外側のスコープで宣言しておく
        ActionPacket myActionPacket;
        Header myHeader;

        switch (interactInfo.interactType)
        {
            case INTERACT_TYPE.ENEMY_MISS:
                //スカしたことをパケット送信
                myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.MISS);
                myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                UdpGameClient.Send(myHeader.ToByte());
                break;
            case INTERACT_TYPE.ENEMY_FRONT:
                //正面に命中させたことをパケット送信
                myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.HIT_FRONT, interactInfo.targetID);
                myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                UdpGameClient.Send(myHeader.ToByte());
                break;
            case INTERACT_TYPE.ENEMY_BACK:
                //背面に命中させたことをパケット送信
                myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.HIT_BACK, interactInfo.targetID, default, interactInfo.punchHitVec);
                myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                UdpGameClient.Send(myHeader.ToByte());
                break;
            case INTERACT_TYPE.CHEST:
                //宝箱を開錠し始めたことをパケット送信
                break;
            case INTERACT_TYPE.MAGIC_ICON:
                //巻物を開いたことをパケット送信
                break;
            case INTERACT_TYPE.MAGIC_CANCEL:
                //巻物を閉じたことをパケット送信
                break;
            case INTERACT_TYPE.MAGIC_USE:
                //魔法を使用したことをパケット送信
                myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.USE_MAGIC, default, (int)m_currentMagicID);
                myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                UdpGameClient.Send(myHeader.ToByte());
                break;
            default:
                break;
        }
    }

    //GameClientManagerが、サーバーから命令があったときに呼び出す。
    //正面から殴られたとき演出を行う
    public void GetPunchFront()
    {
        m_playerAnimationController.SetTriggerGuard();
    }

    //背面から殴られたときStateを強制的に変更する
    public void GetPunchBack()
    {
        this.State = PLAYER_STATE.KNOCKED;
    }

    public void PlayGetCoinAnimation()
    {
        m_UIDisplayer.PlayGetCoinAnimation();
    }

    public void PlayLostCoinAnimation()
    { 
        m_UIDisplayer.PlayLostCoinAnimation();
    }

    //サーバーから魔法の使用許可が降りたらStateを変更する
    public void AcceptUsingMagic()
    {
        switch (m_currentMagicID) //使用中の魔法に応じて次のStateを決める
        {
            case Definer.MID.DASH:
                m_hotbarManager.RemoveMagicFromHotbar(m_currentMagicIndex); //ここでダッシュ魔法を消費させる
                this.State = PLAYER_STATE.DASH;
                //ダッシュ可能時間をカウントする非同期処理があるならキャンセルする
                if (m_isDashable) m_dashableTimeCountCts.Cancel();
                m_isDashable = true; //ダッシュ可能にする
                UniTask.RunOnThreadPool(() => CountDashableTime(m_dashableTime, m_dashableTimeCountCts.Token), cancellationToken: DashableTimeCountCt); //一定時間後にダッシュ可能フラグを解除する
                break;
            default :
                this.State = PLAYER_STATE.WAITING_MAP_ACTION;
                break;
        }
    }

    //魔法の使用を許可しない
    public void DeclineUsingMagic()
    {
        //メッセージなど出す
        this.State = PLAYER_STATE.NORMAL;
    }

    public void EndUsingMagicSuccessfully()
    {
        //プレイヤーが殴られるなどして違うStateになっていないかチェック
        if(this.State != PLAYER_STATE.WAITING_MAP_ACTION) return;
        
        m_allowedUnlockState = true; //ステートロックを解除
    }

    public void SetMagicToHotbar(Definer.MID magicID)
    { 
        m_hotbarManager.SetMagicToHotbar(magicID);
    }

    //金貨を拾うためのトリガー処理
    private void OnTriggerEnter(Collider other)
    {
        //送信用クラスを外側のスコープで宣言しておく
        ActionPacket myActionPacket;
        Header myHeader;

        switch (other.tag)
        {
            case "GoldPile":
                if (!m_isAbleToPickUpGold) return; //金貨を拾えない状態ならreturn

                //金貨の山に触れたというリクエスト送信。（他のプレイヤーが先に触れていた場合、お金は入手できない。早い者勝ち。）
                myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.GET_GOLDPILE, other.GetComponent<Entity>().EntityID);
                myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                UdpGameClient.Send(myHeader.ToByte());
                Debug.Log("金貨Getリクエスト。");
                break;
            case "Scroll":
                if (!m_isAbleToPickUpGold) return; //金貨を拾えない状態ならreturn
                if (!m_hotbarManager.IsAbleToSetMagic()) return; //魔法をこれ以上持てないならreturn

                //巻物をに触れたというリクエスト送信。（他のプレイヤーが先に触れていた場合、巻物は入手できない。早い者勝ち。）
                myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.GET_SCROLL, other.GetComponent<Entity>().EntityID);
                myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                UdpGameClient.Send(myHeader.ToByte());
                Debug.Log("巻物Getリクエスト。");
                break;
            case "Thunder":
                //痺れたことをパケット送信
                this.State = PLAYER_STATE.STUNNED;
                break;
            default:
                break;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        switch (other.tag)
        {
            case "Thunder":
                //痺れたことをパケット送信
                //スタン状態になる
                this.State = PLAYER_STATE.STUNNED;
                break;
            default:
                break;
        }
    }
}
