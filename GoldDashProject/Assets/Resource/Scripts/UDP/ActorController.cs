using UnityEngine;
using UnityEngine.Windows;
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
    [SerializeField] float smoothSpeed = 0.1f;
    [SerializeField] float animationLerpSpeed = 10f;
    [SerializeField] float rotationSmooth = 5f;
    readonly string MoveAnimationStr = "BlendSpeed";

    private void Awake()
    {
        oldPos = transform.position;
        targetPosition = oldPos;
    }

    public void Move(Vector3 pos, Vector3 forward)
    {
        targetPosition = pos;

        // プレイヤーの位置を補間
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothSpeed);

        float distance = (targetPosition - oldPos).magnitude;
        float speed = Mathf.Clamp01(distance / runThreshold);

        float currentSpeed = PlayerAnimator.GetFloat(MoveAnimationStr);

        // 上昇時と下降時で別々にLerpの速度を調整する
        float blendSpeed = (speed > currentSpeed)
                            ? Mathf.Lerp(currentSpeed, speed, Time.deltaTime * animationLerpSpeed * 7f)
                            : Mathf.Lerp(currentSpeed, speed, Time.deltaTime * animationLerpSpeed * 7f);

        PlayerAnimator.SetFloat(MoveAnimationStr, blendSpeed);

        // プレイヤーの向きを移動方向に向ける
        if (forward.magnitude > 0f)
        {
            // ターゲットの回転角を計算 (Y軸の角度)
            Quaternion targetRotation = Quaternion.LookRotation(forward);
            float targetYAngle = targetRotation.eulerAngles.y;

            // 現在の回転角度を取得し、-180度~180度に正規化
            float currentYAngle = transform.rotation.eulerAngles.y;
            currentYAngle = NormalizeAngle(currentYAngle);

            // ターゲットのY軸回転角度を正規化
            targetYAngle = NormalizeAngle(targetYAngle);

            // Mathf.LerpAngleを使用してスムーズに回転
            float smoothedYAngle = Mathf.LerpAngle(currentYAngle, targetYAngle, Time.deltaTime * rotationSmooth);

            // 回転を適用 (Y軸の回転のみ)
            transform.rotation = Quaternion.Euler(0, smoothedYAngle, 0);
        }

        oldPos = targetPosition;
    }
    private float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
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