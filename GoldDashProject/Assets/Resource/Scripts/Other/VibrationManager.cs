using UnityEngine;

public class VibrationManager : MonoBehaviour
{
    public void Vibrate(long milliseconds)
    {
#if UNITY_ANDROID
        // AndroidのVibratorサービスを取得
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");

        // 振動を発生させる
        vibrator.Call("vibrate", milliseconds);
#endif
    }

    public void VibratePattern(long[] pattern, int repeat)
    {
#if UNITY_ANDROID
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");

        // 振動パターンを設定
        vibrator.Call("vibrate", pattern, repeat);
#endif
    }

    public void CancelVibration()
    {
#if UNITY_ANDROID
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");

        // 振動を停止
        vibrator.Call("cancel");
#endif
    }
}