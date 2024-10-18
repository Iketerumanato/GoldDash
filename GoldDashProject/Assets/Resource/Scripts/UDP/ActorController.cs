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
    [SerializeField] float smoothSpeed = 0.1f;
    [SerializeField] float animationLerpSpeed = 10f;
    [SerializeField] float rotationSmooth = 5f;
    readonly string MoveAnimationStr = "BlendSpeed";
    //float SQR_RunThreshold;

    private void Awake()
    {
        //SQR_RunThreshold = runThreshold * runThreshold;
        oldPos = transform.position;
        targetPosition = oldPos;
    }

    public void Move(Vector3 pos, Vector3 forward)
    {
        targetPosition = pos;

        // プレイヤーの移動補間
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothSpeed);

        float distance = (targetPosition - oldPos).magnitude;
        float speed = Mathf.Clamp01(distance / runThreshold);

        float currentSpeed = PlayerAnimator.GetFloat(MoveAnimationStr);

        float targetSpeed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * animationLerpSpeed);

        if (speed < 0.01f)
        {
            targetSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * animationLerpSpeed * 0.5f);  // よりゆっくり減少
        }

        PlayerAnimator.SetFloat(MoveAnimationStr, targetSpeed);

        // プレイヤーの向きも補間してスムーズに回転させる
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(forward), Time.deltaTime * 5f);

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