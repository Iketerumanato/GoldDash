using UnityEngine;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    public string PlayerName { set; get; }
    Vector3 oldPos;
    Vector3 predictedPos;
    [SerializeField] float runThreshold = 0.01f;
    private float SQR_RunThreshold;
    [SerializeField] Animator PlayerAnimator;
    [SerializeField] float positionCorrectionSpeed = 5.0f; // 位置補正のスムーズさを制御する変数
    readonly string MoveAnimationStr = "BlendSpeed";

    private void Start()
    {
        SQR_RunThreshold = runThreshold * runThreshold;
        oldPos = transform.position;
        predictedPos = transform.position;
    }

    public void Move(Vector3 serverPos, Vector3 forward)
    {
        float distance = (serverPos - oldPos).sqrMagnitude;
        float speed = Mathf.Clamp01(distance / SQR_RunThreshold);
        PlayerAnimator.SetFloat(MoveAnimationStr, speed);

        Vector3 moveDirection = oldPos - predictedPos;
        predictedPos += moveDirection;

        predictedPos = Vector3.Lerp(predictedPos, serverPos, Time.deltaTime * positionCorrectionSpeed);

        this.gameObject.transform.position = predictedPos;

        this.gameObject.transform.forward = forward;

        oldPos = serverPos;
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
