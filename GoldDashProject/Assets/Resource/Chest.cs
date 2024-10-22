using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Chest : Entity
{
    int Tier; //レア度

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
