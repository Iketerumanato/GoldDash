using UnityEngine;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    public string PlayerName { set; get; }
    Vector3 oldPos;
    Vector3 predictedPos;
    [SerializeField] float runThreshold = 0.01f;
    private float SQR_RunThreshold;
    [SerializeField] Animator PlayerAnimator;
    [SerializeField] float positionCorrectionSpeed = 5.0f; // 位置補正のスムーズさを制御する変数
    readonly string MoveAnimationStr = "BlendSpeed";

    private void Start()
    {
        SQR_RunThreshold = runThreshold * runThreshold;
        oldPos = transform.position; // 初期位置を設定
        predictedPos = transform.position; // 初期の推測位置を現在位置と同じにする
    }

    // このアクターの座標と向きを更新する
    public void Move(Vector3 serverPos, Vector3 forward)
    {
        float distance = (serverPos - oldPos).sqrMagnitude;
        float speed = Mathf.Clamp01(distance / SQR_RunThreshold);
        PlayerAnimator.SetFloat(MoveAnimationStr, speed);

        // 古い位置を更新
        oldPos = serverPos;
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