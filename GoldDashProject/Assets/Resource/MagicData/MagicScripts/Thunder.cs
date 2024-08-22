using UnityEngine;

[CreateAssetMenu]
public class Thunder : MagicInfo
{
    [SerializeField] int AttackPoint;
    //[SerializeField] Player _player;

    public override void CastMagic(Vector3 position, Quaternion rotation)
    {
        if (UsageCount >= 0) UsageCount--;
        //_player.CmdTakeDamage(AttackPoint);
        Debug.Log("—‹‚ð—Ž‚Æ‚·");
    }
}