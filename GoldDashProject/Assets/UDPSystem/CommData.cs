using System;
using System.Text;
using UnityEngine;

public class CommData
{
    //初回通信用
    private const string KEY_WORD = "ARTORIAS_THE_ABYSSWALKER";
    public UInt16 num; //クライアントからサーバーへは受信ポート番号を、サーバーからクライアントへはプレイヤー番号を

    //プレイヤーの座標同期に使う
    
    public struct POS_DATA //参照型にする必要がなさそうなのでクラスではなく構造体で
    {
        public byte playerNum;
        public Vector3 positionVec;
        public Vector3 forwardVec;
    }

    public POS_DATA[] posDataArray = new POS_DATA[4];

    //private int id;
    //public Vector3 vec;
    //private int messageSize;
    //private string message;

    //攻撃やアイテム入手のトリガーとなる変数
    //byte actionID; //実行アクションの大まかな分類
    //byte detailID; //実行アクションの詳細な分類
    //byte targetID; //アクションの対象
    //Vector3 actionVec; //座標情報をもつアクションである場合使う

    //コンストラクタ　バイトから変換　indexの取り方について、現在力技すぎるのでメソッド化したいかも
    public CommData(byte[] bytes)
    {
        int index = Encoding.UTF8.GetBytes(KEY_WORD).Length;

        //キーワード部分は飛ばしてIDを読む
        this.num = BitConverter.ToUInt16(bytes, index);
        index += sizeof(UInt16);

        for (int i = 0; i < 4; i++)
        {
            this.posDataArray[i].playerNum = bytes[index];
            index++;
            this.posDataArray[i].positionVec.x = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
            this.posDataArray[i].positionVec.y = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
            this.posDataArray[i].positionVec.z = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);

            this.posDataArray[i].forwardVec.x = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
            this.posDataArray[i].forwardVec.y = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
            this.posDataArray[i].forwardVec.z = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
        }
    }

    //コンストラクタ　引数から作成
    public CommData(UInt16 num, POS_DATA[] data)
    {
        this.num = num;

        this.posDataArray = data;
    }

    //このインスタンスのメンバーをバイト配列に変換する
    public byte[] ToByte()
    {
        //サイズ0の配列に対して新たな配列を足して行くことで、可変長のコレクションのように使う
        byte[] ret = new byte[0];

        ret = AddByte(ret, Encoding.UTF8.GetBytes(KEY_WORD)); //キーワードを書く
        ret = AddByte(ret, BitConverter.GetBytes(this.num)); //IDを書く

        foreach (POS_DATA data in posDataArray) //座標データ書き込み
        {
            ret = AddByte(ret, BitConverter.GetBytes(data.playerNum));
            ret = AddByte(ret, BitConverter.GetBytes(data.positionVec.x));
            ret = AddByte(ret, BitConverter.GetBytes(data.positionVec.y));
            ret = AddByte(ret, BitConverter.GetBytes(data.positionVec.z));
            ret = AddByte(ret, BitConverter.GetBytes(data.forwardVec.x));
            ret = AddByte(ret, BitConverter.GetBytes(data.forwardVec.y));
            ret = AddByte(ret, BitConverter.GetBytes(data.forwardVec.z));
        }

        return ret;

        //バイト配列Aの末尾にバイト配列Bをくっつける
        byte[] AddByte(byte[] originBytes, byte[] addBytes)
        { 
            byte[] ret = new byte[originBytes.Length + addBytes.Length];

            for (int i = 0; i < ret.Length; i++)
            {
                ret [i] = i < originBytes.Length ? originBytes[i] : addBytes[i - originBytes.Length];
            }

            return ret;
        }
    }

    //キーワードが合っているか確認する静的メソッド
    public static bool CheckKeyWord(byte[] bytes)
    {
        UnityEngine.Debug.Log($"送られてきたキーワードは{Encoding.UTF8.GetString(bytes, 0, Encoding.UTF8.GetBytes(KEY_WORD).Length)}");
        UnityEngine.Debug.Log($"正しいキーワードは{KEY_WORD}");
        return Encoding.UTF8.GetString(bytes, 0, Encoding.UTF8.GetBytes(KEY_WORD).Length).Equals(KEY_WORD);
    }

    //試験的　キーワードの次に書かれたポート番号を調べる
    public static UInt16 GetPort(byte[] bytes)
    {
        return BitConverter.ToUInt16(bytes, Encoding.UTF8.GetBytes(KEY_WORD).Length);
    }
}
