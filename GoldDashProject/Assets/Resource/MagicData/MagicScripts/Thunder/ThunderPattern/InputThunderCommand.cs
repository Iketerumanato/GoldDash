using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using TMPro;

public class InputThunderCommand : MonoBehaviour
{
    [SerializeField] TMP_Text displayText;
    [SerializeField] TMP_Text outputText;

    private float pressTime = 0f;
    const float InitialPressTime = 0f;
    private string ThunderPatternStr = "";
    public string CurrentThunderPatternStr => ThunderPatternStr;

    private string Outputstr = "";

    [SerializeField, Range(0f, 1f)]
    float thresholdTime = 0.5f;

    [SerializeField, Range(0f, 0.05f)]
    float pressSpeed = 0.01f;

    private CancellationTokenSource _cts;

    #region ボタンの処理群

    #region async/await版

    public async Task OnPointerDownAsync()
    {
        _cts = new CancellationTokenSource();
        await HandlePressDuration(_cts.Token);
    }

    public void OnPointerUp()
    {
        _cts?.Cancel();
        if (pressTime < thresholdTime)
        {
            ThunderPatternStr += ".";
            displayText.text = ThunderPatternStr;
        }
        pressTime = InitialPressTime;
    }

    private async Task HandlePressDuration(CancellationToken token)
    {
        pressTime = InitialPressTime;
        while (pressTime < thresholdTime)
        {
            await Task.Delay(10);

            if (token.IsCancellationRequested) return;

            pressTime += pressSpeed;
        }
        ThunderPatternStr += "-";
        displayText.text = ThunderPatternStr;
    }
    #endregion

    //public void ConfirmMorseSignal()
    //{
    //    var letter = _loadThunderPatternDataIns.GetLetterFromMorseCode(MorseSignal);

    //    if (letter.HasValue)
    //    {
    //        Outputstr += letter;
    //        outputText.text = Outputstr;
    //    }
    //    else Debug.LogWarning("無効なモールス信号です");

    //    ResetMorse();
    //}

    void ResetPattern()
    {
        ThunderPatternStr = "";
        displayText.text = ThunderPatternStr;
    }

    public void ClearPattern()
    {
        ThunderPatternStr = "";
        Outputstr = "";
        displayText.text = ThunderPatternStr;
        outputText.text = Outputstr;
    }
#endregion
}