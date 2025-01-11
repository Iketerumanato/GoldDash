using UnityEngine;
using DG.Tweening;

public class CenterLogoImageAnimation : MonoBehaviour
{
    Sequence CenterLogoAnimation;
    [SerializeField] RectTransform CenterLogoImageTransform;
    [SerializeField] float startRotateSpeed = 1.3f;
    [SerializeField] float secondRotateSpeed = 1.5f;
    [SerializeField] float animationDylayTime = 1f;

    // Start is called before the first frame update
    void Start()
    {
        CenterLogoAnimation = DOTween.Sequence();
        CenterLogoAnimationMain();
    }

    private void CenterLogoAnimationMain()
    {
        CenterLogoAnimation.Append(CenterLogoImageTransform.DOLocalRotate(new Vector3(0f, 0f, 360f), startRotateSpeed, RotateMode.FastBeyond360).SetEase(Ease.InOutBack))
            .SetDelay(animationDylayTime)
            .Append(CenterLogoImageTransform.DOLocalRotate(new Vector3(0f, 0f, 360f), secondRotateSpeed, RotateMode.FastBeyond360).SetEase(Ease.OutBack))
            .SetLoops(-1);
    }
}