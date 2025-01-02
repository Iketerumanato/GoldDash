using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HotbarSlotInfo : MonoBehaviour
{
    [SerializeField] public ushort slotNum; //インタラクト時ushortに格納するので
    public Definer.MID magicID;
    [SerializeField] public SpriteRenderer nameSpriteRenderer;
    [SerializeField] public SpriteRenderer iconSpriteRenderer;
}
