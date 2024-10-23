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
    private Dictionary<ushort, Entity> entityDictionary; //entityIDを鍵としてentityインスタンスを管理

    private ActorController playerActor; //プレイヤーが操作するキャラクターのActorController


    private int numOfActors; //アクターの人数
    private int preparedActors; //生成し終わったアクターの数

    [SerializeField] private GameObject ActorPrefab; //アクターのプレハブ
    [SerializeField] private GameObject PlayerPrefab; //プレイヤーのプレハブ
    [SerializeField] private GameObject GoldPilePrefab; //金貨の山のプレハブ
    [SerializeField] private GameObject ChestPrefab; //宝箱のプレハブ

    private bool inGame; //ゲームは始まっているか

    //クライアントが内部をコントロールするための通知　マップ生成など
    public enum CLIENT_INTERNAL_EVENT
    {
        GENERATE_MAP = 0, //マップを生成せよ
        EDIT_GUI_FOR_GAME, //インゲーム用のUIレイアウトに変更せよ
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
        entityDictionary = new Dictionary<ushort, Entity>();

        Task.Run(() => ProcessPacket());
        Task.Run(() => SendPlayerPosition());
    }

    private async void SendPlayerPosition()
    {
        //ゲーム開始を待つ
        await UniTask.WaitUntil(() => inGame); //ここは本来ハローパケットの送信処理から切り替えるべきだがまだ実装しない

        //返信用クラスを外側のスコープで宣言しておく
        ActionPacket myActionPacket;
        Header myHeader;

        while (true) 
        {
            //プレイヤーアクターの座標をMOVで送信
            myActionPacket = new ActionPacket((byte)Definer.RID.MOV, default, sessionID, default, playerActor.transform.position, playerActor.transform.forward);
            myHeader = new Header(this.sessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
            udpGameClient.Send(myHeader.ToByte());

            await UniTask.Delay(100);
        }
    }

    private async void ProcessPacket()
    {
        //返信用クラスを外側のスコープで宣言しておく
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
                            #region case (byte)Definer.RID.NOT: Noticeの場合
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
                                        //内部通知
                                        ClientInternalSubject.OnNext(CLIENT_INTERNAL_EVENT.GENERATE_MAP); //マップを生成せよ
                                        ClientInternalSubject.OnNext(CLIENT_INTERNAL_EVENT.EDIT_GUI_FOR_GAME); //UIレイアウトを変更せよ
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
                            #endregion
                            case (byte)Definer.RID.EXE:
                                switch (receivedActionPacket.detailID)
                                {
                                    #region case (byte)Definer.EDID.SPAWN:の場合
                                    case (byte)Definer.EDID.SPAWN_ACTOR:
                                        //アクターをスポーンさせる

                                        //ActorControllerインスタンスを作りDictionaryに加える
                                        ActorController actorController;

                                        if (receivedActionPacket.targetID == this.sessionID) //targetIDが自分のsessionIDと同じなら
                                        {
                                            //プレイヤーをインスタンス化しながらActorControllerを取得
                                            actorController = Instantiate(PlayerPrefab).GetComponent<ActorController>();
                                            actorController.gameObject.GetComponent<Player>().SessionID = this.sessionID; //PlayerクラスにはActorControllerとは別にSessionIDを渡しておく。パケット送信を楽にするため。
                                            playerActor = actorController; //プレイヤーのActorControllerはアクセスしやすいように取得しておく
                                            playerActor.gameObject.GetComponent<Player>().GetUdpGameClient(this.udpGameClient, this.sessionID);
                                        }
                                        else //他人のIDなら
                                        {
                                            //アクターををインスタンス化しながらActorControllerを取得
                                            actorController = Instantiate(ActorPrefab).GetComponent<ActorController>();
                                        }
                                        //アクターを指定地点へ移動させる
                                        actorController.Move(receivedActionPacket.pos, Vector3.forward);
                                        //アクターの名前を書き込み
                                        actorController.PlayerName = receivedActionPacket.msg;
                                        //アクターのSessionIDを書き込み
                                        actorController.SessionID = receivedActionPacket.targetID;
                                        //アクターのゲームオブジェクト設定
                                        actorController.name = $"Actor: {actorController.PlayerName} ({actorController.SessionID})"; //ActorControllerはMonoBehaviourを継承しているので"name"はオブジェクトの名称を決める
                                        actorController.gameObject.SetActive(false); //初期設定が済んだら無効化して処理を止める。ゲーム開始時に有効化して座標などをセットする

                                        //アクター辞書に登録
                                        actorDictionary.Add(receivedActionPacket.targetID, actorController);

                                        //準備が完了したアクターの数を加算
                                        //ここバグ疑惑あり
                                        preparedActors++;
                                        if (preparedActors == numOfActors) //準備完了通知をサーバに送る
                                        {
                                            Debug.Log("PSGを送信しました。");
                                            myActionPacket = new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.PSG);
                                            myHeader = new Header(this.sessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                            udpGameClient.Send(myHeader.ToByte());
                                        }
                                        break;
                                    #endregion
                                    case (byte)Definer.EDID.PUNCH:
                                        if (receivedActionPacket.targetID == this.sessionID) break; //プレイヤーが行ったパンチなら無視
                                        actorDictionary[receivedActionPacket.targetID].PunchAnimation();
                                        break;
                                    case (byte)Definer.EDID.HIT_FRONT:
                                        //殴られたのがプレイヤーなら
                                        if (receivedActionPacket.targetID == this.sessionID)
                                        {
                                            //プレイヤー側で演出
                                        }
                                        else
                                        {
                                            //他人が殴られたならモーション同期
                                            actorDictionary[receivedActionPacket.targetID].RecoiledAnimation();
                                        }
                                        break;
                                    case (byte)Definer.EDID.HIT_BACK:
                                        //殴られたのがプレイヤーなら
                                        if (receivedActionPacket.targetID == this.sessionID)
                                        {
                                            //プレイヤー側で演出
                                            playerActor.Blown(receivedActionPacket.pos); //パンチの方向に吹っ飛ぶ
                                        }
                                        else
                                        {
                                            //他人が殴られたならモーション同期。吹っ飛びの同期はPositionPacketで自動的になされる
                                            actorDictionary[receivedActionPacket.targetID].BlownAnimation();
                                        }
                                        break;
                                    case (byte)Definer.EDID.EDIT_GOLD:
                                        //所持金変更の対象がプレイヤーなら
                                        if (receivedActionPacket.targetID == this.sessionID)
                                        {
                                            //プレイヤー側で演出
                                        }
                                        //指定されたアクターの所持金を編集
                                        actorDictionary[receivedActionPacket.targetID].Gold += receivedActionPacket.value;
                                        Debug.Log($"{actorDictionary[receivedActionPacket.targetID].PlayerName}が({receivedActionPacket.value})ゴールドを入手。現在の所持金は({actorDictionary[receivedActionPacket.targetID].Gold})ゴールド。");
                                        break;
                                    case (byte)Definer.EDID.SPAWN_CHEST:
                                        //オブジェクトを生成しつつ、エンティティのコンポーネントを取得
                                        //chestという変数名をここでだけ使いたいのでブロック文でスコープ分け
                                        {
                                            Chest chest = Instantiate(ChestPrefab, receivedActionPacket.pos, Quaternion.identity).GetComponent<Chest>();
                                            entityDictionary.Add(receivedActionPacket.targetID, chest); //管理用のIDと共に辞書へ
                                            chest.EntityID = receivedActionPacket.targetID; //ID割り当て
                                            chest.Tier = receivedActionPacket.value; //金額設定
                                            chest.name = $"Chest ({receivedActionPacket.targetID})";
                                        }
                                        break;
                                    case (byte)Definer.EDID.SPAWN_GOLDPILE:
                                        //オブジェクトを生成しつつ、エンティティのコンポーネントを取得
                                        //goldPileという変数名をここでだけ使いたいのでブロック文でスコープ分け
                                        {
                                            GoldPile goldPile = Instantiate(GoldPilePrefab, receivedActionPacket.pos, Quaternion.identity).GetComponent<GoldPile>();
                                            entityDictionary.Add(receivedActionPacket.targetID, goldPile); //管理用のIDと共に辞書へ
                                            goldPile.EntityID = receivedActionPacket.targetID; //ID割り当て
                                            goldPile.Value = receivedActionPacket.value; //金額設定
                                            goldPile.name = $"GoldPile ({receivedActionPacket.targetID})";
                                        }
                                        break;
                                    case (byte)Definer.EDID.DESTROY_ENTITY:
                                        //エンティティを動的ディスパッチしてオーバーライドされたDestroyメソッド実行
                                        entityDictionary[receivedActionPacket.targetID].Destroy();
                                        entityDictionary.Remove(receivedActionPacket.targetID);
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
