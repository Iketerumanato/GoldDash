using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scroll : Entity
{
    public Definer.MID MagicID { set; get; } //山の金額

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
