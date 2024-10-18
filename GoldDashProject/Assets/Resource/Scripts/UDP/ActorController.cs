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
        // 補間を使用して、現在の位置と新しい位置の間をスムーズに移動
        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * 0.1f); // moveSpeedは調整可能
        transform.forward = forward;

        // 新しい位置と旧位置の距離を計算
        float distance = (transform.position - oldPos).sqrMagnitude;

        // スピードを計算
        float speed = Mathf.Lerp(PlayerAnimator.GetFloat(MoveAnimationStr), Mathf.Clamp01(distance / SQR_RunThreshold), Time.deltaTime * 1f);
        PlayerAnimator.SetFloat(MoveAnimationStr, speed);

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