using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;

public class GameServerManager : MonoBehaviour
{
    bool isRunning;

    UdpGameServer udpGameServer;

    private Queue<byte[]> packetQueue;
     
    [SerializeField] private ushort sessionPass;

    public void InitObservation(UdpButtonManager udpUIManager)
    {
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
}
