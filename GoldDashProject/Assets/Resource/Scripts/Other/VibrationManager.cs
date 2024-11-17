using UnityEngine;

public class VibrationManager : MonoBehaviour
{
    public static void VibrateTablet()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaObject unityActivity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
        {
            using (AndroidJavaObject vibrator = unityActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                if (vibrator != null)
                {
                    // Android API 26 (Oreo)以降でサポートされる場合の振動
                    if (AndroidVersion() >= 26)
                    {
                        AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                        AndroidJavaObject vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", 500, vibrationEffectClass.GetStatic<int>("DEFAULT_AMPLITUDE"));
                        vibrator.Call("vibrate", vibrationEffect);
                    }
                    else
                    {
                        // Android API 25以下の振動方法
                        vibrator.Call("vibrate", 500); // 500ms振動
                    }
                }
            }
        }
#endif
    }

    private static int AndroidVersion()
    {
        using (AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            return versionClass.GetStatic<int>("SDK_INT");
        }
    }
}