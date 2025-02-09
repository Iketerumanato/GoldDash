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

    //プロパティ
    public string PlayerName { set; get; }
    public ushort SessionID { set; get; } //MonoBehaviourからすると、いちいちDictionaryからIDを取るより目の前のアクターのIDを取得した方が速そうなので

    private int gold = 100;//バッキン
    public int Gold//所持金
    {
        set
        {
            gold = value;
            CheckBagSituation(value);
        }

        get { return gold; }
    }

    public Definer.PLAYER_COLOR Color { set; get; } //アクターの色

    private int magicInventry; //魔法の所持数。0より小さくならない。MAGIC_INVENTRY_MAXを越えて増えない。
    public int MagicInventry
    {
        set
        { 
            if (0 <= value && value <= MAGIC_INVENTRY_MAX) 
            { 
                magicInventry = value;
                CheckSmallScrollSituation(value);
            }
        }
        get { return magicInventry; }
    }

    //アイコン後光用設定
    private bool isShining = false;
    [Header("アイコン用後光")]
    [SerializeField] private GameObject ShineEffect;
    public bool IsShining
    {
        set
        {
            isShining = value;
            ShineEffect.SetActive(value);
        }

        get { return isShining; }
    }

    //このアクターはこの場所、この向きを目指してなめらかに移動
    private Vector3 targetPosition;
    private Vector3 targetForward;

    //このアクターがプレイヤーか。Updateの内容をこれで分岐させる
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
    //スクロールのアニメーションを再生する時のみSetActiveをtrueに変更させる
    [SerializeField] GameObject BigScroll;

    //歩行アニメーションのメソッドに渡す
    float blendSpeed;

    const string strRunSpeed = "RunSpeed";
    const string strScrollFlag = "ScrollFlag";
    const string strChestFlag = "ChestFlag";
    const string strStunnedFlag = "StunnedFlag";
    const string strPunchTrigger = "PunchTrigger";
    const string strGuardTrigger = "GuardTrigger";
    const string strBlownTrigger = "BlownTrigger";

    //仮
    //所持金テキスト
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("プレイヤーの体のメッシュレンダラー")]
    [SerializeField] private SkinnedMeshRenderer m_skinnedMeshRenderer;

    [Header("プレイヤーの色に対応したマテリアル。赤→緑→青→黄→白")]
    [SerializeField] private Material materialRed;
    [SerializeField] private Material materialGreen;
    [SerializeField] private Material materialBlue;
    [SerializeField] private Material materialYellow;
    [SerializeField] private Material materialWhite;

    //色変え用　緑と白の表示を切り替えるため
    [SerializeField] private GameObject greenIcon;
    [SerializeField] private GameObject whiteIcon;

    [Header("表示非表示切り替えのオブジェクト一覧")]
    //小、中、大の順(所持金によって切り替わる)
    [SerializeField] GameObject ActorBigBag;
    [SerializeField] GameObject ActorMiddleBag;
    [SerializeField] GameObject ActorSmallBag;

    //現在所持している巻物の数分アクター側で表示する
    [SerializeField] GameObject ActorSmallScrollOne;
    [SerializeField] GameObject ActorSmallScrollTwo;
    [SerializeField] GameObject ActorSmallScrollThree;

    private int smallscrollNum = 0;
    public int SmallScrollNum
    {
        set { if (0 <= value && value <= MAGIC_INVENTRY_MAX) smallscrollNum = value; }
        get { return smallscrollNum; }
    }

    private void Start()
    {
        //プレイヤーか否か確認する
        isPlayer = (GetComponent<PlayerControllerV2>() != null);
        //2乗した定数の計算
        sqrRunThreshold = runThreshold * runThreshold;
    }

    private void Update()
    {
        if (!isPlayer) UpdateForEnemy();
        //CheckSmallScrollSituation();//魔法の所持数のチェック
    }


    private void UpdateForEnemy()
    {
        this.transform.position = Vector3.SmoothDamp(this.transform.position, targetPosition, ref currentVelocity_P, 0.1f);
        this.transform.forward = Vector3.SmoothDamp(this.transform.forward, targetForward, ref currentVelocity_F, 0.1f);

        //モーション関連。そのまま＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
        float distance = (targetPosition - oldPos).sqrMagnitude;
        float speed = Mathf.Clamp01(distance / sqrRunThreshold);
        float currentSpeed = PlayerAnimator.GetFloat(strRunSpeed);
        // 上昇時と下降時で別々にLerpの速度を調整する
        float RunSpeed = (speed > currentSpeed)
                            ? Mathf.Lerp(currentSpeed, speed, Time.deltaTime * animationLerpSpeed)
                            : Mathf.Lerp(currentSpeed, speed, Time.deltaTime * animationLerpSpeed);
        PlayMoveAnimation(RunSpeed);
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

    //モーション関連
    public void PlayMoveAnimation(float blendSpeed)
    {
        PlayerAnimator.SetFloat(strRunSpeed, blendSpeed);
    }

    public void PunchAnimation()
    {
        PlayerAnimator.SetTrigger(strPunchTrigger);
    }

    public void GuardAnimation()
    {
        //正面殴られモーション再生(Actor)
        PlayerAnimator.SetTrigger(strGuardTrigger);
    }

    public void BlownAnimation()
    {
        //背面殴られモーション再生(Actor)
        PlayerAnimator.SetTrigger(strBlownTrigger);
    }

    //スクロースの使用モーション再生※SetBoolでtrueになる前にあらかじめ非表示にしたスクロールのオブジェクトを表示してから再生
    public void PlayScrollAnimation()
    {
        if (BigScroll == null) return;
        BigScroll.SetActive(true);
        PlayerAnimator.SetBool(strScrollFlag, true);
    }
    //スクロールオブジェクトの非表示
    public void EndScrollAnimation()
    {
        if (BigScroll == null) return;
        BigScroll.SetActive(false);
        PlayerAnimator.SetBool(strScrollFlag, false);
    }

    //Actor側で雷を食らった時のモーション再生
    public void PlayStunAnimation()
    {
        PlayerAnimator.SetBool(strStunnedFlag, true);
    }

    public void EndStunAnimation()
    {
        PlayerAnimator.SetBool(strStunnedFlag, false);
    }

    //Actor側で雷を食らった時のモーション再生
    public void PlayChestAnimation()
    {
        PlayerAnimator.SetBool(strChestFlag, true);
    }

    public void EndChestAnimation()
    {
        PlayerAnimator.SetBool(strChestFlag, false);
    }

    //マテリアル変更するやり方での色変え
    public void ChangePlayerColor(Definer.PLAYER_COLOR color)
    {
        switch (color)
        {
            case Definer.PLAYER_COLOR.RED:
                m_skinnedMeshRenderer.material = materialRed;
                break;
            case Definer.PLAYER_COLOR.GREEN:
                m_skinnedMeshRenderer.material = materialGreen;
                break;
            case Definer.PLAYER_COLOR.BLUE:
                m_skinnedMeshRenderer.material = materialBlue;
                break;
            case Definer.PLAYER_COLOR.YELLOW:
                m_skinnedMeshRenderer.material = materialYellow;
                break;
            default:
                break;
        }
    }

    //白に色変え　クライアント用
    public void ChangeGreenToWhiteClient()
    {
        m_skinnedMeshRenderer.material = materialWhite;
    }

    //白に色変え　サーバー用
    public void ChangeGreenToWhiteServer()
    {
        greenIcon.SetActive(false);
        whiteIcon.SetActive(true);
    }

    //毎フレームGoldの値からバッグの外見を変えていくためのチェックを行う
    void CheckBagSituation(int gold)
    {
        if (this == null) return;//nullなら早期リターン

        // バッグオブジェクトのnullチェック
        if (ActorBigBag == null || ActorMiddleBag == null || ActorSmallBag == null)
        {
            Debug.LogWarning("One or more bag objects are null. Check the references.");
            return;
        }

        if (gold < 500)
        {
            ActorBigBag.SetActive(false);//バッグ大
            ActorMiddleBag.SetActive(false);//バッグ中
            ActorSmallBag.SetActive(true);//バッグ小
        }
        else if (500 <= gold && gold < 2000)
        {
            ActorBigBag.SetActive(false);//バッグ大
            ActorMiddleBag.SetActive(true);//バッグ中
            ActorSmallBag.SetActive(false);//バッグ小
        }
        else if (2000 <= gold)
        {
            ActorBigBag.SetActive(true);//バッグ大
            ActorMiddleBag.SetActive(false);//バッグ中
            ActorSmallBag.SetActive(false);//バッグ小
        }
    }

    //毎フレームスクロールの所持数のチェックをベルトについているスクロールで行う
    void CheckSmallScrollSituation(int scrollNum)
    {
        if (this == null) return;//nullなら早期リターン

        // スクロールオブジェクトのnullチェック
        if (ActorSmallScrollOne == null || ActorSmallScrollTwo == null || ActorSmallScrollThree == null)
        {
            Debug.LogWarning("One or more scroll objects are null. Check the references.");
            return;
        }

        switch (scrollNum)//仮のプロパティ
        {
            case 0:
                ActorSmallScrollOne.SetActive(false);
                ActorSmallScrollTwo.SetActive(false);
                ActorSmallScrollThree.SetActive(false);
                break;
            case 1:
                ActorSmallScrollOne.SetActive(true);
                ActorSmallScrollTwo.SetActive(false);
                ActorSmallScrollThree.SetActive(false);
                break;
            case 2:
                ActorSmallScrollOne.SetActive(true);
                ActorSmallScrollTwo.SetActive(true);
                ActorSmallScrollThree.SetActive(false);
                break;
            case 3:
                ActorSmallScrollOne.SetActive(true);
                ActorSmallScrollTwo.SetActive(true);
                ActorSmallScrollThree.SetActive(true);
                break;
            default:
                break;
        }
    }
}