using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] Animator playerAnimator;
    private readonly string strPlayerAnimSpeed = "ArmAnimationSpeed";
    private readonly string strPunchTrigger = "ArmPunchTrigger";
    private readonly string strGetPunchFrontTrigger = "HitedFrontArmTrigger";
    private readonly string strGetPunchBackTrigger = "HitedBackArmTrigger";
    private readonly string strIsUsingScroll = "isUsingScroll";

    //走りモーションの再生(プレイヤーの移動量に依存)
    public void  PlayFPSRunAnimation(Vector3 playerMoveVec)
    {
        playerAnimator.SetFloat(strPlayerAnimSpeed, playerMoveVec.magnitude);
    }

    //パンチのモーション再生
    public void PlayFPSPunchAnimation()
    {
        playerAnimator.SetTrigger(strPunchTrigger);
    }

    //被弾(正面)モーション再生
    public void PlayFPSHitedFrontAnimation()
    {
        playerAnimator.SetTrigger(strGetPunchFrontTrigger);
    }

    //被弾(背面)モーション再生
    public void PlayFPSHitedBackAnimation()
    {
        playerAnimator.SetTrigger(strGetPunchBackTrigger);
    }

    public void SetScrollBool(bool flag)
    {
        playerAnimator.SetBool(strIsUsingScroll, flag);
    }
}