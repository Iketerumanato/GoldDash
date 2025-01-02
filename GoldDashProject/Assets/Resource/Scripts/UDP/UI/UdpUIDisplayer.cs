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

    [SerializeField] private GameObject buttonBack;

    //テキストオブジェクト
    [SerializeField] private GameObject stateMessage;
    [SerializeField] private GameObject stateMessageMini;
    //12/27追記
    [SerializeField] GameObject TitleTextObj;
    [SerializeField] GameObject StartButton;
    [SerializeField] GameObject PlayerTextField;

    //装飾
    [SerializeField] private GameObject line;

    [SerializeField] private GameObject originSign;
    [SerializeField] private GameObject serverSign;
    [SerializeField] private GameObject clientSign;

    [SerializeField] private GameObject originSignMini;
    [SerializeField] private GameObject serverSignMini;
    [SerializeField] private GameObject clientSignMini;

    public void InitObservation(UdpButtonManager udpUIManager, GameServerManager gameServerManager, GameClientManager gameClientManager)
    {
        udpUIManager.udpUIManagerSubject.Subscribe(e => ProcessUdpManagerEvent(e));
        gameServerManager.ServerInternalSubject.Subscribe(e => ProcessServerInternalEvent(e));
        gameClientManager.ClientInternalSubject.Subscribe(e => ProcessClientInternalEvent(e));
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
                buttonBack.SetActive(true);
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_START_CLIENT_MODE:
                //非表示
                //buttonServerMode.gameObject.SetActive(false);
                //buttonClientMode.gameObject.SetActive(false);
                //buttonQuitApp.gameObject.SetActive(false);
                TitleTextObj.SetActive(false);
                StartButton.SetActive(false);


                //表示
                buttonConnect.gameObject.SetActive(true);
                buttonDisconnect.gameObject.SetActive(true);
                //buttonBack.gameObject.SetActive(true);
                PlayerTextField.SetActive(true);
                stateMessage.SetActive(true);
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_BACK_TO_SELECT:
                //非表示
                buttonActivate.gameObject.SetActive(false);
                buttonDeactivate.gameObject.SetActive(false);
                buttonConnect.gameObject.SetActive(false);
                buttonDisconnect.gameObject.SetActive(false);
                buttonBack.gameObject.SetActive(false);

                //表示
                buttonServerMode.gameObject.SetActive(true);
                buttonClientMode.gameObject.SetActive(true);
                buttonQuitApp.gameObject.SetActive(true);
                break;

            //12/27追記
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_CLIENT_GO_TITLE:
                //非表示
                buttonServerMode.SetActive(false);
                buttonClientMode.SetActive(false);
                stateMessage.SetActive(false);
                //表示
                TitleTextObj.SetActive(true);
                StartButton.SetActive(true);
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
                //非表示
                buttonActivate.gameObject.SetActive(false);
                buttonDeactivate.gameObject.SetActive(false);
                buttonBack.gameObject.SetActive(false);
                originSign.gameObject.SetActive(false);
                serverSign.gameObject.SetActive(false);
                clientSign.gameObject.SetActive(false);
                stateMessage.gameObject.SetActive(false);
                line.gameObject.SetActive(false);

                //表示
                originSignMini.gameObject.SetActive(true);
                serverSignMini.gameObject.SetActive(true);
                stateMessageMini.gameObject.SetActive(true);
                break;
            default:
                break;
        }
    }

    private void ProcessClientInternalEvent(GameClientManager.CLIENT_INTERNAL_EVENT e)
    {
        switch (e)
        {
            case GameClientManager.CLIENT_INTERNAL_EVENT.EDIT_GUI_FOR_GAME:
                //非表示
                buttonConnect.gameObject.SetActive(false);
                buttonDisconnect.gameObject.SetActive(false);
                buttonBack.gameObject.SetActive(false);
                originSign.gameObject.SetActive(false);
                serverSign.gameObject.SetActive(false);
                clientSign.gameObject.SetActive(false);
                stateMessage.gameObject.SetActive(false);
                line.gameObject.SetActive(false);

                //表示
                originSignMini.gameObject.SetActive(true);
                clientSignMini.gameObject.SetActive(true);
                stateMessageMini.gameObject.SetActive(true);
                break;
            default:
                break;
        }
    }
}
