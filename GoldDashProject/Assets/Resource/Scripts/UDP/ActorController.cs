using UnityEngine;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    public string PlayerName { set; get; }
    Vector3 oldPos;
    Vector3 targetPos; // サーバーから受け取るターゲット位置
    Vector3 predictedPos;
    [SerializeField] float runThreshold = 0.01f;
    private float SQR_RunThreshold;
    [SerializeField] Animator PlayerAnimator;
    [SerializeField] float positionCorrectionSpeed = 5.0f; // 位置補正のスムーズさを制御する変数
    [SerializeField] float interpolationTime = 0.1f; // 補間時間を設定
    float timeSinceLastUpdate = 0f;
    readonly string MoveAnimationStr = "BlendSpeed";

    private void Start()
    {
        SQR_RunThreshold = runThreshold * runThreshold;
        targetPos = this.transform.position; // 初期のターゲット位置を現在位置と同じにする
        predictedPos = this.transform.position;
    }

    private void Update()
    {
        // サーバーから次の更新が来るまでの間、補間を行う
        timeSinceLastUpdate += Time.deltaTime;

        // 補間を使用して、ターゲット位置へゆっくりと補正する
        transform.position = Vector3.Lerp(transform.position, targetPos, Mathf.Clamp01(timeSinceLastUpdate / interpolationTime));
    }

    public void Move(Vector3 serverPos, Vector3 forward)
    {
        // サーバーからの座標を保存
        targetPos = serverPos;

        // サーバーの座標と前回座標から移動距離を計算
        float distance = (serverPos - oldPos).sqrMagnitude;
        float speed = Mathf.Clamp01(distance / SQR_RunThreshold);
        PlayerAnimator.SetFloat(MoveAnimationStr, speed);

        // キャラクターの向きを更新
        this.gameObject.transform.forward = forward;

        // 古い位置を更新
        oldPos = serverPos;
        timeSinceLastUpdate = 0f; // サーバーから新しいデータを受け取った時点で時間をリセット
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