using UnityEngine;
using DG.Tweening;

public class MagicButton : MonoBehaviour
{
    //[SerializeField] MagicManagement magicManagement;
    [SerializeField] int magicIndex;

    [SerializeField] Transform endPos;  // EndPosオブジェクトの参照
    private Vector3 originalPosition;
    [SerializeField] float buttonMoveSpeed = 20f;
    [SerializeField] float buttonAnimDuration = 0.2f;

    [SerializeField] float flickThreshold = 50f;
    private bool isDragging = false;
    private bool isFlicked = false;

    [SerializeField] Camera MagicButtonCam;

    private void Awake()
    {
        originalPosition = transform.position;
    }

    private void OnMouseDown()
    {
        // タッチ/クリック開始
        transform.DOKill();
        transform.position = originalPosition;
        isDragging = true;
        Debug.Log("ボタンのクリックを検知");
    }

    private void OnMouseUp()
    {
        // タッチ/クリック終了
        if (isDragging)
        {
            Vector3 screenPoint = Input.mousePosition;
            Ray ray = MagicButtonCam.ScreenPointToRay(screenPoint);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 dragVector = hit.point - originalPosition;

                if (dragVector.y > flickThreshold)
                {
                    TriggerFlickAnimation();
                    isFlicked = true;
                }
                else
                {
                    transform.DOMove(originalPosition, buttonAnimDuration).SetEase(Ease.OutQuad);
                }
            }

            isDragging = false;
        }
    }

    private void TriggerFlickAnimation()
    {
        // EndPos位置まで移動
        transform.DOMove(endPos.position, 0.5f)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => Debug.Log("Button reached end position."));
    }

    private void OnMouseEnter()
    {
        // ホバー効果
        if (isFlicked) return;
        transform.DOMoveY(originalPosition.y + buttonMoveSpeed, buttonAnimDuration).SetEase(Ease.OutQuad);
    }

    private void OnMouseExit()
    {
        // ホバー終了効果
        if (isFlicked) return;
        transform.DOMoveY(originalPosition.y, buttonAnimDuration).SetEase(Ease.OutQuad);
    }
}