using TMPro;
using UnityEngine;
using R3;

public class UdpTextWriter : MonoBehaviour
{
    //テキスト
    [SerializeField] private TextMeshProUGUI stateMessage;
    public void InitObservation(UdpButtonManager udpUIManager)
    {
        udpUIManager.udpUIManagerSubject.Subscribe(e => ProcessUdpManagerEvent(e));
    }

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

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_CLIENT_CONNECT:
                stateMessage.text = "CLIENT MODE : RUNNING";
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
}
