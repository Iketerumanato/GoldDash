using UnityEngine;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    public string PlayerName { set; get; }
    public bool IsRun { set; get; }
    Vector3 oldPos;
    [SerializeField] float runThreshold = 0.01f;
    private float SQR_RunThreshold;
    [SerializeField] Animator PlayerAnimator;
    readonly string RunAnimation = "IsRun";

    private void Start()
    {
        SQR_RunThreshold = runThreshold * runThreshold;
        oldPos = transform.position; // 初期位置を設定
    }

    // このアクターの座標と向きを更新する
    public void Move(Vector3 pos, Vector3 forward)
    {
        float distance = (pos - oldPos).sqrMagnitude;
        float speed = Mathf.Clamp01(distance / SQR_RunThreshold);
        PlayerAnimator.SetFloat("BlendSpeed",　speed);

        //if (distance.sqrMagnitude > SQR_RunThreshold) PlayerAnimator.SetBool(RunAnimation, true);
        //else PlayerAnimator.SetBool(RunAnimation, false);

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
