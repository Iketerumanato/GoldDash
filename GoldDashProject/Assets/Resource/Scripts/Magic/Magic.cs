using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//アイテムのソースデータ
[CreateAssetMenu(menuName = "ScriptableObject/MagicData")]
public class MagicData : ScriptableObject
{
    //各パラメータはpublicにしつつ、書き換えできないようgetterのみを宣言

    //魔法の名前
    [SerializeField] private string magicName;
    public string MagicName
    {
        get { return magicName; }
    }

    //魔法のアイコン
    [SerializeField] private Sprite icon;
    public Sprite Icon
    {
        get { return icon; }
    }

    //巻物を広げた際に表示される説明文のスプライト
    [SerializeField] private Sprite explanation;
    public Sprite Explanation
    {
        get { return explanation; }
    }

    //使用した際にサーバーに送るパケットのREID
    [SerializeField] private Definer.REID requestID;
    public Definer.REID RequestID
    {
        get { return requestID; }
    }
}