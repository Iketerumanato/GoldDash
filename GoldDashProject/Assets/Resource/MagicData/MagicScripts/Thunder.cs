using System.Collections;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu]
public class Thunder : MagicInfo
{
    [SerializeField] int AttackPoint;
    [SerializeField] GameObject thunderPrehab;
    [SerializeField] float fadeDuration = 2f; // �t���b�V���̌�������
    [SerializeField] Vector3 fallPos;
    [SerializeField] float DestroyTime = 1f;

    public override void CastMagic(Vector3 position, Quaternion rotation)
    {
        if (UsageCount >= 0) UsageCount--;

        // �v���C���[�̃_���[�W�����͂����ɋL�q

        GameObject thunder = Instantiate(thunderPrehab, fallPos, Quaternion.identity);
        Light thunderLight = thunder.GetComponent<Light>();

        // �񓯊��Ƀ��C�g�̃t�F�[�h�A�E�g�����s
        FadeOutLightAsync(thunderLight);
        DestroyObj(ref thunder, DestroyTime);
        Debug.Log("���𗎂Ƃ�");
    }

    private async void FadeOutLightAsync(Light light)
    {
        float startIntensity = light.intensity;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            light.intensity = Mathf.Lerp(startIntensity, 0, elapsedTime / fadeDuration);
            Debug.Log("��������");

            // await�Ŏ��̃t���[���܂Ŕ񓯊��������ꎞ��~
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