using UnityEngine;

[CreateAssetMenu]
public class Fire : MagicInfo
{
    [SerializeField] int AttackPoint;
    [SerializeField] GameObject fireballPrefab; 
    [SerializeField] float fireballSpeed = 10f;
    [SerializeField] float DestroyTime = 1f;

    public override void CastMagic(Vector3 position, Quaternion rotation)
    {
        if (UsageCount >= 0) UsageCount--;
        //ƒ_ƒ[ƒWˆ—‚Í‚±‚±
        GameObject fireball = Instantiate(fireballPrefab, position, rotation);
        Rigidbody rb = fireball.GetComponent<Rigidbody>();
        rb.velocity = rotation * Vector3.forward * fireballSpeed;
        Debug.Log("‰Š‚ğ”­Ë");
        DestroyObj(ref fireball, DestroyTime);
    }
}