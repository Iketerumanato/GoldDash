using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public abstract class UdpCommnicator
{
    protected const int START_PORT = 60000; //はじめに使用を試みるポート番号

    protected IPEndPoint localEndPointForSend; //自分の送信用エンドポイント
    protected IPEndPoint localEndPointForReceive; //自分の受信用エンドポイント。別に送信用と分けなくてもいいんだけど分けるとポートの仕事量に余裕が生まれる

    protected UdpClient sender; //送信用クライアント
    protected UdpClient receiver; //受信用クライアント

    protected Queue<byte[]> output; //パケットをバイト配列にしてこのキューに出力する

    public abstract void Send(byte[] sendData);

    public abstract void Receive();

    //ローカルのIPv4用IPアドレスを取得する。
    protected IPAddress GetMyIPAddressIPv4()
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
    protected int GetAvailablePort(int startPort)
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
