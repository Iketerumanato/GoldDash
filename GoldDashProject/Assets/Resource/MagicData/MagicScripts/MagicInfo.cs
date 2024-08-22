using UnityEngine;

[CreateAssetMenu]
public class MagicInfo : ScriptableObject
{
    [SerializeField] string MagicName;
    [SerializeField] public int UsageCount;
    readonly int maxUsageCount = 3;

    public void OnEnable()
    {
        UsageCount = maxUsageCount;
    }

    public virtual void CastMagic(Vector3 position, Quaternion rotation)
    {
        return;
    }
}