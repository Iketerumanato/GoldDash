using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIFade : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Image panelImage;
    [SerializeField] float fadeDuration;
    DrawCircle drawCircle;

    private void Start()
    {
        drawCircle = this.gameObject.GetComponent<DrawCircle>();
    }

    #region フェードインメソッド
    public void FadeInCanvasGroup()
    {
        StartCoroutine(ActiveCanvas());
        StartCoroutine(FadeCanvasGroup(canvasGroup, canvasGroup.alpha, 1, fadeDuration));
    }
    public void FadeInImage()
    {
        StartCoroutine(ActiveCanvas());
        StartCoroutine(FadeImage(panelImage, 0f, 0.5f, fadeDuration));
    }
    #endregion

    #region フェードアウトメソッド
    public void FadeOutCanvasGroup()
    {
        StartCoroutine(FadeCanvasGroup(canvasGroup, canvasGroup.alpha, 0, fadeDuration));
        StartCoroutine(NotActiveCanvas());
    }
    public void FadeOutImage()
    {
        StartCoroutine(FadeImage(panelImage, panelImage.color.a, 0f, fadeDuration));
        StartCoroutine(NotActiveCanvas());
    }
    #endregion

    #region フェードさせるコルーチン
    //キャンバス
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsedTime / duration);
            yield return null;
        }
        cg.alpha = end;
    }
    //画像
    private IEnumerator FadeImage(Image image, float startalpha, float endalpha, float duration)
    {
        Color color = image.color;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(startalpha, endalpha, elapsedTime / duration);
            image.color = color;
            yield return null;
        }
        color.a = endalpha;
        image.color = color;
    }
    #endregion

    #region CanvasActive true/false
    IEnumerator ActiveCanvas()
    {
        panelImage.enabled = true;
        drawCircle.enabled = true;
        yield return new WaitForSeconds(fadeDuration);
    }

    IEnumerator NotActiveCanvas()
    {
        yield return new WaitForSeconds(fadeDuration);
        panelImage.enabled = false;
        drawCircle.enabled = false;
    }
    #endregion
}