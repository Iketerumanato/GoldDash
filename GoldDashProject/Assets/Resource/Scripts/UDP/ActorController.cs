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
                            ? Mathf.Lerp(currentSpeed, speed, Time.deltaTime * animationLerpSpeed * 3f)
                            : Mathf.Lerp(currentSpeed, speed, Time.deltaTime * animationLerpSpeed * 2f);

        PlayerAnimator.SetFloat(MoveAnimationStr, blendSpeed);

        // プレイヤーの向きを移動方向に向ける
        if (forward.magnitude > 0)
        {
            UpdateRotation(forward);
        }

        oldPos = targetPosition;
    }

    private void UpdateRotation(Vector3 forward)
    {
        float signedAngle = GetSignedAngle(transform.forward, forward, Vector3.up);
        Quaternion targetRotation = Quaternion.Euler(0, signedAngle, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(forward), Time.deltaTime * rotationSmooth);
    }

    private float GetSignedAngle(Vector3 from, Vector3 to, Vector3 up)
    {
        float angle = Vector3.Angle(from, to);

        Vector3 cross = Vector3.Cross(from, to);
        if (Vector3.Dot(cross, up) < 0)
        {
            angle = -angle;
        }
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