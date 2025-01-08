using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;
using System.Collections.Generic;

public class MessageDisplayer : MonoBehaviour
{
    [SerializeField] GameObject smallTextPrefab;
    [SerializeField] Transform SpawnTextPoint;
    [SerializeField] TextMeshProUGUI largeTextPrefab;
    [SerializeField] int TestGoldNum;
    [SerializeField] private float historyDuration = 3f;//テキストの表示時間
    [SerializeField] private float verticalSpacing = 45f;//テキストの移動量(文字の大きさによる)
    const int MaxTextNum = 3;//テキストの最大表示数
    private List<GameObject> currentSmallTextList = new();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))  // Pキーが押されたとき
        {
            DisplaySmallMessage($"{TestGoldNum} Gold Get !!!");
        }
    }

    // 画面右のメッセージの表示
    public void DisplaySmallMessage(string text)
    {
        // 新しいテキストを生成
        GameObject insSmallText = Instantiate(smallTextPrefab, SpawnTextPoint);
        Transform childTransform = insSmallText.transform.GetChild(0);
        Transform childSecondTransform = childTransform.GetChild(0);
        TextMeshProUGUI smallTextUgi = childSecondTransform.GetComponent<TextMeshProUGUI>();
        smallTextUgi.text = text;
        smallTextUgi.DOFade(1.0f, 0.2f);

        // リストに即座に追加し、再配置処理を呼び出す
        currentSmallTextList.Insert(0, insSmallText);

        // 生成された時に前のテキストを下に移動させる処理
        RepositionAndResizeSmallTexts();

        // 最大数を超えた場合、古いテキストを削除
        if (currentSmallTextList.Count > MaxTextNum)
        {
            GameObject oldestText = currentSmallTextList[currentSmallTextList.Count - 1];
            currentSmallTextList.RemoveAt(currentSmallTextList.Count - 1);
            if (oldestText != null)
            {
                Destroy(oldestText.gameObject);
            }
        }

        //３秒後に消滅
        StartCoroutine(RemoveHistoryAfterDelay(insSmallText, historyDuration));
    }

    // テキストを下に移動させる
    private void RepositionAndResizeSmallTexts()
    {
        // 新しく追加されたテキスト以外のすべてのテキストを下に移動
        for (int smallTextNum = 0; smallTextNum < currentSmallTextList.Count; smallTextNum++)
        {
            if (currentSmallTextList[smallTextNum] != null) // オブジェクトが存在するか確認
            {
                // DOTweenで座標を下にスライド
                currentSmallTextList[smallTextNum].transform.DOKill(); // 既存のアニメーションを中断
                currentSmallTextList[smallTextNum].transform.DOLocalMoveY(-smallTextNum * verticalSpacing, 0.3f); // 0.5fで即座に移動
            }
        }
    }

    // 指定した遅延後に履歴を削除するコルーチン
    private IEnumerator RemoveHistoryAfterDelay(GameObject historyObj,float delay)
    {
        yield return new WaitForSeconds(delay);

        // 履歴が存在していれば削除
        if (currentSmallTextList.Contains(historyObj))
        {
            currentSmallTextList.Remove(historyObj);
            if (historyObj != null)
            {
                Destroy(historyObj);
            }

            // 残りの履歴を再配置し、サイズも再調整
            RepositionAndResizeSmallTexts();
        }
    }

    //画面中央に大きく出るメッセージ
    //public void DisplayLargeMessage(string text ,float displaytime)
    //{

    //}

    ////画面中央のメッセージ専用強制消去
    //public void DeleteLargeMessage()
    //{

    //}
}
