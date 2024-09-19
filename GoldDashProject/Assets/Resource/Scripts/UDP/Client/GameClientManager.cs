using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

public class GameClientManager : MonoBehaviour
{
    private bool isRunning;

    private UdpGameClient udpGameClient;

    private Queue<byte[]> packetQueue;

    [SerializeField] private ushort sessionPass;

    [SerializeField] private ushort initSessionPass;

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

                packetQueue.Dequeue();
                Debug.Log("処理ィ！削除ォ！");
            }
        }
    }
}
