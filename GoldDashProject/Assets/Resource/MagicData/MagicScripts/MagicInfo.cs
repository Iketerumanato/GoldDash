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

    //âΩÇ©ÇµÇÁÇê∂ê¨ÇµÇΩèÍçáÇÕÇ±ÇøÇÁÇ≈îjä¸
    public void DestroyObj<T>(ref T obj, float time = 0) where T : Object
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