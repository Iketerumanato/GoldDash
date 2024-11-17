using UnityEngine;

public class VibrationManager : MonoBehaviour
{
    public static void VibrateTablet()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaObject vibrationService = new AndroidJavaObject("android.os.Vibrator"))
        {
            using (AndroidJavaObject unityActivity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
            {
                vibrationService.Call("vibrate", 500); // 500msバイブレーション
            }
        }
#endif
    }
}