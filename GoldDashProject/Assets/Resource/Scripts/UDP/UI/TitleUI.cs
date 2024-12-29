using R3;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TitleUI : MonoBehaviour
{
    [Header("Titleクラスのボタンイベントを取得")]
    [SerializeField] Title _title;

    [Header("GameClientManagerの接続準備")]
    [SerializeField] GameClientManager _gameClientManager;

    [Header("GameServerManagerの通信準備")]
    [SerializeField] GameServerManager _gameServerManager;

    [Header("マップ作成準備")]
    [SerializeField] MapGenerator _mapGenerator;

    [Header("Mode_Selectのobjectたち")]
    [SerializeField] GameObject StartClientButton;
    [SerializeField] GameObject StartServerButton;

    #region クライアントモードのオブジェクト群
    [Header("Mode_Logoのオブジェクトたち")]
    [SerializeField] GameObject StartGameButton;
    [SerializeField] GameObject GameTitle;

    [Header("Mode_Settingのオブジェクトたち")]
    [SerializeField] GameObject PlayerNameSetting;
    [SerializeField] GameObject StartConnectButton;

    [Header("前に戻るボタン")]
    [SerializeField] GameObject BackStateButton;

    [Header("説明文")]
    [SerializeField] TMP_Text[] TitleExplanationText;
    #endregion

    #region サーバーモードのオブジェクト群
    //[Header("サーバーの画面で扱われるオブジェクト(主に演出の)")]
    #endregion

    [SerializeField] GameObject[] TitleUICamvas;

    private void Start()
    {
        _title.InitObservationClient(_title);
        _gameClientManager.InitObservation(_title);
        _gameServerManager.InitObservation(_title);
        InitObserver(_title,_gameClientManager,_gameServerManager);
        _mapGenerator.InitObservation(_gameServerManager, _gameClientManager);
    }

    public void InitObserver(Title title,GameClientManager gameClientManager,GameServerManager gameServerManager)
    {
        title.titleButtonSubject.Subscribe(buttonevent => ProcessUdpManagerEvent(buttonevent));
        gameServerManager.ServerInternalSubject.Subscribe(e => ProcessServerInternalEvent(e));
        gameClientManager.ClientInternalSubject.Subscribe(e => ProcessClientInternalEvent(e));
    }

    private void ProcessUdpManagerEvent(Title.TITLE_BUTTON_EVENT clientbuttonEvent)
    {
        switch (clientbuttonEvent)
        {
            case Title.TITLE_BUTTON_EVENT.BUTTON_CLIENT_GO_TITLE:
                //非表示
                StartClientButton.SetActive(false);
                StartServerButton.SetActive(false);

                //表示
                StartGameButton.SetActive(true);
                GameTitle.SetActive(true);
                _title.ChangeStateClient(Title.CLIENT_MODE.MODE_LOGO);
                break;

            case Title.TITLE_BUTTON_EVENT.BUTTON_CLIENT_GO_SETTING:
                StartGameButton.SetActive(false);
                GameTitle.SetActive(false);

                PlayerNameSetting.SetActive(true);
                StartConnectButton.SetActive(true);
                TitleExplanationText[0].text = "TAP_TO_CONNECT";
                _title.ChangeStateClient(Title.CLIENT_MODE.MODE_SETTING);
                break;

            case Title.TITLE_BUTTON_EVENT.BUTTON_CLIENT_CONNECT:
                PlayerNameSetting.SetActive(false);
                StartConnectButton.SetActive(false);
                _title.ChangeStateClient(Title.CLIENT_MODE.MODE_WAITING);
                break;

            case Title.TITLE_BUTTON_EVENT.BUTTON_START_SERVER_ACTIVATE:
                StartClientButton.SetActive(false);
                StartServerButton.SetActive(false);
                TitleExplanationText[0].text = "WAITING_CONNECT";
                _title.ChangeStateServer(Title.SERVER_MODE.MODE_ACTIVATE);
                break;
        }
    }

    private void ProcessServerInternalEvent(GameServerManager.SERVER_INTERNAL_EVENT e)
    {
        switch (e)
        {
            case GameServerManager.SERVER_INTERNAL_EVENT.EDIT_GUI_FOR_GAME:
                //非表示
                TitleUICamvas[0].SetActive(false);
                TitleUICamvas[1].SetActive(false);
                //表示
                //originSignMini.gameObject.SetActive(true);
                //serverSignMini.gameObject.SetActive(true);
                //stateMessageMini.gameObject.SetActive(true);
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
                TitleUICamvas[0].SetActive(false);
                TitleUICamvas[1].SetActive(false);

                //表示
                //originSignMini.gameObject.SetActive(true);
                //clientSignMini.gameObject.SetActive(true);
                //stateMessageMini.gameObject.SetActive(true);
                break;
            default:
                break;
        }
    }
}