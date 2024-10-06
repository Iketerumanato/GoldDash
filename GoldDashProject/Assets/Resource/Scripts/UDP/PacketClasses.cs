using System;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;

//パケット系クラスの基底クラス
public abstract class Packet
{
    //バイト配列への変換メソッド実装を強制
    public abstract byte[] ToByte();

    //バイト配列Aの末尾にバイト配列Bをくっつける
    protected byte[] AddBytes(byte[] originBytes, byte[] addBytes)
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
    public ushort sessionID; //サーバーから与えるID。セキュリティが要るならハッシュを使うべきか。
    public ushort indexDiff; //このパケット以降に続くパケット(RUDP用の古いパケット)の位置と、このパケットの先頭インデックスの差を示す
    public uint sendNum; //このパケットの送信番号
    public uint ackNum; //最後に相手から受け取ったパケットの送信番号
    public byte packetType; //このパケットのタイプ
    public byte[] data; //データ本体

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
        this.packetType = bytes[index];
        index++;
        data = bytes.Skip(index).ToArray();
    }

    //変数からバイト配列を出力する
    public override byte[] ToByte()
    {
        byte[] ret = new byte[0];

        ret = AddBytes(ret, BitConverter.GetBytes(sessionID));
        ret = AddBytes(ret, BitConverter.GetBytes(indexDiff));
        ret = AddBytes(ret, BitConverter.GetBytes(sendNum));
        ret = AddBytes(ret, BitConverter.GetBytes(ackNum));
        ret = AddByte(ret, packetType);
        ret = AddBytes(ret, data);

        return ret;
    }
}

//初回通信用パケット
public class InitPacketClient : Packet
{
    public ushort sessionPass; //マッチング用パスワード
    public ushort rcvPort; //クライアントが受信用に空けているポートの番号
    public ushort initSessionPass; ////初回通信時にプレイヤー側から送るセッションパス。得られたレスポンスがサーバーからのものであると断定するときに使う。
    public byte playerNameLength; //プレイヤー名のバイト数
    public string playerName; //プレイヤー名

    public InitPacketClient(ushort pass, ushort rcvPort, ushort initSessionPass, string playerName)
    {
        this.sessionPass = pass;
        this.rcvPort = rcvPort;
        this.initSessionPass = initSessionPass;
        this.playerNameLength = (byte)Encoding.UTF8.GetByteCount(playerName);
        this.playerName = playerName;
    }

    public InitPacketClient(byte[] bytes)
    {
        int index = 0;

        this.sessionPass = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        this.rcvPort = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        this.initSessionPass = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        this.playerNameLength = bytes[index];
        index++;
        this.playerName = Encoding.UTF8.GetString(bytes, index, playerNameLength);
    }

    public override byte[] ToByte()
    {
        byte[] ret = new byte[0];

        ret = AddBytes(ret, BitConverter.GetBytes(sessionPass));
        ret = AddBytes(ret, BitConverter.GetBytes(rcvPort));
        ret = AddBytes(ret, BitConverter.GetBytes(initSessionPass));
        ret = AddByte(ret, playerNameLength);
        ret = AddBytes(ret, Encoding.UTF8.GetBytes(playerName));

        return ret;
    }
}

public class InitPacketServer : Packet
{
    public ushort initSessionPass; //マッチング用パスワード
    public ushort rcvPort; //サーバのポート番号
    public ushort sessionID; //サーバーから与えるID
    public byte error; //エラーコード　現在のサーバの状態を返すプレイヤー名の重複が起きたときなど

    public InitPacketServer(ushort initSessionPass, ushort rcvPort, ushort sessionID, byte error = 0)
    {
        this.initSessionPass = initSessionPass;
        this.rcvPort = rcvPort;
        this.sessionID = sessionID;
        this.error = error;
    }

    public InitPacketServer(byte[] bytes)
    {
        int index = 0;

        this.initSessionPass = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        this.rcvPort = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        this.sessionID = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        this.error = bytes[index];
    }

    public override byte[] ToByte()
    {
        byte[] ret = new byte[0];

        ret = AddBytes(ret, BitConverter.GetBytes(initSessionPass));
        ret = AddBytes(ret, BitConverter.GetBytes(rcvPort));
        ret = AddBytes(ret, BitConverter.GetBytes(sessionID));
        ret = AddByte(ret, error);

        return ret;
    }
}

public class ActionPacket : Packet
{
    public byte roughID; //アクションのカテゴリを示す
    public byte detailID; //アクションの詳細な種類を示す
    public ushort targetID; //アクションの対象を示す
    public Vector3 pos; //座標データを持つアクションで参照する
    public Vector3 pos2; //座標データを2つ持つアクションで参照する
    public byte msgLength; //msgのバイト数
    public string msg; //文字列データを持つアクションで参照する

    public ActionPacket(byte roughID, byte detailID = 0, ushort targetID = 0, Vector3 pos = new Vector3(), Vector3 pos2 = new Vector3(), string msg = "")
    {
        this.roughID = roughID;
        this.detailID = detailID;
        this.targetID = targetID;
        this.pos = pos;
        this.pos2 = pos2;
        this.msgLength = (byte)Encoding.UTF8.GetByteCount(msg);
        this.msg = msg;
    }

    public ActionPacket(byte[] bytes)
    {
        int index = 0;
        float x, y, z;

        this.roughID = bytes[index];
        index++;
        this.detailID = bytes[index];
        index++;
        this.targetID = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        x = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        y = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        z = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        pos = new Vector3(x, y, z);
        x = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        y = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        z = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        pos2 = new Vector3(x, y, z);
        this.msgLength = bytes[index];
        index++;
        this.msg = Encoding.UTF8.GetString(bytes, index, msgLength);
    }

    public override byte[] ToByte()
    {
        byte[] ret = new byte[0];

        ret = AddByte(ret, roughID);
        ret = AddByte(ret, detailID);
        ret = AddBytes(ret, BitConverter.GetBytes(targetID));
        ret = AddBytes(ret, BitConverter.GetBytes(pos.x));
        ret = AddBytes(ret, BitConverter.GetBytes(pos.y));
        ret = AddBytes(ret, BitConverter.GetBytes(pos.z));
        ret = AddBytes(ret, BitConverter.GetBytes(pos2.x));
        ret = AddBytes(ret, BitConverter.GetBytes(pos2.y));
        ret = AddBytes(ret, BitConverter.GetBytes(pos2.z));
        ret = AddByte(ret, msgLength);
        ret = AddBytes(ret, Encoding.UTF8.GetBytes(msg));

        return ret;
    }
}

public class PositionPacket : Packet
{
    public byte id0;
    public Vector3 pos0;
    public Vector3 forward0;
    public byte id1;
    public Vector3 pos1;
    public Vector3 forward1;
    public byte id2;
    public Vector3 pos2;
    public Vector3 forward2;
    public byte id3;
    public Vector3 pos3;
    public Vector3 forward3;

    public PositionPacket(byte id0, Vector3 pos0, Vector3 forward0, byte id1, Vector3 pos1, Vector3 forward1, byte id2, Vector3 pos2, Vector3 forward2, byte id3, Vector3 pos3, Vector3 forward3)
    {
        this.id0 = id0;
        this.pos0 = pos0;
        this.forward0 = forward0;
        this.id1 = id1;
        this.pos1 = pos1;
        this.forward1 = forward1;
        this.id2 = id2;
        this.pos2 = pos2;
        this.forward2 = forward2;
        this.id3 = id3;
        this.pos3 = pos3;
        this.forward3 = forward3;
    }

    public PositionPacket(byte[] bytes)
    {
        int index = 0;
        float x, y, z;

        //1人目
        this.id0 = bytes[index];
        index++;
        x = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        y = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        z = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        this.pos0 = new Vector3(x, y, z);
        x = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        y = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        z = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        this.forward0 = new Vector3(x, y, z);
        //2人目
        this.id1 = bytes[index];
        index++;
        x = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        y = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        z = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        this.pos1 = new Vector3(x, y, z);
        x = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        y = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        z = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        this.forward1 = new Vector3(x, y, z);
        //3人目
        this.id2 = bytes[index];
        index++;
        x = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        y = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        z = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        this.pos2 = new Vector3(x, y, z);
        x = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        y = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        z = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        this.forward2 = new Vector3(x, y, z);
        //4人目
        this.id3 = bytes[index];
        index++;
        x = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        y = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        z = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        this.pos3 = new Vector3(x, y, z);
        x = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        y = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        z = BitConverter.ToSingle(bytes, index);
        this.forward3 = new Vector3(x, y, z);
    }

    public override byte[] ToByte()
    {
        byte[] ret = new byte[0];

        ret = AddByte(ret, id0);
        ret = AddBytes(ret, BitConverter.GetBytes(pos0.x));
        ret = AddBytes(ret, BitConverter.GetBytes(pos0.y));
        ret = AddBytes(ret, BitConverter.GetBytes(pos0.z));
        ret = AddBytes(ret, BitConverter.GetBytes(forward0.x));
        ret = AddBytes(ret, BitConverter.GetBytes(forward0.y));
        ret = AddBytes(ret, BitConverter.GetBytes(forward0.z));
        ret = AddByte(ret, id1);
        ret = AddBytes(ret, BitConverter.GetBytes(pos1.x));
        ret = AddBytes(ret, BitConverter.GetBytes(pos1.y));
        ret = AddBytes(ret, BitConverter.GetBytes(pos1.z));
        ret = AddBytes(ret, BitConverter.GetBytes(forward1.x));
        ret = AddBytes(ret, BitConverter.GetBytes(forward1.y));
        ret = AddBytes(ret, BitConverter.GetBytes(forward1.z));
        ret = AddByte(ret, id2);
        ret = AddBytes(ret, BitConverter.GetBytes(pos2.x));
        ret = AddBytes(ret, BitConverter.GetBytes(pos2.y));
        ret = AddBytes(ret, BitConverter.GetBytes(pos2.z));
        ret = AddBytes(ret, BitConverter.GetBytes(forward2.x));
        ret = AddBytes(ret, BitConverter.GetBytes(forward2.y));
        ret = AddBytes(ret, BitConverter.GetBytes(forward2.z));
        ret = AddByte(ret, id3);
        ret = AddBytes(ret, BitConverter.GetBytes(pos3.x));
        ret = AddBytes(ret, BitConverter.GetBytes(pos3.y));
        ret = AddBytes(ret, BitConverter.GetBytes(pos3.z));
        ret = AddBytes(ret, BitConverter.GetBytes(forward3.x));
        ret = AddBytes(ret, BitConverter.GetBytes(forward3.y));
        ret = AddBytes(ret, BitConverter.GetBytes(forward3.z));

        return ret;
    }
}
