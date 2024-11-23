using UnityEngine;
using TMPro;

public class DisplayRefreshRate : MonoBehaviour
{
    [SerializeField] TMP_Text refreshRateText;
    [SerializeField] TMP_Text fpsText;

    private int tabletRefreshRate;

    private float deltaTime = 0.0f;

    private void Awake()
    {
        tabletRefreshRate = Screen.currentResolution.refreshRate;
    }

    void Update()
    {
        refreshRateText.text = $"Refresh Rate: {tabletRefreshRate} Hz";
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        fpsText.text = $"FPS: {fps:0.0}";  
    }
}