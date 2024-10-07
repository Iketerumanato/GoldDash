using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

public class GameClientManager : MonoBehaviour
{
    private bool isRunning; //稼働中か

    private UdpGameClient udpGameClient; //UdpCommunicatorを継承したUdpGameClientのインスタンス

    private Queue<Header> packetQueue; //udpGameClientは”勝手に”このキューにパケットを入れてくれる。不正パケット処理なども済んだ状態で入る。

    [SerializeField] private ushort sessionPass; //サーバーに接続するためのパスコード

    [SerializeField] private ushort initSessionPass; //初回通信時、サーバーからの返信が安全なものか判別するためのパスコード。今後乱数化する

    [SerializeField] private string myName; //仮です。登録に使うプレイヤーネーム

    private ushort sessionID; //自分のセッションID。サーバー側で決めてもらう。

    private Dictionary<ushort, ActorController> actorDictionary; //sessionパスを鍵としてactorインスタンスを保管。自分以外のプレイヤー（アクター）のセッションIDも記録していく

    private ActorController playerActor; //プレイヤーが操作するキャラクターのActorController

    private int numOfActors; //アクターの人数
    private int preparedActors; //生成し終わったアクターの数

    [SerializeField] private GameObject ActorObject; //アクターのプレハブ
    [SerializeField] private GameObject PlayerObject; //プレイヤーのプレハブ

    private bool inGame; //ゲームは始まっているか

    //クライアントが内部をコントロールするための通知　マップ生成など
    public enum CLIENT_INTERNAL_EVENT
    {
        GENERATE_MAP = 0, //マップを生成せよ
    }

    public Subject<CLIENT_INTERNAL_EVENT> ClientInternalSubject;

    #region ボタンが押されたら有効化したり無効化したり
    public void InitObservation(UdpButtonManager udpUIManager)
    {
        ClientInternalSubject = new Subject<CLIENT_INTERNAL_EVENT>();
        udpUIManager.udpUIManagerSubject.Subscribe(e => ProcessUdpManagerEvent(e));
    }

    private void ProcessUdpManagerEvent(UdpButtonManager.UDP_BUTTON_EVENT e)
    {
        switch (e)
        {
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_START_CLIENT_MODE:
                udpGameClient = new UdpGameClient(ref packetQueue, initSessionPass);
                break;
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_CLIENT_CONNECT:
                if (udpGameClient == null) udpGameClient = new UdpGameClient(ref packetQueue, initSessionPass);

                //Initパケット送信
                udpGameClient.Send(new Header(0, 0, 0, 0, (byte)Definer.PT.IPC, new InitPacketClient(sessionPass, udpGameClient.rcvPort, initSessionPass, myName).ToByte()).ToByte());

                isRunning = true;
                break;
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_CLIENT_DISCONNECT:
                udpGameClient.Dispose();
                isRunning = false;
                break;
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_BACK_TO_SELECT:
                if (udpGameClient != null) udpGameClient.Dispose();
                isRunning = false;
                break;
            default:
                break;
        }
    }
    #endregion

    private void Start()
    {
        inGame = false;

        packetQueue = new Queue<Header>();
        actorDictionary = new Dictionary<ushort, ActorController>();

        Task.Run(() => ProcessPacket());
        Task.Run(() => SendPlayerPosition());
    }

    private async void SendPlayerPosition()
    {
        //ゲーム開始を待つ
        await UniTask.WaitUntil(() => inGame); //ここは本来ハローパケットの送信処理から切り替えるべきだがまだ実装しない

        while (true) 
        {
            //プレイヤーアクターの座標をMOVで送信
            ActionPacket myPacket = new ActionPacket((byte)Definer.RID.MOV, default, sessionID, playerActor.transform.position, playerActor.transform.forward);
            Header myHeader = new Header(this.sessionID, 0, 0, 0, (byte)Definer.PT.AP, myPacket.ToByte());
            udpGameClient.Send(myHeader.ToByte());

            await UniTask.Delay(50);
        }
    }

    private async void ProcessPacket()
    {
        while (true)
        {
            //稼働状態になるのを待つ
            await UniTask.WaitUntil(() => isRunning);

            while (isRunning)
            {
                //キューにパケットが入るのを待つ
                await UniTask.WaitUntil(() => packetQueue.Count > 0);

                Header receivedHeader = packetQueue.Dequeue();

                Debug.Log("パケットを受け取ったぜ！開封するぜ！");

                Debug.Log($"ヘッダーを確認するぜ！パケット種別は{(Definer.PT)receivedHeader.packetType}だぜ！");

                switch (receivedHeader.packetType)
                {
                    case (byte)Definer.PT.IPS:

                        //InitPacketを受け取ったときの処理
                        Debug.Log($"Initパケットを処理するぜ！");

                        if (this.sessionID != 0)
                        {
                            Debug.Log("既にsessionIDは割り振られているぜ。このInitパケットは破棄するぜ。");
                            break;
                        }

                        //クラスに変換する
                        InitPacketServer receivedInitPacket = new InitPacketServer(receivedHeader.data);

                        sessionID = receivedInitPacket.sessionID; //自分のsessionIDを受け取る
                        Debug.Log($"sessionID:{sessionID}を受け取ったぜ。");

                        //エラーコードがあればここで処理
                        break;
                    case (byte)Definer.PT.AP:

                        //ActionPacketを受け取ったときの処理
                        Debug.Log($"Actionパケットを処理するぜ！");

                        ActionPacket receivedActionPacket = new ActionPacket(receivedHeader.data);

                        Debug.Log($"{(Definer.RID)receivedActionPacket.roughID}を処理するぜ！");

                        switch (receivedActionPacket.roughID)
                        {
                            case (byte)Definer.RID.NOT:

                                switch (receivedActionPacket.detailID)
                                {
                                    case (byte)Definer.NDID.HELLO:
                                        break;
                                    case (byte)Definer.NDID.PSG:
                                        //生成すべきアクターの数を受け取る
                                        numOfActors = receivedActionPacket.targetID;
                                        break;
                                    case (byte)Definer.NDID.STG:
                                        //ここでプレイヤーを有効化してゲーム開始
                                        ClientInternalSubject.OnNext(CLIENT_INTERNAL_EVENT.GENERATE_MAP); //マップを生成せよ
                                        //全アクターの有効化
                                        foreach (KeyValuePair<ushort, ActorController> k in actorDictionary)
                                        { 
                                            k.Value.gameObject.SetActive(true);
                                        }
                                        inGame = true;
                                        break;
                                    case (byte)Definer.NDID.EDG:
                                        break;
                                }
                                break;
                            case (byte)Definer.RID.EXE:
                                switch (receivedActionPacket.detailID)
                                {
                                    case (byte)Definer.EDID.SPAWN:
                                        //アクターをスポーンさせる

                                        //ActorControllerインスタンスを作りDictionaryに加える
                                        ActorController actorController;

                                        if (receivedActionPacket.targetID == this.sessionID) //targetIDが自分のsessionIDと同じなら
                                        {
                                            //プレイヤーをインスタンス化しながらActorControllerを取得
                                            actorController = Instantiate(PlayerObject).GetComponent<ActorController>();
                                            playerActor = actorController; //プレイヤーのActorControllerはアクセスしやすいように取得しておく
                                        }
                                        else //他人のIDなら
                                        {
                                            //アクターををインスタンス化しながらActorControllerを取得
                                            actorController = Instantiate(ActorObject).GetComponent<ActorController>();
                                        }
                                        //アクターを指定地点へ移動させる
                                        actorController.Move(receivedActionPacket.pos, Vector3.forward);
                                        //アクターの名前を書き込み
                                        actorController.PlayerName = receivedActionPacket.msg;
                                        //アクターのゲームオブジェクト
                                        actorController.name = "Actor: " + receivedActionPacket.msg; //ActorControllerはMonoBehaviourを継承しているので"name"はオブジェクトの名称を決める
                                        actorController.gameObject.SetActive(false); //初期設定が済んだら無効化して処理を止める。ゲーム開始時に有効化して座標などをセットする

                                        //アクター辞書に登録
                                        actorDictionary.Add(receivedActionPacket.targetID, actorController);

                                        //準備が完了したアクターの数を加算
                                        preparedActors++;
                                        if (preparedActors == numOfActors) //準備完了通知をサーバに送る
                                        {
                                            Debug.Log("PSGを送信しました。");
                                            ActionPacket myPacket = new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.PSG);
                                            Header myHeader = new Header(this.sessionID, 0, 0, 0, (byte)Definer.PT.AP, myPacket.ToByte());
                                            udpGameClient.Send(myHeader.ToByte());
                                        }
                                        break;
                                }
                                break;
                        }
                        break;

                    case (byte)Definer.PT.PP:
                        //PositionPacketを受け取ったときの処理
                        Debug.Log($"Positionパケットを処理するぜ！");

                        //全アクターの位置を更新
                        PositionPacket positionPacket = new PositionPacket(receivedHeader.data); //バイト配列から変換
                        foreach (PositionPacket.PosData p in positionPacket.posDatas) //座標情報を格納している配列にforeachでアクセス
                        {
                            if (p.sessionID == this.sessionID) continue;//もし自分のIDと紐づいた座標情報なら無視する。画面がガタガタしてしまうので。
                            else
                            {
                                ActorController actorController = null;
                                //4人未満でプレイするとき、p.sessionIDが0になっている箇所がある。このときdictionaryが例外「KeyNotFoundException」をスローするのを防ぐため、TryGetValueを用いる。
                                actorDictionary.TryGetValue(p.sessionID, out actorController); //key: sessionIDに対応したvalueが見つからなければoutには何も代入されない。
                                if (actorController != null) //valueが見つかったなら
                                {
                                    actorController.Move(p.pos, p.forward); //そのまま座標をいただいて、対応するアクターの座標を書き換える。
                                }
                            }
                        }
                        break;

                    default:
                        Debug.Log($"{(Definer.PT)receivedHeader.packetType}はクライアントでは処理できないぜ。処理を終了するぜ。");
                        break;
                }
            }
        }
    }
}
