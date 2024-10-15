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
        // 前回位置との距離を計算
        float distance = (serverPos - oldPos).sqrMagnitude;
        float speed = Mathf.Clamp01(distance / SQR_RunThreshold);
        PlayerAnimator.SetFloat(MoveAnimationStr, speed);

        // 予測位置を計算（過去の移動ベクトルを使って予測）
        Vector3 moveDirection = oldPos - predictedPos;
        predictedPos += moveDirection;

        // サーバーから受け取った位置と予測位置との補正
        predictedPos = Vector3.Lerp(predictedPos, serverPos, Time.deltaTime * positionCorrectionSpeed);

        // キャラクターを補正された位置に移動
        this.gameObject.transform.position = predictedPos;

        // キャラクターの向きを更新
        this.gameObject.transform.forward = forward;

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