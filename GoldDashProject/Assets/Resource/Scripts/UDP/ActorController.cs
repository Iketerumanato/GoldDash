using UnityEngine;
using static UnityEditor.PlayerSettings;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    public string PlayerName { set; get; }
    private Vector3 targetPosition;
    private Vector3 oldPos;
    private Vector3 currentVelocity;
    [SerializeField] Animator PlayerAnimator;
    [SerializeField] float runThreshold = 0.01f;
    [SerializeField] float smoothSpeed = 0.1f; // 0.05fから改善。スムーズさの速度を少し早める
    [SerializeField] float animationLerpSpeed = 10f; // アニメーションブレンドの速度調整
    readonly string MoveAnimationStr = "BlendSpeed";
    float SQR_RunThreshold;

    private void Awake()
    {
        SQR_RunThreshold = runThreshold * runThreshold;
        oldPos = transform.position;
        targetPosition = oldPos;
    }

    public void Move(Vector3 pos, Vector3 forward)
    {
        targetPosition = pos;

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothSpeed);

        // 速度の計算: 現在のフレームの位置変化量を使って速度を計算
        float distance = (targetPosition - oldPos).magnitude;
        float speed = Mathf.Clamp01(distance / runThreshold);

        float currentSpeed = PlayerAnimator.GetFloat(MoveAnimationStr);
        float newSpeed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * animationLerpSpeed);
        PlayerAnimator.SetFloat(MoveAnimationStr, newSpeed);

        // 目標の前方ベクトル（目的の向き）と現在の前方ベクトル（現在の向き）を使って角度を計算
        float angle = Vector3.Angle(transform.forward, forward);

        // 外積を使って回転方向を判定
        Vector3 cross = Vector3.Cross(transform.forward, forward); // 外積を計算

        // 外積のY成分が正なら反時計回り、負なら時計回り
        if (cross.y < 0)  angle = -angle; // 時計回りなら負の角度にする

        // 回転が必要な場合のみ回転処理を行う
        if (Mathf.Abs(angle) > 0.1f) // 0.1fは回転しない閾値
        {
            // 回転速度を調整して、徐々に回転させる
            float rotationSpeed = 5f; // 回転速度の調整
            float step = Mathf.Clamp(rotationSpeed * Time.deltaTime, 0f, Mathf.Abs(angle)); // 1フレームで回りすぎないように制限
            transform.Rotate(Vector3.up, Mathf.Sign(angle) * step); // 回転方向に応じて回転
        }

        oldPos = targetPosition;
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