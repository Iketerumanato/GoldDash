using UnityEngine;
using DG.Tweening;

public class UiAnimationTest : MonoBehaviour
{
    [SerializeField] RectTransform uiElement; // アニメーションさせたいUIのRectTransform
    [SerializeField] Vector2 offset = new Vector2(57, 50); // 移動量（オフセット）
    [SerializeField] float duration = 1.0f; // アニメーションの時間

    Sequence UIFrameAnimation;

    private void Start()
    {
        AnimateUI();
    }

    public void AnimateUI()
    {
        // 現在の座標を基準に目標座標を計算
        Vector2 startPosition = uiElement.anchoredPosition;
        Vector2 targetPosition = startPosition + offset;

        uiElement.DOAnchorPos(uiElement.anchoredPosition + offset, duration)
          .SetEase(Ease.InOutQuad)
          .SetLoops(-1, LoopType.Yoyo); // 無限に往復

        //uiElement.DOAnchorPos(-targetPosition, duration)
        //         .SetEase(Ease.InOutQuad);
    }
}