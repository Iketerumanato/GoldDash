using UnityEngine;

public class VibrationManager : MonoBehaviour
{
    readonly string VibrationMethod = "vibrate";

    //millisecondsに関しては30000ミリ秒,amplitudeに関しては1から255まで強さを設定できる
    public void VibrateWithAmplitude(long vibrationTime, int vibratePower)
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
                vibrationTime,
                vibratePower);
            // 振動処理を呼び出す
            vibrator.Call(VibrationMethod, vibrationEffect);
        }
#endif
    }

    //現在のAndroidバージョンが8.0（API 26）以上かどうかを確認
    private bool AndroidVersionIsOreoOrHigher()
    {
#if UNITY_ANDROID
        AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION");
        int sdkInt = versionClass.GetStatic<int>("SDK_INT");
        return sdkInt >= 26;
#endif
    }
}