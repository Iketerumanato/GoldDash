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
    [SerializeField] Animator PlayerAnimator;
    [SerializeField] float runThreshold = 0.01f;
    readonly string MoveAnimationStr = "BlendSpeed";
    float SQR_RunThreshold;

    private void Start()
    {
        SQR_RunThreshold = runThreshold * runThreshold;
        oldPos = transform.position;
    }

    // このアクターの座標と向きを更新する
    public void Move(Vector3 pos, Vector3 forward)
    {
        transform.position = Vector3.Lerp(transform.position, pos, 0.1f);
        transform.forward = Vector3.Lerp(transform.forward, forward, 0.1f);

        float distance = (pos - oldPos).sqrMagnitude;
        float speed = distance / Time.deltaTime;
        float smoothSpeed = Mathf.Lerp(PlayerAnimator.GetFloat(MoveAnimationStr), speed, 0.1f);
        PlayerAnimator.SetFloat(MoveAnimationStr, smoothSpeed);

        //if (distance.sqrMagnitude > SQR_RunThreshold) PlayerAnimator.SetBool(RunAnimation, true);
        //else PlayerAnimator.SetBool(RunAnimation, false);

        oldPos = pos;
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