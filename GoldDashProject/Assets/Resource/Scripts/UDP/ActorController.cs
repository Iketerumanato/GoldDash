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

        // Y軸を中心とした回転角を計算 (-180 ~ 180)
        float angle = Vector3.SignedAngle(transform.forward, forward, Vector3.up);

        // 回転が必要な場合のみ回転処理を行う
        if (Mathf.Abs(angle) > 0.1f) // 0.1fは回転しない閾値
        {
            // Y軸方向の回転角度を徐々に適用する
            float rotationSpeed = 5f; // 回転速度の調整
            float step = Mathf.Clamp(rotationSpeed * Time.deltaTime, 0f, Mathf.Abs(angle));
            transform.Rotate(Vector3.up, Mathf.Sign(angle) * step);
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