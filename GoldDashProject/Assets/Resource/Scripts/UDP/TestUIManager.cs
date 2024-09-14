using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using R3;
using System.Xml.Serialization;

/// <summary>
/// ボタンが押されたことだけを検知して通知を飛ばし、ボタンの表示非表示処理を行う
/// </summary>
public class TestUIManager : MonoBehaviour
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
    Subject<UI_MANAGER_EVENT> uiManagerEvent;

    //subjectの初期化および購読はNull参照頻発地点。外部にエントリーポイントを作って、そこで処理の順序を制御するのがいい、けど今はAwakeで
    private void Awake()
    {
        //通知用にsubjectのインスタンス作成　外部から購読する
        uiManagerEvent = new Subject<UI_MANAGER_EVENT>();

        //ボタンクリック時にSubscribe()で指定された関数を実行できる
        //Subscribeの引数はラムダ式でよいのだが、()=>と書くとエラー。
        //Subscribeの引数にラムダ式を取ることは本来できないのだが、ObservableSubscribeExtensionsというドキュメントの中で
        //引数にAction型を取るオーバーロードが定義されている。ここで、()=>でなく_=>と書くことで引数が明示的にAction型になるので、オーバーロードが参照されるようになる。
        buttonServerMode.OnClickAsObservable().Subscribe(_ => StartServerMode());
        buttonClientMode.OnClickAsObservable().Subscribe(_ => StartClientMode());
        buttonQuitMode.OnClickAsObservable().Subscribe(_ => QuitMode());
    }

    private void StartServerMode()
    {
        //非表示
        buttonServerMode.gameObject.SetActive(false);
        buttonClientMode.gameObject.SetActive(false);
        buttonQuitApp.gameObject.SetActive(false);

        //表示
        buttonActivate.gameObject.SetActive(true);
        buttonDeactivate.gameObject.SetActive(true);
        buttonQuitMode.gameObject.SetActive(true);

        //通知
        uiManagerEvent.OnNext(UI_MANAGER_EVENT.BUTTON_START_SERVER_MODE);
    }
    
    private void StartClientMode()
    {
        //非表示
        buttonServerMode.gameObject.SetActive(false);
        buttonClientMode.gameObject.SetActive(false);
        buttonQuitApp.gameObject.SetActive(false);

        //表示
        buttonConnect.gameObject.SetActive(true);
        buttonDisconnect.gameObject.SetActive(true);
        buttonQuitMode.gameObject.SetActive(true);

        //通知
        uiManagerEvent.OnNext(UI_MANAGER_EVENT.BUTTON_START_CLIENT_MODE);
    }

    private void QuitMode()
    {
        //非表示
        buttonActivate.gameObject.SetActive(false);
        buttonDeactivate.gameObject.SetActive(false);
        buttonConnect.gameObject.SetActive(false);
        buttonDisconnect.gameObject.SetActive(false);
        buttonQuitMode.gameObject.SetActive(false);

        //表示
        buttonServerMode.gameObject.SetActive(true);
        buttonClientMode.gameObject.SetActive(true);
        buttonQuitApp.gameObject.SetActive(true);

        //通知
        uiManagerEvent.OnNext(UI_MANAGER_EVENT.BUTTON_QUIT_MODE);
    }
}
