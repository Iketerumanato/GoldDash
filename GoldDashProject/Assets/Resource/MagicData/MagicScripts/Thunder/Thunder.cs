using UnityEngine;

[CreateAssetMenu]
public class Thunder : MagicInfo
{
    [SerializeField] int AttackPoint;
    [SerializeField] GameObject thunderLightPrehab;
    [SerializeField] Vector3 fallPos;
    [SerializeField] float DestroyTime = 1f;

    public override void CastMagic(Vector3 position, Quaternion rotation)
    {
        if (UsageCount >= 0) UsageCount--;

        // �v���C���[�̃_���[�W�����͂����ɋL�q

        GameObject thunder = Instantiate(thunderLightPrehab, fallPos, Quaternion.identity);
        DestroyObj(ref thunder, DestroyTime);
    }
}