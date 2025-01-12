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
        PlayCenterLogoAnimation();
    }

    private void PlayCenterLogoAnimation()
    {
        CenterLogoAnimation.Append(CenterLogoImageTransform.DOLocalRotate(new Vector3(0f, 0f, 360f), startRotateSpeed, RotateMode.FastBeyond360).SetEase(Ease.InOutBack))//InOutBackを付けつつ一回目の回転
            .SetDelay(animationDylayTime)//少し待機
            .Append(CenterLogoImageTransform.DOLocalRotate(new Vector3(0f, 0f, 360f), secondRotateSpeed, RotateMode.FastBeyond360).SetEase(Ease.OutBack))//InOutBackでの回転速度に追いつくためOutBackで２回目の回転
            .SetLoops(-1);//無限ループ
    }
}