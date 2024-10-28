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
    private Vector3 previousPosition;
    private Vector3 targetForward;

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
        targetPosition = transform.position;
        previousPosition = transform.position;
        isPlayer = GetComponent<Player>() != null;
    }

    private void Update()
    {
        if (isPlayer) return;

        // 現在の位置とターゲット位置の移動量を計算
        Vector3 movement = transform.position - targetPosition;

        // 移動量の大きさを計算
        float speed = movement.magnitude;

        // アニメーションの速度を滑らかに変化させる
        float currentSpeed = PlayerAnimator.GetFloat(strMoveAnimation);
        float blendSpeed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * animationLerpSpeed);
        PlayMoveAnimation(blendSpeed);

        // ターゲット位置の更新（この部分は変更しないでください）
        transform.position = targetPosition; // 実際の位置更新を行う場合
    }

    public void Move(Vector3 pos, Vector3 forward)
    {
        targetPosition = pos;
        targetForward = forward;

        // 直接transformを変更せず、targetPositionとtargetForwardのみ更新
        this.gameObject.transform.position = pos;
        this.gameObject.transform.forward = forward;
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
