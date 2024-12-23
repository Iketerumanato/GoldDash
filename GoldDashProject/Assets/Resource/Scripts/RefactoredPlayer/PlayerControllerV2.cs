using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PLAYER_STATE : int //enumの型はデフォルトでintだが、int型であることを期待しているスクリプト（PlayerMoverなど）があるので明示的にintにしておく
{
    NORMAL = 0, //通常
    DASH, //ダッシュ魔法発動中
    OPENING_SCROLL, //巻物を開いている間
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

    //入力取得用プロパティ
    private float V_InputHorizontal
    {
        get
        {
            if (m_WASD_Available)
            {
                return Mathf.Max(m_variableJoystick.Horizontal, Input.GetAxis("Horizontal"));
            }
            else return m_variableJoystick.Horizontal;
        }
    }
    private float V_InputVertical
    {
        get
        {
            if (m_WASD_Available)
            {
                return Mathf.Max(m_variableJoystick.Vertical, Input.GetAxis("Vertical"));
            }
            else return m_variableJoystick.Vertical;
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
    public PLAYER_STATE State { set { m_state = value; } get { return m_state; } }

    //プレイヤー制御用コンポーネント
    private PlayerCameraController m_playerCameraController;
    private PlayerMover m_playerMover;
    private PlayerInteractor m_playerInteractor;
    private PlayerAnimationController m_playerAnimationController;
    private UIDisplayer m_UIDisplayer;

    private void Update()
    {
        switch (this.State)
        { 
            case PLAYER_STATE.NORMAL:
                break;
            case PLAYER_STATE.DASH:
                break;
            case PLAYER_STATE.OPENING_SCROLL:
                break;
            case PLAYER_STATE.WAITING_MAP_ACTION:
                break;
            case PLAYER_STATE.KNOCKED:
                break;
            case PLAYER_STATE.STUNNED:
                break;
            case PLAYER_STATE.UNCONTROLLABLE:
                break;
        }
    }

    private void NormalUpdate()
    {
        //STEP1 カメラを動かそう
        m_playerCameraController.RotateCamara(V_InputVertical);

        //STEP2 移動・旋回を実行しよう
        float moveLength = m_playerMover.MovePlayer(this.State, V_InputHorizontal, V_InputVertical, D_InputHorizontal);

        //STEP3 インタラクトを実行しよう
        (INTERACT_TYPE interactType, ushort targetID, Definer.MID magicID) interactInfo = m_playerInteractor.Interact();

        //STEP4 パケット送信が必要なら送ろう

        //STEP5 カメラを揺らす必要があれば揺らそう

        //STEP5 モーションを決めよう

        //STEP6 次フレームのStateを決めよう
    }

    private void KnockedUpdate()
    {
    }

    private void StunedUpdate()
    {
    }
}
