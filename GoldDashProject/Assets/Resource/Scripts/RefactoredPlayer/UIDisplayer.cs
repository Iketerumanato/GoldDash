using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIDisplayer : MonoBehaviour
{
    //各種巻物のUI
    [SerializeField] private List<MIDandUIpair> m_magicIDandUIobjectPair;

    //KeyValuePairはインスペクタで表示できない（Serializableでない）ので自前のペアを作って使う
    [Serializable]
    private class MIDandUIpair
    {
        public Definer.MID magicID;
        public GameObject scrollUISet;
    }

    //高速に検索するためのDictionary
    private Dictionary<Definer.MID, GameObject> m_magicIDandUIobjectDictionary;

    //バーチャルスティック
    [SerializeField] private GameObject m_variableJoystick;
    [SerializeField] private GameObject m_dynamicJoystick;

    //メッセージUI
    [SerializeField] private GameObject m_messageUI;

    private void Start()
    {
        //インスペクタで設定した値をDictionaryに登録する
        m_magicIDandUIobjectDictionary = new Dictionary<Definer.MID, GameObject>();
        foreach (MIDandUIpair m in m_magicIDandUIobjectPair)
        {
            m_magicIDandUIobjectDictionary.Add(m.magicID, m.scrollUISet);
        }
    }

    /// <summary>
    /// そのStateに入ったとき1度だけ実行され、UIの表示・非表示切り替えを行う
    /// </summary>
    /// <param name="state">現在のstate</param>
    /// <param name="magicID">USING_SCROLLに入った時、巻物に表示する魔法のイラスト等を決定するためにMIDを指定する</param>
    public void ActivateUIFromState(PLAYER_STATE state, Definer.MID magicID = Definer.MID.NONE)
    {
        switch (state)
        {
            case PLAYER_STATE.OPENING_CHEST: //宝箱の開錠中、移動スティックを非表示にする
                m_variableJoystick.SetActive(false);
                m_dynamicJoystick.SetActive(false);
                break;
            case PLAYER_STATE.USING_SCROLL: //巻物を開いたとき、巻物に表示するUIを決定する
                foreach (KeyValuePair<Definer.MID, GameObject> k in m_magicIDandUIobjectDictionary) //表示中の巻物UIをすべて非表示にする
                { 
                    k.Value.SetActive(false);
                }
                GameObject currentScrollUISet; //null参照防止にTryGetValueしておく
                if (m_magicIDandUIobjectDictionary.TryGetValue(magicID, out currentScrollUISet)) //取得に成功したら
                {
                    currentScrollUISet.SetActive(true); //magicIDに対応した巻物UIを表示する
                }
                break;
            default: //基本的にすべて表示する
                m_variableJoystick.SetActive(true);
                m_dynamicJoystick.SetActive(true);
                break;
        }
    }
}
