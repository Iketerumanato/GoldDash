using UnityEngine;
using UnityEngine.Windows;
using static UnityEditor.PlayerSettings;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    //プロパティ
    public string PlayerName { set; get; }
    public ushort SessionID { set; get; } //MonoBehaviourからすると、いちいちDictionaryからIDを取るより目の前のアクターのIDを取得した方が速そうなので
    public int Gold { set; get; } = 100; //所持金

    private Vector3 targetPosition;
    private Vector3 oldPos;
    private Vector3 currentVelocity;
    [SerializeField] Animator PlayerAnimator;
    [SerializeField] float runThreshold = 0.01f;
    [SerializeField] float smoothSpeed = 0.1f;
    [SerializeField] float animationLerpSpeed = 70f;
    [SerializeField] float rotationSmooth = 10f;
    readonly string strMoveAnimation = "BlendSpeed";
    readonly string strPunchTrigger = "PunchTrigger";

    Vector3 pos;
    Vector3 forward;

    private void Awake()
    {
        oldPos = this.gameObject.transform.position;
        targetPosition = oldPos;

        //pos = this.gameObject.transform.position;
        //forward = this.gameObject.transform.forward;
    }

    private void Update()
    {
        targetPosition = pos;

        //// プレイヤーの位置を補間
        //transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothSpeed);

        float distance = (targetPosition - oldPos).sqrMagnitude;
        float sqrRunThreshold = runThreshold * runThreshold;
        float speed = Mathf.Clamp01(distance / sqrRunThreshold);

        float currentSpeed = PlayerAnimator.GetFloat(strMoveAnimation);

        // 上昇時と下降時で別々にLerpの速度を調整する
        float blendSpeed = (speed > currentSpeed)
                            ? Mathf.Lerp(currentSpeed, speed, Time.deltaTime * animationLerpSpeed)
                            : Mathf.Lerp(currentSpeed, speed, Time.deltaTime * animationLerpSpeed);

        PlayMoveAnimation(blendSpeed);

        //// 現在の向きとターゲットの向きの角度を-180~180で計算
        //float angle = Vector3.SignedAngle(transform.forward, forward, Vector3.up);

        //// 回転を補間
        //if (Mathf.Abs(angle) > 0.01f)
        //{
        //    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(forward), Time.deltaTime * rotationSmooth);
        //}

        //oldPos = targetPosition;
    }

    public void Move(Vector3 pos, Vector3 forward)
    {
        // 座標と向きを更新
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
