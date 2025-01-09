using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;
using System.Collections.Generic;
using static UnityEngine.Rendering.BoolParameter;

public class MessageDisplayer : MonoBehaviour
{
    [SerializeField] GameObject smallTextPrefab;
    [SerializeField] GameObject largeTextPrefab;
    [SerializeField] Transform SpawnTextPoint;
    [SerializeField] int TestGoldNum;//仮のスコアの値
    [SerializeField] private float historyDisplayTime = 3f;//テキストの表示時間(smallText)
    [SerializeField] private float lageTextDisplayTime = 1f;//テキストの表示時間(largeText)
    [SerializeField] private float verticalSpacing = 45f;//テキストの移動量(文字の大きさによる)
    const int MaxSmallTextCnt = 3;//テキストの最大表示数
    const int MaxLargeTextCnt = 1;
    //表示数チェックのためのリスト
    private List<GameObject> currentSmallTextList = new();
    private List<GameObject> currentLargeTextList = new();

    [Range(0f, 1f)]
    [SerializeField] float largeTextFadeDuration = 0.5f;

    //別のクラスで呼び出し予定
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))  // Pキーが押されたとき
        {
            DisplaySmallMessage($"{TestGoldNum}ゴールドを手に入れた!");
        }

        if(Input.GetKeyDown(KeyCode.L))// Lキーが押された時
        {
            DisplayLargeMessage($"あと 1分 !", lageTextDisplayTime);
        }
    }

    #region SmallMessageの処理
    // 画面右のメッセージの表示
    public void DisplaySmallMessage(string text)
    {
        // 新しいテキストを生成
        GameObject insSmallText = Instantiate(smallTextPrefab, SpawnTextPoint);
        insSmallText.transform.SetAsFirstSibling();
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
        if (currentSmallTextList.Count > MaxSmallTextCnt)
        {
            GameObject oldestText = currentSmallTextList[currentSmallTextList.Count - 1];
            currentSmallTextList.RemoveAt(currentSmallTextList.Count - 1);
            if (oldestText != null)
            {
                Destroy(oldestText.gameObject);
            }
        }

        //３秒後に消滅
        StartCoroutine(RemoveHistoryAfterDelay(insSmallText, historyDisplayTime));
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
    #endregion

    #region LargeMessageの処理

    //画面中央に大きく出るメッセージ
    public void DisplayLargeMessage(string text, float displaytime)
    {
        //生成したUIの各コンポーネントの取得
        GameObject insLargeText = Instantiate(largeTextPrefab, SpawnTextPoint);
        insLargeText.transform.SetAsLastSibling();
        CanvasGroup largeTextGroup =  insLargeText.GetComponent<CanvasGroup>();
        Transform childTransform = insLargeText.transform.GetChild(0);
        TextMeshProUGUI largeText = childTransform.GetComponent<TextMeshProUGUI>();     

        largeText.text = text;//テキストに反映
        largeTextGroup.DOFade(1f, largeTextFadeDuration);//現れる
        StartCoroutine(DeleteLargeMessage(displaytime, largeTextGroup, insLargeText));//何秒後かに自動でフェードして削除

        //表示数が1以上になれば即削除
        currentLargeTextList.Insert(0, insLargeText);
        if (currentLargeTextList.Count > MaxLargeTextCnt)
        {
            GameObject oldestText = currentLargeTextList[currentLargeTextList.Count - 1];
            currentLargeTextList.RemoveAt(currentLargeTextList.Count - 1);
            if (oldestText != null)
            {
                Destroy(oldestText.gameObject);
            }
        }
    }

    //画面中央のメッセージ専用強制消去
    public IEnumerator DeleteLargeMessage(float delay,CanvasGroup largeTextGroup,GameObject inslargeText)
    {
        yield return new WaitForSeconds(delay);
        largeTextGroup.DOFade(0f, largeTextFadeDuration).OnComplete(() => Destroy(inslargeText));
    }
    #endregion
}
