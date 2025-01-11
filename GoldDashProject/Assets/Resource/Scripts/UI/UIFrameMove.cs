using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using static UnityEditor.Rendering.FilterWindow;

public class UiAnimationTest : MonoBehaviour
{
    [System.Serializable]
    public class UiElement
    {
        public RectTransform rectTransform; // アニメーションさせたいUIのRectTransform
        public Image image; // 画像のコンポーネント
        public Vector2 offset; // このUI要素固有の移動量
    }

    [SerializeField] private bool isStartMoveAnimation = false; // 実行時アニメーションしているか否か
    [SerializeField] private bool isStartColorAnimation = false; // 実行時アニメーションしているか否か

    [SerializeField] private List<UiElement> uiElements = new List<UiElement>(); // 複数のUI要素を管理するリスト
    [SerializeField] private float durationToTarget = 1.0f; // 外へ移動する際のスピード
    [SerializeField] private float durationToStart = 0.5f; // 元の位置に戻るアニメーション時間
    [SerializeField] private float animationDelayTime = 0.3f; // 次の動きの待機時間
    [SerializeField] private Color targetColor; // 変更したい色
    [SerializeField] private float colorChangeDuration = 0.5f; // 色変更にかかる時間

    private List<Sequence> moveSequences = new List<Sequence>(); // 移動アニメーション用シーケンス
    private List<Sequence> colorSequences = new List<Sequence>(); // 色変更アニメーション用シーケンス
    private List<Vector2> startPositions = new List<Vector2>(); // 各UIの開始位置
    private List<Vector2> targetPositions = new List<Vector2>(); // 各UIの目標位置
    private List<Color> originalColors = new List<Color>(); // 各UIの元の色

    private void Awake()
    {
        foreach (var element in uiElements)
        {
            if (element.rectTransform == null || element.image == null)
            {
                Debug.LogWarning("UI要素にRectTransformまたはImageが指定されていません");
                continue;
            }

            // 各UI要素の情報を初期化
            startPositions.Add(element.rectTransform.anchoredPosition);
            targetPositions.Add(element.rectTransform.anchoredPosition + element.offset);
            originalColors.Add(element.image.color);

            // シーケンスを初期化してリストに追加
            moveSequences.Add(DOTween.Sequence());
            colorSequences.Add(DOTween.Sequence());
        }
    }

    private void Start()
    {
        if (isStartMoveAnimation) FrameMoveAnimation();
        else StopAllFrameAnimations();

        //if(isStartColorAnimation) 
    }

    private void FrameMoveAnimation()
    {
        for (int i = 0; i < uiElements.Count; i++)
        {
            var element = uiElements[i];
            if (element.rectTransform == null || element.image == null) continue;

            // 移動のアニメーションシーケンス
            var moveSequence = moveSequences[i];
            moveSequence.Append(element.rectTransform.DOAnchorPos(targetPositions[i], durationToTarget).SetEase(Ease.InOutQuad)) // 外に移動
                        .Append(element.rectTransform.DOAnchorPos(startPositions[i], durationToStart).SetEase(Ease.InQuad)) // 元の位置に戻る
                        .SetDelay(animationDelayTime) // 待機時間
                        .SetLoops(-1); // 無限ループ
        }
    }

    //非アクティブの色に変えていく
    public void StartFrameDisActiveColorAnimation()
    {
        for (int i = 0; i < uiElements.Count; i++)
        {
            var element = uiElements[i];
            var colorSequence = colorSequences[i];
            colorSequence.Append(element.image.DOColor(targetColor, colorChangeDuration)); // 色を変えていく
        }
    }

    public void ReturnOriginFrameColor()
    {
        for (int i = 0; i < uiElements.Count; i++)
        {
            var element = uiElements[i];
            var colorSequence = colorSequences[i];
            colorSequence.Append(element.image.DOColor(originalColors[i], colorChangeDuration));// 元の色に戻す
        }
    }

    public void StartAllFrameAnimations()
    {
        for (int i = 0; i < uiElements.Count; i++)
        {
            if (moveSequences != null && !moveSequences[i].IsPlaying()) moveSequences[i].Play();
        }
    }

    public void StopAllFrameAnimations()
    {
        for (int i = 0; i < uiElements.Count; i++)
        {
            var element = uiElements[i];
            if (element.rectTransform == null || element.image == null) continue;

            // アニメーションを停止
            moveSequences[i].Pause();

            // 位置と色をリセット
            element.rectTransform.anchoredPosition = startPositions[i];
            element.image.color = originalColors[i];
        }
    }
}