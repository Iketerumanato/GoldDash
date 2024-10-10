using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

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
    }

    private void Update()
    {
        CheckPlayerMove();
    }

    void CheckPlayerMove()
    {
        // 位置の変化を計算
        float distance = (transform.position - oldPos).sqrMagnitude;

        // しきい値を超えた場合にアニメーションを再生
        if (distance > SQR_RunThreshold)
        {
            IsRun = true; // 移動中フラグを設定
            PlayerAnimator.SetBool(RunAnimation, true);
        }
        else
        {
            IsRun = false; // 停止フラグを設定
            PlayerAnimator.SetBool(RunAnimation, false);
        }
        oldPos = transform.position;
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
