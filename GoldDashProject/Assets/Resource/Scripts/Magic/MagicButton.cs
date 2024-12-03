using UnityEngine;
using DG.Tweening;

public class MagicButton : MonoBehaviour
{
    [SerializeField] private Definer.MID magicID;

    [SerializeField] private float rizeUpTargetPosY; //上にフリックしたとき、この高さまで上昇する
    [SerializeField] private float rizeUpTime; //上にフリックしたとき、この秒数で目的地まで上昇する

    [SerializeField] Transform MagicButtonPosition;
    [SerializeField] Transform ButtonEndPoint;

    [SerializeField] private float rotateTime; //左右にフリックしたとき、この秒数で回転アニメーションをする

    [SerializeField] private float returnOriginPosTime; //初期位置に戻るとき、この秒数で目的地まで移動する

    private Vector3 originPos; //初期位置 

    [SerializeField] float ButtonAnimationDuration = 0.2f;

    private void Start()
    {
        originPos = this.transform.position;
    }

    public float FollowFingerPosY(Vector3 pos) //y座標について追従する
    {
        float Diff_Y = pos.y - this.transform.position.y; //Y座標の差分
        this.transform.position = new Vector3(this.transform.position.x, pos.y, this.transform.position.z);

        return Diff_Y;
    }

    public Definer.MID OnFlickUpper() //上にフリックされたときのアニメーション。発動する魔法のIDを返却する
    {
        //決まった高さまで上昇するアニメーション
        MagicButtonPosition.DOMove(ButtonEndPoint.position, ButtonAnimationDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => Debug.Log("Button reached end position."));

        //ディゾルブなど演出

        Debug.Log("上にフリックされたぞ！");
        return this.magicID;
    }

    public void OnFlickRight() //右方向にフリックされたときのアニメーション。回る
    {
        this.transform.DORotate(Vector3.up * 360f, rotateTime, RotateMode.LocalAxisAdd);
    }

    public void OnFlickLeft() //左方向にフリックされたときのアニメーション。回る
    {
        this.transform.DORotate(Vector3.up * 360f, rotateTime, RotateMode.LocalAxisAdd);
    }

    public void ReturnToOriginPos()
    {
        this.transform.DOMove(originPos, returnOriginPosTime);
    }

    public void ReturnOriginPosInstant()
    {
        this.transform.position = originPos;
    }
}