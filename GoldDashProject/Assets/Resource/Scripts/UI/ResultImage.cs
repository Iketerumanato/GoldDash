using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ResultImage : MonoBehaviour
{
    [SerializeField] private int player1Score = 10000;
    [SerializeField] private int player2Score = 300;
    [SerializeField] private int player3Score = 2400;
    [SerializeField] private int player4Score = 34346;

    [SerializeField] private Image[] segmentImages; // プレイヤー1～4のセグメント画像

    [SerializeField] Animator[] ResultAnimators;

    [SerializeField] private float lerpDuration = 2.0f;
    [SerializeField] private float ChangeAnimSpeed = 0.4f;

    private int[] scores;
    private float[] scoreRatios;

    [SerializeField] private GameObject segmentObjectPrefab;

    private void Start()
    {
        scores = new int[] { player1Score, player2Score, player3Score, player4Score };
        CalculateScoreRatios();
    }

    public void ConfigureSegmentImages()
    {
        foreach (var animator in ResultAnimators)
        {
            if (animator != null)
            {
                animator.enabled = false;
            }
        }

        foreach (var image in segmentImages)
        {
            if (image != null)
            {
                // FillOriginをTopに設定
                image.fillOrigin = (int)Image.Origin360.Top;

                // Clockwiseをtrueに設定
                image.fillClockwise = true;
            }
        }
    }

    private void CalculateScoreRatios()
    {
        int totalScore = 0;
        foreach (int score in scores)
            totalScore += score;

        if (totalScore == 0)
        {
            Debug.LogWarning("総スコアが0です。");
            return;
        }

        scoreRatios = new float[scores.Length];
        for (int i = 0; i < scores.Length; i++)
            scoreRatios[i] = (float)scores[i] / totalScore;
    }

    public void UpdatePieChart()
    {
        StartCoroutine(SmoothUpdatePieChart());
    }

    private IEnumerator SmoothUpdatePieChart()
    {
        float previousAngle = 0f;
        float elapsedTime = 0f;

        while (elapsedTime < lerpDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / lerpDuration);

            previousAngle = 0f;

            for (int PieChartNum = 0; PieChartNum < segmentImages.Length; PieChartNum++)
            {
                if (segmentImages[PieChartNum] != null)
                {
                    // アニメーションの現在のfillAmountを取得
                    float currentFillAmount = segmentImages[PieChartNum].fillAmount;

                    // LerpでスムーズにfillAmountを変更
                    segmentImages[PieChartNum].fillAmount = Mathf.Lerp(currentFillAmount, scoreRatios[PieChartNum], t);

                    // 回転を更新
                    segmentImages[PieChartNum].transform.localRotation = Quaternion.Euler(0, 0, -previousAngle);

                    // 次のセグメントの開始角度を計算
                    previousAngle += segmentImages[PieChartNum].fillAmount * 360f;
                }
            }
            yield return null;
        }
        previousAngle = 0f;
        for (int PieChartNum = 0; PieChartNum < segmentImages.Length; PieChartNum++)
        {
            if (segmentImages[PieChartNum] != null)
            {
                // セグメントの半径を動的に取得
                float radius = segmentImages[PieChartNum].rectTransform.rect.width * segmentImages[PieChartNum].transform.lossyScale.x / 2f; // セグメントの横幅の半分を半径とする

                // セグメントの中心座標を計算
                Vector3 segmentCenter = CalculateSegmentCenter(previousAngle, segmentImages[PieChartNum].fillAmount, radius);

                // 各セグメントの中心にオブジェクトを配置
                if (segmentObjectPrefab != null)
                {
                    // オブジェクトをインスタンス化して配置
                    GameObject segmentObject = Instantiate(segmentObjectPrefab, segmentCenter, Quaternion.identity);
                    segmentObject.transform.SetParent(transform); // 親オブジェクトを設定（必要に応じて変更）
                }

                // 次のセグメントの開始角度を計算
                previousAngle += segmentImages[PieChartNum].fillAmount * 360f;

                // 各セグメントの中心をデバッグログで表示
                Debug.Log($"Segment {PieChartNum + 1} Center: {segmentCenter}");
            }
        }
    }

    private Vector3 CalculateSegmentCenter(float startAngle, float fillAmount, float radius)
    {
        // セグメントの中心角を計算
        float centerAngle = startAngle + (fillAmount * 360f) / 2f;

        // ラジアンに変換
        float radians = centerAngle * Mathf.Deg2Rad;

        // セグメント中心の座標を計算
        float centerX = radius * Mathf.Cos(radians);
        float centerY = radius * Mathf.Sin(radians);

        return new Vector3(centerX, centerY, 0); // z座標は0で中心に配置
    }

    public void ChangeAnimatorSpeed()
    {
        foreach (var animator in ResultAnimators)
        {
            if (animator != null)
            {
                animator.speed = ChangeAnimSpeed;
            }
        }
    }
}