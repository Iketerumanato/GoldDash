using TMPro;
using UnityEngine;
using R3;

public class UdpTextWriter : MonoBehaviour
{
    //テキスト
    [SerializeField] private TextMeshProUGUI stateMessage;
    [SerializeField] private TextMeshProUGUI stateMessageMini;
    //public void InitObservation(UdpButtonManager udpUIManager, GameServerManager gameServerManager, GameClientManager gameClientManager)
    //{
    //    udpUIManager.udpUIManagerSubject.Subscribe(e => ProcessUdpManagerEvent(e));
    //    gameServerManager.ServerInternalSubject.Subscribe(e => ProcessServerInternalEvent(e));
    //    gameClientManager.ClientInternalSubject.Subscribe(e => ProcessClientInternalEvent(e));
    //}

    private void ProcessUdpManagerEvent(UdpButtonManager.UDP_BUTTON_EVENT e)
    {
        switch (e)
        {
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_START_SERVER_MODE:
                stateMessage.text = "SERVER MODE : IDLING";
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_SERVER_ACTIVATE:
                stateMessage.text = "SERVER MODE : RUNNING";
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_SERVER_DEACTIVATE:
                stateMessage.text = "SERVER MODE : IDLING";
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_START_CLIENT_MODE:
                stateMessage.text = "CLIENT MODE : IDLING";
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_CLIENT_DISCONNECT:
                stateMessage.text = "CLIENT MODE : IDLING";
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_BACK_TO_SELECT:
                stateMessage.text = "MODE SELECTION";
                break;

            default:
                break;
        }
    }

    private void ProcessServerInternalEvent(GameServerManager.SERVER_INTERNAL_EVENT e)
    {
        switch (e)
        {
            case GameServerManager.SERVER_INTERNAL_EVENT.EDIT_GUI_FOR_GAME:
                stateMessageMini.text = "STABLE";
                break;
            default:
                break;
        }
    }

    //private void ProcessClientInternalEvent(GameClientManager.CLIENT_INTERNAL_EVENT e)
    //{
    //    switch (e)
    //    {
    //        case GameClientManager.CLIENT_INTERNAL_EVENT.EDIT_GUI_FOR_GAME:
    //            stateMessageMini.text = "STABLE";
    //            break;
    //        case GameClientManager.CLIENT_INTERNAL_EVENT.COMM_ESTABLISHED:
    //            stateMessage.text = "CLIENT MODE : RUNNING";
    //            break;
    //        default:
    //            break;
    //    }
    //}
}
