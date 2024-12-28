using R3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#region クライアントのInterafaceとState群
public interface ITitleMode_Client
{
    Title.CLIENT_MODE clientState { get; }
    void Title_EntryMode_Client();
    void Title_ExitMode_Client();
}

//タイトル画面各タブレット(クライアント)で表示
public class Mode_Logo : ITitleMode_Client
{
    Title _title;
    public Title.CLIENT_MODE clientState => Title.CLIENT_MODE.MODE_LOGO;
    public Mode_Logo(Title title) => _title = title;
    public void Title_EntryMode_Client() { Debug.Log($"{clientState}に入ります"); }
    public void Title_ExitMode_Client() { Debug.Log($"{clientState}を出ます"); }
}

//プレイヤーの接続画面(名前入力もここで)
public class Mode_Setting : ITitleMode_Client
{
    Title _title;
    public Title.CLIENT_MODE clientState => Title.CLIENT_MODE.MODE_SETTING;
    public Mode_Setting(Title title) => _title = title;
    public void Title_EntryMode_Client() { Debug.Log($"{clientState}に入ります"); }
    public void Title_ExitMode_Client() { Debug.Log($"{clientState}を出ます"); }
}

//プレイヤー待機画面(4人集まったらゲーム開始)
public class Mode_Waiting : ITitleMode_Client
{
    Title _title;
    public Title.CLIENT_MODE clientState => Title.CLIENT_MODE.MODE_WAITING;
    public Mode_Waiting(Title title) => _title = title;
    public void Title_EntryMode_Client() { Debug.Log($"{clientState}に入ります"); }
    public void Title_ExitMode_Client() { Debug.Log($"{clientState}を出ます"); }
}
#endregion

#region サーバーのInterfaceとState群
public interface ITitleMode_Server
{
    Title.SERVER_MODE serverState { get; }
    void Title_EnterMode_Server();
    void Title_ExitMode_Server();
}

//サーバーの起動(同時にプレイヤー待機状態へ移行)
public class Mode_Activate : ITitleMode_Server
{
    Title _title;
    public Title.SERVER_MODE serverState => Title.SERVER_MODE.MODE_ACTIVATE;
    public Mode_Activate(Title title) => _title = title;
    public void Title_EnterMode_Server() { Debug.Log($"{serverState}に入ります"); }
    public void Title_ExitMode_Server() { Debug.Log($"{serverState}を出ます"); }
}

//マップ(ステージ)の生成
public class Mode_Create_Map : ITitleMode_Server
{
    Title _title;
    public Title.SERVER_MODE serverState => Title.SERVER_MODE.MODE_CREATE_MAP;
    public Mode_Create_Map(Title title) => _title = title;
    public void Title_EnterMode_Server() { Debug.Log($"{serverState}に入ります"); }
    public void Title_ExitMode_Server() { Debug.Log($"{serverState}を出ます"); }
}
#endregion

public class Title : MonoBehaviour
{
    public enum CLIENT_MODE
    {
        MODE_LOGO,
        MODE_SETTING,
        MODE_WAITING
    }

    public enum SERVER_MODE
    {
        MODE_ACTIVATE,
        MODE_CREATE_MAP
    }

    [Header("Mode_Selectのobjectたち")]
    [SerializeField] Button StartClientButton;
    [SerializeField] Button StartServerButton;

    #region クライアントモードのオブジェクト群
    [Header("Mode_Logoのオブジェクトたち")]
    [SerializeField] Button StartButton;

    [Header("Mode_Settingのオブジェクトたち")]
    [SerializeField] GameObject PlayerNameSetting;
    [SerializeField] GameObject StartConnectButton;

    [Header("Stateによって振る舞いを違うものにする(前のStateに戻るか,サーバーへの接続をなしにするか)")]
    [SerializeField] GameObject BackStateButton;
    #endregion

    #region サーバーモードのオブジェクト群
    //[Header("サーバーの画面で扱われるオブジェクト(主に演出)")]
    //エフェクトなどの宣言
    #endregion

    [Header("各Stateによって異なる振る舞いを行うオブジェクト(サーバーとクライアント共通)")]
    [SerializeField] GameObject TitleObjs;
    [SerializeField] GameObject[] TitleExplanationText;

    ITitleMode_Client _currentClientState;//現在のState(クライアント)
    ITitleMode_Client _previousClientState;//前のState(クライアント)
    Dictionary<CLIENT_MODE, ITitleMode_Client> _clientStateTable;//クライアントStateのテーブル

    ITitleMode_Server _currentServerState;//現在のState(サーバー)
    ITitleMode_Server _previousServerState;//現在のState(サーバー)
    Dictionary<SERVER_MODE, ITitleMode_Server> _serverStateTable;//サーバーStateのテーブル

    //ボタン処理まとめ
    public enum TITLE_BUTTON_EVENT_CLIENT : byte
    {
        //どちらのモードで起動するか
        BUTTON_START_SERVER_MODE,//サーバーモード起動
        BUTTON_START_CLIENT_MODE,//クライアントモード起動

        //クライアント処理一覧
        BUTTON_CLIENT_GO_TITLE,//タイトルロゴのあるStateへ移動
        BUTTON_CLIENT_CONNECT,//サーバーへの通信開始
        BUTTON_CLIENT_DISCONNECT,//サーバーへの通信取りやめ
        BUTTON_CLIENT_BACK//ひとつ前のStateに移動
    }

    //通知するSubjectの宣言
    public Subject<TITLE_BUTTON_EVENT_CLIENT> clientUISubject;

    public void InitObservationClient(Title title)
    {
        //通知用にsubjectのインスタンス作成　外部から購読する
        clientUISubject = new Subject<TITLE_BUTTON_EVENT_CLIENT>();

        if (_clientStateTable != null) return;

        //各テーブルの初期化
        Dictionary<CLIENT_MODE, ITitleMode_Client> clienttable = new()
        {
            { CLIENT_MODE.MODE_LOGO,new Mode_Logo(title) },
            { CLIENT_MODE.MODE_SETTING,new Mode_Setting(title) },
            { CLIENT_MODE.MODE_WAITING,new Mode_Waiting(title) },
        };
        _clientStateTable = clienttable;

    }

    public void InitObservationServer(Title title)
    {
        if (_serverStateTable != null) return;

        Dictionary<SERVER_MODE, ITitleMode_Server> servertable = new()
        {
            { SERVER_MODE.MODE_ACTIVATE,new Mode_Activate(title) },
            { SERVER_MODE.MODE_CREATE_MAP,new Mode_Create_Map(title) }
        };

        _serverStateTable = servertable;
    }

    public void ChangeStateClient(CLIENT_MODE nextClientState)
    {
        if (_clientStateTable == null) return;//初期化してない場合は無視
        if (_currentClientState == null || _currentClientState.clientState == nextClientState) return;//同じ状態には遷移しないように

        var nextState = _clientStateTable[nextClientState];
        _previousClientState = _currentClientState;
        _previousClientState?.Title_ExitMode_Client();
        _currentClientState = nextState;
        _currentClientState.Title_EntryMode_Client();
    }
}