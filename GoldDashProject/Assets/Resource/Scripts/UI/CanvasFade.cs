using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CanvasFade : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Image panelImage;
    [SerializeField] float fadeDuration = 1.0f;//�t�F�[�h���鑬��
    [SerializeField] DrawCircle drawcircle;

    public static CanvasFade _canvusfadeIns { get; private set; }
    private void Awake()
    {
        if (_canvusfadeIns != null && _canvusfadeIns != this) Destroy(gameObject);
        else
        {
            _canvusfadeIns = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    #region �t�F�[�h�C�����\�b�h
    public void FadeInCanvasGroup()
    {
        StartCoroutine(ActiveCanvus());
        StartCoroutine(FadeCanvasGroup(canvasGroup, canvasGroup.alpha, 1, fadeDuration));
    }
    public void FadeInImage()
    {
        StartCoroutine(ActiveCanvus());
        StartCoroutine(FadeImage(panelImage, 0f, 0.5f, fadeDuration));
    }
    #endregion

    #region �t�F�[�h�A�E�g���\�b�h
    public void FadeOutCanvasGroup()
    {
        StartCoroutine(FadeCanvasGroup(canvasGroup, canvasGroup.alpha, 0, fadeDuration));
        StartCoroutine(NotActiveCanvus());
    }
    public void FadeOutImage()
    {
        StartCoroutine(FadeImage(panelImage, 0.5f, 0f, fadeDuration));
        StartCoroutine(NotActiveCanvus());
    }
    #endregion

    #region �t�F�[�h������R���[�`��
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

    #region Active true/false
    IEnumerator ActiveCanvus()
    {
        panelImage.enabled = true;
        drawcircle.enabled = true;
        yield return new WaitForSeconds(fadeDuration);
    }

    IEnumerator NotActiveCanvus()
    {
        yield return new WaitForSeconds(fadeDuration);
        panelImage.enabled = false;
        drawcircle.enabled = false;
    }
    #endregion
}