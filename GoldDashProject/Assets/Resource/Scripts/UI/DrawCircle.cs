using UnityEngine;
using System.Collections.Generic;

public class DrawCircle : MonoBehaviour
{
    [SerializeField] RectTransform drawPanel;
    [SerializeField] MagicManagement _magicmanagement;
    [SerializeField] CameraControll cameraControll;
    UIFade uiFade;
    MagicList magicList;

    readonly private List<Vector2> drawpoints = new();
    private Vector2 center = Vector2.zero;
    private float angleSum = 0;
    private int circleCount = 0;
    private float previousSign = 0;
    const int MaxDrawCount = 5;
    const int NoneNum = 0;

    private void Start()
    {
        magicList = FindObjectOfType<MagicList>();
        uiFade = this.gameObject.GetComponent<UIFade>();
    }

    void Update()
    {
        #region 円を描く
        if (Input.GetMouseButtonDown(0))
        {
            drawpoints.Clear();
            angleSum = NoneNum;
            previousSign = NoneNum;
        }

        if (Input.GetMouseButton(0))
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
                if (previousSign == NoneNum)
                {
                    previousSign = sign;
                }
                else if (sign != previousSign)
                {
                    Debug.Log("角度の方向が逆転しました。カウントをリセットします");
                    circleCount = NoneNum;
                    angleSum = NoneNum;
                    previousSign = sign;
                }

                angleSum += angle;

                if (angleSum >= 360f)
                {
                    circleCount++;
                    angleSum -= 360f;
                    Debug.Log("円を一周しました！ 現在のカウント: " + circleCount);
                    if (circleCount == MaxDrawCount)
                    {
                        Debug.Log("宝箱オープン");
                        uiFade.FadeOutImage();
                        cameraControll.ActiveCameraControll();
                        magicList.GrantRandomMagics(_magicmanagement);
                        circleCount = 0;
                    }
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            Debug.Log("マウスを離しました。円のカウントをリセットします。");
            circleCount = 0;
        }
        #endregion
    }

    #region 中心点の取得
    Vector2 GetCenter(List<Vector2> points)
    {
        if (points.Count < 2)
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
        if (points.Count < 2)
            return 0;

        Vector2 prevVector = points[points.Count - 2] - center;
        Vector2 currentVector = points[points.Count - 1] - center;

        float angle = Vector2.SignedAngle(prevVector, currentVector);

        return Mathf.Abs(angle);
    }
    #endregion
}