using UnityEngine;

[CreateAssetMenu]
public class Fire : MagicInfo
{
    [SerializeField] int AttackPoint;
    public GameObject fireballPrefab; // �e��Prefab
    public float fireballSpeed = 10f; // �e�̑��x

    public override void CastMagic(Vector3 position, Quaternion rotation)
    {
        if (UsageCount >= 0) UsageCount--;
        //Player.Instance.TakeDamage(AttackPoint);
        GameObject fireball = Instantiate(fireballPrefab, position, rotation);
        Rigidbody rb = fireball.GetComponent<Rigidbody>();
        rb.velocity = rotation * Vector3.forward * fireballSpeed;
        Debug.Log("���𔭎�");
    }
}