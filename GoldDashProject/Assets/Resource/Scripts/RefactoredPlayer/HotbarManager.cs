using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HotbarManager : MonoBehaviour
{
    [Serializable]
    private class HotbarAssets
    {
        public Definer.MID magicID;
        public Sprite magicNameSprite;
        public Sprite magicIconSprite;
    }
    [Header("各魔法に関連するアセットの対応表")]
    //インスペクターから設定するためのリスト
    [SerializeField] private List<HotbarAssets> m_hotbarAssetsList;
    //高速検索用辞書
    private Dictionary<Definer.MID, HotbarAssets> m_hotbarAssetsDictionary;

    [Header("ホットバーのオブジェクト")]
    [SerializeField] private GameObject[] m_objectSlots;
    //以下はstart関数内で取得する
    private SpriteRenderer[] m_hotbarNameSprites;
    private SpriteRenderer[] m_hotbarIconSprites;
    private HotbarSlotInfo[] m_hotbarSlotInfos;

    [Header("ホットバーのマテリアル")]
    [SerializeField] private Material[] m_hotbarMaterials;

    //ホットバー内部データ
    private const int HOTBAR_SIZE = 3; //ホットバーのサイズ
    private Definer.MID[] m_hotbarArray; //ホットバーにセットされたmagicIDの配列

    private void Start()
    {
        //リストの要素をすべて使って辞書作成
        m_hotbarAssetsDictionary = new Dictionary<Definer.MID, HotbarAssets>();
        foreach (HotbarAssets h in m_hotbarAssetsList)
        { 
            m_hotbarAssetsDictionary.Add(h.magicID, h); //ここ、keyに使うためのmagicIDがvalue側にも書き込まれており、無駄があるが、インスペクターの見やすさを優先し妥協しました
        }

        //ホットバーのオブジェクト参照を使ってコンポーネントを取得する
        //まず配列初期化
        m_hotbarNameSprites = new SpriteRenderer[HOTBAR_SIZE];
        m_hotbarIconSprites = new SpriteRenderer[HOTBAR_SIZE];
        m_hotbarSlotInfos = new HotbarSlotInfo[HOTBAR_SIZE];
        //インスペクターで設定したオブジェクトへの参照を使って、各コンポーネントへの参照を取得する
        for (int i = 0; i < m_objectSlots.Length; i++)
        {
            m_hotbarSlotInfos[i] = m_objectSlots[i].GetComponent<HotbarSlotInfo>();
            m_hotbarNameSprites[i] = m_hotbarSlotInfos[i].nameSpriteRenderer;
            m_hotbarIconSprites[i] = m_hotbarSlotInfos[i].iconSpriteRenderer;
        }

        //ホットバー用配列の初期化
        m_hotbarArray = new Definer.MID[HOTBAR_SIZE];
        for (int i = 0; i < HOTBAR_SIZE; i++)
        {
            m_hotbarArray[i] = Definer.MID.NONE; //初期状態で持っている巻物はなし
        }
    }

    //ホットバー関連
    /// <summary>
    /// ホットバーの空いているところにmagicIDをセットする
    /// </summary>
    /// <param name="magicID">セットしたいmagicID</param>
    /// <returns>セットに成功したらtrue, ホットバーがすべて埋まっており、失敗したらfalse</returns>
    public bool SetMagicToHotbar(Definer.MID magicID)
    {
        for (int i = 0; i < HOTBAR_SIZE; i++) //ホットバーのサイズぶんループ
        {
            if (m_hotbarArray[i] == Definer.MID.NONE)
            {
                m_hotbarArray[i] = magicID; //魔法がセットされていないところを探して代入する。インデックスの先頭が優先

                //magicIDから対応するスプライトを取得してオブジェクトに適用しつつ、オブジェクトを有効化
                m_hotbarNameSprites[i].sprite = m_hotbarAssetsDictionary[magicID].magicNameSprite;
                m_hotbarIconSprites[i].sprite = m_hotbarAssetsDictionary[magicID].magicIconSprite;
                m_objectSlots[i].SetActive(true);

                //hotbarSlotInfoにmagicIDを書き込み（インタラクト時、レイキャストで情報を取得できるように）
                m_hotbarSlotInfos[i].magicID = magicID;

                return true; //代入できたらtrueを返却
            }
        }
        return false; //代入できなければfalseを返却
    }

    /// <summary>
    /// ホットバーに空きがあるか調べる
    /// </summary>
    /// <returns>空きがあればtrue, なければfalse</returns>
    public bool IsAbleToSetMagic()
    {
        for (int i = 0; i < HOTBAR_SIZE; i++) //ホットバーのサイズぶんループ
        {
            if (m_hotbarArray[i] == Definer.MID.NONE)
            {
                return true; //代入できるならtrueを返却
            }
        }
        return false; //代入できなければfalseを返却
    }

    /// <summary>
    /// 指定されたスロット番号の箇所のmagicIDをNONEにする。
    /// </summary>
    /// <param name="index">NONEをセットしたいスロット番号</param>
    public void RemoveMagicFromHotbar(int index)
    {
        //IDをNONEにしてオブジェクト無効化
        m_hotbarArray[index] = Definer.MID.NONE;
        m_objectSlots[index].SetActive(false);
        //hotbarSlotInfoのIDもNONEにする
        m_hotbarSlotInfos[index].magicID = Definer.MID.NONE;
    }
}
