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
    [SerializeField] TMP_Text GameTitle;

    [Header("Mode_Settingのオブジェクトたち")]
    [SerializeField] TMP_InputField PlayerNameSetting;
    [SerializeField] GameObject StartConnectButton;

    [Header("前に戻るボタン")]
    [SerializeField] GameObject BackStateButton;

    [Header("説明文")]
    [SerializeField] TMP_Text[] TitleExplanationText;
    #endregion

    #region サーバーモードのオブジェクト群
    //[Header("サーバーの画面で扱われるオブジェクト(主に演出の)")]
    #endregion

    private void Start()
    {
        _title.InitObservationClient(_title);
        InitObserver(_title);
        _gameClientManager.InitObservation(_title);
        _gameServerManager.InitObservation(_title);
        _mapGenerator.InitObservation(_gameServerManager, _gameClientManager);
    }

    public void InitObserver(Title title)//,GameClientManager gameClientManager,GameServerManager gameServerManager)
    {
        title.titleButtonSubject.Subscribe(buttonevent => ProcessUdpManagerEvent(buttonevent));
        //gameServerManager.ServerInternalSubject.Subscribe(e => ProcessServerInternalEvent(e));
        //gameClientManager.ClientInternalSubject.Subscribe(e => ProcessClientInternalEvent(e));
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
                GameTitle.enabled = true;
                _title.ChangeStateClient(Title.CLIENT_MODE.MODE_LOGO);
                break;
            case Title.TITLE_BUTTON_EVENT.BUTTON_CLIENT_GO_SETTING:
                StartGameButton.SetActive(false);
                GameTitle.enabled = false;

                PlayerNameSetting.enabled = true;
                StartConnectButton.SetActive(true);
                TitleExplanationText[0].text = "TAP_TO_CONNECT";
                _title.ChangeStateClient(Title.CLIENT_MODE.MODE_SETTING);
                break;
            case Title.TITLE_BUTTON_EVENT.BUTTON_CLIENT_CONNECT:
                PlayerNameSetting.enabled = false;
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
}