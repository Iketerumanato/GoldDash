using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class ThunderObj : MonoBehaviour
{
    [SerializeField] float fadeDuration = 2f; // フラッシュの減少時間
    [SerializeField] Light FlashLightPrefab;
    [SerializeField] GameObject FlashImagePrefab;
    Camera mainCamera;
    CancellationTokenSource cancellationToken;

    private void Start()
    {
        // メインカメラの取得
        mainCamera = Camera.main;
        cancellationToken = new CancellationTokenSource();

        // ライトがカメラの視界にあるかをチェック
        if (IsInCameraView(transform.position, mainCamera) && mainCamera != null) FlashImagePrefab.SetActive(true);

        FadeOutLightAsync(FlashLightPrefab, fadeDuration, cancellationToken.Token).Forget();
    }

    private async UniTask FadeOutLightAsync(Light light, float duration, CancellationToken token)
    {
        // フェードアウト処理
        float startIntensity = light.intensity;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            light.intensity = Mathf.Lerp(startIntensity, 0, elapsedTime / duration);
            await UniTask.Yield(token);
        }
    }

    private bool IsInCameraView(Vector3 targetposition, Camera playercamera)
    {
        // ワールド座標からスクリーン座標に変換
        Vector3 screenPoint = playercamera.WorldToViewportPoint(targetposition);

        // スクリーン座標が0から1の範囲内にあるかをチェック
        return (screenPoint.x >= 0 && screenPoint.x <= 1 &&
                screenPoint.y >= 0 && screenPoint.y <= 1 &&
                screenPoint.z > 0); // z > 0 はカメラの前にあることを確認
    }

    //破壊時にUniTaskをキャンセル
    private void OnDestroy()
    {
        cancellationToken.Cancel();
    }
}