using UnityEngine;
using TMPro;

public class DisplayRefreshRate : MonoBehaviour
{
    [SerializeField] TMP_Text refreshRateText;

    private int tabletRefreshRate;

    // Start is called before the first frame update
    void Awake()
    {
        tabletRefreshRate = Screen.currentResolution.refreshRate;
    }

    // Update is called once per frame
    void Update()
    {
        // UIにリフレッシュレートを表示
        if (refreshRateText != null)
        {
            refreshRateText.text = $"Refresh Rate: {tabletRefreshRate} Hz";
        }
        else
        {
            Debug.Log($"Refresh Rate: {tabletRefreshRate} Hz");
        }
    }
}
