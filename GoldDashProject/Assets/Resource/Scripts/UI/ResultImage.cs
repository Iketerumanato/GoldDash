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

    private int[] scores;
    private float[] scoreRatios;

    private void Start()
    {
        // スコアを配列にまとめる
        scores = new int[] { player1Score, player2Score, player3Score, player4Score };
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


    public void UpdatePieChart()
    {
        // 総スコアを計算
        int totalScore = 0;
        foreach (int score in scores)
            totalScore += score;

        if (totalScore == 0)
        {
            Debug.LogWarning("総スコアが0です。");
            return;
        }

        // 比率を計算
        scoreRatios = new float[scores.Length];
        for (int i = 0; i < scores.Length; i++)
            scoreRatios[i] = (float)scores[i] / totalScore;

        // 円グラフを構成
        float previousAngle = 0f; // 累積角度
        for (int i = 0; i < segmentImages.Length; i++)
        {
            if (segmentImages[i] != null)
            {
                // 比率に基づくFillAmountを設定
                segmentImages[i].fillAmount = scoreRatios[i];

                // 前のセグメントの終了角度を基準に回転を設定
                segmentImages[i].transform.localRotation = Quaternion.Euler(0, 0, -previousAngle);

                // 累積角度を更新
                previousAngle += scoreRatios[i] * 360f;
            }
        }
    }
}