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

        // プレイヤーのダメージ処理はここに記述

        GameObject thunder = Instantiate(thunderLightPrehab, fallPos, Quaternion.identity);
        DestroyObj(ref thunder, DestroyTime);
    }
}