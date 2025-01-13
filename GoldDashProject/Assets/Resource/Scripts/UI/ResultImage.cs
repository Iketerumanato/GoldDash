using System.Collections;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class ResultImage : MonoBehaviour
{
    [SerializeField] GameServerManager _gameserverManager;
    private List<(string name, int gold, Definer.PLAYER_COLOR color)> pairPlayerDataList;

    //各プレイヤーの色
    //アクターの色別テクスチャ(アクターに渡していく)
    [SerializeField] private Renderer[] targetRenderers; // Scene上のRendererを参照 (順番に配置)
     // プレイヤーの色に対応するテクスチャ
    [SerializeField]　Texture[] ResultActorsBodyColors;

    //各プレイヤーの名前、スコア表示のテキスト
    //画面右のテキスト
    [SerializeField] TMP_Text RightResultText;
    //画面左のテキスト
    [SerializeField] TMP_Text LeftResultText;

    [SerializeField] CanvasGroup ResultTextCanvas;

    [SerializeField] private UnityEngine.UI.Image[] segmentImages; // プレイヤー1～4のセグメント画像
    [SerializeField] Material[] segmentImageMaterials;

    [SerializeField] Animator[] ResultAnimators;

    [SerializeField] private float lerpDuration = 2.0f;
    [SerializeField] private float ChangeAnimSpeed = 0.4f;

    private List<int> scoresList;
    private float[] scoreRatios;
    const string WinerActorTag = "1stActor";

    private void Start()
    {
        pairPlayerDataList = topFourPlayerDataList(); //あらかじめゲーム終了時のプレイヤーのデータの統合
        InitializePlayerData();
        DisplayFinalScore();
        UpdateHighestScoreTag();
        CalculateScoreRatios();
    }

    private void InitializePlayerData()
    {
        scoresList = new List<int>();

        // GetGameResult() からゴールド値を取得
        //var gameResults = _gameserverManager.GetGameResult();

        ApplyTextures(pairPlayerDataList);

        // スコアだけを抽出してリストに追加
        foreach (var result in pairPlayerDataList)
        {
            scoresList.Add(result.gold);
        }
    }

    private void ApplyTextures(List<(string name, int gold, Definer.PLAYER_COLOR color)> playerDataList)
    {
        // プレイヤーデータと対象オブジェクトを順番に対応付け
        for (int i = 0; i < targetRenderers.Length && i < playerDataList.Count; i++)
        {
            var playerData = playerDataList[i];
            var colorIndex = (int)playerData.color;

            // 有効な色インデックスか確認
            if (colorIndex >= 0 && colorIndex < ResultActorsBodyColors.Length)
            {
                // Rendererのマテリアルを取得 (複製作成)
                var materialInstance = targetRenderers[i].material;

                // テクスチャを設定
                materialInstance.mainTexture = ResultActorsBodyColors[colorIndex];
            }
        }

        // プレイヤーのデータをスコア順に並べる
        var sortedPlayerData = pairPlayerDataList
            .OrderByDescending(player => player.gold) // ゴールド順に並べる
            .ToList();

        // segmentImagesをスコア順に対応させる
        for (int i = 0; i < segmentImages.Length; i++)
        {
            // 順位に基づいてsegmentImagesのマテリアルを変更
            if (i < segmentImageMaterials.Length && i < sortedPlayerData.Count)
            {
                var playerData = sortedPlayerData[i];

                // 各playerDataに対応する色を使用してマテリアルを設定
                //segmentImages[i].material = segmentImageMaterials[i];

                // 必要に応じて色を変更
                segmentImages[i].color = GetColorForPlayer(playerData.color); // プレイヤーの色に対応する色を設定
            }
        }
    }

    private void UpdateHighestScoreTag()
    {
        // 最高スコアのプレイヤーデータを取得
        var highestScoringPlayer = pairPlayerDataList.OrderByDescending(player => player.gold).First();

        // 最高スコアのプレイヤーに対応するオブジェクトを見つける
        for (int i = 0; i < pairPlayerDataList.Count && i < targetRenderers.Length; i++)
        {
            var playerData = pairPlayerDataList[i];

            // 最高スコアのプレイヤーに対応するオブジェクトのタグを変更
            if (playerData.name == highestScoringPlayer.name)
            {
                var parentObject = targetRenderers[i].transform.parent;
                parentObject.gameObject.tag = WinerActorTag;
                Debug.Log($"タグを変更しました: {playerData.name} -> {WinerActorTag}");
            }
        }
    }

    private Color GetColorForPlayer(Definer.PLAYER_COLOR playerColor)
    {
        // PLAYER_COLORに基づいて適切な色を返す
        switch (playerColor)
        {
            case Definer.PLAYER_COLOR.RED: return Color.red;
            case Definer.PLAYER_COLOR.BLUE: return Color.blue;
            case Definer.PLAYER_COLOR.GREEN: return Color.green;
            case Definer.PLAYER_COLOR.YELLOW: return Color.yellow;
            default: return Color.white;
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

            // pairPlayerDataListを利用して、segmentImagesの順番がgoldの降順に並ぶようにする
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
    public List<(string playerName,int FinalScore, Definer.PLAYER_COLOR color)> topFourPlayerDataList()
    {
        //var gameResult = _gameserverManager.GetGameResult();
        var gameResult = GetGameResult();//テスト用

        return gameResult
            .OrderByDescending(player => player.gold)
            .Take(4)
            .ToList();
    }

    public void ResultTextCanvasAlphaToMax(float delayInSeconds, float duration)
    {
        if (ResultTextCanvas != null)
        {
            ResultTextCanvas.DOFade(1.0f, duration)
                .SetEase(Ease.InOutQuad)
                .SetDelay(delayInSeconds); // 遅延を設定
        }
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
    List<(string name, int gold, Definer.PLAYER_COLOR color)> GetGameResult()
    {
        return new List<(string name, int gold, Definer.PLAYER_COLOR color)>
        {
            ("Alice", 1000, Definer.PLAYER_COLOR.RED),
            ("Bob", 4000, Definer.PLAYER_COLOR.BLUE),
            ("Charlie", 3000, Definer.PLAYER_COLOR.GREEN),
            ("Diana", 2000, Definer.PLAYER_COLOR.YELLOW),
        };
    }
}