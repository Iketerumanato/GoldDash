using UnityEngine;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    public string PlayerName { set; get; }
    Vector3 oldPos;
    Vector3 predictedPos;
    Vector3 lastServerPos; // サーバーからの最後の位置
    Vector3 velocity; // 現在の速度を計算
    [SerializeField] float runThreshold = 0.01f;
    private float SQR_RunThreshold;
    [SerializeField] Animator PlayerAnimator;
    [SerializeField] float positionCorrectionSpeed = 5.0f; // 位置補正のスムーズさを制御する変数
    [SerializeField] float lerpFactor = 0.1f; // サーバーからの位置情報に対する補間係数
    readonly string MoveAnimationStr = "BlendSpeed";

    private void Start()
    {
        SQR_RunThreshold = runThreshold * runThreshold;
        oldPos = transform.position; // 初期位置を設定
        predictedPos = transform.position; // 初期の推測位置を現在位置と同じにする
        lastServerPos = transform.position; // 初期のサーバー位置も同様に設定
    }

    // このアクターの座標と向きを更新する
    public void Move(Vector3 serverPos, Vector3 forward)
    {
        // サーバーからの最新位置と古い位置の距離を計算
        float distance = (serverPos - oldPos).sqrMagnitude;
        float speed = Mathf.Clamp01(distance / SQR_RunThreshold);
        PlayerAnimator.SetFloat(MoveAnimationStr, speed);

        // 前回のサーバー位置との差分を計算して予測
        Vector3 moveDirection = serverPos - lastServerPos;

        // サーバーからの位置に対して予測位置をスムーズに補正する
        predictedPos = Vector3.SmoothDamp(transform.position, serverPos, ref velocity, lerpFactor);

        // サーバーから受け取った位置と予測位置の補正
        predictedPos = Vector3.Lerp(predictedPos, serverPos, Time.deltaTime * positionCorrectionSpeed);

        // キャラクターを補正された位置に移動
        this.gameObject.transform.position = predictedPos;

        // キャラクターの向きを更新
        this.gameObject.transform.forward = forward;

        // 前回のサーバー位置を更新
        lastServerPos = serverPos;

        // 古い位置も更新
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