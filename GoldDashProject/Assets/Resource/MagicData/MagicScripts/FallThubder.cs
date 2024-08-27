using System.Collections;
using UnityEngine;

public class FallThubder : MonoBehaviour
{
    public GameObject pointLightPrefab; // ポイントライトのプレハブ
    [SerializeField] float fadeDuration = 2f; // フラッシュの減少時間

    //Camera cam = Camera.main;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // マウスクリックの位置を取得
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // クリックした位置にポイントライトを生成
                GameObject pointLight = Instantiate(pointLightPrefab, hit.point, Quaternion.identity);

                // ライトのフェードアウトを開始
                StartCoroutine(FadeOutLight(pointLight.GetComponent<Light>()));
            }
        }
    }

    private IEnumerator FadeOutLight(Light light)
    {
        float startIntensity = light.intensity;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            light.intensity = Mathf.Lerp(startIntensity, 0, elapsedTime / fadeDuration);
            yield return null;
        }

        // Intensityが0になったらライトを破壊
        Destroy(light.gameObject);
    }

    //private bool checkVisibility()
    //{
    //    var planes = GeometryUtility.CalculateFrustumPlanes(cam);
    //    var position = transform.position;

    //    foreach (var p in planes)
    //    {
    //        if(p.GetDistanceToPoint(position) > 0f)
    //        {
    //            Ray ray = new(cam.transform.position, transform.position - cam.transform.position);
    //            RaycastHit Hit;
    //            if (Physics.Raycast(ray, out Hit)) return Hit.transform.gameObject == this.gameObject;
    //            else return false;
    //        }
    //        else return false;
    //    }
    //    return false;
    //}
}