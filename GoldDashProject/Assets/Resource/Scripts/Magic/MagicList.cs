using System.Collections.Generic;
using UnityEngine;

public class MagicList : MonoBehaviour
{
    [Header("宝箱に格納されている魔法")]
    [SerializeField] private List<MagicInfo> storedMagics = new();
    const int magicSelectionCount = 3;

    #region プレイヤーのMagicManagementに魔法を追加するメソッド
    public void GrantRandomMagics(MagicManagement playerMagicManagement)
    {
        List<MagicInfo> selectedMagics = new();

        // 一時リストを作成してランダムに魔法を選ぶ
        List<MagicInfo> tempMagicList = new(storedMagics);

        for (int MagicCnt = 0; MagicCnt < magicSelectionCount && tempMagicList.Count > 0; MagicCnt++)
        {
            int randomIndex = Random.Range(0, tempMagicList.Count);
            MagicInfo selectedMagic = tempMagicList[randomIndex];
            tempMagicList.RemoveAt(randomIndex);
            selectedMagics.Add(selectedMagic);
        }

        // 選ばれた魔法をプレイヤーのMagicManagementに追加
        playerMagicManagement.AddMagics(selectedMagics);
    }
    #endregion
}