using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;

public class UdpUIDisplayer : MonoBehaviour
{
    //ボタンオブジェクト
    [SerializeField] private GameObject buttonServerMode;
    [SerializeField] private GameObject buttonClientMode;
    [SerializeField] private GameObject buttonQuitApp;

    [SerializeField] private GameObject buttonActivate;
    [SerializeField] private GameObject buttonDeactivate;

    [SerializeField] private GameObject buttonConnect;
    [SerializeField] private GameObject buttonDisconnect;

    [SerializeField] private GameObject buttonQuitMode;

    //テキストオブジェクト
    [SerializeField] private GameObject stateMessage;
    [SerializeField] private GameObject GeneralMessage;
    [SerializeField] private GameObject ImportantMessage;
    [SerializeField] private GameObject informations;

    //装飾
    [SerializeField] private GameObject line;

    public void InitObservation(UdpButtonManager udpUIManager)
    {
        udpUIManager.udpUIManagerSubject.Subscribe(e => ProcessUdpManagerEvent(e));
    }

    private void ProcessUdpManagerEvent(UdpButtonManager.UDP_BUTTON_EVENT e)
    {
        switch (e)
        { 
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_START_SERVER_MODE:
                //非表示
                buttonServerMode.SetActive(false);
                buttonClientMode.SetActive(false);
                buttonQuitApp.SetActive(false);

                //表示
                buttonActivate.SetActive(true);
                buttonDeactivate.SetActive(true);
                buttonQuitMode.SetActive(true);
                informations.SetActive(true);
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_START_CLIENT_MODE:
                //非表示
                buttonServerMode.gameObject.SetActive(false);
                buttonClientMode.gameObject.SetActive(false);
                buttonQuitApp.gameObject.SetActive(false);

                //表示
                buttonConnect.gameObject.SetActive(true);
                buttonDisconnect.gameObject.SetActive(true);
                buttonQuitMode.gameObject.SetActive(true);
                informations.SetActive(true);
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_QUIT_MODE:
                //非表示
                buttonActivate.gameObject.SetActive(false);
                buttonDeactivate.gameObject.SetActive(false);
                buttonConnect.gameObject.SetActive(false);
                buttonDisconnect.gameObject.SetActive(false);
                buttonQuitMode.gameObject.SetActive(false);
                informations.SetActive(false);

                //表示
                buttonServerMode.gameObject.SetActive(true);
                buttonClientMode.gameObject.SetActive(true);
                buttonQuitApp.gameObject.SetActive(true);
                break;

            default:
                break;
        }
    }
}
