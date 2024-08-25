using UnityEngine;

[CreateAssetMenu]
public class Fire : MagicInfo
{
    [SerializeField] int AttackPoint;
    [SerializeField] GameObject fireballPrefab; // 弾のPrefab
    [SerializeField] float fireballSpeed = 10f; // 弾の速度

    [SerializeField] float DestroyTime = 1f;

    public override void CastMagic(Vector3 position, Quaternion rotation)
    {
        if (UsageCount >= 0) UsageCount--;
        //ダメージ処理はここ
        GameObject fireball = Instantiate(fireballPrefab, position, rotation);
        Rigidbody rb = fireball.GetComponent<Rigidbody>();
        rb.velocity = rotation * Vector3.forward * fireballSpeed;
        Debug.Log("炎を発射");
        DestroyObj(ref fireball, DestroyTime);
    }

    void DestroyObj<T>(ref T obj, float time = 0) where T : Object
    {
        if (obj != null)
        {
#if UNITY_EDITOR
            if (Application.isPlaying) Object.Destroy(obj, time);
            else Object.DestroyImmediate(obj);
#else
        Object.Destroy(obj, t);
#endif
            obj = null;
        }
    }
}