using System.Collections.Generic;
using UnityEngine;

public class MagicManagement : MonoBehaviour
{
    [Header("���@�͂R�܂�")]
    [SerializeField] List<MagicInfo> availabMagics = new();
    readonly Dictionary<int, MagicInfo> magicDictionary = new();

    [Header("���@�����̂��߂̃{�^��")]
    [SerializeField] GameObject[] MagicButtons;

    const int maxMagicCount = 3;

    #region ���@�̏������{�^���Ɋ��蓖��
    // Start is called before the first frame update
    void Start()
    {
        // ���X�g���疂�@���{�^���ɏ��Ɋ��蓖��
        AssignMagicsToButtons();
    }

    // ���X�g���疂�@���{�^���Ɋ��蓖�Ă�
    void AssignMagicsToButtons()
    {
        // ���݂̊��蓖�Ă��N���A
        magicDictionary.Clear();

        // �{�^����������
        for (int ButtonCnt = 0; ButtonCnt < MagicButtons.Length; ButtonCnt++)
        {
            MagicButtons[ButtonCnt].SetActive(false);
        }

        for (int MagicCnt = 0; MagicCnt < maxMagicCount && MagicCnt < availabMagics.Count; MagicCnt++)
        {
            MagicInfo selectedMagic = availabMagics[MagicCnt];
            MagicButtons[MagicCnt].SetActive(true);
            int buttonIndex = MagicCnt + 1;//�{�^���̐��Ƃ��낦��
            magicDictionary[buttonIndex] = selectedMagic;
        }
    }
    #endregion

    #region //���@�̔���
    public void ActivateMagic(int magicNum)
    {
        if (magicDictionary.ContainsKey(magicNum))
        {
            MagicInfo magicinfo = magicDictionary[magicNum];
            if (magicinfo != null)
            {
                Debug.Log($"Using Magic: {magicinfo.name}");
                magicinfo.CastMagic(this.transform.position, this.transform.rotation);
                if (magicinfo.UsageCount <= 0)
                {
                    SetMagicToNull(magicNum);
                    CheckAllButtonsInactive();
                }
            }
        }
    }
    #endregion

    #region //���@�̍폜
    void SetMagicToNull(int magicNum)
    {
        if (magicDictionary.ContainsKey(magicNum))
        {
            MagicInfo magicinfo = magicDictionary[magicNum];
            if (magicinfo != null)
            {
                //availabMagics[magicNum -1] = null;
                MagicButtons[magicNum - 1].SetActive(false);
                // ���@��������
                magicinfo.OnEnable();
            }
        }
    }

    //���@���S���Ȃ��Ȃ�����
    void CheckAllButtonsInactive()
    {
        bool allButtonsInactive = true;

        foreach (GameObject button in MagicButtons)
        {
            if (button.activeSelf)
            {
                // 1�ł��A�N�e�B�u�ȃ{�^���������false
                allButtonsInactive = false;
                break;
            }
        }
        if (allButtonsInactive) availabMagics.Clear();
    }
    #endregion

    #region //���@�̒ǉ�
    // ���@���v���C���[�̃��X�g�Ɋi�[���郁�\�b�h
    public void AddMagics(List<MagicInfo> newMagics)
    {
        foreach (MagicInfo magic in newMagics)
        {
            if (availabMagics.Count < maxMagicCount)
            {
                availabMagics.Add(magic);
                Debug.Log($"�ȉ����ǉ�����܂���: {magic.name}");
            }
            else
            {
                Debug.Log("�C���x���g���������ς��ł�");
                break;
            }
        }
        // �ǉ����ꂽ���@���{�^���ɍĊ��蓖��
        AssignMagicsToButtons();
    }
    #endregion
}