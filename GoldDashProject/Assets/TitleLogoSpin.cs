using UnityEngine;
using DG.Tweening;

public class TitleLogoSpin : MonoBehaviour
{
    Sequence TitleLogoAnimatoin;
    [SerializeField] RectTransform TitleLogoImageTransform;
    [SerializeField] float ratateSpeed = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        TitleLogoAnimatoin = DOTween.Sequence();
        PlayTitleLogoAnimation();
    }

    private void PlayTitleLogoAnimation()
    {
        TitleLogoAnimatoin
            .SetDelay(1f)
            .Append(TitleLogoImageTransform.DOLocalRotate(new Vector3(0f, 0f, 360f), ratateSpeed, RotateMode.FastBeyond360).SetEase(Ease.OutQuad))
            .SetLoops(-1);
    }
}
