using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System.Xml.Linq;
using JetBrains.Annotations;

public class GameServerManager : MonoBehaviour
{
    private bool isRunning; //サーバーが稼働中か

    //GameServer関連のインスタンスがDisposeメソッド以外で破棄されることは想定していない。そのときはおしまいだろう。
    private UdpGameServer udpGameServer; //UdpCommunicatorを継承したUdpGameServerのインスタンス

    private ushort rcvPort; //udPGameServerの受信用ポート番号

    private ushort serverSessionID; //クライアントにサーバーを判別させるためのID

    private Queue<Header> packetQueue; //udpGameServerは”勝手に”このキューにパケットを入れてくれる。不正パケット処理なども済んだ状態で入る。

    [SerializeField] private ushort sessionPass; //サーバーに入るためのパスワード。udpGameServerのコンストラクタに渡す。

    private Dictionary<ushort, ActorController> actorDictionary; //sessionパスを鍵としてactorインスタンスを保管

    private HashSet<ushort> usedID; //sessionIDの重複防止に使う。使用済IDを記録して新規発行時にはcontainsで調べる

    private HashSet<string> usedName; //プレイヤーネームの重複防止に使う。

    [SerializeField] private int numOfPlayers; //何人のプレイヤーを募集するか
    private int preparedPlayers; //準備が完了したプレイヤーの数

    //private Dictionary<ushort, uint> sendNums; //各セッションIDを鍵として、送信番号を記録。受信管理（パケロス処理）はUDPGameServerでやる

    [SerializeField] private GameObject ActorObject; //アクターのプレハブ

    private bool inGame; //ゲームは始まっているか

    //サーバーが内部をコントロールするための通知　マップ生成など
    //クライアントサーバーのクライアント部分の処理をここでやると機能過多になるため、通知を飛ばすだけにする。脳が体内の器官に命令を送るようなイメージ。実行するのはあくまで器官側。
    public enum SERVER_INTERNAL_EVENT
    { 
        GENERATE_MAP = 0, //マップを生成せよ
    }

    public Subject<SERVER_INTERNAL_EVENT> ServerInternalSubject;

    #region ボタンが押されたらサーバーを有効化したり無効化したり
    public void InitObservation(UdpButtonManager udpUIManager)
    {
        ServerInternalSubject = new Subject<SERVER_INTERNAL_EVENT>();
        udpUIManager.udpUIManagerSubject.Subscribe(e => ProcessUdpManagerEvent(e));
    }

    private void ProcessUdpManagerEvent(UdpButtonManager.UDP_BUTTON_EVENT e)
    {
        switch (e)
        {
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_START_SERVER_MODE:
                udpGameServer = new UdpGameServer(ref packetQueue, sessionPass);
                rcvPort = udpGameServer.GetReceivePort(); //受信用ポート番号とサーバーのセッションIDがここで決まるので取得
                serverSessionID = udpGameServer.GetServerSessionID();
                usedID.Add(serverSessionID); //サーバーIDを使用済に
                break;
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_SERVER_ACTIVATE:
                if (udpGameServer == null) udpGameServer = new UdpGameServer(ref packetQueue, sessionPass);
                isRunning = true;
                break;
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_SERVER_DEACTIVATE:
                udpGameServer.Dispose();
                isRunning = false;
                break;
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_BACK_TO_SELECT:
                if (udpGameServer != null) udpGameServer.Dispose();
                isRunning = false;
                break;
            default:
                break;
        }
    }
    #endregion

    private void Start()
    {
        packetQueue = new Queue<Header>();
        actorDictionary = new Dictionary<ushort, ActorController>();
        usedID = new HashSet<ushort>();
        usedName = new HashSet<string>();

        //sendNums = new Dictionary<ushort, uint>();

        //sessionIDについて、0はsessionIDを持っていないクライアントを表すナンバーなので、予め使用済にしておく。
        usedID.Add(0);

        inGame = false;

        //パケットの処理をUpdateでやると1フレームの計算量が保障できなくなる（カクつきの原因になり得る）のでマルチスレッドで
        //スレッドが何個いるのかは試してみないと分からない
        Task.Run(() => ProcessPacket());
        Task.Run(() => SendAllActorsPosition());
    }

    private async void SendAllActorsPosition()
    {
        //ゲーム開始を待つ
        await UniTask.WaitUntil(() => inGame); //ここは本来ハローパケットの送信処理から切り替えるべきだがまだ実装しない

        //返信用の変数宣言
        PositionPacket myPositionPacket = new PositionPacket();
        Header myHeader;

        while (true)
        {
            int index = 0; //foreachしながら配列に順にアクセスするためのindex
            //アクターの座標をPPで送信
            foreach (KeyValuePair<ushort, ActorController> k in actorDictionary)
            {
                //DictionaryコレクションはHashSet同様登録順が保持されないが、この配列への書き込みは順不同でよい。ご安心を。
                myPositionPacket.posDatas[index] = new PositionPacket.PosData(k.Key, k.Value.transform.position, k.Value.transform.forward);
                index++;
            }
            myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.PP, myPositionPacket.ToByte());
            udpGameServer.Send(myHeader.ToByte());

            await UniTask.Delay(100);
        }
    }

    private async void ProcessPacket()
    {
        //返信用クラスを外側のスコープで宣言しておく
        InitPacketServer myInitPacket;
        ActionPacket myActionPacket;
        Header myHeader;

        while (true)
        {
            //稼働状態になるのを待つ
            await UniTask.WaitUntil(() => isRunning);

            while (isRunning)
            {
                //キューにパケットが入るのを待つ
                await UniTask.WaitUntil(() => packetQueue.Count > 0);

                //パケット（Headerクラス）を取り出す
                Header receivedHeader = packetQueue.Dequeue();

                Debug.Log("パケットを受け取ったぜ！開封するぜ！");
                Debug.Log($"ヘッダーを確認するぜ！パケット種別は{(Definer.PT)receivedHeader.packetType}だぜ！");

                switch (receivedHeader.packetType)
                {
                    #region case (byte)Definer.PT.IPC: InitPacketの場合
                    case (byte)Definer.PT.IPC:

                        //InitPacketを受け取ったときの処理
                        Debug.Log($"Initパケットを処理するぜ！ActorDictionaryに追加するぜ！");

                        //クラスに変換する
                        InitPacketClient receivedInitPacket = new InitPacketClient(receivedHeader.data);

                        //送られてきたプレイヤーネームが使用済ならエラーコード1番を返す。sessionIDは登録しない。
                        if (usedName.Contains(receivedInitPacket.playerName))
                        {
                            myInitPacket = new InitPacketServer(receivedInitPacket.initSessionPass, rcvPort, receivedHeader.sessionID, 1);
                            myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.IPS, myInitPacket.ToByte());
                            udpGameServer.Send(myHeader.ToByte());

                            Debug.Log($"プレイヤーネーム:{receivedInitPacket.playerName} は既に使われていたぜ。出直してもらうぜ。");
                            break;
                        }
                        //TODO プレイヤーが規定人数集まっていたらエラーコード2番

                        //ActorControllerインスタンスを作りDictionaryに加える
                        //Actorをインスタンス化しながらActorControllerを取得
                        ActorController actorController = Instantiate(ActorObject).GetComponent<ActorController>();

                        //アクターの名前を書き込み
                        actorController.PlayerName = receivedInitPacket.playerName;
                        //アクターのゲームオブジェクト
                        actorController.name = "Actor: " + receivedInitPacket.playerName; //ActorControllerはMonoBehaviourを継承しているので"name"はオブジェクトの名称を決める
                        actorController.gameObject.SetActive(false); //初期設定が済んだら無効化して処理を止める。ゲーム開始時に有効化して座標などをセットする

                        //アクター辞書に登録
                        actorDictionary.Add(receivedHeader.sessionID, actorController);

                        usedID.Add(receivedHeader.sessionID); //このIDを使用済にする
                        usedName.Add(receivedInitPacket.playerName); //登録したプレイヤーネームを使用済にする

                        //TODO 送信番号の記録開始
                        //sendNums.Add(receivedHeader.sessionID, 0);

                        Debug.Log($"sessionID:{receivedHeader.sessionID},プレイヤーネーム:{receivedInitPacket.playerName} でactorDictionaryに登録したぜ！");
                        Debug.Log($"actorDictionaryには現在、{actorDictionary.Count}人のプレイヤーが登録されているぜ！");

                        //パケットを返信する
                        myInitPacket = new InitPacketServer(receivedInitPacket.initSessionPass, rcvPort, receivedHeader.sessionID);
                        myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.IPS, myInitPacket.ToByte());
                        udpGameServer.Send(myHeader.ToByte());
                        Debug.Log($"パケット返信したぜ！");

                        //規定人数のプレイヤーが集まった時の処理
                        if (actorDictionary.Count == numOfPlayers)
                        {
                            Debug.Log($"十分なプレイヤーが集まったぜ。闇のゲームの始まりだぜ。");

                            //TODO Dictionaryへの登録を締め切る処理

                            //ゲーム開始処理
                            //内部通知
                            ServerInternalSubject.OnNext(SERVER_INTERNAL_EVENT.GENERATE_MAP); //マップを生成せよ

                            //全クライアントにアクターの生成命令を送る

                            Debug.Log($"パケット送ってゲームはじめるぜ。");

                            //何人分のアクターを生成すべきか伝える
                            myActionPacket = new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.PSG, (ushort)actorDictionary.Count);
                            myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                            udpGameServer.Send(myHeader.ToByte());

                            Debug.Log($"{actorDictionary.Count}人分のアクターを生成すべきだと伝えたぜ。");

                            //4つのリスポーン地点を取得する
                            Vector3[] respawnPoints = MapGenerator.instance.Get4RespawnPointsRandomly(); //テストプレイでは4人未満でデバッグするかもしれないが、そのときは先頭の要素だけ使う
                            int index = 0;

                            foreach (KeyValuePair<ushort, ActorController> k in actorDictionary)
                            {
                                //リスポーン地点を参照しながら各プレイヤーの名前とIDを載せてアクター生成命令を飛ばす
                                myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.SPAWN, k.Key, respawnPoints[index], default, k.Value.PlayerName);
                                myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                udpGameServer.Send(myHeader.ToByte());
                                index++;
                            }

                            Debug.Log($"アクターを生成命令を出したぜ。");

                            foreach (KeyValuePair<ushort, ActorController> k in actorDictionary)
                            {
                                Debug.Log($"辞書の内容{k.Key}:{k.Value.PlayerName}");
                            }
                        }
                        break;
                    #endregion
                    #region (byte)Definer.PT.AP: ActionPacketの場合
                    case (byte)Definer.PT.AP:

                        //ActionPacketを受け取ったときの処理
                        Debug.Log($"Actionパケットを処理するぜ！");

                        ActionPacket receivedActionPacket = new ActionPacket(receivedHeader.data);

                        switch (receivedActionPacket.roughID)
                        {
                            #region case (byte)Definer.RID.MOV: Moveの場合
                            case (byte)Definer.RID.MOV:
                                //アクター辞書からアクターの座標を更新
                                actorDictionary[receivedActionPacket.targetID].Move(receivedActionPacket.pos, receivedActionPacket.pos2);
                                break;
                            #endregion
                            #region case (byte)Definer.RID.NOT: Noticeの場合
                            case (byte)Definer.RID.NOT:
                                switch (receivedActionPacket.detailID)
                                {
                                    case (byte)Definer.NDID.PSG:
                                        preparedPlayers++; //準備ができたプレイヤーの人数を加算
                                        if (preparedPlayers == numOfPlayers) //全プレイヤーの準備ができたら
                                        {
                                            //ゲーム開始命令を送る
                                            myActionPacket = new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.STG);
                                            myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                            udpGameServer.Send(myHeader.ToByte());

                                            Debug.Log("やったー！全プレイヤーの準備ができたよ！");

                                            //全アクターの有効化
                                            foreach (KeyValuePair<ushort, ActorController> k in actorDictionary)
                                            {
                                                k.Value.gameObject.SetActive(true);
                                            }
                                            //ゲーム開始
                                            inGame = true;
                                        }
                                        break;
                                }
                                break;
                            #endregion
                            #region (byte)Definer.RID.REQ: Requestの場合
                            case (byte)Definer.RID.REQ:
                                switch (receivedActionPacket.detailID)
                                {
                                    case (byte)Definer.REID.MISS: //空振り
                                        myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.PUNCH, receivedHeader.sessionID);
                                        myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                        udpGameServer.Send(myHeader.ToByte());
                                        break;
                                    case (byte)Definer.REID.HIT_FRONT: //正面に命中
                                        //パンチの同期
                                        myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.PUNCH, receivedHeader.sessionID);
                                        myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                        udpGameServer.Send(myHeader.ToByte());
                                        //被パンチ者に通達
                                        myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.HIT_FRONT, receivedActionPacket.targetID);
                                        myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                        udpGameServer.Send(myHeader.ToByte());
                                        break;
                                    case (byte)Definer.REID.HIT_BACK: //背面に命中
                                        //パンチの同期
                                        myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.PUNCH, receivedHeader.sessionID);
                                        myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                        udpGameServer.Send(myHeader.ToByte());
                                        //被パンチ者に通達
                                        myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.HIT_BACK, receivedActionPacket.targetID);
                                        myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                        udpGameServer.Send(myHeader.ToByte());
                                        break;
                                }
                                break;
                            #endregion
                        }

                        break;
                    #endregion
                    default:
                        Debug.Log($"{(Definer.PT)receivedHeader.packetType}はサーバーでは処理できないぜ。処理を終了するぜ。");
                        break;
                }
            }
        }
    }

    public Dictionary<ushort, ActorController> PropertyActorDictionary { get { return actorDictionary; } }
}
