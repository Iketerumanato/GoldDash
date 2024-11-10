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
    private Vector2 originalPosition;
    [SerializeField] float buttonMoveSpeed = 20f;
    [SerializeField] float buttonAnimDuration = 0.2f;

    [SerializeField] float ButtonFlickLength = 500f;

    private bool isDragging = false;
    private float flickThreshold = 50f;
    private bool isFlicked = false;

    private void Awake()
    {
        originalPosition = buttonRectTransform.localPosition;
    }

    void Start()
    {
        magicbutton.onClick.AddListener(() => magicManagement.ActivateMagic(magicIndex));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        originalPosition = eventData.position;
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
                isFlicked = true; // フリックしたことを記録
            }
            else
            {
                // 元の位置に戻す
                buttonRectTransform.DOLocalMove(originalPosition, buttonAnimDuration).SetEase(Ease.OutQuad);
            }

            isDragging = false;
        }
    }

    private void TriggerFlickAnimation()
    {
        buttonRectTransform.DOLocalMoveY(buttonRectTransform.localPosition.y + ButtonFlickLength, 0.5f)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => Debug.Log("Button finished flying up."));
    }

    private void Update()
    {
        if (isDragging && !isFlicked)
        {
            // カーソルのY位置のみ追従
            Vector2 mousePosition = Input.mousePosition;
            buttonRectTransform.localPosition = new Vector2(buttonRectTransform.localPosition.x, mousePosition.y - originalPosition.y);
        }
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