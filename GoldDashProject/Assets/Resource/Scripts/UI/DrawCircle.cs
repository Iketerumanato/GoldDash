using UnityEngine;
using System.Collections.Generic;

public class DrawCircle : MonoBehaviour
{
    [SerializeField] RectTransform drawPanel;
    //[SerializeField] MagicManagement _magicmanagement;
    [SerializeField] PlayerController _playerController;
    [SerializeField] UIFade uiFade;
    //MagicList magicList;
    [SerializeField] MagicButton[] _magicButton;

    readonly private List<Vector2> drawpoints = new();
    private Vector2 center = Vector2.zero;
    private float angleSum = 0;//角度の合計値
    private int circleCount = 0;//描いた円の数
    private float previousSign = 0;//描き途中のsin角の値
    const int MaxDrawCount = 5;
    const float MaxCircleAngle = 360f;
    const int NoneNum = 0;
    const float NoneAngle = 0f;
    const int MaxCirclePoint = 2;

    private void Start()
    {
        //magicList = FindObjectOfType<MagicList>();
        uiFade = this.gameObject.GetComponent<UIFade>();
    }

    void Update()
    {
        #region 円を描く(タブレットver)
        if (Input.touchCount > 0)
        {
            //タブレットでのタッチ操作を行うための宣言
            Touch UiTouch = Input.GetTouch(0);

            switch (UiTouch.phase)
            {
                //タッチ始め
                case TouchPhase.Began:
                    StartDrawCircle();
                    break;

                //タッチ中
                case TouchPhase.Moved:
                    DrawingCircle();
                    break;

                //タッチ終わり
                case TouchPhase.Ended:
                    ResetCircleDraw();
                    break;
            }
        }
        #endregion

        #region 円を描く(クリックver)
        //if (Input.GetMouseButtonDown(0))
        //{
        //    StartDrawCircle();
        //}

        //if (Input.GetMouseButton(0))
        //{
        //    DrawingCircle();
        //}

        //if (Input.GetMouseButtonUp(0))
        //{
        //    ResetCircleDraw();
        //}
        #endregion
    }

    private void StartDrawCircle()
    {
        drawpoints.Clear();
        angleSum = NoneNum;
        previousSign = NoneAngle;
    }

    private void DrawingCircle()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(drawPanel, Input.mousePosition, null, out Vector2 localPoint);
        drawpoints.Add(localPoint);

        // ポイントが2つ以上ある場合のみ処理を行う
        if (drawpoints.Count > 1)
        {
            if (drawpoints.Count > 1) center = GetCenter(drawpoints);

            float angle = CalculateAngle(drawpoints, center);

            //中心点とクリックしたときの点とのなす角を求めて角度の変化方向をチェック
            float sign = Mathf.Sign(Vector2.SignedAngle(drawpoints[drawpoints.Count - 2] - center, drawpoints[drawpoints.Count - 1] - center));
            if (previousSign == NoneAngle)
            {
                previousSign = sign;
            }
            else if (sign != previousSign)
            {
                Debug.Log("角度の方向が逆転しました。カウントをリセットします");
                circleCount = NoneNum;
                angleSum = NoneAngle;
                previousSign = sign;
            }

            angleSum += angle;

            //何周かの円を描けたら(今は5周)
            if (angleSum >= MaxCircleAngle)
            {
                circleCount++;
                angleSum -= MaxCircleAngle;
                Debug.Log("円を一周しました！ 現在のカウント: " + circleCount);
                if (circleCount == MaxDrawCount)
                {
                    Debug.Log("宝箱オープン");
                    uiFade.FadeOutImage();
                    _playerController.isControllCam = true;
                    //magicList.GrantRandomMagics(_magicmanagement);
                    for (int buttonNum = 0; buttonNum < 3; buttonNum++)
                    {
                        _magicButton[buttonNum].ActiveButton();
                    }
                    circleCount = NoneNum;
                }
            }
        }
    }

    private void ResetCircleDraw()
    {
        Debug.Log("マウスを離しました。円のカウントをリセットします。");
        circleCount = 0;
    }

    #region 中心点の取得
    Vector2 GetCenter(List<Vector2> points)
    {
        if (points.Count < MaxCirclePoint)
            return Vector2.zero;

        Vector2 sum = Vector2.zero;
        foreach (var point in points)
        {
            sum += point;
        }
        return sum / points.Count;
    }
    #endregion

    #region 描かれた円の角度計算
    float CalculateAngle(List<Vector2> points, Vector2 center)
    {
        // ポイントが2つ未満の場合、角度を計算しない
        if (points.Count < MaxCirclePoint)
            return 0;

        //前フレームの円の角度
        Vector2 prevVector = points[points.Count - MaxCirclePoint] - center;

        //現在の円の角度
        Vector2 currentVector = points[points.Count - 1] - center;

        //最終的な円の角度
        float angle = Vector2.SignedAngle(prevVector, currentVector);

        return Mathf.Abs(angle);
    }
    #endregion
}