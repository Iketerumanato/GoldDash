using System.Collections;
using UnityEngine;
using System.Threading.Tasks;

public class ThunderObj : MonoBehaviour
{
    [SerializeField] float fadeDuration = 2f; // フラッシュの減少時間

    Camera maincam;

    private void Start()
    {
        maincam = Camera.main;
    }

    public bool checkBlindility()
    {
        var planes = GeometryUtility.CalculateFrustumPlanes(maincam);
        var point = transform.position;

        foreach(var p in planes)
        {
            return (p.GetDistanceToPoint(point) > 0);
        }

        return true;
    }

    public async void FadeOutLightAsync(Light light)
    {

        if(checkBlindility()) Debug.Log("go blind!");
        else Debug.Log("Don't get affected!");

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
        if (additionalLightData != null)
        {
            Destroy(additionalLightData);
        }
    }
}