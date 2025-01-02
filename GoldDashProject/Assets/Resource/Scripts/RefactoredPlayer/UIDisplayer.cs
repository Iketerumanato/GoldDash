using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIDisplayer : MonoBehaviour
{
    //各種巻物のUI
    [Header("magicIDと、それに対応する説明文オブジェクトの参照")]
    [SerializeField] private List<MIDAndExplainObjects> m_magicIDAndExplainPrefabList;

    //KeyValuePairはインスペクタで表示できない（Serializableでない）ので自前のペアを作って使う
    [Serializable]
    private class MIDAndExplainObjects
    {
        public Definer.MID magicID;
        public GameObject scrollExplainPrefab;
    }

    //高速に検索するためのDictionary
    private Dictionary<Definer.MID, MIDAndExplainObjects> m_magicIDAndExplainPrefabDictionary;

    //バーチャルスティック
    [Header("スティックUIのゲームオブジェクト")]
    [SerializeField] private GameObject m_variableJoystick;
    [SerializeField] private GameObject m_dynamicJoystick;

    [Header("ホットバー全体の親オブジェクト")]
    [SerializeField] private GameObject m_HotbarParent;

    private void Start()
    {
        //インスペクタで設定した値をDictionaryに登録する
        m_magicIDAndExplainPrefabDictionary = new Dictionary<Definer.MID, MIDAndExplainObjects>();
        foreach (MIDAndExplainObjects m in m_magicIDAndExplainPrefabList)
        {
            m_magicIDAndExplainPrefabDictionary.Add(m.magicID, m);
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
            case PLAYER_STATE.OPENING_CHEST:
                m_variableJoystick.SetActive(false); //宝箱の開錠中、移動スティック非表示
                m_dynamicJoystick.SetActive(false);
                m_HotbarParent.SetActive(false); //ホットバー非表示
                break;
            case PLAYER_STATE.USING_SCROLL: //巻物を開いたとき、巻物に表示するUIを決定する
                foreach (KeyValuePair<Definer.MID, MIDAndExplainObjects> k in m_magicIDAndExplainPrefabDictionary) //表示中の巻物UIをすべて非表示にする
                {
                    k.Value.scrollExplainPrefab.SetActive(false);
                }
                MIDAndExplainObjects currentScrollUISet; //null参照防止にTryGetValueしておく
                if (m_magicIDAndExplainPrefabDictionary.TryGetValue(magicID, out currentScrollUISet)) //取得に成功したら
                {
                    currentScrollUISet.scrollExplainPrefab.SetActive(true); //magicIDに対応した巻物UIを表示する
                }
                m_HotbarParent.SetActive(false); //ホットバー非表示
                break;
            default: //基本的にすべて表示する
                m_variableJoystick.SetActive(true);
                m_dynamicJoystick.SetActive(true);
                m_HotbarParent.SetActive(true);
                break;
        }
    }
}
