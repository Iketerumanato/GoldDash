using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class UdpGameClient : UdpCommnicator
{
    private const int BROADCAST_RANGE = 4; //ブロードキャスト送信時相手のポート番号がわからないのでSTART_PORT番～START_PORT + BROADCAST_RANGE番までのポートに一つずつ送って反応を伺う
    private const int WAIT_RESPONSE_TIME = 100; //ブロードキャスト送信時、リモートコンピュータからのレスポンスをWAIT_RESPONSE_TIMEミリ秒待ち、レスポンスがなければ新たなポートにブロードキャスト送信をする
    private const int NUM_OF_RETRY_BROADCAST = 2; //ブロードキャスト送信時、リモートコンピュータからのレスポンスが確認できなかったときNUM_OF_RETRY_BROADCAST回再送する

    protected ushort initSessionPass; //初回通信時にプレイヤー側から送るセッションパス。得られたレスポンスがサーバーからのものであると断定するときに使う。その後は使わない。

    private ushort serverSessionID; //サーバーから渡された、サーバーのsessionID

    private IPEndPoint serverEndpoint; //サーバの受信用エンドポイント

    public ushort rcvPort; //自分の受信用ポート番号。GameClientManagerから読み取るためpublic

    private Queue<Header> output; //パケットをHeaderクラスとして開封し整合性チェックをしてからこのキューに出力する

    private CancellationTokenSource receiveCts; //パケット受信タスクのキャンセル用
    private CancellationToken ReceiveCts
    {
        get
        { 
            receiveCts = new CancellationTokenSource();
            return receiveCts.Token;
        }
    }

    public UdpGameClient(ref Queue<Header> output, ushort initSessionPass)
    {
        serverEndpoint = null; //サーバーを見つけていないときはnullになっていることを前提としているので明示的に代入
        this.output = output; //パケット排出用キューをセット
        this.initSessionPass = initSessionPass;

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

        //パケットの受信を非同期で行う
        Task.Run(() => Receive(), ReceiveCts);
    }

    public override void Send(byte[] sendData)
    {
        //サーバーが登録されていないならブロードキャスト送信する
        if (serverEndpoint == null)
        {
            UnityEngine.Debug.Log("サーバーの登録がないため、ブロードキャスト送信を行います。");

            //ポート番号を変えながらブロードキャスト　同じポート番号に対して数回送信するためfor文の変化式には何も書いていない
            //START_PORTは送信用に使われていると考えられるので+1する
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
                        UnityEngine.Debug.Log($"サーバーの登録が確認されました。ブロードキャスト送信を終了します。");
                        remotePort = START_PORT + BROADCAST_RANGE;
                        return;
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"サーバーの登録が確認できませんでした。あと{NUM_OF_RETRY_BROADCAST - retryCount}回再送します。");
                    }
                }
                //次のポート番号へ
                remotePort++;
            }
            UnityEngine.Debug.Log($"サーバーの登録が確認できませんでした。ブロードキャスト送信を終了します。");
        }
        //サーバーが登録されているならサーバーに送信する
        else
        {
            sender.Send(sendData, sendData.Length, serverEndpoint);
            UnityEngine.Debug.Log($"{sendData.Length}バイト以上のパケットをサーバーに送信しました。");
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
            //判別するためにHeaderを展開してsessionIDを調べる
            Header receivedHeader = new Header(receivedData);

            //ここはもともとIPEndPoint情報で判別していたのだが、クライアントに致命的な脆弱性をもたらすのでsessionIDを使う方向で
            if (receivedHeader.sessionID == serverSessionID)
            {
                UnityEngine.Debug.Log("サーバーからのパケットです。エンキューします。");
                output.Enqueue(receivedHeader); //せっかく展開したので、Headerの状態でエンキューする
            }
            else if (serverEndpoint != null)
            {
                UnityEngine.Debug.Log("サーバーからのパケットではありません。サーバーを既に登録既なので、パケットを破棄します。");
            }
            else
            {
                UnityEngine.Debug.Log("未知のリモートコンピュータからのパケットです。パケットを精査します。");

                if (RegisterServer(receivedHeader, remoteEndPoint.Address))
                {
                    UnityEngine.Debug.Log("該当リモートコンピュータをサーバーと確認し、登録しました。エンキューします。");
                    output.Enqueue(receivedHeader);
                }
                else
                {
                    UnityEngine.Debug.Log("該当リモートコンピュータをサーバーと確認できませんでした。パケットを破棄します。");
                }
            }
        }

        //リモートをハッシュリストに登録
        bool RegisterServer(Header receivedHeader, IPAddress addr)
        {
            //基本的にこのクラスでHeader.dataは参照しないのだが、InitパケットはsessionPassを見る必要がある
            InitPacketServer receivedData = new InitPacketServer(receivedHeader.data);

            if (receivedData.initSessionPass == initSessionPass) //相手が自分の送信した初期化用セッションパスをそのまま返して来たら
            {
                //サーバーのエンドポイント情報を登録
                serverEndpoint = new IPEndPoint(addr, receivedData.rcvPort);
                //サーバーのsessionIDを記録
                serverSessionID = receivedHeader.sessionID;
                return true;
            }

            //そうでないならsessionIDを知らないのにInitPacket以外を送ってきていることになるのでおかしい。
            //ここでエラーコードを返すことは、ハッカーにヒントを与えることになるらしい。のでなにもしない。
            return false;
        }
    }

    public override void Dispose()
    {
        //Taskのキャンセル処理など
        receiveCts.Cancel();
        sender.Dispose();
        receiver.Dispose();
    }
}
