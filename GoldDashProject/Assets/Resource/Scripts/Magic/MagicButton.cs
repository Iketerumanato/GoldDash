using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class MagicButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{ 
    [SerializeField] MagicManagement magicManagement;
    [SerializeField] int magicIndex;
    [SerializeField] Button magicbutton;

    [SerializeField] RectTransform buttonRectTransform;
    private Vector3 originalPosition;
    [SerializeField] float buttonMoveduration = 20f;

    void Start()
    {
        originalPosition = buttonRectTransform.localPosition;
        magicbutton.onClick.AddListener(() => magicManagement.ActivateMagic(magicIndex));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // ボタンがHighlightedになったときのアニメーション
        buttonRectTransform.DOLocalMoveY(originalPosition.y + buttonMoveduration, 0.2f).SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // ボタンが元の位置に戻るアニメーション
        buttonRectTransform.DOLocalMoveY(originalPosition.y, 0.2f).SetEase(Ease.OutQuad);
    }
}