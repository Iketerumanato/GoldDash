using System.Collections;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu]
public class Thunder : MagicInfo
{
    [SerializeField] int AttackPoint;
    [SerializeField] GameObject thunderPrehab;
    [SerializeField] float fadeDuration = 2f; // フラッシュの減少時間
    [SerializeField] Vector3 fallPos;
    [SerializeField] float DestroyTime = 1f;

    public override void CastMagic(Vector3 position, Quaternion rotation)
    {
        if (UsageCount >= 0) UsageCount--;

        // プレイヤーのダメージ処理はここに記述

        GameObject thunder = Instantiate(thunderPrehab, fallPos, Quaternion.identity);
        Light thunderLight = thunder.GetComponent<Light>();

        // 非同期にライトのフェードアウトを実行
        FadeOutLightAsync(thunderLight);
        DestroyObj(ref thunder, DestroyTime);
        Debug.Log("雷を落とす");
    }

    private async void FadeOutLightAsync(Light light)
    {
        float startIntensity = light.intensity;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            light.intensity = Mathf.Lerp(startIntensity, 0, elapsedTime / fadeDuration);
            Debug.Log("雷発生中");

            // awaitで次のフレームまで非同期処理を一時停止
            await Task.Yield();
        }

        var additionalLightData = light.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
        if (additionalLightData != null) Destroy(additionalLightData);
    }

    void DestroyObj<T>(ref T obj, float time = 0) where T : Object
    {
        if (obj != null)
        {
#if UNITY_EDITOR
            if (Application.isPlaying) Object.Destroy(obj, time);
            else Object.DestroyImmediate(obj);
#else
            Object.Destroy(obj, time);
#endif
            obj = null;
        }
    }
}