using UnityEngine;
using DG.Tweening;

public class UiAnimationTest : MonoBehaviour
{
    [SerializeField] RectTransform uiElement; // アニメーションさせたいUIのRectTransform
    [SerializeField] Vector2 offset = new Vector2(57, 50); // 移動量（オフセット）
    [SerializeField] float durationToTarget = 1.0f;
    [SerializeField] float durationToStart = 0.5f; // 元の位置に戻るアニメーション時間

    private void Start()
    {
        AnimateUI();
    }

    public void AnimateUI()
    {
        if (uiElement == null)
        {
            Debug.LogWarning("UI要素が指定されていません");
            return;
        }

        // 現在の位置を基準に目標座標を計算
        Vector2 startPosition = uiElement.anchoredPosition;
        Vector2 targetPosition = startPosition + offset;

        // アニメーションチェーン
        Sequence sequence = DOTween.Sequence();
        sequence.Append(uiElement.DOAnchorPos(targetPosition, durationToTarget).SetEase(Ease.InOutQuad));
        sequence.Append(uiElement.DOAnchorPos(startPosition, durationToStart).SetEase(Ease.InQuad));
        sequence.SetLoops(-1); // 無限ループ
    }
}