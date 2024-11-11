using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class MagicButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] MagicManagement magicManagement;
    [SerializeField] int magicIndex;
    [SerializeField] Button magicbutton;

    [SerializeField] RectTransform buttonRectTransform;
    [SerializeField] Transform endPos;  // EndPosオブジェクトの参照
    private Vector2 originalPosition;
    [SerializeField] float buttonMoveSpeed = 20f;
    [SerializeField] float buttonAnimDuration = 0.2f;

    [SerializeField] float flickThreshold = 50f;
    private bool isDragging = false;
    private bool isFlicked = false;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        originalPosition = buttonRectTransform.localPosition;
        canvasGroup = buttonRectTransform.GetComponent<CanvasGroup>();
    }

    void Start()
    {
        magicbutton.onClick.AddListener(() => magicManagement.ActivateMagic(magicIndex));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        buttonRectTransform.DOKill();
        buttonRectTransform.localPosition = originalPosition;
        canvasGroup.alpha = 1f;  // 透明度をリセット

        isDragging = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isDragging)
        {
            Vector2 releasePointerPosition = eventData.position;
            Vector2 dragVector = releasePointerPosition - originalPosition;

            if (dragVector.y > flickThreshold)
            {
                TriggerFlickAnimation();
                isFlicked = true;
            }
            else
            {
                buttonRectTransform.DOLocalMove(originalPosition, buttonAnimDuration).SetEase(Ease.OutQuad);
            }

            isDragging = false;
        }
    }

    private void TriggerFlickAnimation()
    {
        // EndPos位置まで移動し、透明度を徐々に0にする
        buttonRectTransform.DOMove(endPos.position, 0.5f)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => Debug.Log("Button reached end position."));

        // 透明度を徐々に0にするアニメーション
        canvasGroup.DOFade(0f, 0.5f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isFlicked) return;
        buttonRectTransform.DOLocalMoveY(originalPosition.y + buttonMoveSpeed, buttonAnimDuration).SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isFlicked) return;
        buttonRectTransform.DOLocalMoveY(originalPosition.y, buttonAnimDuration).SetEase(Ease.OutQuad);
    }
}