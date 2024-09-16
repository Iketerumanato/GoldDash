using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class UdpGameClient : UdpCommnicator
{
    private const int BROADCAST_RANGE = 5; //ブロードキャスト送信時相手のポート番号がわからないのでSTART_PORT番～START_PORT + BROADCAST_RANGE番までのポートに一つずつ送って反応を伺う
    private const int WAIT_RESPONSE_TIME = 100; //ブロードキャスト送信時、リモートコンピュータからのレスポンスをWAIT_RESPONSE_TIMEミリ秒待ち、レスポンスがなければ新たなポートにブロードキャスト送信をする
    private const int NUM_OF_RETRY_BROADCAST = 2; //ブロードキャスト送信時、リモートコンピュータからのレスポンスが確認できなかったときNUM_OF_RETRY_BROADCAST回再送する

    private ushort mySessionID;

    IPEndPoint serverEndpoint;

    public UdpGameClient(ref Queue<byte[]> output)
    {
        serverEndpoint = null;
        this.output = output;

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

        Task.Run(() => Receive());
    }

    public override void Send(byte[] sendData)
    {
        //サーバーが登録されていないならブロードキャスト送信する
        if (serverEndpoint == null)
        {
            UnityEngine.Debug.Log("サーバの登録がないため、ブロードキャスト送信を行います。");

            //ポート番号を変えながらブロードキャスト　同じポート番号に対して数回送信するためfor文の変化式には何も書いていない
            UnityEngine.Debug.Log("リモートエンドポイントの登録がないため、ブロードキャスト送信を行います。");

            for (int remotePort = START_PORT + 1; remotePort <= START_PORT + BROADCAST_RANGE;)
            {
                UnityEngine.Debug.Log($"{remotePort}番のポートを対象にブロードキャスト送信を行います。");
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Broadcast, remotePort);

                //パケットロスを考慮して、数回トライする
                for (int retryCount = 0; retryCount <= NUM_OF_RETRY_BROADCAST; retryCount++)
                {
                    sender.Send(sendData, sendData.Length, remoteEndPoint);
                    UnityEngine.Debug.Log($"{sendData.Length}バイト以上のパケットを送信しました。");

                    UnityEngine.Debug.Log($"ブロードキャスト送信に対するレスポンスを{WAIT_RESPONSE_TIME}ミリ秒待ちます・・・");
                    Task waitResponse = Task.Delay(WAIT_RESPONSE_TIME);
                    waitResponse.Wait();

                    if (serverEndpoint != null)
                    {
                        UnityEngine.Debug.Log($"リモートエンドポイントの登録が確認されました。ブロードキャスト送信を終了します。");
                        remotePort = START_PORT + BROADCAST_RANGE;
                        break;
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"リモートエンドポイントの登録が確認できませんでした。あと{NUM_OF_RETRY_BROADCAST - retryCount}回再送します。");
                    }
                }
                //次のポート番号へ
                remotePort++;
            }
        }
        //サーバーが登録されているならサーバーに送信する
        else
        {
            sender.Send(sendData, sendData.Length, serverEndpoint);
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

            //サーバーのパケットならエンキューする、そうでないなら登録処理またはパケット破棄
            if (remoteEndPoint.Equals(serverEndpoint))
            {
                UnityEngine.Debug.Log("登録済クライアントからのパケットです。エンキューします。");
                output.Enqueue(receivedData);
            }
            else
            {
                UnityEngine.Debug.Log("未知のリモートエンドポイントからのパケットです。パケットを精査します。");
                if (RegisterClient(receivedData, remoteEndPoint.Address)) output.Enqueue(receivedData);
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

    public override void Dispose()
    {
        //Taskのキャンセル処理など
    }
}
