using System.Threading.Tasks;
using UnityEngine;

public class ThunderObj : MonoBehaviour
{
    [SerializeField] float fadeDuration = 2f; // フラッシュの減少時間
    [SerializeField] Light FlashLightPrefab;
    [SerializeField] GameObject FlashImagePrefab;
    private Camera mainCamera;

    private void Start()
    {
        // メインカメラの取得
        mainCamera = Camera.main;

        // ライトがカメラの視界にあるかをチェック
        if (IsInCameraView(transform.position, mainCamera) && mainCamera != null)
        {
            Debug.Log("ライトはカメラの視界にあります");
            GameObject flashInstance = Instantiate(FlashImagePrefab);
            flashInstance.transform.SetParent(this.transform, false);
        }
        else Debug.Log("ライトはカメラの視界にありません");

        FadeOutLightAsync(FlashLightPrefab, fadeDuration);
    }

    public async void FadeOutLightAsync(Light light, float duration)
    {
        // フェードアウト処理
        float startIntensity = light.intensity;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            light.intensity = Mathf.Lerp(startIntensity, 0, elapsedTime / duration);
            await Task.Yield();
        }
    }

    private bool IsInCameraView(Vector3 position, Camera camera)
    {
        // ワールド座標からスクリーン座標に変換
        Vector3 screenPoint = camera.WorldToViewportPoint(position);

        // スクリーン座標が0から1の範囲内にあるかをチェック
        return (screenPoint.x >= 0 && screenPoint.x <= 1 &&
                screenPoint.y >= 0 && screenPoint.y <= 1 &&
                screenPoint.z > 0); // z > 0 はカメラの前にあることを確認
    }
}