using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System.Collections.Concurrent;

public class GameServerManager : MonoBehaviour
{
    private bool isRunning; //サーバーが稼働中か

    //GameServer関連のインスタンスがDisposeメソッド以外で破棄されることは想定していない。そのときはおしまいだろう。
    private UdpGameServer udpGameServer; //UdpCommunicatorを継承したUdpGameServerのインスタンス
    private ushort rcvPort; //udpGameServerの受信用ポート番号
    private ushort serverSessionID; //クライアントにサーバーを判別させるためのID
    private ConcurrentQueue<Header> packetQueue; //udpGameServerは”勝手に”このキューにパケットを入れてくれる。不正パケット処理なども済んだ状態で入る。

    private Dictionary<ushort, ActorController> actorDictionary; //sessionパスを鍵としてactorインスタンスを管理
    private Dictionary<ushort, Entity> entityDictionary; //entityIDを鍵としてentityインスタンスを管理

    private HashSet<string> usedName; //プレイヤーネームの重複防止に使う。
    private HashSet<ushort> usedEntityID; //EntityIDの重複防止に使う。

    [SerializeField] private ushort sessionPass; //サーバーに入るためのパスワード。udpGameServerのコンストラクタに渡す。
    [SerializeField] private int numOfPlayers; //何人のプレイヤーを募集するか
    private int preparedPlayers; //準備が完了したプレイヤーの数

    [SerializeField] private int maxNumOfChests; //現在の（ゲーム開始時もそう）宝箱の同時出現数の上限。この値より少なければ生成される。
    //動的に減らしたり増やしたりしても問題ないが、宝箱の出現候補地点の数より多くならないように注意が必要。宝箱が同じ位置に重なって生成されてしまう。
    private int currentNumOfChests; //現在生成されている宝箱の数

    [SerializeField] private GameObject ActorPrefab; //アクターのプレハブ
    [SerializeField] private GameObject GoldPilePrefab; //金貨の山のプレハブ
    [SerializeField] private GameObject ChestPrefab; //宝箱のプレハブ
    [SerializeField] private GameObject ScrollPrefab; //巻物のプレハブ
    [SerializeField] private GameObject ThunderPrefab; //雷のプレハブ

    private bool inGame; //ゲームは始まっているか

    [SerializeField] private BGGradientController gradientController;

    //12/29追記
    [SerializeField] TitleUI _titleUi;

    //サーバーが内部をコントロールするための通知　マップ生成など
    //クライアントサーバーのクライアント部分の処理をここでやると機能過多になるため、通知を飛ばすだけにする。脳が体内の器官に命令を送るようなイメージ。実行するのはあくまで器官側。
    public enum SERVER_INTERNAL_EVENT
    {
        GENERATE_MAP = 0, //マップを生成せよ
        EDIT_GUI_FOR_GAME, //インゲーム用のUIレイアウトに変更せよ
    }

    public Subject<SERVER_INTERNAL_EVENT> ServerInternalSubject;

    //魔法抽選用コンポーネント
    private MagicLottely magicLottely;

    //レイを飛ばすためのカメラ
    [SerializeField] private Camera mapCamera;

    #region Stateインターフェース
    public interface ISetverState
    {
        void EnterState(GameServerManager gameServerManager, Definer.MID magicID, ushort magicUserID);
        void UpdateProcess(GameServerManager gameServerManager);
        void ExitState(GameServerManager gameServerManager);
    }

    //現在のstate
    private ISetverState currentSetverState;
    //魔法の実行待機ならtrue
    private bool isAwaitingMagic;
    //待機中の魔法の種類
    private Definer.MID awaitingMagicID;
    //魔法を使用をしようとしているプレイヤーのsessionID
    private ushort magicUserID;

    //魔法IDを渡しつつStateの切り替え
    public void ChangeServerState(ISetverState newState, Definer.MID magicID = Definer.MID.NONE, ushort magicUserID = 0)
    {
        if (currentSetverState != null) currentSetverState.ExitState(this);
        currentSetverState = newState;
        currentSetverState.EnterState(this, magicID, magicUserID);
    }

    //通常の状態
    public class NormalState : ISetverState
    {
        public void EnterState(GameServerManager gameServerManager, Definer.MID magicID, ushort magicUserID)
        {
        }

        public void UpdateProcess(GameServerManager gameServerManager)
        {
            //if (Input.GetKeyDown(KeyCode.Alpha1))
            //{
            //    gameServerManager.gradientController.State = BGGradientController.BG_GRAD_STATE.NORMAL;
            //}
            //else if (Input.GetKeyDown(KeyCode.Alpha2))
            //{
            //    gameServerManager.gradientController.State = BGGradientController.BG_GRAD_STATE.IN_USE_PLAYER_1;
            //}
            //else if (Input.GetKeyDown(KeyCode.Alpha3))
            //{
            //    gameServerManager.gradientController.State = BGGradientController.BG_GRAD_STATE.IN_USE_PLAYER_2;
            //}
            //else if (Input.GetKeyDown(KeyCode.Alpha4))
            //{
            //    gameServerManager.gradientController.State = BGGradientController.BG_GRAD_STATE.IN_USE_PLAYER_3;
            //}
            //else if (Input.GetKeyDown(KeyCode.Alpha5))
            //{
            //    gameServerManager.gradientController.State = BGGradientController.BG_GRAD_STATE.IN_USE_PLAYER_4;
            //}
        }

        public void ExitState(GameServerManager gameServerManager)
        {
        }
    }

    //魔法のための画面タッチを待機している状態
    public class AwaitTouchState : ISetverState
    {
        public void EnterState(GameServerManager gameServerManager, Definer.MID magicID, ushort magicUserID)
        {
            gameServerManager.isAwaitingMagic = true;
            gameServerManager.awaitingMagicID = magicID;
            gameServerManager.magicUserID = magicUserID;
        }

        public void UpdateProcess(GameServerManager gameServerManager)
        {
            switch (gameServerManager.awaitingMagicID)
            {
                //雷待機
                case Definer.MID.THUNDER:
                    if (Input.GetMouseButtonDown(0))
                    {
                        //カメラの位置からタッチした位置に向けrayを飛ばす
                        RaycastHit hit;
                        Ray ray = gameServerManager.mapCamera.ScreenPointToRay(Input.mousePosition);

                        //rayがなにかに当たったら調べる
                        if (Physics.Raycast(ray, out hit))
                        {
                            Debug.Log(hit.collider.gameObject.name);

                            switch (hit.collider.gameObject.tag)
                            {
                                case "Floor": //床にタッチしたら雷落とす
                                              //！あぶない！　ここで雷を生成すると最悪entityDictionaryが別スレッドの処理とぶつかってデッドロックして世界が終わるよ
                                    ActionPacket myActionPacket; //いったい何をするの！？
                                    Header myHeader; //パケットの送信はメインスレッドでやらないことにしてるよね！？大丈夫！？

                                    //INTERNAL_THUNDER?? サーバーが一体なぜリクエストパケットを？
                                    Vector3 thunderPos = new Vector3(hit.collider.gameObject.transform.position.x, 0.5f, hit.collider.gameObject.transform.position.z);

                                    myActionPacket = new ActionPacket((byte)Definer.RID.REQ, (byte)Definer.REID.INTERNAL_THUNDER, default, default, thunderPos);
                                    myHeader = new Header(gameServerManager.serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());

                                    //そうか！サーバー内部から別スレッドで使われているConcurrentQueueにパケットを直接エンキューすることで、
                                    //ネットワークを介さずとも他の処理と同様のフローでオブジェクト生成を実行できるだけでなく、
                                    //プレイヤー達のパケット処理の順番を待ってから処理されることでゲームルールの公平性も確保されるんだね！すごいや！
                                    gameServerManager.packetQueue.Enqueue(myHeader);
                                    //Monobehaviorの処理をパケット処理スレッドに一任している（これは俺が決めました）以上、
                                    //entityDictionaryをスレッドセーフなコレクションにしてUpdate()内でオブジェクト生成を行うことは禁忌だし、この実装が一番よさそうだね！
                                    //参考:https://learn.microsoft.com/ja-jp/dotnet/standard/collections/thread-safe/when-to-use-a-thread-safe-collection
                                    //スレッドセーフにしてくれてマジ、感謝。

                                    //魔法が正しく実行されたことを通知
                                    myActionPacket = new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.END_MAGIC_SUCCESSFULLY, gameServerManager.magicUserID);
                                    myHeader = new Header(gameServerManager.serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                    gameServerManager.udpGameServer.Send(myHeader.ToByte());

                                    gameServerManager.ChangeServerState(new NormalState()); //雷を落としたらノーマルステートに戻る
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    break;
                case Definer.MID.TELEPORT:
                    if (Input.GetMouseButtonDown(0))
                    {
                        //カメラの位置からタッチした位置に向けrayを飛ばす
                        RaycastHit hit;
                        Ray ray = gameServerManager.mapCamera.ScreenPointToRay(Input.mousePosition);

                        //rayがなにかに当たったら調べる
                        if (Physics.Raycast(ray, out hit))
                        {
                            Debug.Log(hit.collider.gameObject.name);

                            switch (hit.collider.gameObject.tag)
                            {
                                case "Floor": //床にタッチしたら転移
                                    ActionPacket myActionPacket;
                                    Header myHeader;

                                    //テレポートを実行
                                    Vector3 teleportPos = new Vector3(hit.collider.gameObject.transform.position.x, 0.5f, hit.collider.gameObject.transform.position.z);

                                    myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.TELEPORT_ACTOR, gameServerManager.magicUserID, default, teleportPos);
                                    myHeader = new Header(gameServerManager.serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                    gameServerManager.udpGameServer.Send(myHeader.ToByte());

                                    //魔法が正しく実行されたことを通知
                                    myActionPacket = new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.END_MAGIC_SUCCESSFULLY, gameServerManager.magicUserID);
                                    myHeader = new Header(gameServerManager.serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                    gameServerManager.udpGameServer.Send(myHeader.ToByte());

                                    gameServerManager.ChangeServerState(new NormalState()); //転移先を指定したらノーマルステートに戻る
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public void ExitState(GameServerManager gameServerManager)
        {
            gameServerManager.isAwaitingMagic = false;
        }
    }
    #endregion

    #region ボタンが押されたらサーバーを有効化したり無効化したり
    //public void InitObservation(UdpButtonManager udpUIManager)
    //{
    //    ServerInternalSubject = new Subject<SERVER_INTERNAL_EVENT>();
    //    udpUIManager.udpUIManagerSubject.Subscribe(e => ProcessUdpManagerEvent(e));
    //}

    public void InitObservation(Title title)
    {
        ServerInternalSubject = new Subject<SERVER_INTERNAL_EVENT>();
        title.titleButtonSubject.Subscribe(e => ProcessUdpManagerEvent(e));
    }

    //private void ProcessUdpManagerEvent(UdpButtonManager.UDP_BUTTON_EVENT e)
    //{
    //    switch (e)
    //    {
    //        case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_START_SERVER_MODE:
    //            udpGameServer = new UdpGameServer(ref packetQueue, sessionPass);
    //            rcvPort = udpGameServer.GetReceivePort(); //受信用ポート番号とサーバーのセッションIDがここで決まるので取得
    //            serverSessionID = udpGameServer.GetServerSessionID();
    //            break;
    //        case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_SERVER_ACTIVATE:
    //            if (udpGameServer == null) udpGameServer = new UdpGameServer(ref packetQueue, sessionPass);
    //            isRunning = true;
    //            break;
    //        case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_SERVER_DEACTIVATE:
    //            if (isRunning) //稼働中なら切断パケット
    //            {
    //                udpGameServer.Send(new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.DISCONNECT).ToByte()).ToByte());
    //            }
    //            if (udpGameServer != null) udpGameServer.Dispose();
    //            udpGameServer = null;
    //            actorDictionary.Clear(); //変数リセットなど
    //            preparedPlayers = 0;
    //            isRunning = false;
    //            break;
    //        case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_BACK_TO_SELECT:
    //            if (udpGameServer != null) udpGameServer.Dispose();
    //            udpGameServer = null;
    //            isRunning = false;
    //            break;
    //        default:
    //            break;
    //    }
    //}
    private void ProcessUdpManagerEvent(Title.TITLE_BUTTON_EVENT titlebuttonEvent)
    {
        switch (titlebuttonEvent)
        {
            case Title.TITLE_BUTTON_EVENT.BUTTON_START_SERVER_ACTIVATE:
                udpGameServer = new UdpGameServer(ref packetQueue, sessionPass);
                rcvPort = udpGameServer.GetReceivePort(); //受信用ポート番号とサーバーのセッションIDがここで決まるので取得
                serverSessionID = udpGameServer.GetServerSessionID();
                isRunning = true;
                break;
            //case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_SERVER_DEACTIVATE:
            //    if (isRunning) //稼働中なら切断パケット
            //    {
            //        udpGameServer.Send(new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.DISCONNECT).ToByte()).ToByte());
            //    }
            //    if (udpGameServer != null) udpGameServer.Dispose();
            //    udpGameServer = null;
            //    actorDictionary.Clear(); //変数リセットなど
            //    preparedPlayers = 0;
            //    isRunning = false;
            //    break;
            //case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_BACK_TO_SELECT:
            //    if (udpGameServer != null) udpGameServer.Dispose();
            //    udpGameServer = null;
            //    isRunning = false;
            //    break;
            default:
                break;
        }
    }
    #endregion

    private void Start()
    {
        packetQueue = new ConcurrentQueue<Header>();
        actorDictionary = new Dictionary<ushort, ActorController>();
        entityDictionary = new Dictionary<ushort, Entity>();

        usedName = new HashSet<string>();
        usedEntityID = new HashSet<ushort>();

        inGame = false;

        //パケットの処理をUpdateでやると1フレームの計算量が保障できなくなる（カクつきの原因になり得る）のでマルチスレッドで
        //スレッドが何個いるのかは試してみないと分からない
        Task.Run(() => ProcessPacket());
        Task.Run(() => SendAllActorsPosition());

        //MagicLottely取得
        magicLottely = GetComponent<MagicLottely>();

        //State初期化
        ChangeServerState(new NormalState());
    }

    private void Update()
    {
        currentSetverState.UpdateProcess(this);
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

            await UniTask.Delay(170);
        }
    }

    private async void ProcessPacket()
    {
        //返信用クラスを外側のスコープで宣言しておく
        InitPacketServer myInitPacket;
        ActionPacket myActionPacket;
        Header myHeader;
        //オブジェクト生成用の変数を外側のスコープで宣言しておく
        ushort entityID;
        Entity entity; //TryGetValueの第二引数でoutで使う

        try
        {
            while (true)
            {
                //稼働状態になるのを待つ
                await UniTask.WaitUntil(() => isRunning);

                while (isRunning)
                {
                    //キューにパケットが入るのを待つ
                    await UniTask.WaitUntil(() => packetQueue.Count > 0);

                    //パケット（Headerクラス）を取り出す
                    Header receivedHeader;
                    packetQueue.TryDequeue(out receivedHeader);

                    Debug.Log("パケットを受け取ったぜ！開封するぜ！");
                    Debug.Log($"ヘッダーを確認するぜ！パケット種別は{(Definer.PT)receivedHeader.packetType}だぜ！");

                    switch (receivedHeader.packetType)
                    {
                        #region case (byte)Definer.PT.IPC: InitPacketの場合
                        case (byte)Definer.PT.IPC:

                            //受理済のInitPacketは無視
                            ActorController tmp;
                            if (actorDictionary.TryGetValue(receivedHeader.sessionID, out tmp))
                            {
                                Debug.Log($"SessionID: {receivedHeader.sessionID}のIPCは処理済だぜ。パケットを破棄するぜ。");
                                break;
                            }

                            //InitPacketを受け取ったときの処理
                            Debug.Log($"Initパケットを処理するぜ！ActorDictionaryに追加するぜ！");

                            //クラスに変換する
                            InitPacketClient receivedInitPacket = new InitPacketClient(receivedHeader.data);

                            //送られてきたプレイヤーネームが使用済ならエラーコード1番を返す。sessionIDは登録しない。
                            //if (usedName.Contains(receivedInitPacket.playerName))
                            //{
                            //    myInitPacket = new InitPacketServer(receivedInitPacket.initSessionPass, rcvPort, receivedHeader.sessionID, 1);
                            //    myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.IPS, myInitPacket.ToByte());
                            //    udpGameServer.Send(myHeader.ToByte());

                            //    Debug.Log($"プレイヤーネーム:{receivedInitPacket.playerName} は既に使われていたぜ。出直してもらうぜ。");
                            //    break;
                            //}
                            //TODO プレイヤーが規定人数集まっていたらエラーコード2番

                            //ActorControllerインスタンスを作りDictionaryに加える
                            //Actorをインスタンス化しながらActorControllerを取得
                            ActorController actorController = Instantiate(ActorPrefab).GetComponent<ActorController>();

                            //アクターの名前を書き込み
                            actorController.PlayerName = receivedInitPacket.playerName;
                            //アクターのゲームオブジェクト
                            actorController.name = $"Actor: {receivedInitPacket.playerName} ({receivedHeader.sessionID})"; //ActorControllerはMonoBehaviourを継承しているので"name"はオブジェクトの名称を決める
                            actorController.gameObject.SetActive(false); //初期設定が済んだら無効化して処理を止める。ゲーム開始時に有効化して座標などをセットする

                            //アクター辞書に登録
                            actorDictionary.Add(receivedHeader.sessionID, actorController);

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
                                ServerInternalSubject.OnNext(SERVER_INTERNAL_EVENT.EDIT_GUI_FOR_GAME); //UIレイアウトを変更せよ

                                _titleUi.ChangeStateServer(TitleUI.SERVER_MODE.MODE_CREATE_MAP);

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
                                    myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.SPAWN_ACTOR, k.Key, default, respawnPoints[index], default, k.Value.PlayerName);
                                    myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                    udpGameServer.Send(myHeader.ToByte());
                                    index++;
                                }

                                Debug.Log($"アクターを生成命令を出したぜ。");

                                for (int i = 0; i < maxNumOfChests; i++) //宝箱が上限数に達するまで宝箱を生成する
                                {
                                    //まずサーバー側のシーンで
                                    entityID = GetUniqueEntityID(); //エンティティID生成
                                    Vector3 chestPos = MapGenerator.instance.GetUniqueChestPointRandomly(); //座標決め
                                    Chest chest = Instantiate(ChestPrefab, chestPos, Quaternion.identity).GetComponent<Chest>();
                                    chest.EntityID = entityID; //ID書き込み
                                    chest.Tier = 1; //レア度はまだ適当に1
                                    chest.gameObject.name = $"Chest ({entityID})";
                                    entityDictionary.Add(entityID, chest); //辞書に登録

                                    //ティア（１）と座標を指定して、宝箱を生成する命令
                                    myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.SPAWN_CHEST, entityID, 1, chestPos);
                                    myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                    udpGameServer.Send(myHeader.ToByte());

                                    //宝箱の数を記録
                                    currentNumOfChests++;
                                }
                            }
                            break;
                        #endregion
                        #region case (byte)Definer.PT.AP: ActionPacketの場合
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
                                        case (byte)Definer.NDID.DISCONNECT:
                                            if (inGame)
                                            {
                                                //ゲーム中の切断処理
                                            }
                                            else
                                            {
                                                Debug.Log($"{receivedActionPacket.targetID}からのセッション切断通知がありました。クライアント登録・アクター登録を抹消します。");
                                                //サーバー側で登録の抹消
                                                usedName.Remove(actorDictionary[receivedActionPacket.targetID].PlayerName);
                                                Destroy(actorDictionary[receivedActionPacket.targetID].gameObject);
                                                udpGameServer.RemoveClientFromDictionary(receivedActionPacket.targetID);
                                                actorDictionary.Remove(receivedActionPacket.targetID);
                                                //各クライアントにも通知
                                                myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.DELETE_ACTOR, receivedActionPacket.targetID);
                                                myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                udpGameServer.Send(myHeader.ToByte());
                                            }
                                            break;
                                    }
                                    break;
                                #endregion
                                #region (byte)Definer.RID.REQ: Requestの場合
                                case (byte)Definer.RID.REQ:

                                    Debug.Log($"REQ受信:{(Definer.REID)receivedActionPacket.detailID}");

                                    switch (receivedActionPacket.detailID)
                                    {
                                        #region case パンチ系:
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
                                            myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.HIT_BACK, receivedActionPacket.targetID, default, receivedActionPacket.pos);
                                            myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                            udpGameServer.Send(myHeader.ToByte());

                                            //所持金の計算
                                            int gold = actorDictionary[receivedActionPacket.targetID].Gold;
                                            Debug.Log($"{actorDictionary[receivedActionPacket.targetID].name}のGold:{actorDictionary[receivedActionPacket.targetID].Gold}");
                                            int lostGold = gold / 2; //所持金半減。小数点以下切り捨て。
                                            Debug.Log($"{actorDictionary[receivedActionPacket.targetID].name}の取得されたgold:{gold}");
                                            Debug.Log($"{actorDictionary[receivedActionPacket.targetID].name}のLostGold:{lostGold}");

                                            if (lostGold == 0) break;//lostGoldが0なら処理終了

                                            //被パンチ者の所持金を減らす
                                            actorDictionary[receivedActionPacket.targetID].Gold -= lostGold;//まずサーバー側で
                                            myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.EDIT_GOLD, receivedActionPacket.targetID, -lostGold);
                                            myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                            udpGameServer.Send(myHeader.ToByte());

                                            //重複しないentityIDを作り、オブジェクトを生成しつつ、エンティティのコンポーネントを取得
                                            //goldPileという変数名をここでだけ使いたいのでブロック文でスコープ分け
                                            {
                                                entityID = GetUniqueEntityID();
                                                Vector3 goldPos = new Vector3(actorDictionary[receivedActionPacket.targetID].transform.position.x, 0.1f, actorDictionary[receivedActionPacket.targetID].transform.position.z);
                                                GoldPile goldPile = Instantiate(GoldPilePrefab, goldPos, Quaternion.identity).GetComponent<GoldPile>();
                                                goldPile.EntityID = entityID; //値を書き込み
                                                goldPile.Value = lostGold;
                                                goldPile.name = $"GoldPile ({entityID})";
                                                entityDictionary.Add(entityID, goldPile); //管理用のIDと共に辞書へ

                                                //金額を指定して、殴られた人の足元に金貨の山を生成する命令
                                                myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.SPAWN_GOLDPILE, entityID, lostGold, goldPos);
                                                myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                udpGameServer.Send(myHeader.ToByte());
                                            }
                                            break;
                                        #endregion
                                        #region case 金貨の山・巻物系:
                                        case (byte)Definer.REID.GET_GOLDPILE:
                                            //エンティティが存在するか確かめる。存在しないなら何もしない。エラーコードも返さない。エラーコードを返すとチーターは喜ぶ
                                            if (entityDictionary.TryGetValue(receivedActionPacket.targetID, out entity))
                                            {
                                                //エンティティをGoldPileにキャスト
                                                GoldPile goldPile = (GoldPile)entity;

                                                //存在するなら入手したプレイヤーにゴールドを振り込む
                                                //まずサーバー側で
                                                actorDictionary[receivedHeader.sessionID].Gold += goldPile.Value;
                                                myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.EDIT_GOLD, receivedHeader.sessionID, goldPile.Value);
                                                myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                udpGameServer.Send(myHeader.ToByte());
                                                //その金貨の山を消す
                                                //エンティティを動的ディスパッチしてオーバーライドされたDestroyメソッド実行
                                                entityDictionary[receivedActionPacket.targetID].DestroyEntity();
                                                entityDictionary.Remove(receivedActionPacket.targetID);
                                                usedEntityID.Remove(receivedActionPacket.targetID); //IDも解放
                                                //パケット送信
                                                myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.DESTROY_ENTITY, receivedActionPacket.targetID);
                                                myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                udpGameServer.Send(myHeader.ToByte());
                                            }
                                            break;
                                        case (byte)Definer.REID.GET_SCROLL:
                                            //エンティティが存在するか確かめる。存在しないなら何もしない。エラーコードも返さない。エラーコードを返すとチーターは喜ぶ
                                            if (entityDictionary.TryGetValue(receivedActionPacket.targetID, out entity))
                                            {
                                                //エンティティをGoldPileにキャスト
                                                Scroll scroll = (Scroll)entity;

                                                //存在するなら入手したプレイヤーに巻物を与える
                                                myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.GIVE_MAGIC, receivedHeader.sessionID, (int)scroll.MagicID);
                                                myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                udpGameServer.Send(myHeader.ToByte());
                                                //その巻物を消す
                                                //エンティティを動的ディスパッチしてオーバーライドされたDestroyメソッド実行
                                                entityDictionary[receivedActionPacket.targetID].DestroyEntity();
                                                entityDictionary.Remove(receivedActionPacket.targetID);
                                                usedEntityID.Remove(receivedActionPacket.targetID); //IDも解放
                                                //パケット送信
                                                myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.DESTROY_ENTITY, receivedActionPacket.targetID);
                                                myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                udpGameServer.Send(myHeader.ToByte());
                                            }
                                            break;
                                        #endregion
                                        #region case 宝箱系:
                                        case (byte)Definer.REID.OPEN_CHEST_SUCCEED:
                                            //エンティティが存在するか確かめる。存在しないなら何もしない。エラーコードも返さない。エラーコードを返すとチーターは喜ぶ
                                            if (entityDictionary.TryGetValue(receivedActionPacket.targetID, out entity))
                                            {
                                                //エンティティをChestにキャスト
                                                Chest chest = (Chest)entity;

                                                //存在するなら入手したプレイヤーにランダムなゴールドを振り込む 適当に80~200ゴールド
                                                //System.Random random = new System.Random();
                                                //int chestGold = random.Next(80, 201);
                                                ////まずサーバー側で
                                                //actorDictionary[receivedHeader.sessionID].Gold += chestGold;
                                                ////ゴールド振込パケット送信
                                                //myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.EDIT_GOLD, receivedHeader.sessionID, chestGold);
                                                //myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                //udpGameServer.Send(myHeader.ToByte());

                                                //重複しないentityIDを作り、オブジェクトを生成しつつ、エンティティのコンポーネントを取得
                                                //goldPileという変数名をここでだけ使いたいのでブロック文でスコープ分け
                                                {
                                                    entityID = GetUniqueEntityID();
                                                    Vector3 goldPos = new Vector3(entityDictionary[receivedActionPacket.targetID].transform.position.x, 0.1f, entityDictionary[receivedActionPacket.targetID].transform.position.z);
                                                    GoldPile goldPile = Instantiate(GoldPilePrefab, goldPos, Quaternion.identity).GetComponent<GoldPile>();
                                                    goldPile.EntityID = entityID; //値を書き込み
                                                    System.Random random = new System.Random();
                                                    int chestGold = random.Next(80, 201);
                                                    goldPile.Value = chestGold; //ランダムなゴールド量の金貨の山を生成 適当に80~200ゴールド
                                                    goldPile.name = $"GoldPile ({entityID})";
                                                    entityDictionary.Add(entityID, goldPile); //管理用のIDと共に辞書へ

                                                    //金額を指定して、宝箱の足元に金貨の山を生成する命令
                                                    myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.SPAWN_GOLDPILE, entityID, chestGold, goldPos);
                                                    myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                    udpGameServer.Send(myHeader.ToByte());
                                                }

                                                //重複しないentityIDを作り、オブジェクトを生成しつつ、エンティティのコンポーネントを取得
                                                //goldPileという変数名をここでだけ使いたいのでブロック文でスコープ分け
                                                {
                                                    entityID = GetUniqueEntityID();
                                                    Vector3 scrollPos = new Vector3(entityDictionary[receivedActionPacket.targetID].transform.position.x, 0.3f, entityDictionary[receivedActionPacket.targetID].transform.position.z);
                                                    Scroll scroll = Instantiate(ScrollPrefab, scrollPos, Quaternion.identity).GetComponent<Scroll>();
                                                    scroll.EntityID = entityID; //値を書き込み
                                                    //魔法（の巻物）を抽選
                                                    int scrollMagicID = magicLottely.Lottely();
                                                    scroll.MagicID = (Definer.MID)scrollMagicID;
                                                    scroll.name = $"Scroll ({entityID}) ({scrollMagicID})";
                                                    entityDictionary.Add(entityID, scroll); //管理用のIDと共に辞書へ

                                                    //魔法IDを指定して、宝箱の足元に巻物を生成する命令
                                                    myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.SPAWN_SCROLL, entityID, scrollMagicID, scrollPos);
                                                    myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                    udpGameServer.Send(myHeader.ToByte());
                                                }

                                                //その宝箱を消す
                                                //エンティティを動的ディスパッチしてオーバーライドされたDestroyメソッド実行
                                                //座標を抽選デッキに戻す
                                                MapGenerator.instance.AddChestPointToDeck(entityDictionary[receivedActionPacket.targetID].transform.position);
                                                entityDictionary[receivedActionPacket.targetID].DestroyEntity();
                                                entityDictionary.Remove(receivedActionPacket.targetID);
                                                usedEntityID.Remove(receivedActionPacket.targetID); //IDも解放
                                                currentNumOfChests--; //宝箱の数デクリメント

                                                //パケット送信
                                                myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.DESTROY_ENTITY, receivedActionPacket.targetID);
                                                myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                udpGameServer.Send(myHeader.ToByte());
                                                //宝箱の数が足りなければ新たに宝箱を作り出す
                                                if (currentNumOfChests < maxNumOfChests)
                                                {
                                                    //まずサーバー側のシーンで
                                                    entityID = GetUniqueEntityID(); //エンティティID生成
                                                    Vector3 chestPos = MapGenerator.instance.GetUniqueChestPointRandomly(); //座標決め
                                                    chest = Instantiate(ChestPrefab, chestPos, Quaternion.identity).GetComponent<Chest>(); //ここ1つ外のスコープの変数chestを使ってるけど様子見なので問題ないかと
                                                    chest.EntityID = entityID; //ID書き込み
                                                    chest.Tier = 1; //レア度はまだ適当に1
                                                    chest.gameObject.name = $"Chest ({entityID})";
                                                    entityDictionary.Add(entityID, chest); //辞書に登録

                                                    //ティア（１）と座標を指定して、宝箱を生成する命令
                                                    myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.SPAWN_CHEST, entityID, 1, chestPos);
                                                    myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                    udpGameServer.Send(myHeader.ToByte());

                                                    currentNumOfChests++; //宝箱の数インクリメント
                                                }
                                            }
                                            break;
                                        #endregion
                                        #region case 魔法系
                                        case (byte)Definer.REID.USE_MAGIC:
                                            //魔法の種類がvalueに書かれているので確認して分岐
                                            switch ((Definer.MID)receivedActionPacket.value)
                                            {
                                                //雷
                                                case Definer.MID.THUNDER:
                                                    //もし違う魔法の実行待機をしているならエラー返す
                                                    if (isAwaitingMagic)
                                                    {
                                                        //魔法の使用却下通知を送る
                                                        myActionPacket = new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.DECLINE_MAGIC, receivedHeader.sessionID);
                                                        myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                        udpGameServer.Send(myHeader.ToByte());
                                                        break;
                                                    }
                                                    //魔法の使用許可を送る
                                                    myActionPacket = new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.ALLOW_MAGIC, receivedHeader.sessionID);
                                                    myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                    udpGameServer.Send(myHeader.ToByte());

                                                    ChangeServerState(new AwaitTouchState(), Definer.MID.THUNDER, receivedHeader.sessionID); //雷を待機する状態にする
                                                    break;
                                                //ダッシュ
                                                case Definer.MID.DASH:
                                                    //魔法の使用許可を送る（無条件）
                                                    myActionPacket = new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.ALLOW_MAGIC, receivedHeader.sessionID);
                                                    myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                    udpGameServer.Send(myHeader.ToByte());
                                                    break;
                                                case Definer.MID.TELEPORT:
                                                    //もし違う魔法の実行待機をしているならエラー返す
                                                    if (isAwaitingMagic)
                                                    {
                                                        //魔法の使用却下通知を送る
                                                        myActionPacket = new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.DECLINE_MAGIC, receivedHeader.sessionID);
                                                        myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                        udpGameServer.Send(myHeader.ToByte());
                                                        break;
                                                    }
                                                    //魔法の使用許可を送る
                                                    myActionPacket = new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.ALLOW_MAGIC, receivedHeader.sessionID);
                                                    myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                    udpGameServer.Send(myHeader.ToByte());

                                                    ChangeServerState(new AwaitTouchState(), Definer.MID.TELEPORT, receivedHeader.sessionID); //テレポートを待機する状態にする
                                                    break;
                                            }
                                            break;
                                        #endregion
                                        case (byte)Definer.REID.DROP_GOLD:
                                            {
                                                int ownedGold = actorDictionary[receivedHeader.sessionID].Gold;

                                                //所持金がすっからかんなら金貨を生成しない
                                                if (ownedGold <= 0) break;

                                                //落とす金額を抽選
                                                int dropGold;
                                                System.Random random = new System.Random();

                                                if (ownedGold < 10) //所持金が10ゴールド未満なら1~所持金の値ゴールドの間で抽選
                                                {
                                                    dropGold = random.Next(1, ownedGold + 1);
                                                }
                                                else
                                                {
                                                    dropGold = random.Next(10, 51); //所持金が10ゴールドより多いなら10~50ゴールドの間で抽選する
                                                }
                                                dropGold = Mathf.Clamp(dropGold, dropGold, ownedGold); //所持金を超えないようにclampする

                                                //プレイヤーから送られてきた座標をもとに金貨の山を生成
                                                //対象プレイヤーの所持金を減らす
                                                //まずサーバー側で
                                                actorDictionary[receivedHeader.sessionID].Gold -= dropGold;
                                                //パケット送信    
                                                myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.EDIT_GOLD, receivedHeader.sessionID, -dropGold);
                                                myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                udpGameServer.Send(myHeader.ToByte());

                                                //まずサーバー側で金貨の山を生成
                                                entityID = GetUniqueEntityID();
                                                Vector3 goldPos = receivedActionPacket.pos;
                                                GoldPile goldPile = Instantiate(GoldPilePrefab, goldPos, Quaternion.identity).GetComponent<GoldPile>();
                                                goldPile.EntityID = entityID; //値を書き込み
                                                goldPile.Value = dropGold;
                                                goldPile.name = $"GoldPile ({entityID})";
                                                entityDictionary.Add(entityID, goldPile); //管理用のIDと共に辞書へ

                                                //金額を指定して、対象プレイヤーの背後に金貨の山を生成する命令
                                                myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.SPAWN_GOLDPILE, entityID, dropGold, goldPos);
                                                myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                udpGameServer.Send(myHeader.ToByte());
                                            }
                                            break;
                                        #region case サーバー内部専用パケット:
                                        case (byte)Definer.REID.INTERNAL_THUNDER:
                                            //オブジェクトを生成しつつ、エンティティのコンポーネントを取得
                                            //thunderという変数名をここでだけ使いたいのでブロック文でスコープ分け
                                            {
                                                //雷は自動消滅するのでDictionaryで管理しない
                                                ThunderEntity thunder = Instantiate(ThunderPrefab, receivedActionPacket.pos, Quaternion.Euler(0, 0, 90)).GetComponent<ThunderEntity>();
                                                thunder.InitEntity(); //生成時のメソッドを呼ぶ
                                                                      //雷を生成する命令
                                                myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.SPAWN_THUNDER, default, default, receivedActionPacket.pos);
                                                myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                udpGameServer.Send(myHeader.ToByte());
                                            }
                                            break;
                                            #endregion
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
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
    }

    //重複しないentityIDを作る
    private ushort GetUniqueEntityID()
    {
        ushort entityID;
        do
        {
            System.Random random = new System.Random(); //UnityEngine.Randomはマルチスレッドで使用できないのでSystemを使う
            entityID = (ushort)random.Next(0, 65535); //0から65535までの整数を生成して2バイトにキャスト


            Debug.Log($"乱数つくった{entityID}");
        }
        while (usedEntityID.Contains(entityID)); //使用済IDと同じ値を生成してしまったならやり直し
        usedEntityID.Add(entityID); //このIDは使用済にする。
        return entityID;
    }
}
