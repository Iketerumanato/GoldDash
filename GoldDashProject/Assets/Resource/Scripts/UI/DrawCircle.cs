using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class DrawCircle : MonoBehaviour
{
    //[SerializeField] RectTransform drawPanel;
    //[SerializeField] MagicManagement _magicmanagement;
    [SerializeField] PlayerController _playerController;
    [SerializeField] UIFade uiFade;
    //MagicList magicList;
    [SerializeField] GameObject KeyObj;
    [SerializeField] MagicButton[] _magicButton;
    [SerializeField] Animator keyAnimator;

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
    const string isActiveKeyAnim = "isOpenTresure";

    //private void Start()
    //{
    //    magicList = FindObjectOfType<MagicList>();
    //    uiFade = this.gameObject.GetComponent<UIFade>();
    //}

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
        if (Input.GetMouseButtonDown(0))
        {
            StartDrawCircle();
        }

        if (Input.GetMouseButton(0))
        {
            DrawingCircle();
        }

        if (Input.GetMouseButtonUp(0))
        {
            ResetCircleDraw();
        }
        #endregion
    }

    private void StartDrawCircle()
    {
        drawpoints.Clear();
        angleSum = 0;
        previousSign = 0;
    }

    private void DrawingCircle()
    {
        Vector2 localPoint = Input.mousePosition;
        //Vector2 localPoint = UiTouch.position;
        //RectTransformUtility.ScreenPointToLocalPointInRectangle(drawPanel, Input.mousePosition, null, out Vector2 localPoint);
        drawpoints.Add(localPoint);

        if (drawpoints.Count > 1)
        {
            if (drawpoints.Count > 1) center = GetCenter(drawpoints);
            float angle = CalculateAngle(drawpoints, center);
            float sign = Mathf.Sign(Vector2.SignedAngle(drawpoints[drawpoints.Count - 2] - center, drawpoints[drawpoints.Count - 1] - center));

            if (previousSign == 0f)
            {
                previousSign = sign;
            }
            else if (sign != previousSign)
            {
                circleCount = 0;
                angleSum = 0;
                previousSign = sign;
            }

            angleSum += angle;

            if (angleSum >= MaxCircleAngle)
            {
                circleCount++;
                angleSum -= MaxCircleAngle;

                // アニメーション同期
                if (circleCount <= MaxDrawCount)
                {
                    string paramName = $"{isActiveKeyAnim}{circleCount}";
                    keyAnimator.SetBool(paramName, true);
                    Debug.Log($"パラメータ {paramName} を true にしました");
                }
                if (circleCount == MaxDrawCount)
                {
                    uiFade.FadeInCanvasGroup();
                }
            }
        }
    }

    private void ResetCircleDraw()
    {
        circleCount = 0;
        // アニメーションを逆再生
        //StartCoroutine(ReverseAnimation());
    }

    private IEnumerator ReverseAnimation()
    {
        // 現在再生中のアニメーションの状態を取得
        AnimatorStateInfo stateInfo = keyAnimator.GetCurrentAnimatorStateInfo(0);

        // アニメーションが再生中か確認
        if (stateInfo.length > 0)
        {
            // アニメーションの速度を負にして逆再生開始
            keyAnimator.speed = -2.0f; // 負の値を大きくするほど速く逆再生

            // アニメーションの進行度が0に戻るまで待機
            while (stateInfo.normalizedTime > 0)
            {
                stateInfo = keyAnimator.GetCurrentAnimatorStateInfo(0);
                yield return null;
            }

            // アニメーションを停止し速度をリセット
            keyAnimator.speed = 1.0f;
            keyAnimator.Play(isActiveKeyAnim, -1, 0); // 0フレームに戻す
        }
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

    float CalculateAngle(List<Vector2> points, Vector2 center)
    {
        if (points.Count < 2)
            return 0;

        Vector2 prevVector = points[points.Count - 2] - center;
        Vector2 currentVector = points[points.Count - 1] - center;
        float angle = Vector2.SignedAngle(prevVector, currentVector);

        return Mathf.Abs(angle);
    }
}