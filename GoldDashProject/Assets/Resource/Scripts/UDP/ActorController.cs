using UnityEngine;
using static UnityEditor.PlayerSettings;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    public string PlayerName { set; get; }
    private Vector3 targetPosition; // サーバーから受信した目標位置
    private Vector3 oldPos;         // 前回の位置
    [SerializeField] Animator PlayerAnimator;
    readonly string MoveAnimationStr = "BlendSpeed";

    private void Start()
    {
        oldPos = transform.position; // 初期位置を設定
        targetPosition = transform.position; // 初期目標位置
    }

    private void Update()
    {
        // キャラクターを目標位置に移動
        transform.position = targetPosition;

        // アニメーションのブレンドを更新
        UpdateAnimationBlend();
    }

    // サーバーから受け取った位置を設定するメソッド
    public void Move(Vector3 serverPos, Vector3 forward)
    {
        // サーバーから受信した新しい位置を目標位置として設定
        targetPosition = serverPos;

        // キャラクターの向きを更新
        transform.forward = forward;

        // 古い位置を更新
        oldPos = serverPos;
    }

    private void UpdateAnimationBlend()
    {
        // 前回位置との距離を計算し、アニメーションのブレンドを更新
        float distance = (targetPosition - oldPos).sqrMagnitude;
        float speed = Mathf.Clamp01(distance); // 適切なスピードを計算
        PlayerAnimator.SetFloat(MoveAnimationStr, speed);
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