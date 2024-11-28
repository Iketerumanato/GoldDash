using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MagicLottely : MonoBehaviour
{
    //MagicIDとその重み
    [SerializeField] private List<MagicWheights> magicWeightsList;

    //KeyValuePairはインスペクタで表示できない（Serializableでない）ので自前のペアを作って使う
    [Serializable]
    private class MagicWheights
    {
        public Definer.MID magicID;
        public int weight;
    }

    //その後の抽選処理はDictionaryで高速に行いたい
    private Dictionary<int, int> magicWeightsDictionary; //ここMID構造体を使うとキャストの手間がかかるのでintにする
    //重みの合計値
    private int weightSum;

    private void Start()
    {
        //インスペクタで設定した重みをDictionaryに変換する
        magicWeightsDictionary = new Dictionary<int, int>();
        foreach (MagicWheights m in magicWeightsList)
        {
            magicWeightsDictionary.Add((int)m.magicID, m.weight); //インスペクタで構造体だったものをintにキャストして辞書に登録
        }

        //重みの合計値計算
        weightSum = magicWeightsDictionary.Values.Sum();

        //確立シミュレーションしたい場合はこちら
        //for (int i = 0; i < 1000; i++)
        //{
        //    Debug.Log((Definer.MID)Lottely());
        //}
    }

    public int Lottely()
    {
        //intを抽選するので第二引数は排他的上限
        int rand = UnityEngine.Random.Range(0, weightSum);

        //合計値をコピー
        int tmpWeightSum = weightSum;

        //商品を選択
        foreach (KeyValuePair<int, int> k in magicWeightsDictionary)
        {
            //Dictionaryの端から範囲を狭めながら、randの値がどのKeyの指す範囲にあるか調べていく
            if ((tmpWeightSum -= k.Value) <= rand)
            {
                return k.Key; //intで返す。取り出してからキャストしなおす
            }
        }

        throw new Exception(); //ここには絶対到達しないはず。テスト済。到達したらエラー
    }
}
