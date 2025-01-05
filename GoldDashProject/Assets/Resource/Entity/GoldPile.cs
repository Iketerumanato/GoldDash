using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldPile : Entity
{
    public int Value { set; get; } //山の金額

    private Rigidbody m_rigidbody;

    [Header("生成されたとき上向きに跳ねる力")]
    [SerializeField] private float m_hopPower;

    public override void InitEntity()
    {
        m_rigidbody = this.gameObject.GetComponent<Rigidbody>();
    }

    public override void ActivateEntity()
    {
        m_rigidbody.AddForce(Vector3.up * m_hopPower);
    }

    public override void DestroyEntity()
    {
        Destroy(this.gameObject);
    }
}
