using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldPile : Entity
{
    public ushort test;

    public override void Init()
    {

    }

    public override void Activate()
    {
    }

    public override void Destroy()
    {
        Destroy(this.gameObject);
    }
}
