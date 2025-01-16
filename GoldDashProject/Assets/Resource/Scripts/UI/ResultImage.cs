using System.Collections;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using System;

public class ResultImage : MonoBehaviour
{
    [SerializeField] GameServerManager _gameserverManager;
    private List<(string name, int gold, Definer.PLAYER_COLOR color)> pairPlayerDataList;//サーバーとのペアリング用のリスト

    [Header("緑のアクターの体を白に変える用")]
    [SerializeField] Material WhitePlayerMaterial;//白のテクスチャ
    [SerializeField] SkinnedMeshRenderer greenSkin;//緑のアクターの体

    //各プレイヤーの名前、スコア表示のテキスト
    //画面右のテキスト
    [Header("")]
    [SerializeField] TMP_Text RightRankingText;
    [SerializeField] TMP_Text RightNameText;
    [SerializeField] TMP_Text RightScoreText;

    //画面左のテキスト
    [Header("")]
    [SerializeField] TMP_Text LeftRankingText;
    [SerializeField] TMP_Text LeftNameText;
    [SerializeField] TMP_Text LeftScoreText;

    [Header("プレイヤーのアイコン")]
    [SerializeField] List<UnityEngine.UI.Image> RightPlayerIconImage;//右のアイコン画像4つ
    [SerializeField] List<UnityEngine.UI.Image> LeftPlayerIconImage;//左のアイコン画像4つ
    [SerializeField] Sprite redIcon;
    [SerializeField] Sprite blueIcon;
    [SerializeField] Sprite greenIcon;
    [SerializeField] Sprite yellowIcon;
    [SerializeField] Sprite whiteIcon;


    //スコアキャンバスのフェード
    [SerializeField] CanvasGroup ResultTextCanvas;

    [Header("")]
    [SerializeField] private UnityEngine.UI.Image[] segmentImages; // プレイヤー1～4のセグメント画像
    [SerializeField] Material[] segmentImageMaterials;//セグメントのマテリアル

    [SerializeField] Animator[] ResultAnimators;//結果発表用のアクターのアニメーター

    [SerializeField] private float lerpDuration = 2.0f;//円グラフアニメーションのデュレーション量
    [SerializeField] private float ChangeAnimSpeed = 0.4f;//変化させるアニメーションのスピード量

    [SerializeField] private List<ColorAndObjPair> colorAndObjList;
    [Serializable]
    private class ColorAndObjPair
    {
        public Definer.PLAYER_COLOR color;
        public GameObject obj;
    }

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
        var gameResults = _gameserverManager.GetGameResult();
        //ApplyTextures(gameResults);

        ApplyTextures(pairPlayerDataList);

        // スコアだけを抽出してリストに追加
        foreach (var result in pairPlayerDataList)
        {
            scoresList.Add(result.gold);
        }
    }

    private void ApplyTextures(List<(string name, int gold, Definer.PLAYER_COLOR color)> playerDataList)
    {
        //    // プレイヤーデータと対象オブジェクトを順番に対応付け
        //    for (int i = 0; i < targetRenderers.Length && i < playerDataList.Count; i++)
        //    {
        //        var playerData = playerDataList[i];
        //        var colorIndex = (int)playerData.color;

        //        // 有効な色インデックスか確認
        //        if (colorIndex >= 0 && colorIndex < ResultActorsBodyColors.Length)
        //        {
        //            // Rendererのマテリアルを取得 (複製作成)
        //            var materialInstance = targetRenderers[i].material;

        //           // テクスチャを設定
        //           materialInstance.mainTexture = ResultActorsBodyColors[colorIndex];
        //           if (playerData.color == Definer.PLAYER_COLOR.GREEN && _gameserverManager.currentColorType == GameServerManager.COLOR_TYPE.CHANGE_GREEN_TO_WHITE)
        //           {
        //                materialInstance.mainTexture = WhitePlayerTexture;
        //           }//ここで緑を白に変更
        //        }
        //    }

        // プレイヤーのデータをスコア順に並べる
        var sortedPlayerData = playerDataList;

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
                if (playerData.color == Definer.PLAYER_COLOR.GREEN && _gameserverManager.currentColorType == GameServerManager.COLOR_TYPE.CHANGE_GREEN_TO_WHITE)
                { segmentImages[i].color = Color.white; }//ここで緑を白に変更

            }
        }
    }

    private void UpdateHighestScoreTag()
    {
        // 最高スコアのプレイヤーデータを取得
        var highestScoringPlayer = pairPlayerDataList.OrderByDescending(player => player.gold).First();


        foreach (ColorAndObjPair c in colorAndObjList)
        {
            if (c.color == highestScoringPlayer.color)
            {
                c.obj.tag = WinerActorTag;
                Debug.Log($"タグを変更しました: -> {WinerActorTag}");
            }
        }

        //// 最高スコアのプレイヤーに対応するオブジェクトを見つける
        //for (int i = 0; i < pairPlayerDataList.Count && i < targetRenderers.Length; i++)
        //{
        //    var playerData = pairPlayerDataList[i];

        //    // 最高スコアのプレイヤーに対応するオブジェクトのタグを変更
        //    if (playerData.name == highestScoringPlayer.name)
        //    {
        //        var parentObject = targetRenderers[i].transform.parent;
        //        parentObject.gameObject.tag = WinerActorTag;
        //        Debug.Log($"タグを変更しました: {playerData.name} -> {WinerActorTag}");
        //    }
        //}
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

    //各円グラフの初期化
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

    //４人のスコアの合計値から円グラフのFillAmountを操作
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

    //演出上の都合でアニメーションのスピードを変更する
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

    //最高スコア(gold)を基準にプレイヤーのデータを順位の順番に整理
    public List<(string playerName, int FinalScore, Definer.PLAYER_COLOR color)> topFourPlayerDataList()
    {
        var gameResult = _gameserverManager.GetGameResult();
        //var gameResult = GetGameResult();//テスト用

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


    //最終歴なプレイヤーの名前やスコアなどの表示
    void DisplayFinalScore()
    {
        //テキストの初期化
        RightRankingText.text = "";
        RightNameText.text = "";
        RightScoreText.text = "";
        LeftRankingText.text = "";
        LeftNameText.text = "";
        LeftScoreText.text = "";

        for (int playerRank = 0; playerRank < pairPlayerDataList.Count; playerRank++)
        {
            var playerdata = pairPlayerDataList[playerRank];
            //var actorData = colorAndObjList[playerRank];

            // 順位、名前、スコアを1行ずつ表示
            RightRankingText.text += $"{playerRank + 1}:\n";
            if (playerRank == 0) RightRankingText.text += "<size=72>";
            if (playerRank == pairPlayerDataList.Count) RightRankingText.text += "</size>";

            LeftRankingText.text += $"{playerRank + 1}:\n";
            if (playerRank == 0) LeftRankingText.text += "<size=72>";
            if (playerRank == pairPlayerDataList.Count) LeftRankingText.text += "</size>";

            RightNameText.text += $"{playerdata.name}\n";
            if (playerRank == 0) RightNameText.text += "<size=48>";
            if (playerRank == pairPlayerDataList.Count) RightNameText.text += "</size>";

            LeftNameText.text += $"{playerdata.name}\n";
            if (playerRank == 0) LeftNameText.text += "<size=48>";
            if (playerRank == pairPlayerDataList.Count) LeftNameText.text += "</size>";

            RightScoreText.text += $"{playerdata.gold}\n";
            if (playerRank == 0) RightScoreText.text += "<size=48>";
            if (playerRank == pairPlayerDataList.Count) RightScoreText.text += "</size>";

            LeftScoreText.text += $"{playerdata.gold}\n";
            if (playerRank == 0) LeftScoreText.text += "<size=48>";
            if (playerRank == pairPlayerDataList.Count) LeftScoreText.text += "</size>";

            //下で指定した情報を使ってImageのListのSpriteを変えていく
            RightPlayerIconImage[playerRank].sprite = GetSpriteFromColor(pairPlayerDataList[playerRank].color);
            LeftPlayerIconImage[playerRank].sprite = GetSpriteFromColor(pairPlayerDataList[playerRank].color);
            //各プレイヤーの色情報からスプライトを指定
            Sprite GetSpriteFromColor(Definer.PLAYER_COLOR playerColor)
            {
                switch (playerColor)
                {
                    case Definer.PLAYER_COLOR.RED:
                        return redIcon;
                    case Definer.PLAYER_COLOR.BLUE:
                        return blueIcon;
                    case Definer.PLAYER_COLOR.GREEN:
                        if (_gameserverManager.currentColorType == GameServerManager.COLOR_TYPE.CHANGE_GREEN_TO_WHITE) return whiteIcon;
                        else return greenIcon;
                    case Definer.PLAYER_COLOR.YELLOW:
                        return yellowIcon;
                    default:
                        break;
                }
                throw new Exception("死");
            }
        }
    }

    //アニメーションイベントで呼ぶものたち
    public void CallPlayBoomSE()
    {
        SEPlayer.instance.PlayResultBoomSE();
    }

    public void CallResultDrumrollBGM()
    {
        SEPlayer.instance.resultdrumrollBGMPlayer.Play();
    }

    public void StopResultDrumrollBGM()
    {
        SEPlayer.instance.resultdrumrollBGMPlayer.Stop();
        Debug.Log("ドラムロールBGM終了");
    }

    public void CallResultBGM()
    {
        SEPlayer.instance.resultBGMPlayer.Play();
    }

    //テスト用のList
    //List<(string name, int gold, Definer.PLAYER_COLOR color)> GetGameResult()
    //{
    //    return new List<(string name, int gold, Definer.PLAYER_COLOR color)>
    //    {
    //        ("Alice", 1000, Definer.PLAYER_COLOR.RED),
    //        ("Bob", 2000, Definer.PLAYER_COLOR.BLUE),
    //        ("Charlie", 3000, Definer.PLAYER_COLOR.GREEN),
    //        ("Diana", 4000, Definer.PLAYER_COLOR.YELLOW),
    //    };
    //}

    private void OnEnable()
    {
        if (_gameserverManager.currentColorType == GameServerManager.COLOR_TYPE.CHANGE_GREEN_TO_WHITE)
        {
            greenSkin.material = WhitePlayerMaterial;
        }
    }
}