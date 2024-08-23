using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Linq;
using System.Threading.Tasks;
using System;

public class UdpCommunicator
{
    private const int START_PORT = 60000; //はじめに使用を試みるポート番号
    private const int BROADCAST_RANGE = 10; //ブロードキャスト送信時相手のポート番号がわからないのでSTART_PORT番～START_PORT + BROADCAST_RANGE番までのポートに一つずつ送って反応を伺う
    private const int WAIT_RESPONSE_TIME = 500; //ブロードキャスト送信時、リモートコンピュータからのレスポンスをWAIT_RESPONSE_TIMEミリ秒待ち、レスポンスがなければ新たなポートにブロードキャスト送信をする
    private const int NUM_OF_RETRY_BROADCAST = 2; //ブロードキャスト送信時、リモートコンピュータからのレスポンスが確認できなかったときNUM_OF_RETRY_BROADCAST回再送する

    private IPEndPoint localEndPointForSend; //自分の送信用エンドポイント
    private IPEndPoint localEndPointForReceive; //自分の受信用エンドポイント。別に送信用と分けなくてもいいんだけど分けるとポートの仕事量に余裕が生まれる

    private HashSet<IPEndPoint> remoteEndPoints; //通信相手のエンドポイントをまとめるハッシュセット。クライアント情報の重複対策でもある

    private UdpClient sender; //送信用クライアント
    private UdpClient receiver; //受信用クライアント

    private Queue<byte[]> output; //外部
    private readonly int numOfRequiredRemoteEndPoints;

    private bool findingRemoteEndPoints; //通信相手を募集中ならtrue、募集を閉め切ったらfalse

    UInt16 giveID = 0;

    //コンストラクタ
    public UdpCommunicator(ref Queue<byte[]> output, int numOfRequiredClients)
    {
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
        //リモートエンドポイント必要数を書き込み
        this.numOfRequiredRemoteEndPoints = numOfRequiredClients;

        //ハッシュセット作成。まだ空っぽ
        remoteEndPoints = new HashSet<IPEndPoint>();
        //通信相手の募集開始
        StartListen();

        //受信用スレッド作成
        Task.Run(() => Receive());
        UnityEngine.Debug.Log("パケット受信用の非同期処理を開始します。");
    }

    //publicメソッド
    //パケットを送信
    public void Send(byte[] sendData)
    {
        //リモートエンドポイントのハッシュセットに誰も登録されていないならLAN内にブロードキャスト送信する。誰か登録されているならその全員に送る。
        if (remoteEndPoints.Count() == 0)
        {
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

                    if (remoteEndPoints.Count != 0)
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
        else
        {
            UnityEngine.Debug.Log("登録済のリモートエンドポイントに対してパケットを送信します。");
            foreach (IPEndPoint ep in remoteEndPoints)
            {
                sender.Send(sendData, sendData.Length, ep);
                UnityEngine.Debug.Log($"{sendData.Length}バイト以上のパケットを送信しました。");
            }
        }
    }

    public bool HasRemoteEndPoint()
    { 
        return remoteEndPoints.Count > 0;
    }

    public UInt16 GetReceivePort()
    {
        return (UInt16)this.localEndPointForReceive.Port;
    }

    //クライアントが切断されたときに呼び出す予定のメソッド。任意のエンドポイントを指定して、リモートエンドポイントのハッシュセットから抹消する。
    public void RemoveRemoteEndPoint(IPEndPoint ep)
    {
        if (remoteEndPoints.Contains(ep))
        {
            remoteEndPoints.Remove(ep);
            UnityEngine.Debug.Log("1個のリモートエンドポイントの登録を解除しました。");

            //リモートエンドポイントの必要数が足りなくなったら募集再開
            if (remoteEndPoints.Count() < numOfRequiredRemoteEndPoints)
            {
                StartListen();
            }
        }
    }

    //privateメソッド
    //受信用スレッドで実行するメソッド
    private void Receive()
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

            if (remoteEndPoints.Contains(remoteEndPoint))
            {
                UnityEngine.Debug.Log("登録済リモートエンドポイントからのパケットです。エンキューします。");
                output.Enqueue(receivedData);
            }
            else
            {
                if (findingRemoteEndPoints)
                {
                    UnityEngine.Debug.Log("未知のリモートエンドポイントからのパケットです。パケットを精査します。");
                    RegisterClient(receivedData);
                }
                else
                {
                    UnityEngine.Debug.Log("未知のリモートエンドポイントからのパケットです。現在新たなリモートエンドポイントを募集していないため、パケットを破棄します。");
                    continue;
                }
            }
        }

        //リモートをハッシュリストに登録
        void RegisterClient(byte[] receivedData)
        {
            //receivedDataを元にパケットの精査をして、想定しているリモートエンドポイントからのパケットであればエンキュー
            if (CommData.CheckKeyWord(receivedData))
            {
                UnityEngine.Debug.Log("キーワードの一致を確認。IDを与え、リモートエンドポイント情報を登録します。");
                //output.Enqueue(receivedData);


                UnityEngine.Debug.Log("パケットを開封し、受信用ポートの情報を取得して登録します。");
                remoteEndPoints.Add(new IPEndPoint(remoteEndPoint.Address, CommData.GetPort(receivedData)));
                UnityEngine.Debug.Log($"現在{remoteEndPoints.Count()}個のリモートエンドポイントが登録されています。");

                sender.Send(new CommData(giveID, new CommData.POS_DATA[4]).ToByte(), new CommData(giveID, new CommData.POS_DATA[4]).ToByte().Length, new IPEndPoint(remoteEndPoint.Address, CommData.GetPort(receivedData)));
                giveID++;
            }
            else
            {
                UnityEngine.Debug.Log("キーワードが一致しないため、パケットを破棄します。");
            }
        }
    }

    private void StartListen()
    {
        UnityEngine.Debug.Log($"{numOfRequiredRemoteEndPoints - remoteEndPoints.Count()}個のリモートエンドポイントの募集を開始します。");
        findingRemoteEndPoints = true;
    }

    private void StopLesten()
    {
        UnityEngine.Debug.Log($"{numOfRequiredRemoteEndPoints}個のリモートエンドポイントが登録されたため、リモートエンドポイントの募集を終了します。");
        findingRemoteEndPoints = false;
    }

    //ローカルのIPv4用IPアドレスを返す。
    private IPAddress GetMyIPAddressIPv4()
    {
        IPAddress ret = null;

        IPAddress[] addrs = Dns.GetHostAddresses(Dns.GetHostName());

        foreach (IPAddress addr in addrs)
        {
            //IPv4用アドレスであるか調べる
            if (addr.AddressFamily.Equals(AddressFamily.InterNetwork))
            {
                //IPv4用アドレスを見つけたら返却
                ret = addr;
                break;
            }
        }

        return ret;
    }

    //使用可能なポート番号を返す。参考：https://note.dokeep.jp/post/csharp-get-active-port/
    private int GetAvailablePort(int startPort)
    {
        //ローカルのネットワーク接続情報を取得
        IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

        //アクティブなTCPコネクションを取得。IEnumインターフェースで返ってくるので配列にする
        IPEndPoint[] tcpConnections = ipGlobalProperties.GetActiveTcpConnections().Select(x => x.LocalEndPoint).ToArray();
        //すべてのTCPリスナーを取得する
        IPEndPoint[] tcpListeners = ipGlobalProperties.GetActiveTcpListeners();
        //すべてのUDPリスナーを取得する
        IPEndPoint[] udpListeners = ipGlobalProperties.GetActiveUdpListeners();

        //Containsの計算量を減らすためリストではなくハッシュセットを作り、上記のエンドポイント配列をくっつけて格納する
        //これもSelect関数がIEnumインターフェースを返してくるが、ハッシュセットのコンストラクタが適当に処理してくれる
        HashSet<int> activePorts = new HashSet<int>(
                                                    tcpConnections.
                                                    Concat(tcpListeners).
                                                    Concat(udpListeners). //ここまで配列の合成
                                                    Where(ipEndPoint => ipEndPoint.Port >= startPort).//startPort以降のエンドポイントを絞り込む
                                                    Select(_ipEndPoint => _ipEndPoint.Port)//絞り込んだIPEndPointから、ドット演算子でポート番号をゲット
                                                    );

        //startPortで指定した番号以降のポートについて、使用済ポートのハッシュセットに含まれない（最小の）ポート番号を探して返す
        for (int port = startPort; port <= 65535; port++)
        {
            if (!activePorts.Contains(port))
                return port;
        }
        //見つからなかったら-1を返す
        return -1;
    }
}

/*
～落書き～
ForEach(item => 処理)メソッドがList<T>でしか定義されてなくて厭なきもちになりました。笑
検証記事 https://dasuma20.hatenablog.com/entry/cs/type-of-speed によれば、ForEachの処理はどのコレクション型で実行してもパフォーマンスあんまり変わらないらしいッスよ？
てなわけで、正義のもとにIEnumerable<T>を拡張してForEachメソッドを実装してしまおうと思い、しかし思いとどまりました。
『拡張メソッドは対象とする型の提供者以外は作成するべきではありません』https://yone-ken.hatenadiary.org/entry/20090304/p1

public static class EnumerableExtensionMethods
{
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }
}
*/
