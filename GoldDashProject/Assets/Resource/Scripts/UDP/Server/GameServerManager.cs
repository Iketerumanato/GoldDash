using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

public class GameServerManager : MonoBehaviour
{
    private bool isRunning;

    private UdpGameServer udpGameServer;

    private Queue<byte[]> packetQueue;
     
    [SerializeField] private ushort sessionPass;

    public void InitObservation(UdpButtonManager udpUIManager)
    {
        packetQueue = new Queue<byte[]>();

        udpUIManager.udpUIManagerSubject.Subscribe(e => ProcessUdpManagerEvent(e));
    }

    private void ProcessUdpManagerEvent(UdpButtonManager.UDP_BUTTON_EVENT e)
    {
        switch (e)
        {
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_START_SERVER_MODE:
                udpGameServer = new UdpGameServer(ref packetQueue, sessionPass);
                break;
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_SERVER_ACTIVATE:
                if (udpGameServer == null) udpGameServer = new UdpGameServer(ref packetQueue, sessionPass);
                isRunning = true;
                break;
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_SERVER_DEACTIVATE:
                udpGameServer.Dispose();
                isRunning = false;
                break;
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_BACK_TO_SELECT:
                if (udpGameServer != null) udpGameServer.Dispose();
                isRunning = false;
                break;
            default:
                break;
        }
    }

    private void Start()
    {
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

                //パケットを取り出す
                byte[] receivedBytes = packetQueue.Dequeue();

                Debug.Log("処理ィ！削除ォ！");

                //まずHeaderを取り出す
                Header header = new Header(receivedBytes);

                switch (header.packetType)
                {
                    case (byte)PacketBuilder.PACKET_TYPE.INIT_PACKET_CLIENT:
                        //InitPacketを受け取ったときの処理
                        break;
                    case (byte)PacketBuilder.PACKET_TYPE.ACTION_PACKET:
                        //ActionPacketを受け取ったときの処理
                        break;
                    default:
                        Debug.Log($"{(PacketBuilder.PACKET_TYPE)header.packetType}はサーバーでは処理できません。処理を終了します。");
                        break;
                }
            }
        }
    }
}
