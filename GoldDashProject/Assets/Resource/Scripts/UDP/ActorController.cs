using UnityEngine;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    public string PlayerName { set; get; }
    public ushort SessionID { set; get; } //MonoBehaviourからすると、いちいちDictionaryからIDを取るより目の前のアクターのIDを取得した方が速そうなので
    public bool IsRun { set; get; }
    Vector3 oldPos;
    [SerializeField] float runThreshold = 0.01f;
    private float SQR_RunThreshold;
    [SerializeField] Animator PlayerAnimator;
    readonly string RunAnimation = "IsRun";

    // このアクターの座標と向きを更新する
    public void Move(Vector3 pos, Vector3 forward)
    {
        SQR_RunThreshold = runThreshold * runThreshold;
        oldPos = transform.position; // 初期位置を設定
        Vector3 distance = pos - oldPos;

        if (distance.sqrMagnitude > SQR_RunThreshold) PlayerAnimator.SetBool(RunAnimation, true);
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

    //以下手動マージ
    public void PunchAnimation()
    { 
        //パンチモーション再生
    }

    public void RecoiledAnimation()
    { 
        //怯みアニメーション再生
    }

    public void BlownAnimation()
    { 
        //吹っ飛ぶアニメーション再生
    }

    public void DropGold()
    { 
        //金貨の山を落とす
    }

    public void Blown(Vector3 vector)
    {
        //引数の方向にAddForceで吹っ飛ぶ
        //仮
        float blowDistance = 3.0f;
        transform.position += Vector3.Normalize(vector) * blowDistance;
    }
}
