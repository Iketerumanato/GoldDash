using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;

public class UdpTextWriter : MonoBehaviour
{
    //テキスト
    [SerializeField] private TextMeshProUGUI stateMessage;
    [SerializeField] private TextMeshProUGUI GeneralMessage;
    [SerializeField] private TextMeshProUGUI ImportantMessage;
    [SerializeField] private TextMeshProUGUI informations;
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
                GeneralMessage.text = "Server mode ready. To activate the server, press ACTIVATE button.";
                ImportantMessage.text = "";
                informations.text = "";
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_SERVER_ACTIVATE:
                stateMessage.text = "SERVER MODE : RUNNING";
                GeneralMessage.text = "Running in Server mode. To start the stop process, press DEACTIVATE button.";
                ImportantMessage.text = "";
                informations.text = "";
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_SERVER_DEACTIVATE:
                stateMessage.text = "SERVER MODE : IDLING";
                GeneralMessage.text = "Server mode ended and ready. To activate the server, press ACTIVATE button.";
                ImportantMessage.text = "";
                informations.text = "";
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_START_CLIENT_MODE:
                stateMessage.text = "CLIENT MODE : IDLING";
                GeneralMessage.text = "Client mode ready. To try connecting to the server, press CONNECT button.";
                ImportantMessage.text = "";
                informations.text = "";
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_CLIENT_CONNECT:
                stateMessage.text = "CLIENT MODE : RUNNING";
                GeneralMessage.text = "Running in Client mode. To start the stop process, press DISCONNECT button.";
                ImportantMessage.text = "";
                informations.text = "";
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_CLIENT_DISCONNECT:
                stateMessage.text = "CLIENT MODE : IDLING";
                GeneralMessage.text = "Client mode ended and ready. To try connecting to the server, press CONNECT button.";
                ImportantMessage.text = "";
                informations.text = "";
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_BACK_TO_SELECT:
                stateMessage.text = "MODE SELECTION";
                GeneralMessage.text = "Please select a mode. To quit application, press QUIT button.";
                ImportantMessage.text = "";
                informations.text = "";
                break;

            default:
                break;
        }
    }
}
