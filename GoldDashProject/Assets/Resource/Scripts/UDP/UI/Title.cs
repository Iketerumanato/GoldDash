using R3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UdpButtonManager;

public interface TitleMode_Client
{
    void Title_EnterMode_Client(Title title);
    void Title_Exit_Client(Title title);
}

public interface TitleMode_Server
{
    void Title_EnterMode_Servaer(Title title);
    void Title_Exit__Server(Title title);
}


public class Title : MonoBehaviour
{
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


    public enum TITLE_BUTTON_EVENT_CLIENT : byte
    {
        //どちらのモードで起動するか
        BUTTON_START_SERVER_MODE,//サーバーモード起動
        BUTTON_START_CLIENT_MODE,//クライアントモード起動

        //クライアント処理一覧
        BUTTON_CLIENT_GO_TITLE,//タイトルロゴのあるStateへ移動
        BUTTON_CLIENT_CONNECT,//サーバーへの通信開始
        BUTTON_CLIENT_DISCONNECT,//サーバーへの通信取りやめ
        BUTTON_CLIENT_BACK,//ひとつ前のStateに移動

        //BUTTON_QUIT_APP,
    }

    //通知するSubjectの宣言
    public Subject<TITLE_BUTTON_EVENT_CLIENT> clientUISubject;

    public void InitObservation()
    {
        //通知用にsubjectのインスタンス作成　外部から購読する
        clientUISubject = new Subject<TITLE_BUTTON_EVENT_CLIENT>();
    }
}