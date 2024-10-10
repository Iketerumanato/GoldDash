using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    public string PlayerName { set; get; }
    public bool IsRun { set; get; }
    Vector3 oldPos;
    [SerializeField] float runThreshold;
    private float SQR_RunThreshold;
    [SerializeField] Animator PlayerAnimator;
    readonly string RunAnimation = "IsRun";

    //このアクターの座標と向きを更新する
    public void Move(Vector3 pos, Vector3 forward)
    {
        this.gameObject.transform.position = pos;
        this.gameObject.transform.forward = forward;

        SQR_RunThreshold = runThreshold * runThreshold;
        Vector3 moveVec = pos - oldPos;
        oldPos = pos;

        if (moveVec.sqrMagnitude > SQR_RunThreshold)
        {
            PlayerAnimator.SetBool(RunAnimation, true);
        }

        else
        {
            PlayerAnimator.SetBool(RunAnimation, false);
            return;
        }
        float moveAngle = Vector3.Angle(moveVec, forward);
    }

    //メソッドの例。正式実装ではない
    public void Kill()
    { 
    }

    public void GiveItem()
    { 
    }

    public void GiveStatus()
    { 
    }
}
