using UnityEngine;
using DG.Tweening;

public class MagicButton : MonoBehaviour
{
    [SerializeField] private Definer.MID magicID;

    [SerializeField] private float rizeUpTargetPosY; //上にフリックしたとき、この高さまで上昇する
    [SerializeField] private float rizeUpTime; //上にフリックしたとき、この秒数で目的地まで上昇する

    [SerializeField] private float rotateTime; //左右にフリックしたとき、この秒数で回転アニメーションをする

    [SerializeField] private float returnOriginPosTime; //初期位置に戻るとき、この秒数で目的地まで移動する

    [SerializeField] float ButtonAnimationDuration = 0.2f;

    [SerializeField] float FlickThreshold = 1.0f; // フリック距離の閾値

    [SerializeField] float minVerticalRatio = 0.8f;

    bool isActive = true;//魔法使い終わりで使用

    [SerializeField] Transform MoveEndPos;
    private Vector3 localMoveEndPos;

    [SerializeField] Transform buttonOriginPos;
    private Vector3 locabuttonOriginPos;

    [SerializeField] Material buttonDissolveMat;
    const string offsetName = "_DissolveOffest";
    readonly float maxButtonAlpha = 1f;

    [SerializeField] GameObject ButtonGuideObj;

    private void OnEnable()
    {
        SetDissolveMatOffset(maxButtonAlpha);
    }

    private void Start()
    {
        localMoveEndPos = transform.parent.InverseTransformPoint(MoveEndPos.position);
        locabuttonOriginPos = transform.parent.InverseTransformPoint(buttonOriginPos.position);
    }

    public float FollowFingerPosY(Vector3 pos) //y座標について追従する
    {
        float Diff_Y = pos.y - this.transform.position.y; //Y座標の差分
        this.transform.position = new Vector3(this.transform.position.x, pos.y, this.transform.position.z);
        if (transform.position.y > 0.2f) OnFlickAnimation(localMoveEndPos);
        ButtonGuideObj.SetActive(true);
        return Diff_Y;
    }

    public void FrickUpper(Vector3 dragVector)
    {
        //Vector3 EndPosVec = transform.parent.InverseTransformPoint(MoveEndPos.position);
        ButtonGuideObj.SetActive(false);

        if (dragVector.sqrMagnitude > FlickThreshold * FlickThreshold && IsUpwardFlick(dragVector))
        {
            OnFlickAnimation(localMoveEndPos);
            Debug.Log("上にフリックされたぞ！");
        }
        else ReturnToOriginPos();
    }

    public Definer.MID OnFlickAnimation(Vector3 targetLocalPos) //上にフリックされたときのアニメーション。発動する魔法のIDを返却する
    {
        //決まった高さまで上昇するアニメーション
        this.transform.DOLocalMove(targetLocalPos, ButtonAnimationDuration)
            .SetEase(Ease.Linear).OnComplete(() => ReturnToOriginPos());

        //ディゾルブなど演出
        AnimateDissolve(ButtonAnimationDuration);

        return this.magicID;
    }

    private bool IsUpwardFlick(Vector3 dragVector)
    {
        // ベクトルを正規化して上方向への割合を確認
        Vector3 normalizedDrag = dragVector.normalized;

        // y成分が一定以上の場合のみ「上方向」と判定
        return normalizedDrag.y > minVerticalRatio;
    }

    public void OnFlickRight() //右方向にフリックされたときのアニメーション。回る
    {
        this.transform.DOLocalRotate(Vector3.up * 360f, rotateTime, RotateMode.LocalAxisAdd);
    }

    public void OnFlickLeft() //左方向にフリックされたときのアニメーション。回る
    {
        this.transform.DOLocalRotate(Vector3.up * 360f, rotateTime, RotateMode.LocalAxisAdd);
    }

    public void ReturnToOriginPos()
    {
        this.transform.DOLocalMove(locabuttonOriginPos, returnOriginPosTime).SetEase(Ease.Linear);
        ReturnAnimateDissolve(returnOriginPosTime);
    }

    //public void ReturnOriginPosInstant()
    //{
    //    this.transform.position = originPos;
    //}

    private void AnimateDissolve(float currentduration)
    {
        // 初期値と目標値を設定（現在のduration → 1の範囲で進行）
        float startValue = currentduration;
        float endValue = -1f;

        DOTween.To(
            () => startValue,             // 開始値の取得
            value => SetDissolveMatOffset(value), // 値を更新する処理
            endValue,                     // 目標値
            currentduration                      // アニメーション時間
        ).SetEase(Ease.InOutSine)
        .OnComplete(() => isActive = false);//非アクティブ状態へ
    }

    private void ReturnAnimateDissolve(float returnDuration)
    {
        // 初期値と目標値を設定（-1 → 1の範囲で進行）
        float returnStartValue = -1;
        float returnEndValue = maxButtonAlpha;

        DOTween.To(
            () => returnStartValue,
            value => SetDissolveMatOffset(value), 
            returnEndValue,
            returnDuration
        ).SetEase(Ease.InOutSine)
        .OnComplete(() => isActive = true);//アクティブ状態へ
    }

    //ディゾルブマテリアルのオフセットの変化
    private void SetDissolveMatOffset(float dissolveValue)
    {
        buttonDissolveMat.SetVector(offsetName, new Vector4(0f, dissolveValue, 0f, 0f));
    }

    public void ActiveButton()
    {
        isActive = true;
        this.gameObject.SetActive(true);
    }
}