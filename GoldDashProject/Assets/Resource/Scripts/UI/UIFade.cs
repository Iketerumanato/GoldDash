using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIFade : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Image panelImage;
    [SerializeField] float fadeDuration;
    [SerializeField] DrawCircle drawCircle;

    //[Range(0f,1f)]
    //[SerializeField] float maxImageAlpha = 1f;


    #region フェードインメソッド
    public void FadeInCanvasGroup()
    {
        //drawCircle.NotActiveKey();
        //StartCoroutine(NotActiveDrawSys());
        StartCoroutine(FadeCanvasGroup(canvasGroup, canvasGroup.alpha, 1f, fadeDuration));
    }
    //public void FadeInImage()
    //{
    //    StartCoroutine(ActiveCanvas());
    //    StartCoroutine(FadeImage(panelImage, 0f, maxImageAlpha, fadeDuration));
    //}
    #endregion

    #region フェードアウトメソッド
    public void FadeOutCanvasGroup()
    {
        StartCoroutine(FadeCanvasGroup(canvasGroup, canvasGroup.alpha, 0f, fadeDuration));
        StartCoroutine(ActiveDrawSys());
        drawCircle.ActiveKey();
    }
    //public void FadeOutImage()
    //{
    //    StartCoroutine(FadeImage(panelImage, panelImage.color.a, 0f, fadeDuration));
    //    StartCoroutine(NotActiveCanvas());
    //}
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
    //private IEnumerator FadeImage(Image image, float startalpha, float endalpha, float duration)
    //{
    //    Color color = image.color;
    //    float elapsedTime = 0f;
    //    while (elapsedTime < duration)
    //    {
    //        elapsedTime += Time.deltaTime;
    //        color.a = Mathf.Lerp(startalpha, endalpha, elapsedTime / duration);
    //        image.color = color;
    //        yield return null;
    //    }
    //    color.a = endalpha;
    //    image.color = color;
    //}
    #endregion

    #region CanvasActive true/false
    IEnumerator ActiveDrawSys()
    {
        drawCircle.enabled = true;
        yield return new WaitForSeconds(fadeDuration);
    }

    IEnumerator NotActiveDrawSys()
    {
        yield return new WaitForSeconds(fadeDuration);
        drawCircle.enabled = false;
    }
    #endregion
}