using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    public string PlayerName { set; get; }

    //このアクターの座標と向きを更新する
    public void Move(Vector3 pos, Vector3 forward)
    { 
        this.gameObject.transform.position = pos;
        this.gameObject.transform.forward = forward;
    }

    //メソッドの例。正式実装ではない
    public void Kill()
    { 
    }

    public void GiveItem()
    { 
    }

    public void GiveStatus()
    { 
    }
}
