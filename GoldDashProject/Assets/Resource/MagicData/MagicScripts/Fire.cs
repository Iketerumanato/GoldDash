using UnityEngine;

[CreateAssetMenu]
public class Fire : MagicInfo
{
    [SerializeField] int AttackPoint;
    [SerializeField] GameObject fireballPrefab; // �e��Prefab
    [SerializeField] float fireballSpeed = 10f; // �e�̑��x

    [SerializeField] float DestroyTime = 1f;

    public override void CastMagic(Vector3 position, Quaternion rotation)
    {
        if (UsageCount >= 0) UsageCount--;
        //�_���[�W�����͂���
        GameObject fireball = Instantiate(fireballPrefab, position, rotation);
        Rigidbody rb = fireball.GetComponent<Rigidbody>();
        rb.velocity = rotation * Vector3.forward * fireballSpeed;
        Debug.Log("���𔭎�");
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