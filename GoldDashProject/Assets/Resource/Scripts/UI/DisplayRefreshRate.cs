using UnityEngine;
using TMPro;

public class DisplayRefreshRate : MonoBehaviour
{
    [SerializeField] TMP_Text fpsText;
    [SerializeField] TMP_Text refreshRateText;

    private float deltaTime = 0.0f;

    void Start()
    {
        // リフレッシュレートを取得して表示
        if (refreshRateText != null)
        {
            int refreshRate = Screen.currentResolution.refreshRate; // またはScreen.refreshRate
            refreshRateText.text = $"Refresh Rate: {refreshRate} Hz";
        }
    }

    void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;

        // TextMeshProにFPSを表示
        if (fpsText != null)
        {
            fpsText.text = $"FPS: {fps:0.0}";
        }
        else
        {
            Debug.LogWarning("fpsText is not assigned!");
        }
    }
}