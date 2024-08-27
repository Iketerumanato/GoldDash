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
        // プレイヤーのダメージ処理はここに記述
        GameObject thunder = Instantiate(thunderPrehab, fallPos, Quaternion.identity);
        Light thunderLight = thunder.GetComponent<Light>();
        ThunderObj _thunderObj = thunderPrehab.GetComponent<ThunderObj>();

        // 非同期にライトのフェードアウトを実行
        _thunderObj.FadeOutLightAsync(thunderLight);
        DestroyObj(ref thunder, DestroyTime);
        Debug.Log("雷を落とす");
    }
}