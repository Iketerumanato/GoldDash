using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scroll : Entity
{
    public Definer.MID MagicID { set; get; } //山の金額

    [Header("ふわふわ浮ぶ動きの振れ幅")]
    [SerializeField] private float m_floatLength;

    [Header("ふわふわ浮ぶ動きの速さ")]
    [SerializeField] private float m_floatSpeed;

    [Header("回転する動きの速さ")]
    [SerializeField] private float m_spinSpeed;

    private float time;

    private void Update()
    {
        time += Time.deltaTime; //Sin関数用
        this.transform.position = new Vector3(transform.position.x, 0.3f + Mathf.Sin(m_floatSpeed * time) * m_floatLength, transform.position.z); //Sinで上下にふわふわ動かす
        this.transform.Rotate(0f, m_spinSpeed * Time.deltaTime, 0f); //少しずつ回転させる
    }

    public override void InitEntity()
    {
    }

    public override void ActivateEntity()
    {
    }

    public override void DestroyEntity()
    {
        Destroy(this.gameObject);
    }
}
