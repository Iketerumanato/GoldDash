using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;


public class ResultImage : MonoBehaviour
{
    [SerializeField] GameServerManager _gameserverManager;
    private List<(string name, int gold, Definer.PLAYER_COLOR color)> pairPlayerDataList;
    //各プレイヤーの色
    //色を変える最終結果発表用のアクターの体
    [SerializeField]
    Renderer[] ResultActorsBodyRenderer;
    //アクターの色別テクスチャ
    [SerializeField]
    Texture[] ResultActorsBodyColors;

    const string ActorBodyMap = "_BaseMap";

    //各プレイヤーの名前、スコア表示のテキスト
    //画面右の４つ
    [SerializeField] TMP_Text RightResultText;
    //画面左の４つ
    [SerializeField] TMP_Text LeftResultText;

    [SerializeField] private UnityEngine.UI.Image[] segmentImages; // プレイヤー1～4のセグメント画像

    [SerializeField] Animator[] ResultAnimators;

    [SerializeField] private float lerpDuration = 2.0f;
    [SerializeField] private float ChangeAnimSpeed = 0.4f;

    private List<int> scoresList;
    private float[] scoreRatios;

    private void Start()
    {
        InitializeScores();
        ApplyPlayerColors();
        pairPlayerDataList = topFourPlayerDataList(); //あらかじめゲーム終了時のプレイヤーのデータの統合
        DisplayFinalScore();
        CalculateScoreRatios();
    }

    private void InitializeScores()
    {
        scoresList = new List<int>();

        // GetGameResult() からゴールド値を取得
        //var gameResults = _gameserverManager.GetGameResult();
        var gameResults = GetGameResult();

        // スコアだけを抽出してリストに追加
        foreach (var result in gameResults)
        {
            scoresList.Add(result.gold);
        }
    }

    void ApplyPlayerColors()
    {
        // プレイヤーごとの色を設定
        for (int i = 0; i < pairPlayerDataList.Count && i < ResultActorsBodyRenderer.Length; i++)
        {
            var playerData = pairPlayerDataList[i];
            var renderer = ResultActorsBodyRenderer[i];

            // Definer.PLAYER_COLOR に基づいて対応するテクスチャを取得
            int colorIndex = (int)playerData.color;
            if (colorIndex < 0 || colorIndex >= ResultActorsBodyColors.Length)
            {
                Debug.LogWarning($"無効な色インデックス: {colorIndex}");
                continue;
            }

            var texture = ResultActorsBodyColors[colorIndex];

            if (renderer != null && texture != null)
            {
                // Renderer のマテリアルの BaseMap を更新
                renderer.material.SetTexture(ActorBodyMap, texture);
            }
        }
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
                image.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;

                // Clockwiseをtrueに設定
                image.fillClockwise = true;
            }
        }
    }

    private void CalculateScoreRatios()
    {
        int totalScore = 0;
        foreach (int score in scoresList)
            totalScore += score;

        if (totalScore == 0)
        {
            Debug.LogWarning("総スコアが0です。");
            return;
        }

        scoreRatios = scoresList.Select(score => (float)score / totalScore).ToArray();
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

    //最高スコアを基準にプレイヤーのデータを順位の順番に整理
    List<(string playerName,int FinalScore, Definer.PLAYER_COLOR color)> topFourPlayerDataList()
    {
        //var gameResult = _gameserverManager.GetGameResult();
        var gameResult = GetGameResult();//テスト用

        return gameResult
            .OrderByDescending(player => player.gold)
            .Take(4)
            .ToList();
    }   

    void DisplayFinalScore()
    {
        RightResultText.text = "";
        LeftResultText.text = "";

        for (int i = 0; i <pairPlayerDataList.Count; i++)
        {
            var player = pairPlayerDataList[i];

            // 順位、名前、スコアを1行ずつ表示
            RightResultText.text += $"{i + 1}: {player.name} - Score: {player.gold}\n";
            LeftResultText.text += $"{i + 1}: {player.name} - : {player.gold}\n";
        }
    }

    //テスト用のList
    private List<(string name, int gold, Definer.PLAYER_COLOR color)> GetGameResult()
    {
        return new List<(string name, int gold, Definer.PLAYER_COLOR color)>
        {
            ("Alice", 1500, Definer.PLAYER_COLOR.RED),
            ("Bob", 1200, Definer.PLAYER_COLOR.BLUE),
            ("Charlie", 900, Definer.PLAYER_COLOR.GREEN),
            ("Diana", 800, Definer.PLAYER_COLOR.YELLOW),
        };
    }
}