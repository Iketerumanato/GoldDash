using R3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Title : MonoBehaviour
{
    [Header("モード決定のボタン")]
    [SerializeField] Button StartClientButton;
    [SerializeField] Button StartServerButton;

    #region クライアントモードのオブジェクト群
    [Header("Mode_Logoのボタン")]
    [SerializeField] Button StartGameButton;

    [Header("Mode_Settingのボタン")]
    [SerializeField] Button StartConnectButton;

    [Header("Stateによって振る舞いを違うものにする(前のStateに戻るか,サーバーへの接続をなしにするか両方か)")]
    [SerializeField] Button BackStateButton;
    #endregion

    //ボタン処理まとめ
    public enum TITLE_BUTTON_EVENT : byte
    {
        //サーバー側ボタンの処理一覧
        BUTTON_START_SERVER_ACTIVATE,//サーバーモード起動
        //クライアント側ボタンの処理一覧
        BUTTON_CLIENT_GO_TITLE,//クライアントとして起動(タイトルロゴのあるStateへ移動)

        BUTTON_CLIENT_GO_SETTING,//名前決めと接続画面
        BUTTON_CLIENT_CONNECT,//サーバーへの通信開始

        BUTTON_CLIENT_DISCONNECT,//サーバーへの通信取りやめ
        //BUTTON_CLIENT_BACK//ひとつ前のStateに移動
    }

    //通知するSubjectの宣言
    public Subject<TITLE_BUTTON_EVENT> titleButtonSubject;

    //各ボタンの処理登録とステートクラスの初期化
    public void InitObservationClient(Title title)
    {
        //通知用にsubjectのインスタンス作成　外部から購読する
        titleButtonSubject = new Subject<TITLE_BUTTON_EVENT>();

        //各ボタンをクリック(タッチ)で処理の実行(クライアント側)
        StartClientButton.OnClickAsObservable().Subscribe(_ => titleButtonSubject.OnNext(TITLE_BUTTON_EVENT.BUTTON_CLIENT_GO_TITLE));
        StartGameButton.OnClickAsObservable().Subscribe(_ => titleButtonSubject.OnNext(TITLE_BUTTON_EVENT.BUTTON_CLIENT_GO_SETTING));
        StartConnectButton.OnClickAsObservable().Subscribe(_ => titleButtonSubject.OnNext(TITLE_BUTTON_EVENT.BUTTON_CLIENT_CONNECT));
        BackStateButton.OnClickAsObservable().Subscribe(_ => titleButtonSubject.OnNext(TITLE_BUTTON_EVENT.BUTTON_CLIENT_DISCONNECT));

        //各ボタンをクリック(タッチ)して処理の実行(サーバー側)
        StartServerButton.OnClickAsObservable().Subscribe(_ => titleButtonSubject.OnNext(TITLE_BUTTON_EVENT.BUTTON_START_SERVER_ACTIVATE));
    }

    //いち早く初期化を行う
    public void Awake()
    {   
        Application.targetFrameRate = 45;
    }
}