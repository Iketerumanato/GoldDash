using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

public class GameClientManager : MonoBehaviour
{
    private bool isRunning; //稼働中か

    private UdpGameClient udpGameClient; //UdpCommunicatorを継承したUdpGameClientのインスタンス

    private Queue<Header> packetQueue; //udpGameClientは”勝手に”このキューにパケットを入れてくれる。不正パケット処理なども済んだ状態で入る。

    [SerializeField] private ushort sessionPass; //サーバーに接続するためのパスコード

    [SerializeField] private ushort initSessionPass; //初回通信時、サーバーからの返信が安全なものか判別するためのパスコード

    private ushort sessionID; //自分のセッションID。サーバー側で決めてもらう。

    #region ボタンが押されたら有効化したり無効化したり
    public void InitObservation(UdpButtonManager udpUIManager)
    {
        udpUIManager.udpUIManagerSubject.Subscribe(e => ProcessUdpManagerEvent(e));
    }

    private void ProcessUdpManagerEvent(UdpButtonManager.UDP_BUTTON_EVENT e)
    {
        switch (e)
        {
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_START_CLIENT_MODE:
                udpGameClient = new UdpGameClient(ref packetQueue, initSessionPass);
                break;
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_CLIENT_CONNECT:
                if (udpGameClient == null) udpGameClient = new UdpGameClient(ref packetQueue, initSessionPass);

                //Initパケット送信
                udpGameClient.Send(new Header(0, 0, 0, 0, (byte)PacketDefiner.PACKET_TYPE.INIT_PACKET_CLIENT, new InitPacketClient(sessionPass, udpGameClient.rcvPort, initSessionPass, "").ToByte()).ToByte());

                isRunning = true;
                break;
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_CLIENT_DISCONNECT:
                udpGameClient.Dispose();
                isRunning = false;
                break;
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_BACK_TO_SELECT:
                if (udpGameClient != null) udpGameClient.Dispose();
                isRunning = false;
                break;
            default:
                break;
        }
    }
    #endregion

    private void Start()
    {
        packetQueue = new Queue<Header>();

        Task.Run(() => ProcessPacket());
    }

    private async void ProcessPacket()
    {
        while (true)
        {
            await UniTask.WaitUntil(() => isRunning);

            while (isRunning)
            {
                //キューにパケットが入るのを待つ
                await UniTask.WaitUntil(() => packetQueue.Count > 0);

                Header receivedHeader = packetQueue.Dequeue();

                Debug.Log("パケットを受け取ったぜ！開封するぜ！");

                Debug.Log($"ヘッダーを確認するぜ！パケット種別は{(PacketDefiner.PACKET_TYPE)receivedHeader.packetType}だぜ！");

                switch (receivedHeader.packetType)
                {
                    case (byte)PacketDefiner.PACKET_TYPE.INIT_PACKET_SERVER:

                        //InitPacketを受け取ったときの処理
                        Debug.Log($"Initパケットを処理するぜ！SessionIDを受け取るぜ！");

                        //クラスに変換する
                        InitPacketServer receivedInitPacket = new InitPacketServer(receivedHeader.data);

                        sessionID = receivedInitPacket.sessionID; //自分のsessionIDを受け取る
                        Debug.Log($"sessionID:{sessionID}を受け取ったぜ。");

                        //エラーコードがあればここで処理
                        break;
                    case (byte)PacketDefiner.PACKET_TYPE.ACTION_PACKET:
                        //ActionPacketを受け取ったときの処理
                        break;
                    default:
                        Debug.Log($"{(PacketDefiner.PACKET_TYPE)receivedHeader.packetType}はクライアントでは処理できないぜ。処理を終了するぜ。");
                        break;
                }
            }
        }
    }
}
