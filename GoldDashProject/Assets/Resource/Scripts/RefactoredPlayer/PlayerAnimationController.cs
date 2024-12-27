using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("使用するAnimator")]
    [SerializeField] Animator m_animator;

    private readonly string strRunSpeed = "RunSpeed";
    private readonly string strScrollFlag = "ScrollFlag";
    private readonly string strChestFlag = "ChestFlag";
    private readonly string strStunnedFlag = "StunnedFlag";
    private readonly string strPunchTrigger = "PunchTrigger";
    private readonly string strGuardTrigger = "GuardTrigger";
    private readonly string strBlownTrigger = "BlownTrigger";

    /// <summary>
    /// インタラクトの結果からモーションを決定する。
    /// </summary>
    /// <param name="interactType">インタラクトの種別</param>
    /// <param name="moveAmount">そのフレームのプレイヤーの速度</param>
    public void SetAnimationFromInteract(INTERACT_TYPE interactType, float runSpeed)
    {
        switch (interactType)
        {
            case INTERACT_TYPE.NONE: //インタラクトしていないなら走りモーションのスピード変更
                m_animator.SetFloat(strRunSpeed, Mathf.Clamp01(runSpeed));
                break;
            case INTERACT_TYPE.ENEMY_MISS: //パンチした結果にならパンチモーション
            case INTERACT_TYPE.ENEMY_FRONT:
            case INTERACT_TYPE.ENEMY_BACK:
                m_animator.SetTrigger(strPunchTrigger);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// そのStateに入ったとき1度だけ実行され、モーションを決定する。
    /// </summary>
    /// <param name="state">現在のState</param>
    public void SetAnimationFromState(PLAYER_STATE state)
    {
        switch (state)
        {
            case PLAYER_STATE.NORMAL: //通常状態に戻るので各種フラグは解除
            case PLAYER_STATE.DASH: //巻物からダッシュに移行することもある
                m_animator.SetBool(strChestFlag, false);
                m_animator.SetBool(strScrollFlag, false);
                m_animator.SetBool(strStunnedFlag, false);
                break;
            case PLAYER_STATE.OPENING_CHEST:
                m_animator.SetBool(strChestFlag, true);
                break;
            case PLAYER_STATE.USING_SCROLL:
                m_animator.SetBool(strScrollFlag, true);
                break;
            case PLAYER_STATE.KNOCKED:
                m_animator.SetTrigger(strBlownTrigger);
                break;
            case PLAYER_STATE.STUNNED:
                m_animator.SetBool(strStunnedFlag, true);
                break;
        }
    }
}
