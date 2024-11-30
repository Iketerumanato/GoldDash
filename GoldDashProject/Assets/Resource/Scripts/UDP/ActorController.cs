using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    //定数
    private const int MAGIC_INVENTRY_MAX = 3; //魔法の最大所持数
    private int magicInventry = 0; //現在の魔法の所持数

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

    //魔法関連
    //所持している魔法を管理する固定長の配列
    public int[] magicIDArray;

    //仮
    //所持金テキスト
    [SerializeField] private TextMeshProUGUI goldText;

    //魔法をスロットに入れる
    public void SetMagicToSlot(int magicID)
    {
        if (magicInventry == MAGIC_INVENTRY_MAX)
        { 
            //魔法をこれ以上持てないときの処理
        }

        for (int i = 0; i < magicIDArray.Count(); i++)
        {
            //未所持ならそのスロットに入れる
            if(magicIDArray[i] == -1) //Definer.MID.NONEは-1
            {
                magicIDArray[i] = magicID;
            }
        }
    }

    private void Start()
    {
        //プレイヤーか否か確認する
        isPlayer = (GetComponent<PlayerController>() != null);
        //2乗した定数の計算
        sqrRunThreshold = runThreshold * runThreshold;

        //コレクションのインスタンス作成
        magicIDArray = new int[MAGIC_INVENTRY_MAX];
        for (int i = 0; i < magicIDArray.Count(); i++)
        {
            magicIDArray[i] = (int)Definer.MID.NONE; //すべて未所持にする
        }
    }

    private void Update()
    {
        if (isPlayer) UpdateForPlayer();
        else UpdateForEnemy();
    }

    private void UpdateForPlayer()
    {
        goldText.text = $"Gold:{Gold}";
    }

    private void UpdateForEnemy()
    {
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