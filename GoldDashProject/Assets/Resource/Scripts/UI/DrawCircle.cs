using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class DrawCircle : MonoBehaviour
{
    [SerializeField] PlayerController _playerController;
    [SerializeField] UIFade uiFade;
    [SerializeField] GameObject KeyObj;
    [SerializeField] MagicButton[] _magicButton;
    [SerializeField] Animator keyAnimator;

    private readonly List<Vector2> drawPoints = new();
    private Vector2 center = Vector2.zero;
    private float totalAngle = 0f; // 累計角度
    private float previousAngle = 0f; // 前回の角度
    private int circleCount = 0; // 完了した円の数
    private int currentCircleCount = 0; // 現在描いた円の数の保存
    private bool isClockwise = true; // 時計回りかどうかを記録

    const int MaxButtonCount = 3;
    const int MaxDrawCount = 5;
    const float MaxCircleAngle = 360f;
    const string isActiveKeyAnim = "isOpenTresure";
    const float noneAngle = 0f;
    private const float MinDistanceThreshold = 5f;
    const int CenterRecalculationInterval = 10;

    [SerializeField] TMP_Text[] DebugTexts;

    void Update()
    {
        #region 円を描く(タブレットver)
        if (Input.touchCount > 0)
        {
            ////タブレットでのタッチ操作を行うための宣言
            //Touch UiTouch = Input.GetTouch(0);
            //DebugTexts[2].text = $"TouchPos is {UiTouch.position}";

            //switch (UiTouch.phase)
            //{
            //    //タッチ始め
            //    case TouchPhase.Began:
            //        StartDrawCircle();
            //        break;

            //    //タッチ中
            //    case TouchPhase.Moved:
            //        DrawingCircle(UiTouch.position);
            //        break;

            //    //タッチ終わり
            //    case TouchPhase.Ended:
            //        ResetCircleDraw();
            //        break;
            //}
        }
        #endregion

        #region 円を描く(クリックver)
        if (Input.GetMouseButtonDown(0))
        {
            StartDrawCircle();
        }

        if (Input.GetMouseButton(0))
        {
            DrawingCircle(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            ResetCircleDraw();
        }

        #endregion
    }

    private void StartDrawCircle()
    {
        drawPoints.Clear();
        totalAngle = 0f;
        previousAngle = 0f;
        isClockwise = true; // 初期値をリセット
    }

    private void DrawingCircle(Vector2 inputPosition)
    {
        // ポイント間隔をフィルタリング
        if (drawPoints.Count > 0)
        {
            float distance = Vector2.Distance(drawPoints[drawPoints.Count - 1], inputPosition);
            if (distance < MinDistanceThreshold)
                return;
        }

        // 入力ポイントを記録
        drawPoints.Add(inputPosition);

        if (drawPoints.Count > 1)
        {
            // 一定間隔で中心点を再計算
            if (drawPoints.Count % CenterRecalculationInterval == 0)
            {
                center = GetCenter(drawPoints);
            }

            // 現在の角度を計算
            Vector2 currentVector = drawPoints[drawPoints.Count - 1] - center;
            float currentAngle = Mathf.Repeat(Mathf.Atan2(currentVector.y, currentVector.x) * Mathf.Rad2Deg + 360f, 360f);

            DebugTexts[0].text = $"currentAngle is {currentAngle}";

            // 角度の差分を計算
            if (drawPoints.Count > 2)
            {
                float deltaAngle = Mathf.DeltaAngle(previousAngle, currentAngle);

                // 回転方向が切り替わったかチェック
                if ((isClockwise && deltaAngle < noneAngle) || (!isClockwise && deltaAngle > noneAngle))
                {
                    totalAngle = 0f; // 累計角度をリセット
                    isClockwise = deltaAngle > 0; // 回転方向を更新
                }

                totalAngle += deltaAngle;

                // 円を描いたか確認
                if (Mathf.Abs(totalAngle) >= MaxCircleAngle)
                {
                    circleCount++;
                    currentCircleCount = circleCount; // 円の数を保存
                    Debug.Log($"現在{currentCircleCount}周完了中（{(isClockwise ? "時計回り" : "反時計回り")}）");
                    DebugTexts[1].text = $"currentCircleCount is {currentCircleCount}";

                    totalAngle %= MaxCircleAngle; // 累計角度を更新

                    // アニメーション同期
                    if (currentCircleCount <= MaxDrawCount)
                    {
                        string paramName = $"{isActiveKeyAnim}{currentCircleCount}";
                        keyAnimator.SetBool(paramName, true);
                    }
                    if (currentCircleCount == MaxDrawCount)
                    {
                        for (int buttonCnt = 0; buttonCnt < MaxButtonCount; buttonCnt++) _magicButton[buttonCnt].ActiveButton();
                        uiFade.FadeInCanvasGroup();
                    }
                }
            }

            previousAngle = currentAngle; // 前回の角度を更新
        }
    }

    private void ResetCircleDraw()
    {
        totalAngle = 0f;
    }

    public void ActiveKey()
    {
        KeyObj.SetActive(true);
    }

    public void NotActiveKey()
    {
        KeyObj.SetActive(false);
    }

    public void FinishAnimation()
    {
        Debug.Log("アニメーション終了！");
        keyAnimator.SetBool($"{isActiveKeyAnim}{6}", true);
    }

    private Vector2 GetCenter(List<Vector2> points)
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
}