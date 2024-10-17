using UnityEngine;
using static UnityEditor.PlayerSettings;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    public string PlayerName { set; get; }
    private Vector3 oldPos;

    [SerializeField] Animator PlayerAnimator;
    [SerializeField] float runThreshold = 0.01f; // 移動速度のしきい値
    [SerializeField] float speed = 5.0f;         // 移動速度
    readonly string MoveAnimationStr = "BlendSpeed";
    private Rigidbody rb;

    private void Start()
    {
        oldPos = transform.position;
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // ジョイスティックの入力を取得
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);

        // 移動量に基づいてアニメーションのブレンド速度を決定
        HandleMovementAndAnimation(movement);
    }

    // 移動とアニメーションの処理
    private void HandleMovementAndAnimation(Vector3 movement)
    {
        // ジョイスティックの入力の大きさを計算
        float joystickMagnitude = Mathf.Clamp01(movement.magnitude);

        // アニメーションのブレンド速度をセット (0:待機, 0.5:歩き, 1:走り)
        PlayerAnimator.SetFloat(MoveAnimationStr, joystickMagnitude);

        // キャラクターを移動させる処理
        if (joystickMagnitude > 0)
        {
            // Rigidbodyを使って移動
            rb.MovePosition(transform.position + movement * speed * Time.deltaTime);

            // 移動方向に応じてキャラクターの向きを変更
            transform.rotation = Quaternion.LookRotation(movement);
        }
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