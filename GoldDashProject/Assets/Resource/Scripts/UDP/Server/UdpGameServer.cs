using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

public class UdpGameServer : UdpCommnicator
{
    private ushort sessionPass; //このサーバーに接続するためのパスコード。コンストラクタから取得

    private Dictionary<ushort, IPEndPoint> clientDictionary; //ユーザーに割り当てた固有のIDを鍵とし、現在のIPアドレスを保存
    //脆弱性：ユーザーIDが短すぎて偽装パケットの危険性あり。サーバー性能が貧弱なので我慢。
    //同じsessionIDのパケットが違うIPアドレスから送られてきたら登録を上書きする
    //また、クライアントIPアドレスが変わってしまうケースは考慮しない。卒展の環境なら問題ない。甘える。

    private HashSet<ushort> usedID; //sessionIDの重複防止に使う。使用済IDを記録して新規発行時にはcontainsで調べる
                                    //private List<ushort> reuseID; //IDのオーバーフロー対策

    private ushort serverSessionID; //クライアントにサーバーを判別させるためのsessionID

    private ushort rcvPort; //受信用ポート番号

    //サーバー内部からのインターナルリクエストパケットがマルチスレッドでエンキューされるのでスレッドセーフなConcurrentQueueを使用
    private ConcurrentQueue<Header> output; //パケットをHeaderクラスとして開封し整合性チェックをしてからこのキューに出力する。

    private CancellationTokenSource receiveCts; //パケット受信タスクのキャンセル用
    private CancellationToken token;

    public UdpGameServer(ref ConcurrentQueue<Header> output, ushort sessionPass)
    { 
        this.output = output; //パケット排出用キューをセット
        this.sessionPass = sessionPass;

        //reuseID = new List<ushort>();

        usedID = new HashSet<ushort>(); //ハッシュセットインスタンス生成

        //サーバーIDの生成
        Random random = new Random(); //Receiveメソッド内でSystem.Randomを使わなければならないのでここでもSystem.Randomで統一する
        serverSessionID = (ushort)random.Next(0, 65535); //0から65535までの整数を生成して2バイトにキャスト

        usedID.Add(serverSessionID); //サーバーIDと同じ値をクライアントに与えないように

        clientDictionary = new Dictionary<ushort, IPEndPoint>(); //Dictionaryインスタンス生成

        //ローカルコンピュータのエンドポイント作成
        //ローカルのエンドポイントにバインドしたクライアント作成
        this.localEndPointForSend = new IPEndPoint(GetMyIPAddressIPv4(), GetAvailablePort(START_PORT));
        UnityEngine.Debug.Log($"送信用ローカルエンドポイントを生成しました。 IPアドレス：{localEndPointForSend.Address} ポート：{localEndPointForSend.Port}");
        this.sender = new UdpClient(localEndPointForSend);
        UnityEngine.Debug.Log("送信用UDPクライアントを生成しました。");

        this.localEndPointForReceive = new IPEndPoint(GetMyIPAddressIPv4(), GetAvailablePort(START_PORT));
        UnityEngine.Debug.Log($"受信用ローカルエンドポイントを生成。 IPアドレス：{localEndPointForReceive.Address} ポート：{localEndPointForReceive.Port}");
        this.receiver = new UdpClient(localEndPointForReceive);
        UnityEngine.Debug.Log("受信用UDPクライアントを生成しました。");
        this.rcvPort = (ushort)localEndPointForReceive.Port;

        //タスクとキャンセルトークン
        receiveCts = new CancellationTokenSource();
        token = receiveCts.Token;

        //パケットの受信を非同期で行う
        Task.Run(() => Receive(), token);
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
            //判別するためにHeaderを展開してsessionIDを調べる
            Header receivedHeader = new Header(receivedData);

            if (clientDictionary.ContainsKey(receivedHeader.sessionID)) //登録済なら
            {
                UnityEngine.Debug.Log("登録済クライアントからのパケットです。エンキューします。");
                output.Enqueue(receivedHeader); //せっかく展開したので、Headerの状態でエンキューする
            }
            else
            {
                UnityEngine.Debug.Log("未知のリモートコンピュータからのパケットです。パケットを精査します。");

                if (RegisterClient(receivedHeader, remoteEndPoint.Address))
                {
                    output.Enqueue(receivedHeader);
                }
                else
                {
                    UnityEngine.Debug.Log("当該リモートコンピュータをクライアントと確認できませんでした。パケットを破棄します。");
                }
            }
        }

        //リモートをハッシュリストに登録
        bool RegisterClient(Header receivedHeader, IPAddress addr)
        {
            //未知のクライアントから送られてくるパケットの種類を調べる
            switch (receivedHeader.packetType)
            {
                case (byte)Definer.PT.IPC: //initパケットなら開封する
                    //基本的にこのクラスでHeader.dataは参照しないのだが、InitパケットはsessionPassを見る必要がある
                    InitPacketClient receivedData = new InitPacketClient(receivedHeader.data);

                    //既にこのInitPacketは処理済かもしれないので辞書を精査
                    foreach (KeyValuePair<ushort, IPEndPoint> k in clientDictionary)
                    {
                        //このInitPacketの送り主のアドレスにバインドされたIPEndPointがclientDictionaryに既にないか確かめる
                        //4人プレイゲームなので、最大値登録数は4のはず、なのでこれでも良いはずだ。数万人のプレイヤーがいるとまずい。
                        if (k.Value.Address.ToString().Equals(addr.ToString()))
                        {
                            UnityEngine.Debug.Log("当該リモートコンピュータは、精査したところ既知のクライアントでした。ヘッダを編集し、エンキューします。");
                            receivedHeader.sessionID = k.Key;
                            return true;
                        }
                    }

                    if (receivedData.sessionPass == this.sessionPass) //パスワードが正しければ
                    {
                        //重複しないsessionIDを作る
                        ushort sessionID;
                        do
                        {
                            Random random = new Random(); //UnityEngine.Randomはマルチスレッドで使用できないのでSystemを使う
                            sessionID = (ushort)random.Next(0, 65535); //0から65535までの整数を生成して2バイトにキャスト
                        }
                        while (usedID.Contains(sessionID)); //使用済IDと同じ値を生成してしまったならやり直し
                        //ここかなり粗製です。クライアントが65000人くらいになるとほぼ猿になる。4人プレイ用なのでこれで。
                        //マジで数万人のクライアントを捌く場合は予め生成したIDを1個ずつ割り振っていくか、ハッシュ関数などを使うしかない。

                        //IDを決めてdictionaryに書き込む
                        clientDictionary.Add(sessionID, new IPEndPoint(addr, receivedData.rcvPort)); //受信用ポート番号でIPEndPointを登録
                        usedID.Add(sessionID);
                        //IDのオーバーフローは今は対策しない
                        //というか、切断するたびにDictionaryがデカくなっていくので
                        //一定時間パケットを送ってきていないIPEndPointは登録を抹消したい。

                        //最後に、作ってあげたsessionIDをHeaderに書き込んであげる
                        receivedHeader.sessionID = sessionID;

                        UnityEngine.Debug.Log("当該リモートコンピュータをクライアントと確認し、登録しました。エンキューします。");
                        return true;
                    }
                    break;

                default: //そうでないならsessionIDを知らないのにInitPacket以外を送ってきていることになるのでおかしい。
                    //ここでエラーコードを返すことは、ハッカーにヒントを与えることになるらしい。のでなにもしない。
                    break;
            }

            return false;
        }
    }

    public void RemoveClientFromDictionary(ushort sessionID)
    { 
        clientDictionary.Remove(sessionID);
        usedID.Remove(sessionID);
    }

    public ushort GetReceivePort() { return rcvPort; }

    public ushort GetServerSessionID() { return serverSessionID; }

    public override void Dispose()
    {
        //Taskのキャンセル処理など
        if (receiveCts != null) receiveCts.Cancel();
        sender.Dispose();
        receiver.Dispose();
    }
}
