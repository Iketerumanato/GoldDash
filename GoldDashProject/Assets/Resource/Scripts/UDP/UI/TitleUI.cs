using R3;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

#region クライアントのInterafaceとState群
public interface ITitleMode_Client
{
    TitleUI.CLIENT_MODE clientState { get; }
    void Title_EntryMode_Client();
    void Title_ExitMode_Client();
}

//タイトル画面各タブレット(クライアント)で表示
public class Mode_Logo : ITitleMode_Client
{
    TitleUI _titleUi;
    public TitleUI.CLIENT_MODE clientState => TitleUI.CLIENT_MODE.MODE_LOGO;
    public Mode_Logo(TitleUI titleUi) => _titleUi = titleUi;
    public void Title_EntryMode_Client()
    {
        //非表示
        _titleUi.startclientButton.SetActive(false);
        _titleUi.startserverButton.SetActive(false);
        _titleUi.titleExplanationText[0].gameObject.SetActive(false);

        //表示
        _titleUi.startgameButton.SetActive(true);
        _titleUi.gametitle.gameObject.SetActive(true);
    }
    public void Title_ExitMode_Client()
    {
        //ステートに入って表示したものをここで非表示
        _titleUi.startgameButton.SetActive(false);
        _titleUi.gametitle.gameObject.SetActive(false);
    }
}

//プレイヤーの接続画面(名前入力もここで)
public class Mode_Setting : ITitleMode_Client
{
    TitleUI _titleUi;
    public TitleUI.CLIENT_MODE clientState => TitleUI.CLIENT_MODE.MODE_SETTING;
    public Mode_Setting(TitleUI titleUi) => _titleUi = titleUi;
    public void Title_EntryMode_Client() 
    {
        _titleUi.playernameSetting.gameObject.SetActive(true);
        _titleUi.startconnectButton.SetActive(true);
        _titleUi.backstateButton.SetActive(true);
        _titleUi.titleExplanationText[0].gameObject.SetActive(true);
        _titleUi.titleExplanationText[0].text = "SETTING_NAME_AND\nTAP_TO_CONNECT";
    }
    public void Title_ExitMode_Client()
    {
        _titleUi.playernameSetting.gameObject.SetActive(false);
        _titleUi.startconnectButton.SetActive(false);
        _titleUi.backstateButton.SetActive(false);
        _titleUi.titleExplanationText[0].gameObject.SetActive(false);

    }
}

//プレイヤー待機画面(4人集まったらゲーム開始)
public class Mode_Waiting : ITitleMode_Client
{
    TitleUI _titleUi;
    public TitleUI.CLIENT_MODE clientState => TitleUI.CLIENT_MODE.MODE_WAITING;
    public Mode_Waiting(TitleUI titleUi) => _titleUi = titleUi;
    public void Title_EntryMode_Client()
    {
        _titleUi.backstateButton.SetActive(true);
        _titleUi.titleExplanationText[1].gameObject.SetActive(true);
        _titleUi.titleExplanationText[1].text = "CONNECT_COMPLETE";
        _titleUi.titleExplanationText[2].gameObject.SetActive(true);
        _titleUi.titleExplanationText[2].text = "PLEYER_ 0 / 4";
        
    }
    public void Title_ExitMode_Client()
    {
        _titleUi.backstateButton.SetActive(false);
        _titleUi.titleExplanationText[1].gameObject.SetActive(false);
        _titleUi.titleExplanationText[2].gameObject.SetActive(false);
    }
}
#endregion

#region サーバーのInterfaceとState群
public interface ITitleMode_Server
{
    TitleUI.SERVER_MODE serverState { get; }
    void Title_EntryMode_Server();
    void Title_ExitMode_Server();
}

//サーバーの起動(同時にプレイヤー待機状態へ移行)
public class Mode_Activate : ITitleMode_Server
{
    TitleUI _titleUi;
    public TitleUI.SERVER_MODE serverState => TitleUI.SERVER_MODE.MODE_ACTIVATE;
    public Mode_Activate(TitleUI titleUi) => _titleUi = titleUi;
    public void Title_EntryMode_Server()
    {
        Debug.Log($"{serverState}に入ります");
        _titleUi.startclientButton.SetActive(false);
        _titleUi.startserverButton.SetActive(false);
        _titleUi.titleExplanationText[0].text = "WAITING_CONNECT";
    }
    public void Title_ExitMode_Server()
    {
        Debug.Log($"{serverState}を出ます");
        _titleUi.titleExplanationText[0].text = "PLAYERS_GATHERED";
    }
}

//マップ(ステージ)の生成
public class Mode_Create_Map : ITitleMode_Server
{
    TitleUI _titleUi;
    public TitleUI.SERVER_MODE serverState => TitleUI.SERVER_MODE.MODE_CREATE_MAP;
    public Mode_Create_Map(TitleUI title) => _titleUi = title;
    public void Title_EntryMode_Server() { Debug.Log($"{serverState}に入ります"); }
    public void Title_ExitMode_Server() { Debug.Log($"{serverState}を出ます"); }
}
#endregion

public class TitleUI : MonoBehaviour
{
    public enum CLIENT_MODE
    {
        MODE_LOGO,
        MODE_SETTING,
        MODE_WAITING
    }
    public CLIENT_MODE? CurrentClientMode => _currentClientState?.clientState;

    public enum SERVER_MODE
    {
        MODE_ACTIVATE,
        MODE_CREATE_MAP
    }

    ITitleMode_Client _currentClientState;//現在のState(クライアント)
    Dictionary<CLIENT_MODE, ITitleMode_Client> _clientStateTable;//クライアントStateのテーブル
    Stack<ITitleMode_Client> _clientStateHistory = new();

    ITitleMode_Server _currentServerState;//現在のState(サーバー)
    ITitleMode_Server _previousServerState;//現在のState(サーバー)
    Dictionary<SERVER_MODE, ITitleMode_Server> _serverStateTable;//サーバーStateのテーブル

    [Header("Titleクラスのボタンイベントを取得")]
    [SerializeField] Title _title;

    [Header("GameClientManagerの接続準備")]
    [SerializeField] GameClientManager _gameClientManager;

    [Header("GameServerManagerの通信準備")]
    [SerializeField] GameServerManager _gameServerManager;
    public GameServerManager gameserverManager => _gameServerManager;

    [Header("マップ作成準備")]
    [SerializeField] MapGenerator _mapGenerator;

    [Header("Mode_Selectのobjectたち")]
    [SerializeField] GameObject StartClientButton;
    public GameObject startclientButton => StartClientButton;

    [SerializeField] GameObject StartServerButton;
    public GameObject startserverButton => StartServerButton;

    #region クライアントモードのオブジェクト群
    [Header("Mode_Logoのオブジェクトたち")]
    [SerializeField] GameObject StartGameButton;
    public GameObject startgameButton => StartGameButton;

    [SerializeField] TMP_Text GameTitle;
    public TMP_Text gametitle => GameTitle;

    [Header("Mode_Settingのオブジェクトたち")]
    [SerializeField] TMP_InputField PlayerNameSetting;
    public TMP_InputField playernameSetting => PlayerNameSetting;

    [SerializeField] GameObject StartConnectButton;
    public GameObject startconnectButton => StartConnectButton;

    [Header("前に戻るボタン")]
    [SerializeField] GameObject BackStateButton;
    public GameObject backstateButton => BackStateButton;
    #endregion

    [Header("説明文")]
    [SerializeField] TMP_Text[] TitleExplanationText;
    public TMP_Text[] titleExplanationText => TitleExplanationText;
   

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

        if (_clientStateTable != null && _serverStateTable != null) return;

        //各テーブルの初期化
        Dictionary<CLIENT_MODE, ITitleMode_Client> clienttable = new()
        {
            { CLIENT_MODE.MODE_LOGO,new Mode_Logo(this) },
            { CLIENT_MODE.MODE_SETTING,new Mode_Setting(this) },
            { CLIENT_MODE.MODE_WAITING,new Mode_Waiting(this) },
        };
        _clientStateTable = clienttable;

        Dictionary<SERVER_MODE, ITitleMode_Server> servertable = new()
        {
            { SERVER_MODE.MODE_ACTIVATE,new Mode_Activate(this) },
            { SERVER_MODE.MODE_CREATE_MAP,new Mode_Create_Map(this) }
        };
        _serverStateTable = servertable;
    }

    private void ProcessUdpManagerEvent(Title.TITLE_BUTTON_EVENT clientbuttonEvent)
    {
        switch (clientbuttonEvent)
        {
            case Title.TITLE_BUTTON_EVENT.BUTTON_CLIENT_GO_TITLE:
                ChangeStateClient(CLIENT_MODE.MODE_LOGO);
                break;

            case Title.TITLE_BUTTON_EVENT.BUTTON_CLIENT_GO_SETTING:
                ChangeStateClient(CLIENT_MODE.MODE_SETTING);
                break;

            case Title.TITLE_BUTTON_EVENT.BUTTON_CLIENT_CONNECT:
                ChangeStateClient(CLIENT_MODE.MODE_WAITING);
                break;

            case Title.TITLE_BUTTON_EVENT.BUTTON_CLIENT_DISCONNECT:
                BackStateClient();
                break;

            case Title.TITLE_BUTTON_EVENT.BUTTON_START_SERVER_ACTIVATE:
                ChangeStateServer(SERVER_MODE.MODE_ACTIVATE);
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

    //Stateの遷移(クライアント)
    public void ChangeStateClient(CLIENT_MODE nextClientState)
    {
        if (_currentClientState != null)
        {
            _clientStateHistory.Push(_currentClientState); // 現在の状態を履歴に追加
            _currentClientState.Title_ExitMode_Client();
        }

        _currentClientState = _clientStateTable[nextClientState];
        _currentClientState.Title_EntryMode_Client();
    }

    //Stateの遷移(サーバー)※やることはクライアントと同じ
    public void ChangeStateServer(SERVER_MODE nextServerState)
    {
        var nextState = _serverStateTable[nextServerState];
        _previousServerState = _currentServerState;
        _previousServerState?.Title_ExitMode_Server();
        _currentServerState = nextState;
        _currentServerState.Title_EntryMode_Server();
    }

    // 前のステートに戻る
    public void BackStateClient()
    {
        if (_clientStateHistory.Count == 0)
        {
            Debug.LogWarning("前のステートが存在しません。");
            return;
        }

        // 現在のステートから抜ける
        _currentClientState?.Title_ExitMode_Client();

        // 前のステートを取得し適用
        _currentClientState = _clientStateHistory.Pop();
        _currentClientState.Title_EntryMode_Client();

        Debug.Log($"ステートを{_currentClientState.clientState}に戻しました。");
    }

    public void InputPlayerName()
    {
        _gameClientManager.myName = PlayerNameSetting.text;
        Debug.Log($"{_gameClientManager.myName}の名前でゲームを始めます");
    }
}