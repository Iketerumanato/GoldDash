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
    [SerializeField] float soomthSpeed = 0.05f;
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

        // 補間処理
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, soomthSpeed); // 0.1fはスムーズさの調整値

        float distance = (targetPosition - oldPos).sqrMagnitude;
        float speed = Mathf.Lerp(PlayerAnimator.GetFloat(MoveAnimationStr), Mathf.Clamp01(distance / SQR_RunThreshold), Time.deltaTime * 1f);
        PlayerAnimator.SetFloat(MoveAnimationStr, speed);

        transform.forward = forward;

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