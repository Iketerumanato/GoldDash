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

    [SerializeField] float ButtonFlickLenge = 500f;

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

            isDragging = false;
        }
    }

    private void TriggerFlickAnimation()
    {
        buttonRectTransform.DOLocalMoveY(buttonRectTransform.localPosition.y + 500f, 0.5f)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => Debug.Log("Button finished flying up."));
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