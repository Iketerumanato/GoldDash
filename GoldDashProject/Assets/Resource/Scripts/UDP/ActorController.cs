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

        // 補間処理。スムーズな位置の更新
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothSpeed);

        // 速度の計算: 現在のフレームの位置変化量を使って速度を計算
        float distance = (targetPosition - oldPos).magnitude;
        float speed = Mathf.Clamp01(distance / runThreshold);

        // アニメーションブレンドの反映速度を改善
        float currentSpeed = PlayerAnimator.GetFloat(MoveAnimationStr);
        float newSpeed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * animationLerpSpeed);
        PlayerAnimator.SetFloat(MoveAnimationStr, newSpeed);  // アニメーションのブレンド速度を反映

        // 向きの更新
        transform.forward = forward;

        oldPos = targetPosition;  // 前フレームの位置を更新
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