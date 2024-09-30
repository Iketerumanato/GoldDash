using UnityEngine;
using TMPro;

public class JudgeThunderPattern : MonoBehaviour
{
    [SerializeField] TMP_Text patternText;
    [SerializeField] TMP_Text resultText;
    [SerializeField] InputThunderCommand _inputThunderCommandIns;

    LoadThunderPatternData _loadThunderPatternIns;
    [SerializeField] TextAsset thunderPatternCSVData;

    private string targetMorsePattern = "";
    private string currentInputPattern = "";

    void Start()
    {
        _loadThunderPatternIns = new(thunderPatternCSVData);
        targetMorsePattern = _loadThunderPatternIns.GetRandomMorsePattern();
        patternText.text = targetMorsePattern;
        resultText.text = "";
    }

    void Update()
    {
        // TapScreen から現在のモールス入力を取得
        currentInputPattern = _inputThunderCommandIns.CurrentThunderPatternStr;
        if (currentInputPattern.Length == targetMorsePattern.Length) CheckMorsePattern();
    }

    //成功か失敗かの判定
    void CheckMorsePattern()
    {
        if (currentInputPattern == targetMorsePattern) resultText.text = "Success!";
        else resultText.text = "Failure...";
    }

    //リセットし、もう一度
    public void InitializationPattern()
    {
        _inputThunderCommandIns.ClearPattern();
        targetMorsePattern = _loadThunderPatternIns.GetRandomMorsePattern();
        patternText.text = targetMorsePattern;
        resultText.text = "";
    }
}
