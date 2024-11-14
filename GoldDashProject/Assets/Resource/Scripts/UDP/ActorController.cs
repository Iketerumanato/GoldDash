using UnityEngine;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    //プロパティ
    public string PlayerName { set; get; }
    public ushort SessionID { set; get; } //MonoBehaviourからすると、いちいちDictionaryからIDを取るより目の前のアクターのIDを取得した方が速そうなので
    public int Gold { set; get; } = 100; //所持金

    //このアクターはこの場所、この向きを目指してなめらかに移動
    private Vector3 targetPosition;
    private Vector3 targetForward;

    //このアクターがプレイヤーか。プレイヤーならUpdateの内容は実行されない
    private bool isPlayer;

    //SmoothDamp計算用
    private Vector3 currentVelocity_P; //pos
    private Vector3 currentVelocity_F; //forward

    //アニメーションのフラグに使用
    Vector3 oldPos; //前フレームの座標を見て、その大きさから走りモーションのブレンドスピードを決定する

    [SerializeField] Animator PlayerAnimator;
    [SerializeField] float runThreshold = 1f;
    float sqrRunThreshold;
    [SerializeField] float animationLerpSpeed = 70f;

    //歩行アニメーションのメソッドに渡す
    float blendSpeed;

    readonly string strMoveAnimation = "BlendSpeed";
    readonly string strPunchTrigger = "PunchTrigger";
    readonly string strHitedFrontTrigger = "HitFrontActorTrigger";
    readonly string strHitedBackTrigger = "HitBackActorTrigger";

    private void Awake()
    {
        isPlayer = (GetComponent<Player>() != null);
        sqrRunThreshold = runThreshold * runThreshold;
    }

    private void Update()
    {
        if (isPlayer) return;
        this.transform.position = Vector3.SmoothDamp(this.transform.position, targetPosition, ref currentVelocity_P, 0.1f);
        this.transform.forward = Vector3.SmoothDamp(this.transform.forward, targetForward, ref currentVelocity_F, 0.1f);

        //モーション関連。そのまま＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
        float distance = (targetPosition - oldPos).sqrMagnitude;
        float speed = Mathf.Clamp01(distance / sqrRunThreshold);
        float currentSpeed = PlayerAnimator.GetFloat(strMoveAnimation);
        // 上昇時と下降時で別々にLerpの速度を調整する
        float blendSpeed = (speed > currentSpeed)
                            ? Mathf.Lerp(currentSpeed, speed, Time.deltaTime * animationLerpSpeed)
                            : Mathf.Lerp(currentSpeed, speed, Time.deltaTime * animationLerpSpeed);
        PlayMoveAnimation(blendSpeed);
        //＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝

        oldPos = transform.position; //最後にoldPos更新
    }

    public void Move(Vector3 pos, Vector3 forward)
    {
        targetPosition = pos;
        targetForward = forward;
    }

    public void Warp(Vector3 pos, Vector3 forward)
    { 
        this.transform.position = pos;
        this.transform.forward = forward;
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
        //正面殴られモーション再生(Actor)
        PlayerAnimator.SetTrigger(strHitedFrontTrigger);
    }

    public void BlownAnimation()
    {
        //背面殴られモーション再生(Actor)
        PlayerAnimator.SetTrigger(strHitedBackTrigger);
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