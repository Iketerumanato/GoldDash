using UnityEngine;

public class VibrationManager
{
    public void VibrateWithAmplitude(long milliseconds, int amplitude)
    {
#if UNITY_ANDROID
        // UnityPlayer クラスの取得
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        // Vibratorサービスを取得
        AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");

        //AndroidVersionIsOreoOrHigherでtrue判定を受けた時のみVibrationEffectを利用
        if (vibrator != null && AndroidVersionIsOreoOrHigher())
        {
            // VibrationEffectクラスの生成
            AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
            AndroidJavaObject vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                "createOneShot",
                milliseconds,
                amplitude
            );
            // 振動処理を呼び出す
            vibrator.Call("vibrate", vibrationEffect);
        }
#endif
    }

    /// <summary>
    /// 現在のAndroidバージョンが8.0（API 26）以上かどうかを確認します。
    /// </summary>
    /// <returns>API 26以上ならtrue、それ以外はfalse</returns>
    private bool AndroidVersionIsOreoOrHigher()
    {
#if UNITY_ANDROID
        AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION");
        int sdkInt = versionClass.GetStatic<int>("SDK_INT");
        return sdkInt >= 26; // API 26 = Android 8.0
#else
        return false;
#endif
    }
}