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

    [SerializeField] bool isActive = false;

    [SerializeField] Transform MoveEndPos;
    [SerializeField] Transform originPos;

    //private void Start()
    //{
    //    originPos = this.transform.localPosition;
    //}

    private void OnEnable()
    {
        if (isActive) ActiveButton();
    }

    public float FollowFingerPosY(Vector3 pos) //y座標について追従する
    {
        float Diff_Y = pos.y - this.transform.position.y; //Y座標の差分
        this.transform.position = new Vector3(this.transform.position.x, pos.y, this.transform.position.z);

        return Diff_Y;
    }

    public void FrickUpper(Vector3 dragVector)
    {
        Vector3 EndPosVec = transform.parent.InverseTransformPoint(MoveEndPos.position);

        if (dragVector.sqrMagnitude > FlickThreshold * FlickThreshold && IsUpwardFlick(dragVector))
        {
            OnFlickAnimation(EndPosVec);
        }
        else ReturnToOriginPos();
    }

    public Definer.MID OnFlickAnimation(Vector3 targetLocalPos) //上にフリックされたときのアニメーション。発動する魔法のIDを返却する
    {
        //決まった高さまで上昇するアニメーション
        this.transform.DOLocalMove(targetLocalPos, ButtonAnimationDuration)
            .SetEase(Ease.OutCubic).OnComplete(() => ReturnToOriginPos());

        //ディゾルブなど演出

        Debug.Log("上にフリックされたぞ！");
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
        var localOriginPos = transform.parent.InverseTransformPoint(originPos.position);
        this.transform.DOLocalMove(localOriginPos, returnOriginPosTime).SetEase(Ease.OutQuad);
    }

    //public void ReturnOriginPosInstant()
    //{
    //    this.transform.position = originPos;
    //}

    void ActiveButton()
    {
        this.gameObject.SetActive(true);
        ReturnToOriginPos();
    }
}