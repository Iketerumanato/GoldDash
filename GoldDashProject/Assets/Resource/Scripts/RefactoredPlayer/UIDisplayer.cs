using Cysharp.Threading.Tasks;
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

    [Header("巻物の長い紙")]
    [SerializeField] private GameObject m_ScrollLongPaper;

    [Header("宝箱を開錠するときの鍵")]
    [SerializeField] private GameObject m_key;

    [Header("殴られた時にこぼすコイン")]
    [SerializeField] private GameObject m_coinLost;
    private Animator m_coinLostAnimator;

    [Header("金貨の山に触れたとき吸い込むコイン")]
    [SerializeField] private GameObject m_coinGet;
    private Animator m_coinGetAnimator;

    private void Start()
    {
        //インスペクタで設定した値をDictionaryに登録する
        m_magicIDAndExplainPrefabDictionary = new Dictionary<Definer.MID, MIDAndExplainObjects>();
        foreach (MIDAndExplainObjects m in m_magicIDAndExplainPrefabList)
        {
            m_magicIDAndExplainPrefabDictionary.Add(m.magicID, m);
        }

        //アニメーター取得
        m_coinLostAnimator = m_coinLost.GetComponent<Animator>();
        m_coinGetAnimator = m_coinGet.GetComponent<Animator>();
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
                m_key.SetActive(true); //鍵を表示

                m_variableJoystick.SetActive(false); //宝箱の開錠中、移動スティック非表示
                m_HotbarParent.SetActive(false); //ホットバー非表示
                break;
            case PLAYER_STATE.USING_SCROLL: //巻物を開いたとき、巻物に表示するUIを決定する
                m_key.SetActive(false); //鍵を非表示

                foreach (KeyValuePair<Definer.MID, MIDAndExplainObjects> k in m_magicIDAndExplainPrefabDictionary) //表示中の巻物UIをすべて非表示にする
                {
                    k.Value.scrollExplainPrefab.SetActive(false);
                }
                UniTask u = UniTask.RunOnThreadPool(() => ActivateScrollObjects(magicID)); //モーションに合わせて時間差でUI表示
                m_HotbarParent.SetActive(false); //ホットバー非表示
                break;
            case PLAYER_STATE.WAITING_MAP_ACTION:
                m_variableJoystick.SetActive(false); //移動スティック非表示
                break;
            default: //基本的なものはすべて表示する
                m_variableJoystick.SetActive(true);
                m_dynamicJoystick.SetActive(true);
                m_HotbarParent.SetActive(true);

                m_ScrollLongPaper.SetActive(false); //巻物の長い紙を非表示
                m_key.SetActive(false); //鍵を非表示
                break;
        }
    }

    private async void ActivateScrollObjects(Definer.MID magicID)
    {
        await UniTask.Delay(520);
        m_magicIDAndExplainPrefabDictionary[magicID].scrollExplainPrefab.SetActive(true); //magicIDに対応した巻物UIを表示する
        m_ScrollLongPaper.SetActive(true); //長い紙を表示する
    }

    public void PlayGetCoinAnimation()
    {
        m_coinGetAnimator.SetTrigger("GetTrigger");
    }

    public void PlayLostCoinAnimation()
    {
        m_coinLostAnimator.SetTrigger("LostTrigger");
    }
}
