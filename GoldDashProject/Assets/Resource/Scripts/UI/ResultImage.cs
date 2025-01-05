using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ResultImage : MonoBehaviour
{
    [SerializeField] private int player1Score;
    [SerializeField] private int player2Score;
    [SerializeField] private int player3Score;
    [SerializeField] private int player4Score;

    [SerializeField] private Image[] segmentImages; // プレイヤー1～4のセグメント画像

    [SerializeField] Animator[] ResultAnimators;

    [SerializeField] private float lerpDuration = 2.0f;
    [SerializeField] private float ChangeAnimSpeed = 0.4f;

    private int[] scores;
    private float[] scoreRatios;

    [SerializeField] private GameObject ResultActorPrefab;
    [SerializeField] float ResultCharaFallHeight = 470f;
    [SerializeField] Vector3 ResultCharaQuaternion;
    [SerializeField] GameObject centerPoint;
    [SerializeField] bool isDirectionCenter;

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
        float elapsedTime = 0f;

        // アニメーション中
        while (elapsedTime < lerpDuration)
        {
            elapsedTime += Time.deltaTime;
            float segAnimationTime = Mathf.Clamp01(elapsedTime / lerpDuration);

            float previousAngle = 0f;

            for (int PieChartNum = 0; PieChartNum < segmentImages.Length; PieChartNum++)
            {
                if (segmentImages[PieChartNum] != null)
                {
                    // アニメーションの現在のfillAmountを取得
                    float currentFillAmount = segmentImages[PieChartNum].fillAmount;

                    // LerpでスムーズにfillAmountを変更
                    segmentImages[PieChartNum].fillAmount = Mathf.Lerp(currentFillAmount, scoreRatios[PieChartNum], segAnimationTime);

                    // 回転を更新
                    segmentImages[PieChartNum].transform.localRotation = Quaternion.Euler(0, 0, -previousAngle);

                    // 次のセグメントの開始角度を計算
                    previousAngle += segmentImages[PieChartNum].fillAmount * 360f;
                }
            }

            yield return null;
        }

        // アニメーションが終わった後にオブジェクトを生成
        GenerateSegmentObjects();
        DisplayHighestScore();
    }

    // アニメーション終了後に各セグメントの中心にオブジェクトを生成するメソッド
    private void GenerateSegmentObjects()
    {
        //float previousAngle = 0f;
        //Vector3 pieChartCenter = transform.position; // 円グラフ全体の中心座標
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

    //４人の中で一番高いスコア(プレイヤー)を導き出し表記
    private void DisplayHighestScore()
    {
        int maxScore = scores[0];
        int maxScorePlayer = 1;

        for (int playerNum = 1; playerNum < scores.Length; playerNum++)
        {
            if (scores[playerNum] > maxScore)
            {
                maxScore = scores[playerNum];
                maxScorePlayer = playerNum + 1;
            }
        }

        Debug.Log($"最高スコアは: Player {maxScorePlayer} の {maxScore} です。");
    }
}