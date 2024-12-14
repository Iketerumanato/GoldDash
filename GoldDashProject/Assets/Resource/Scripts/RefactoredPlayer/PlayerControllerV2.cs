using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState
{
    NORMAL = 0, //通常
    OPENING_SCROLL, //巻物を開いている間
    WAITING_MAP_ACTION, //魔法を使用したのち、地図による座標決定を待っている間
    KNOCKED, //殴られたリアクションを取っている間
    STUNNED, //スタンしている間
    UNCONTROLLABLE, //リザルト画面など、操作不能にしたい間
}

public class PlayerControllerV2 : MonoBehaviour
{
    //入力取得用プロパティ
    private float InputHorizontal { get { return Input.GetAxis("Horizontal"); } }
    private float InputVertical { get { return Input.GetAxis("Vertical"); } }

    //stateプロパティ
    private PlayerState m_state;
    public PlayerState State { set { m_state = value; } get { return m_state; } }

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
            case PlayerState.NORMAL:
                break;
            case PlayerState.OPENING_SCROLL:
                break;
            case PlayerState.WAITING_MAP_ACTION:
                break;
            case PlayerState.KNOCKED:
                break;
            case PlayerState.STUNNED:
                break;
            case PlayerState.UNCONTROLLABLE:
                break;
        }
    }

    private void NormalUpdate()
    {
        //STEP1 カメラを動かそう

        //STEP2 移動を実行しよう

        //STEP3 インタラクトを実行しよう

        //STEP4 カメラを揺らす必要があれば、揺らそう

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
