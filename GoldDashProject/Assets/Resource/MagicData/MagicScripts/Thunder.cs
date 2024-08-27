using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu]
public class Thunder : MagicInfo
{
    [SerializeField] int AttackPoint;
    [SerializeField] GameObject thunderPrehab;
    [SerializeField] Vector3 fallPos;
    [SerializeField] float DestroyTime = 1f;

    public override void CastMagic(Vector3 position, Quaternion rotation)
    {
        if (UsageCount >= 0) UsageCount--;
        // �v���C���[�̃_���[�W�����͂����ɋL�q
        GameObject thunder = Instantiate(thunderPrehab, fallPos, Quaternion.identity);
        Light thunderLight = thunder.GetComponent<Light>();
        ThunderObj _thunderObj = thunderPrehab.GetComponent<ThunderObj>();

        // �񓯊��Ƀ��C�g�̃t�F�[�h�A�E�g�����s
        _thunderObj.FadeOutLightAsync(thunderLight);
        DestroyObj(ref thunder, DestroyTime);
        Debug.Log("���𗎂Ƃ�");
    }
}