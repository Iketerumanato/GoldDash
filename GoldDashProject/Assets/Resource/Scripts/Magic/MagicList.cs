using System.Collections.Generic;
using UnityEngine;

public class MagicList : MonoBehaviour
{
    [Header("�󔠂Ɋi�[����Ă��閂�@")]
    [SerializeField] private List<MagicInfo> storedMagics = new();
    const int magicSelectionCount = 3;

    #region �v���C���[��MagicManagement�ɖ��@��ǉ����郁�\�b�h
    public void GrantRandomMagics(MagicManagement playerMagicManagement)
    {
        List<MagicInfo> selectedMagics = new();

        // �ꎞ���X�g���쐬���ă����_���ɖ��@��I��
        List<MagicInfo> tempMagicList = new(storedMagics);

        for (int MagicCnt = 0; MagicCnt < magicSelectionCount && tempMagicList.Count > 0; MagicCnt++)
        {
            int randomIndex = Random.Range(0, tempMagicList.Count);
            MagicInfo selectedMagic = tempMagicList[randomIndex];
            tempMagicList.RemoveAt(randomIndex);
            selectedMagics.Add(selectedMagic);
        }

        // �I�΂ꂽ���@���v���C���[��MagicManagement�ɒǉ�
        playerMagicManagement.AddMagics(selectedMagics);
    }
    #endregion
}