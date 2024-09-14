using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using R3;

public class UdpUIManager
    : MonoBehaviour
{
    //subjectに乗せて通知を飛ばすためのenum
    public enum UI_MANAGER_EVENT : byte
    {
        BUTTON_START_SERVER_MODE,
        BUTTON_START_CLIENT_MODE,
        BUTTON_QUIT_MODE,

        BUTTON_SERVER_ACTIVATE,
        BUTTON_SERVER_DEACTIVATE,

        BUTTON_CLIENT_CONNECT,
        BUTTON_CLIENT_DISCONNECT,

        BUTTON_QUIT_APP,
    }

    //最初から表示されているボタン
    [SerializeField] private Button buttonServerMode;
    [SerializeField] private Button buttonClientMode;
    [SerializeField] private Button buttonQuitApp;

    //各モード用ボタン
    [SerializeField] private Button buttonActivate;
    [SerializeField] private Button buttonDeactivate;

    [SerializeField] private Button buttonConnect;
    [SerializeField] private Button buttonDisconnect;

    [SerializeField] private Button buttonQuitMode;

    //通知用subject
    public Subject<UI_MANAGER_EVENT> udpUIManagerSubject;

    public void InitObservation()
    {
        //通知用にsubjectのインスタンス作成　外部から購読する
        udpUIManagerSubject = new Subject<UI_MANAGER_EVENT>();

        //ボタンクリック時にSubscribe(関数名)で指定された関数を実行できる
        //Subscribeの引数はラムダ式でよいのだが、()=>と書くとエラー。
        //Subscribeの引数にラムダ式を取ることは本来できないのだが、ObservableSubscribeExtensionsというドキュメントの中で
        //引数にAction型を取るオーバーロードが定義されている。ここで、()=>でなく_=>と書くことで引数が明示的にAction型になるので、オーバーロードが参照されるようになる。
        buttonServerMode.OnClickAsObservable().Subscribe(_ => udpUIManagerSubject.OnNext(UI_MANAGER_EVENT.BUTTON_START_SERVER_MODE));
        buttonClientMode.OnClickAsObservable().Subscribe(_ => udpUIManagerSubject.OnNext(UI_MANAGER_EVENT.BUTTON_START_CLIENT_MODE));
        buttonQuitMode.OnClickAsObservable().Subscribe(_ => udpUIManagerSubject.OnNext(UI_MANAGER_EVENT.BUTTON_QUIT_MODE));
    }
}
