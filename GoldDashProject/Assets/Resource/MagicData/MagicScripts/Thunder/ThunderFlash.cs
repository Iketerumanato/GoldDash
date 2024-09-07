using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;

public class ThunderFlash : MonoBehaviour
{
    [SerializeField] Image FlashImg;
    [SerializeField] float fadeDuration = 2f;
    CancellationTokenSource cancellationToken;

    private void Start()
    {
        cancellationToken = new CancellationTokenSource();

        FadeFlash(FlashImg, FlashImg.color.a, 0f, fadeDuration, cancellationToken.Token).Forget();
    }

    private async UniTask FadeFlash(Image image, float startalpha, float endalpha, float duration, CancellationToken token)
    {
        Color color = image.color;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(startalpha, endalpha, elapsedTime / duration);
            image.color = color;
            await UniTask.Yield(token);
        }
        color.a = endalpha;
        image.color = color;
        this.gameObject.SetActive(false);
    }

    //”j‰óŽž‚ÉUniTask‚ðƒLƒƒƒ“ƒZƒ‹
    private void OnDestroy()
    {
        cancellationToken.Cancel();
    }
}