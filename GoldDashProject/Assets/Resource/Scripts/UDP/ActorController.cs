using UnityEngine;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    //プロパティ
    public string PlayerName { set; get; }
    public ushort SessionID { set; get; }
    public int Gold { set; get; } = 100;

    private Vector3 targetPosition;
    private Vector3 targetForward;
    private Vector3 oldPos;
    private Vector3 currentVelocity;

    [SerializeField] Animator PlayerAnimator;
    [SerializeField] float runThreshold = 0.01f;
    [SerializeField] float smoothSpeed = 0.1f;
    [SerializeField] float animationLerpSpeed = 10f;
    [SerializeField] float rotationSmooth = 5f;

    readonly string strMoveAnimation = "BlendSpeed";
    readonly string strPunchTrigger = "PunchTrigger";

    public bool isPlayer;

    private void Awake()
    {
        isPlayer = GetComponent<Player>() != null;
    }

    private void Start()
    {
        oldPos = transform.position;
        targetPosition = oldPos;
    }

    private void Update()
    {
        if (isPlayer) return;

        // 位置を滑らかに補間
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothSpeed);

        // 移動速度を計算してアニメーションに反映
        float distance = (targetPosition - transform.position).sqrMagnitude;
        float speed = Mathf.Clamp01(distance / (runThreshold * runThreshold)); // 速度を0〜1で正規化

        // デバッグログ: targetPositionとtransform.positionの差分を確認
        Debug.Log("Target Position: " + targetPosition + ", Current Position: " + transform.position + ", Speed: " + speed);

        PlayerAnimator.SetFloat(strMoveAnimation, speed); // 直接速度を設定

        // 回転補間
        float angle = Vector3.SignedAngle(transform.forward, targetForward, Vector3.up);
        if (Mathf.Abs(angle) > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetForward), Time.deltaTime * rotationSmooth);
        }

        oldPos = targetPosition;
    }

    public void Move(Vector3 pos, Vector3 forward)
    {
        targetPosition = pos;
        targetForward = forward;

        // Moveメソッドの呼び出し確認
        Debug.Log("Move method called with Position: " + pos + " and Forward: " + forward);
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

    //モーション関連
    public void PlayMoveAnimation(float blendSpeed)
    {
        PlayerAnimator.SetFloat(strMoveAnimation, blendSpeed);
    }

    public void PunchAnimation()
    {
        PlayerAnimator.SetTrigger(strPunchTrigger);
    }

    public void RecoiledAnimation()
    {
        //怯みアニメーション再生
    }

    public void BlownAnimation()
    {
        //吹っ飛ぶアニメーション再生
    }

    //所持金関連
    public void DropGold()
    {
        //金貨の山を落とす
    }

    //吹っ飛び処理
    public void Blown(Vector3 vector)
    {
        transform.position += this.transform.forward * -0.8f;
        //引数の方向にAddForceで吹っ飛ぶ
        //仮
        //float blowDistance = 3.0f;
        //transform.position += Vector3.Normalize(vector) * blowDistance;
    }
}
