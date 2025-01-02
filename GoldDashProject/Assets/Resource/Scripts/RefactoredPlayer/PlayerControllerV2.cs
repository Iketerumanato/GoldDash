using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
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
    private CancellationToken m_stateLockCt; //同ct
    //巻物を開いているとき、使おうとしている魔法のID
    private Definer.MID m_currentMagicID;
    //巻物を開いているとき、使おうとしている魔法のホットバースロット番号
    private int m_currentMagicIndex;

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
    private CancellationToken m_forbidPickUpGoldCt; //同ct

    //パケット関連
    //GameClientManagerからプレイヤーの生成タイミングでsetterを呼び出し
    public UdpGameClient UdpGameClient { set; get; } //パケット送信用。
    public ushort SessionID { set; get; } //パケットに差出人情報を書くため必要

    private void Start()
    {
        //ctの発行
        m_stateLockCts = new CancellationTokenSource();
        m_stateLockCt = m_stateLockCts.Token;
        m_forbidPickUpGoldCts = new CancellationTokenSource();
        m_forbidPickUpGoldCt = m_forbidPickUpGoldCts.Token;

        //コンポーネントの取得
        m_playerCameraController = this.gameObject.GetComponent<PlayerCameraController>();
        m_playerMover = this.gameObject.GetComponent<PlayerMover>();
        m_playerInteractor = this.gameObject.GetComponent<PlayerInteractor>();
        m_playerAnimationController = this.gameObject.GetComponent<PlayerAnimationController>();
        m_UIDisplayer = this.gameObject.GetComponent<UIDisplayer>();
        m_hotbarManager = this.gameObject.GetComponent<HotbarManager>();
        m_chestUnlocker = this.gameObject.GetComponent<ChestUnlocker>();
        m_Rigidbody = this.gameObject.GetComponent<Rigidbody>();
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

        stateTxt.text = this.State.ToString(); //デバッグ用
    }

    private void NormalUpdate()
    {
        if (m_isFirstFrameOfState) //このstateに入った最初のフレームなら
        {
            //STEP_A UI表示を切り替えよう
            m_UIDisplayer.ActivateUIFromState(this.State);

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
        (INTERACT_TYPE interactType, ushort targetID, Definer.MID magicID, Vector3 punchHitVec) interactInfo = m_playerInteractor.Interact();

        //STEP4 パケット送信が必要なら送ろう
        this.MakePacketFromInteract(interactInfo);

        //STEP5 カメラを揺らす必要があれば揺らそう
        m_playerCameraController.InvokeShakeEffectFromInteract(interactInfo.interactType);

        //STEP6 モーションを決めよう
        m_playerAnimationController.SetAnimationFromInteract(interactInfo.interactType, runSpeed); //インタラクト結果に応じてモーションを再生

        //STEP7 次フレームのStateを決めよう
        PLAYER_STATE nextState = GetNextStateFromInteract(interactInfo.interactType, interactInfo.magicID, interactInfo.targetID); //インタラクト結果に応じて次のState決定
        if (this.State != nextState)
        {
            this.State = nextState; //nextStateと現在のStateが異なるならStateプロパティのセッター呼び出し
            this.m_UIDisplayer.ActivateUIFromState(this.State, interactInfo.magicID); //次フレームのStateに応じてUI表示状況を切り替え
        }
    }

    private void ChestUpdate()
    {
        if (m_isFirstFrameOfState) //このstateに入った最初のフレームなら
        {
            //STEP_A UI表示を切り替えよう
            m_UIDisplayer.ActivateUIFromState(this.State, m_currentMagicID);

            //STEP_B モーションを切り替えよう
            m_playerAnimationController.SetAnimationFromState(this.State);

            //STEP_C 宝箱を開錠するために必要な回転数をサーバーから取得してプロパティに書き込もう
            m_chestUnlocker.MaxDrawCount = 5; //仮に5

            //STEP_C 最初のフレームではなくなるのでフラグを書き変えよう
            m_isFirstFrameOfState = false;
        }

        //STEP1 宝箱を開錠できたかどうかのフラグを宣言しておこう
        bool isUnlocked = false;

        //STEP1 タッチ・クリックされている座標を使って宝箱を開錠しよう
        if (Input.GetMouseButtonDown(0))
        {
            m_chestUnlocker.StartDrawCircle();
        }
        else if (Input.GetMouseButton(0))
        {
            isUnlocked = m_chestUnlocker.DrawingCircle(Input.mousePosition); //開錠できたかどうか変数で受け取る
        }

        //STEP2 開錠できたらパケットを送信しよう
        //if (isUnlocked)
        //{ 

        //}

        //STEP3 開錠できたら通常stateに戻ろう
        PLAYER_STATE nextState = this.State;
        if (isUnlocked)
        {
            nextState = PLAYER_STATE.NORMAL;
        }

        //STEP4 次フレームのStateを決めよう
        //宝箱を開錠済で、殴られていたり雷に打たれていたりした場合は対応したStateに行こう。このとき開錠したというパケットはサーバーに送られているが、その後巻物が振り込まれることは許容する。
        if (this.State != nextState)
        {
            m_chestUnlocker.ResetCircleDraw(); //他のstateに行く前に、鍵の状態をリセットしておこう
            this.State = nextState; //nextStateと現在のStateが異なるならStateプロパティのセッター呼び出し
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
        (INTERACT_TYPE interactType, ushort targetID, Definer.MID magicID, Vector3 punchHitVec) interactInfo = m_playerInteractor.Interact();

        //STEP4 パケット送信が必要なら送ろう
        this.MakePacketFromInteract(interactInfo);

        //STEP5 巻物を使ったならホットバー情報を書き換えよう
        if(interactInfo.interactType == INTERACT_TYPE.MAGIC_USE) m_hotbarManager.RemoveMagicFromHotbar(m_currentMagicIndex);

        //STEP6 モーションを決めよう
        m_playerAnimationController.SetAnimationFromInteract(interactInfo.interactType, runSpeed); //インタラクト結果に応じてモーションを再生

        //STEP7 次フレームのStateを決めよう
        PLAYER_STATE nextState = GetNextStateFromInteract(interactInfo.interactType, m_currentMagicID); //インタラクト結果に応じて次のState決定
        if (this.State != nextState)
        {
            this.State = nextState; //nextStateと現在のStateが異なるならStateプロパティのセッター呼び出し
            this.m_UIDisplayer.ActivateUIFromState(this.State, interactInfo.magicID); //次フレームのStateに応じてUI表示状況を切り替え
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
            UniTask u = UniTask.RunOnThreadPool(() => CountStateLockTime(1500), default, m_stateLockCt);

            //STEP_A 吹き飛ぼう
            //金貨を拾えない状態にする
            if (!m_isAbleToPickUpGold) m_forbidPickUpGoldCts.Cancel(); //既に拾えない状態であれば実行中のForbidPickタスクが存在するはずなので、キャンセルする
            //一定時間金貨を拾えない状態にする
            m_isAbleToPickUpGold = false;
            UniTask.RunOnThreadPool(() => CountForbidPickTime(1000), default, m_forbidPickUpGoldCt);

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
            UniTask u = UniTask.RunOnThreadPool(() => CountStateLockTime(3000), default, m_stateLockCt);

            //STEP_A カメラを揺らそう
            m_playerCameraController.InvokeShakeEffectFromState(this.State);

            //STEP_B モーションを再生しよう
            m_playerAnimationController.SetAnimationFromState(this.State);

            //STEP_C 最初のフレームではなくなるのでフラグを書き変えよう
            m_isFirstFrameOfState = false;
        }

        //STEP1 カメラを動かそう
        m_playerCameraController.RotateCamara(V_InputVertical);

        //STEP2 通常stateに戻ることができるなら戻ろう
        if (m_allowedUnlockState) this.State = PLAYER_STATE.NORMAL;
    }

    private PLAYER_STATE GetNextStateFromInteract(INTERACT_TYPE interactType, Definer.MID magicID, int magicIndex = 0)
    {
        switch (interactType)
        {
            case INTERACT_TYPE.CHEST: //宝箱を開ける
                return PLAYER_STATE.OPENING_CHEST;
            case INTERACT_TYPE.MAGIC_ICON: //巻物を開く
                m_currentMagicID = magicID; //使う魔法のIDを書き込み
                m_currentMagicIndex = magicIndex;
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
    private async void CountStateLockTime(int cooldownTimeMilliSec)
    {
        await UniTask.Delay(cooldownTimeMilliSec); //指定された秒数待ったら
        m_allowedUnlockState = true; //クールダウン終了
    }

    //金貨を拾うためのフラグを一定時間後にtrueにする
    private async void CountForbidPickTime(int cooldownTimeMilliSec)
    {
        await UniTask.Delay(cooldownTimeMilliSec); //指定された時間待つ
        m_isAbleToPickUpGold = true; //金貨を拾えるようにする
    }

    private void MakePacketFromInteract((INTERACT_TYPE interactType, ushort targetID, Definer.MID magicID, Vector3 punchHitVec) interactInfo)
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
                //宝箱を開錠したことをパケット送信
                myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.OPEN_CHEST_SUCCEED, interactInfo.targetID);
                myHeader = new Header(this.SessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                UdpGameClient.Send(myHeader.ToByte());
                break;
            case INTERACT_TYPE.MAGIC_USE:
                //魔法を使用したことをパケット送信
                myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.USE_MAGIC, interactInfo.targetID, (int)interactInfo.magicID);
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

    public void SetMagicToHotbar(Definer.MID magicID)
    { 
        m_hotbarManager.SetMagicToHotbar(magicID);
    }
}
