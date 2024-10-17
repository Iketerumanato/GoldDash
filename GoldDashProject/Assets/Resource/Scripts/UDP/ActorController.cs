using UnityEngine;
using static UnityEditor.PlayerSettings;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    public string PlayerName { set; get; }
    private Vector3 velocity;
    private Vector3 oldPos;
    private float lastUpdateTime;
    private bool isServerDataReceived;
    [SerializeField] Animator PlayerAnimator;
    [SerializeField] float runThreshold = 0.01f;
    readonly string MoveAnimationStr = "BlendSpeed";
    float SQR_RunThreshold;

    private void Awake()
    {
        SQR_RunThreshold = runThreshold * runThreshold;
        oldPos = transform.position;
        lastUpdateTime = Time.time;
        isServerDataReceived = false;
    }

    // このアクターの座標と向きを更新する
    public void Move(Vector3 pos, Vector3 forward)
    {
        float currentTime = Time.time;
        float deltaTime = currentTime - lastUpdateTime;

        if (isServerDataReceived)
        {
            // サーバーからのデータが到着した場合、そのデータを元に速度を計算
            velocity = (pos - oldPos) / deltaTime;
            transform.position = Vector3.Lerp(transform.position, pos, 0.1f); // サーバーの位置に補間で近づける
            lastUpdateTime = currentTime;
            oldPos = pos;
        }
        else
        {
            // サーバーからのデータがない場合は、予測された位置を計算
            Vector3 predictedPos = transform.position + velocity * deltaTime;
            transform.position = predictedPos;
        }

        float distance = (transform.position - oldPos).magnitude;
        float speed = distance / deltaTime;    
        PlayerAnimator.SetFloat(MoveAnimationStr, speed);

        transform.forward = Vector3.Slerp(transform.forward, forward, 0.1f);
        isServerDataReceived = false;
    }

    public void UpdateFromServer(Vector3 serverPos, Vector3 forward)
    {
        isServerDataReceived = true;
        Move(serverPos, forward);  // サーバーからのデータに基づいてキャラクターを更新
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