using UnityEngine;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    public string PlayerName { set; get; }
    private Vector3 targetPos;//目標の地点
    private Vector3 currentPos;//現在の地点
    private Quaternion targetRot;
    private Quaternion currentRot;
    [SerializeField] float runThreshold = 0.01f;
    [SerializeField] float interpolationSpeed = 5.0f;//動きを補間する速度
    private float SQR_RunThreshold;
    [SerializeField] Animator PlayerAnimator;
    readonly string MoveAnimationStr = "BlendSpeed";

    private void Start()
    {
        SQR_RunThreshold = runThreshold * runThreshold;

        targetPos = this.gameObject.transform.position;
        currentPos = this.gameObject.transform.position;
        targetRot = this.gameObject.transform.rotation;
        currentRot = this.gameObject.transform.rotation;
    }

    private void Update()
    {
        currentPos = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * interpolationSpeed);
        currentRot = Quaternion.Lerp(currentRot, targetRot, Time.deltaTime * interpolationSpeed);
        this.transform.position = currentPos;
        this.transform.rotation = currentRot;
    }

    // このアクターの座標と向きを更新する
    public void Move(Vector3 pos, Vector3 forward)
    {
        targetPos = pos;
        targetRot = Quaternion.LookRotation(forward);

        float distance = (pos - currentPos).sqrMagnitude;
        float speed = Mathf.Clamp01(distance / SQR_RunThreshold);
        PlayerAnimator.SetFloat(MoveAnimationStr, speed);

        Debug.Log($"PlayerSpeed : {speed}");

        //if (distance.sqrMagnitude > SQR_RunThreshold) PlayerAnimator.SetBool(RunAnimation, true);
        //else PlayerAnimator.SetBool(RunAnimation, false);
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
