using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using R3;

public class UdpTextManager : MonoBehaviour
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
                stateMessage.text = "SERVER MODE : IDLE";
                GeneralMessage.text = "Server mode ready. Press the ACTIVATE button to activate the server.";
                ImportantMessage.text = "";
                informations.text = "";
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_START_CLIENT_MODE:
                stateMessage.text = "CLIENT MODE : IDLE";
                GeneralMessage.text = "Client mode ready. Press the CONNECT button to try to connect to the server.";
                ImportantMessage.text = "";
                informations.text = "";
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_QUIT_MODE:
                stateMessage.text = "MODE SELECTION";
                GeneralMessage.text = "Please select a mode. To exit application, press the QUIT button or knead a shrimp.";
                ImportantMessage.text = "";
                informations.text = "";
                break;

            default:
                break;
        }
    }
}
