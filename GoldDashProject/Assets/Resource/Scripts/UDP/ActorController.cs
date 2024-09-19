using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤー達を管理するクラス。GameServerManagerでSessionIDをKeyとしてDictionaryで管理される
/// </summary>
public class ActorController : MonoBehaviour
{
    string playerName;

    public ActorController(string playerName)
    { 
        this.playerName = playerName;
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
