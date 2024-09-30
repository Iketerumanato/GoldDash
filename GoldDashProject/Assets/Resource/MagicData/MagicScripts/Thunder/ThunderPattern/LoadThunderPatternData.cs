using System.Collections.Generic;
using UnityEngine;

public class LoadThunderPatternData : MonoBehaviour
{
    private List<string> ThunderPatternList = new();
    readonly int NonPatternCount = 0;

    public LoadThunderPatternData(TextAsset csvData)
    {
        LoadThunderPatternFromCSV(csvData);
    }

    void LoadThunderPatternFromCSV(TextAsset csvData)
    {
        if (csvData != null)
        {
            var lines = csvData.text.Split('\n');
            foreach (var line in lines)
            {
                // 空行をスキップ
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                ThunderPatternList.Add(line.Trim());
            }
            Debug.Log($"CSVデータ {csvData.name} を読み込みました");
        }
        else
        {
            Debug.LogError("CSVデータがアサインされていません");
        }
    }

    // ランダムにパターンを取得
    public string GetRandomMorsePattern()
    {
        if (ThunderPatternList.Count == NonPatternCount)
        {
            Debug.LogError("モールスパターンが読み込まれていません。");
            return null;
        }
        int randomIndex = Random.Range(0, ThunderPatternList.Count);
        return ThunderPatternList[randomIndex];
    }
}
