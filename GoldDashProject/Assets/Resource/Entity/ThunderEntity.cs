using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThunderEntity : Entity
{
    public override async void InitEntity() //これが呼ばれてから1000ミリ秒で消える
    {
        await UniTask.Delay(400);
        DestroyEntity();
    }

    public override void ActivateEntity()
    {
    }

    public override void DestroyEntity()
    {
        Destroy(this.gameObject);
    }
}
