using System;
using System.Linq;
using System.Text;

//パケット系クラスの基底クラス
public abstract class Packet
{
    //バイト配列への変換メソッド実装を強制
    public abstract byte[] ToByte();

    //バイト配列Aの末尾にバイト配列Bをくっつける
    protected byte[] AddByte(byte[] originBytes, byte[] addBytes)
    {
        byte[] ret = new byte[originBytes.Length + addBytes.Length];

        for (int i = 0; i < ret.Length; i++)
        {
            ret[i] = i < originBytes.Length ? originBytes[i] : addBytes[i - originBytes.Length];
        }

        return ret;
    }
    //バイト配列の末尾に任意のバイトを一つくっつける
    protected byte[] AddByte(byte[] originBytes, byte addByte)
    {
        byte[] ret = new byte[originBytes.Length + 1];

        for (int i = 0; i < ret.Length; i++)
        {
            ret[i] = i < originBytes.Length ? originBytes[i] : addByte;
        }

        return ret;
    }
}

//UDPClientから送信するパケットの先頭に付与するカスタムUDPヘッダ。送信番号を持たせて通信交換するとRUDPに進化。おめでとう！
public class Header : Packet
{
    private ushort sessionID; //サーバーから与えるID。セキュリティが要るならハッシュを使うべきか。
    private ushort indexDiff; //このパケット以降に続くパケット(RUDP用の古いパケット)の位置と、このパケットの先頭インデックスの差を示す
    private uint sendNum; //このパケットの送信番号
    private uint ackNum; //最後に相手から受け取ったパケットの送信番号
    private ushort packetType; //このパケットのタイプ
    private byte[] data; //データ本体

    //コンストラクタ１　各変数の値を直接指定する
    public Header(ushort sessionID, ushort indexDiff, uint sendNum, uint ackNum, byte packetType, byte[] data)
    {
        this.sessionID = sessionID;
        this.indexDiff = indexDiff; //古いパケットとの位置関係は送信時に分かるので、引数から直接とればよい
        this.sendNum = sendNum;
        this.ackNum = ackNum;
        this.packetType = packetType;
        this.data = data;
    }

    //コンストラクタ２　バイト配列を読んで変数を初期化
    public Header(byte[] bytes)
    {
        int index = 0;

        this.sessionID = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        this.indexDiff = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        this.sendNum = BitConverter.ToUInt32(bytes, index);
        index += sizeof(uint);
        this.ackNum = BitConverter.ToUInt32(bytes, index);
        index += sizeof(uint);
        this.sessionID = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        data = bytes.Skip(index).ToArray();
    }

    //変数からバイト配列を出力する
    public override byte[] ToByte()
    {
        byte[] ret = new byte[0];

        ret = AddByte(ret, BitConverter.GetBytes(sessionID));
        ret = AddByte(ret, BitConverter.GetBytes(indexDiff));
        ret = AddByte(ret, BitConverter.GetBytes(sendNum));
        ret = AddByte(ret, BitConverter.GetBytes(ackNum));
        ret = AddByte(ret, BitConverter.GetBytes(packetType));
        ret = AddByte(ret, data);

        return ret;
    }
}

//初回通信用パケット
public class InitPacket : Packet
{
    private ushort pass; //マッチング用パスワード
    private ushort rcvPort; //クライアントが受信用に空けているポートの番号
    private byte playerNameLength; //プレイヤー名のバイト数
    private string playerName; //プレイヤー名

    public InitPacket(ushort pass, ushort rcvPort, string playerName)
    {
        this.pass = pass;
        this.rcvPort = rcvPort;
        this.playerName = playerName;
        this.playerNameLength = (byte)playerName.Length;
    }

    public InitPacket(byte[] bytes)
    {
        int index = 0;

        this.pass = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        this.rcvPort = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        this.playerNameLength = bytes[index];
        index++;
        this.playerName = Encoding.UTF8.GetString(bytes, index, playerNameLength);
    }

    public override byte[] ToByte()
    {
        byte[] ret = new byte[0];

        ret = AddByte(ret, BitConverter.GetBytes(pass));
        ret = AddByte(ret, BitConverter.GetBytes(rcvPort));
        ret = AddByte(ret, BitConverter.GetBytes(playerNameLength));
        ret = AddByte(ret, Encoding.UTF8.GetBytes(playerName));

        return ret;
    }
}

public class ActionPacket : Packet
{
    byte roughID; //アクションのカテゴリを示す
    byte detailID; //アクションの詳細な種類を示す
    byte targetID; //アクションの対象を示す
    UnityEngine.Vector3 pos; //座標データを持つアクションで参照する

    public ActionPacket(byte roughID, byte detailID, byte targetID, UnityEngine.Vector3 pos)
    {
        this.roughID = roughID;
        this.detailID = detailID;
        this.targetID = targetID;
        this.pos = pos;
    }

    public ActionPacket(byte[] bytes)
    {
        int index = 0;
        float x, y, z;

        this.roughID = bytes[index];
        index++;
        this.detailID = bytes[index];
        index++;
        this.targetID = bytes[index];
        index++;
        x = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        y = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        z = BitConverter.ToSingle(bytes, index);
        pos = new UnityEngine.Vector3(x, y, z);
    }

    public override byte[] ToByte()
    {
        byte[] ret = new byte[0];

        ret = AddByte(ret, roughID);
        ret = AddByte(ret, detailID);
        ret = AddByte(ret, targetID);
        ret = AddByte(ret, BitConverter.GetBytes(pos.x));
        ret = AddByte(ret, BitConverter.GetBytes(pos.y));
        ret = AddByte(ret, BitConverter.GetBytes(pos.z));

        return ret;
    }
}
