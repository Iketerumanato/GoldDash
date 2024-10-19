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
            Quaternion targetRotation = Quaternion.LookRotation(forward);
            float targetYRotation = Mathf.Repeat(targetRotation.eulerAngles.y, 360f);
            float currentYRotation = transform.eulerAngles.y;

            // 急激な回転を避けるため、補間処理
            float angleDifference = Mathf.DeltaAngle(currentYRotation, targetYRotation);
            if (Mathf.Abs(angleDifference) > 180)
            {
                angleDifference = angleDifference > 0 ? angleDifference - 360 : angleDifference + 360;
            }

            // 新しい回転をスムーズに補間
            float newYRotation = currentYRotation + angleDifference * Time.deltaTime * rotationSmooth;
            transform.rotation = Quaternion.Euler(0, newYRotation, 0);
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