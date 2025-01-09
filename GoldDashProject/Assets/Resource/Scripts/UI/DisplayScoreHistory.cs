using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;
using System.Collections.Generic;

public class DisplayScoreHistory : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI historyPrefab;  // 履歴を表示するテキストのプレハブ
    [SerializeField] private Transform historyContainer;     // 履歴の表示場所
    [SerializeField] private int maxHistoryCount = 3;        // 表示する履歴の最大数
    [SerializeField] private float historyDuration = 3f;     // 各履歴を表示する時間
    [SerializeField] private float verticalSpacing = 30f;    // 履歴間の垂直スペース(文字の大きさを考慮)
    [SerializeField] private float scaleDecreaseFactor = 0.9f; // テキストの縮小率

    private List<TextMeshProUGUI> historyList = new List<TextMeshProUGUI>();

    // 履歴を追加するメソッド
    public void AddScoreHistory(int changeAmount)
    {
        // スコア変動のテキストを設定
        string text = changeAmount > 0 ? $"+{changeAmount}" : changeAmount.ToString();

        // 新しい履歴テキストを生成
        TextMeshProUGUI newHistory = Instantiate(historyPrefab, historyContainer);
        newHistory.text = text;
        newHistory.transform.SetAsFirstSibling(); // 最新の履歴が上に表示されるようにする

        // リストに新しい履歴を追加し、アニメーション処理を実行
        historyList.Insert(0, newHistory);
        RepositionAndResizeHistoryTexts();

        // 最大履歴数を超えたら一番古い履歴を削除
        if (historyList.Count > maxHistoryCount)
        {
            TextMeshProUGUI oldestHistory = historyList[historyList.Count - 1];
            historyList.RemoveAt(historyList.Count - 1);
            if (oldestHistory != null)
            {
                Destroy(oldestHistory.gameObject);
            }
        }

        // 指定時間後に削除するコルーチンを開始
        StartCoroutine(RemoveHistoryAfterDelay(newHistory, historyDuration));
    }

    // 全ての履歴テキストを再配置し、サイズをアニメーションで変更するメソッド
    private void RepositionAndResizeHistoryTexts()
    {
        for (int historyNum = 0; historyNum < historyList.Count; historyNum++)
        {
            if (historyList[historyNum] != null) // オブジェクトが存在するか確認
            {
                // DOTweenで座標を下にスライド
                historyList[historyNum].rectTransform.DOKill(); // 既存のアニメーションを中断
                historyList[historyNum].rectTransform.DOAnchorPosY(-historyNum * verticalSpacing, 0.3f);

                // DOTweenでサイズを変更(0.3ずつ小さくなっていく)
                float historyscale = Mathf.Pow(scaleDecreaseFactor, historyNum); // インデックスに応じて小さくする
                historyList[historyNum].rectTransform.DOScale(historyscale, 0.3f);
            }
        }
    }

    // 指定した遅延後に履歴を削除するコルーチン
    private IEnumerator RemoveHistoryAfterDelay(TextMeshProUGUI historyText, float delay)
    {
        yield return new WaitForSeconds(delay);

        // 履歴が存在していれば削除
        if (historyList.Contains(historyText))
        {
            historyList.Remove(historyText);
            if (historyText != null)
            {
                Destroy(historyText.gameObject);
            }

            // 残りの履歴を再配置し、サイズも再調整
            RepositionAndResizeHistoryTexts();
        }
    }
}