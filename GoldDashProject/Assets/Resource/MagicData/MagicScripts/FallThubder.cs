using System.Collections;
using UnityEngine;

public class FallThubder : MonoBehaviour
{
    public GameObject pointLightPrefab; // �|�C���g���C�g�̃v���n�u
    [SerializeField] float fadeDuration = 2f; // �t���b�V���̌�������

    //Camera cam = Camera.main;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // �}�E�X�N���b�N�̈ʒu���擾
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // �N���b�N�����ʒu�Ƀ|�C���g���C�g�𐶐�
                GameObject pointLight = Instantiate(pointLightPrefab, hit.point, Quaternion.identity);

                // ���C�g�̃t�F�[�h�A�E�g���J�n
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

        // Intensity��0�ɂȂ����烉�C�g��j��
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