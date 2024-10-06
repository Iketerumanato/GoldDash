using System;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.XR;

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
    const int MAX_NUM_OF_PLAYER = 4; //プレイヤーの最大人数。4人未満でプレイするときも必ず4で処理する。パケット種別ごとにバイト数は一定でなければならないので。

    public struct PosData //sessionIDと、2つのVector3をまとめるための構造体。4人未満でプレイする場合、値が代入されない領域がnullになることを避けるため、参照型のクラスではなく値型の構造体を使う。
    {
        public ushort sessionID;
        public Vector3 pos;
        public Vector3 forward;

        public PosData(ushort sessionID, Vector3 pos, Vector3 forward) //コンストラクタの引数で変数を初期化可能
        { 
            this.sessionID = sessionID;
            this.pos = pos;
            this.forward = forward;
        }
    }

    public PosData[] posDatas; //変数はこれだけ！

    public PositionPacket() //コンストラクタでコレクションのインスタンスを生成
    { 
        posDatas = new PosData[MAX_NUM_OF_PLAYER]; //必ず4人分の枠を確保する。代入されない部分は初期値で補完される、はずだ。
    }

    public PositionPacket(byte[] bytes) //バイト配列からのコンストラクタは配列に要素を代入するためfor文を使っているものの、やっていることは他と同じ。
    {
        int index = 0;
        ushort sessionID;
        float x, y, z;
        float x2, y2, z2;

        //配列posDatasのサイズ分繰り返す。4回。
        for (int i = 0; i < MAX_NUM_OF_PLAYER; i++)
        {
            sessionID = BitConverter.ToUInt16(bytes, index);
            index += sizeof(ushort);
            x = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
            y = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
            z = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
            x2 = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
            y2 = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
            z2 = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);

            posDatas[i] = new PosData(sessionID, new Vector3(x, y, z), new Vector3(x2, y2, z2));
        }
    }

    public override byte[] ToByte() //配列から要素を読み取るためfor文を使っているものの、やっていることは他と同じ。
    {
        byte[] ret = new byte[0];

        //配列posDatasのサイズ分繰り返す。4回。
        for (int i = 0; i < MAX_NUM_OF_PLAYER; i++)
        {
            ret = AddBytes(ret, BitConverter.GetBytes(posDatas[i].sessionID));
            ret = AddBytes(ret, BitConverter.GetBytes(posDatas[i].pos.x));
            ret = AddBytes(ret, BitConverter.GetBytes(posDatas[i].pos.y));
            ret = AddBytes(ret, BitConverter.GetBytes(posDatas[i].pos.z));
            ret = AddBytes(ret, BitConverter.GetBytes(posDatas[i].forward.x));
            ret = AddBytes(ret, BitConverter.GetBytes(posDatas[i].forward.y));
            ret = AddBytes(ret, BitConverter.GetBytes(posDatas[i].forward.z));
        }

        return ret;
    }
}
