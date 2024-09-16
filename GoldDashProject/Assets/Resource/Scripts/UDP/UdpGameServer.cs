using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System;

public class UdpGameServer : UdpCommnicator
{
    private Dictionary<ushort, IPEndPoint> clientDictionary;

    private ushort sessionPass;

    private ushort giveID;
    //private List<ushort> reuseID; //IDのオーバーフロー対策

    public UdpGameServer(ref Queue<byte[]> output, ushort sessionPass)
    { 
        this.output = output;
        this.sessionPass = sessionPass;

        giveID = 0;
        //reuseID = new List<ushort>();

        //ローカルコンピュータのエンドポイント作成
        //ローカルのエンドポイントにバインドしたクライアント作成
        this.localEndPointForSend = new IPEndPoint(GetMyIPAddressIPv4(), GetAvailablePort(START_PORT));
        UnityEngine.Debug.Log($"送信用ローカルエンドポイントを生成しました。　IPアドレス：{localEndPointForSend.Address}　ポート：{localEndPointForSend.Port}");
        this.sender = new UdpClient(localEndPointForSend);
        UnityEngine.Debug.Log("送信用UDPクライアントを生成しました。");

        this.localEndPointForReceive = new IPEndPoint(GetMyIPAddressIPv4(), GetAvailablePort(START_PORT));
        UnityEngine.Debug.Log($"受信用ローカルエンドポイントを生成。　IPアドレス：{localEndPointForReceive.Address}　ポート：{localEndPointForReceive.Port}");
        this.receiver = new UdpClient(localEndPointForReceive);
        UnityEngine.Debug.Log("受信用UDPクライアントを生成しました。");

        //パケットを出力先（外部クラスの持つキューの参照）をセット
        this.output = output;
    }

    public override void Send(byte[] sendData)
    {
        //クライアントがいないなら送信しない
        if (clientDictionary.Count == 0) 
        {
            UnityEngine.Debug.Log("クライアントの登録がないためパケットを送信できません。");
            return;
        }
        //クライアントがいるなら一斉送信する
        foreach (KeyValuePair<ushort, IPEndPoint> k in clientDictionary)
        {
            sender.Send(sendData, sendData.Length, k.Value);
        }
    }

    public override void Receive()
    {

        //パケット送信者のIPEndPoint。あらゆるIPアドレス、あらゆるポートを認める
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            //デバッグログ出力
            UnityEngine.Debug.Log("リッスン状態に入ります。");

            //受信したデータを保管
            byte[] receivedData = receiver.Receive(ref remoteEndPoint);

            //自分自身からのブロードキャスト送信はここで弾く
            if (remoteEndPoint.Address.Equals(GetMyIPAddressIPv4()))
            {
                UnityEngine.Debug.Log("ローカルコンピュータからのパケットを受信しました。パケットを破棄します。");
                continue;
            }

            UnityEngine.Debug.Log("パケットを受信しました。処理します。");

            //通信相手一覧に登録された相手からのパケットならエンキューする、そうでないなら登録処理またはパケット破棄
            if (clientDictionary.ContainsValue(remoteEndPoint))
            {
                UnityEngine.Debug.Log("登録済クライアントからのパケットです。エンキューします。");
                output.Enqueue(receivedData);
            }
            else
            {
                UnityEngine.Debug.Log("未知のリモートエンドポイントからのパケットです。パケットを精査します。");
                if(RegisterClient(receivedData, remoteEndPoint.Address)) output.Enqueue(receivedData);
            }
        }

        //リモートをハッシュリストに登録
        bool RegisterClient(byte[] receivedData, IPAddress addr)
        {
            //未知のクライアントから送られてくるパケットの種類を調べる
            switch (receivedData[6]) //7バイト目にパケット種別が書かれているので
            {
                case 0: //initパケットなら
                    if (BitConverter.ToUInt16(receivedData, 0) == sessionPass) //パスワードが正しければ
                    {
                        //IDを決めてdictionaryに書き込む
                        clientDictionary.Add(giveID, new IPEndPoint(addr, BitConverter.ToUInt16(receivedData, 2)));
                        giveID++;
                        //IDのオーバーフローは今は対策しない
                        return true;
                    }
                    break;

                default: //そうでないならsessionIDからこのゲーム用のパケットなのか調べて、dictionaryを編集
                    break;
            }

            return false;
        }
    }
}
