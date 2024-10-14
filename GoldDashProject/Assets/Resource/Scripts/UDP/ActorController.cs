using UnityEngine;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    //ユーザーに見えるパラメータ
    public string PlayerName { set; get; } //プレイヤー名
    public int GoldCount; //所持金 ２１億ゴールドくらい持てる

    //モーション管理
    public bool IsRun { set; get; }
    Vector3 oldPos;
    [SerializeField] float runThreshold = 0.01f;
    private float SQR_RUN_THRESHOLD;
    [SerializeField] Animator PlayerAnimator;
    readonly string RunAnimation = "IsRun";


    private void Start()
    {
        SQR_RUN_THRESHOLD = runThreshold * runThreshold;
        oldPos = transform.position; // 初期位置を設定
    }

    // このアクターの座標と向きを更新する
    public void Move(Vector3 pos, Vector3 forward)
    {
        Vector3 distance = pos - oldPos;

        if (distance.sqrMagnitude > SQR_RUN_THRESHOLD) PlayerAnimator.SetBool(RunAnimation, true);
        else PlayerAnimator.SetBool(RunAnimation, false);

        // 座標と向きを更新
        this.gameObject.transform.position = pos;
        this.gameObject.transform.forward = forward;

        oldPos = pos;
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
