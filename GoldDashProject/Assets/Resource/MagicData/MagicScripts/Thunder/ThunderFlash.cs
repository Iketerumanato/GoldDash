using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ThunderFlash : MonoBehaviour
{
    [SerializeField] Image FlashImg;
    [SerializeField] float fadeDuration = 2f;

    private void Start()
    {
        FadeFlash(FlashImg, FlashImg.color.a, 0f, fadeDuration);
    }

    public async void FadeFlash(Image image, float startalpha, float endalpha, float duration)
    {
        Color color = image.color;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(startalpha, endalpha, elapsedTime / duration);
            image.color = color;
            await Task.Yield();
        }
        color.a = endalpha;
        image.color = color;
    }
}
