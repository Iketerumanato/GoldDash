using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : Entity
{
    public int Tier { set; get; } //レア度

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