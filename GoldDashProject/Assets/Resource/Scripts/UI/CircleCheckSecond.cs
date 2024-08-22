using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleCheckSecond : MonoBehaviour
{
	private float angle;
    private float angleSum;
    private Vector3 swipePosition;
    private Vector3 previousSwipePosition;
    private Vector3 vector;
    private Vector3 previousVector;
    private int sign;
    private int previousSign;

    //スワイプ地点の角度の総和が、この値を超した場合は円を描いたと見なす。
    static readonly float AngleSumThreshold = 330.0f;

    //前回からのタッチ地点の差が、この距離未満だと無視する(sqrMagnitudeなので、2乗。ワールド座標ではなく、スクリーン座標なので大きめで)。
    static readonly float SwipeDeltaSqrThreshold = 15.0f * 15.0f;

    Coroutine checkCircleSwipeGesture;
    Coroutine checkCircleSwipeGestureMouseButtonUp;

    //スワイプジェスチャーのチェック間隔。
    static readonly WaitForSeconds CheckCircleSwipeGestureWait = new(0.05f);

    public LineRenderer lineRenderer;
    private List<Vector3> circlePoints = new List<Vector3>();
    private int pointsCount = 0;

    private void Start()
    {
        lineRenderer.positionCount = 0;
    }

    private void Update()
    {
        StartCoroutine(CheckCircleSwipeGesture());
    }

    // ゲーム冒頭等、ジェスチャーチェック受付開始時に、1回だけコレを呼ぶ。
    public void StartCheckCircleSwipeGesture()
    {
        // 一応、重複チェックしている。
        if (checkCircleSwipeGesture != null)
        {
            StopCoroutine(checkCircleSwipeGesture);
        }

        checkCircleSwipeGesture = StartCoroutine(CheckCircleSwipeGesture());

        if (checkCircleSwipeGestureMouseButtonUp != null)
        {
            StopCoroutine(checkCircleSwipeGestureMouseButtonUp);
        }

        checkCircleSwipeGestureMouseButtonUp = StartCoroutine(CheckCircleSwipeGestureMouseButtonUp());
    }

    // 停止時には、コレを呼ぶ。
    public void StopCheckCircleSwipeGesture()
    {
        if (checkCircleSwipeGesture != null)
        {
            StopCoroutine(checkCircleSwipeGesture);
            checkCircleSwipeGesture = null;
        }

        if (checkCircleSwipeGestureMouseButtonUp != null)
        {
            StopCoroutine(checkCircleSwipeGestureMouseButtonUp);
            checkCircleSwipeGestureMouseButtonUp = null;
        }
    }

    // 計算に使用する値の初期化。
    void ResetCheckCircleSwipeGesture()
    {
        previousSwipePosition = Input.mousePosition;
        angleSum = 0;

        vector = Vector3.zero;
        previousVector = Vector3.zero;

        sign = 0;
        previousSign = 0;

        lineRenderer.positionCount = 0;
        circlePoints.Clear();
        pointsCount = 0;
    }

    IEnumerator CheckCircleSwipeGesture()
    {
        ResetCheckCircleSwipeGesture();

        yield return CheckCircleSwipeGestureWait;

        while (true)
        {
            if (Input.GetMouseButton(0))
            {
                swipePosition = Input.mousePosition;

                if ((swipePosition - previousSwipePosition).sqrMagnitude < SwipeDeltaSqrThreshold)
                {
                    yield return CheckCircleSwipeGestureWait;
                    continue;
                }

                angle = Vector3.Angle(previousVector, vector);

                if (vector != Vector3.zero)
                    previousVector = vector;

                vector = swipePosition - previousSwipePosition;

                previousSwipePosition = swipePosition;

                if (previousVector != Vector3.zero && previousSign == 0)
                    previousSign = Vector3.Cross(previousVector, vector).z < 0 ? 1 : -1;

                if (previousVector != Vector3.zero)
                {
                    sign = Vector3.Cross(previousVector, vector).z < 0 ? 1 : -1;

                    // 逆回転になっているので、角度の総和をリセット。
                    if (previousSign != sign)
                    {
                        previousSign = sign;
                        angleSum = 0;
                    }
                    else
                    {
                        angleSum += angle;
                    }
                }
                else
                {
                    angleSum += angle;
                }

                // LineRendererにポイントを追加
                Vector3 worldPosition = Camera.main.ScreenToWorldPoint(swipePosition);
                worldPosition.z = 0;
                circlePoints.Add(worldPosition);
                lineRenderer.positionCount = circlePoints.Count;
                lineRenderer.SetPosition(pointsCount, worldPosition);
                pointsCount++;

                if (AngleSumThreshold <= angleSum)
                {
                    Debug.Log("円を描いたよ");

                    // 円を描いたと判定された時の処理をココに記述。
                    // previousSignの値を見ると、時計回りか反時計回りか分かる(-1 == 反時計回り、1 == 時計回り)。
                    ResetCheckCircleSwipeGesture();
                }
            }

            yield return CheckCircleSwipeGestureWait;
        }
    }

    // スワイプが中断された判定は、毎フレームしないといけないので、別コルーチンに分ける。
    IEnumerator CheckCircleSwipeGestureMouseButtonUp()
    {
        while (true)
        {
            if (Input.GetMouseButtonUp(0))
            {
                ResetCheckCircleSwipeGesture();
            }

            yield return null;
        }
    }
}