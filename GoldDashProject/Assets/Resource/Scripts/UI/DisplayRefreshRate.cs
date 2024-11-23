using UnityEngine;
using TMPro;

public class DisplayRefreshRate : MonoBehaviour
{
    [SerializeField] TMP_Text refreshRateText;
    [SerializeField] TMP_Text fpsText;

    private int tabletRefreshRate;

    private float deltaTime = 0.0f;

    void Update()
    {
        tabletRefreshRate = Screen.currentResolution.refreshRate; // または Screen.refreshRate (Unity 2021.2以降)
        if (refreshRateText != null)
        {
            refreshRateText.text = $"Refresh Rate: {tabletRefreshRate} Hz";
        }

        // FPSを計算
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;

        // UIにFPSを表示
        fpsText.text = $"FPS: {fps:0.0}";
    }
}