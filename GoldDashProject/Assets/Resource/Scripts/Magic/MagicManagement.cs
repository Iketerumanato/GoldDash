using System.Collections.Generic;
using UnityEngine;

public class MagicManagement : MonoBehaviour
{
    [Header("魔法は３つまで")]
    [SerializeField] List<MagicInfo> availabMagics = new();
    readonly Dictionary<int, MagicInfo> magicDictionary = new();

    [Header("魔法発動のためのボタン")]
    [SerializeField] GameObject[] MagicButtons;

    const int maxMagicCount = 3;

    #region 魔法の処理をボタンに割り当て
    // Start is called before the first frame update
    void Start()
    {
        // リストから魔法をボタンに順に割り当て
        AssignMagicsToButtons();
    }

    // リストから魔法をボタンに割り当てる
    void AssignMagicsToButtons()
    {
        // 現在の割り当てをクリア
        magicDictionary.Clear();

        // ボタンを初期化
        for (int ButtonCnt = 0; ButtonCnt < MagicButtons.Length; ButtonCnt++)
        {
            MagicButtons[ButtonCnt].SetActive(false);
        }

        for (int MagicCnt = 0; MagicCnt < maxMagicCount && MagicCnt < availabMagics.Count; MagicCnt++)
        {
            MagicInfo selectedMagic = availabMagics[MagicCnt];
            MagicButtons[MagicCnt].SetActive(true);
            int buttonIndex = MagicCnt + 1;//ボタンの数とそろえる
            magicDictionary[buttonIndex] = selectedMagic;
        }
    }
    #endregion

    #region //魔法の発動
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

    #region //魔法の削除
    void SetMagicToNull(int magicNum)
    {
        if (magicDictionary.ContainsKey(magicNum))
        {
            MagicInfo magicinfo = magicDictionary[magicNum];
            if (magicinfo != null)
            {
                //availabMagics[magicNum -1] = null;
                MagicButtons[magicNum - 1].SetActive(false);
                // 魔法を初期化
                magicinfo.OnEnable();
            }
        }
    }

    //魔法が全部なくなった時
    void CheckAllButtonsInactive()
    {
        bool allButtonsInactive = true;

        foreach (GameObject button in MagicButtons)
        {
            if (button.activeSelf)
            {
                // 1つでもアクティブなボタンがあればfalse
                allButtonsInactive = false;
                break;
            }
        }
        if (allButtonsInactive) availabMagics.Clear();
    }
    #endregion

    #region //魔法の追加
    // 魔法をプレイヤーのリストに格納するメソッド
    public void AddMagics(List<MagicInfo> newMagics)
    {
        foreach (MagicInfo magic in newMagics)
        {
            if (availabMagics.Count < maxMagicCount)
            {
                availabMagics.Add(magic);
                Debug.Log($"以下が追加されました: {magic.name}");
            }
            else
            {
                Debug.Log("インベントリがいっぱいです");
                break;
            }
        }
        // 追加された魔法をボタンに再割り当て
        AssignMagicsToButtons();
    }
    #endregion
}