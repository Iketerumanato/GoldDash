using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class GameClientManager : MonoBehaviour
{
    #region 変数宣言リージョン
    [Header("このクライアントで使うプレイヤーカラー")]
    [SerializeField] private Definer.PLAYER_COLOR playerColor;

    private bool isRunning; //稼働中か
    private bool inGame; //ゲームは始まっているか

    private UdpGameClient udpGameClient; //UdpCommunicatorを継承したUdpGameClientのインスタンス
    private Queue<Header> packetQueue; //udpGameClientは”勝手に”このキューにパケットを入れてくれる。不正パケット処理なども済んだ状態で入る。

    [SerializeField] private ushort sessionPass; //サーバーに接続するためのパスコード
    [SerializeField] private ushort initSessionPass; //初回通信時、サーバーからの返信が安全なものか判別するためのパスコード。今後乱数化する
    public string myName; //仮です。登録に使うプレイヤーネーム(TitleUIのインプットフィールドからもらう)

    private ushort sessionID; //自分のセッションID。サーバー側で決めてもらう。

    private Dictionary<ushort, ActorController> actorDictionary; //sessionパスを鍵としてactorインスタンスを保管。自分以外のプレイヤー（アクター）のセッションIDも記録していく
    private Dictionary<ushort, Entity> entityDictionary; //entityIDを鍵としてentityインスタンスを管理

    private PlayerControllerV2 playerController; //プレイヤーが操作するキャラクターのplayerController

    [SerializeField] private int numOfActors = 4; //アクターの人数
    private int preparedActors; //生成し終わったアクターの数

    [SerializeField] private GameObject ActorPrefab; //アクターのプレハブ
    [SerializeField] private GameObject PlayerPrefab; //プレイヤーのプレハブ
    [SerializeField] private GameObject GoldPilePrefab; //金貨の山のプレハブ
    [SerializeField] private GameObject GoldPileMiniPrefab; //小金貨の山のプレハブ
    [SerializeField] private GameObject ChestPrefab; //宝箱のプレハブ
    [SerializeField] private GameObject ScrollPrefab; //巻物のプレハブ
    [SerializeField] private GameObject ThunderPrefab; //雷のプレハブ

    private CancellationTokenSource sendCts; //パケット送信タスクのキャンセル用。ブロードキャストは時間がかかるので
    private CancellationToken SendCts
    {
        get 
        {
            sendCts = new CancellationTokenSource();
            return sendCts.Token;
        }
    }

    //現在のstate
    IClientState currentClientState;

    //UI関連
    [SerializeField] private GameObject Phase0UniqueUI;
    [SerializeField] private GameObject Phase1UniqueUI;

    //Gマーク
    [SerializeField] private GameObject processingLogo;
    [SerializeField] private GameObject arrow;

    //文字数が多すぎエラー
    [SerializeField] private GameObject characterCountError;
    [SerializeField] private TMP_InputField inputField;

    //暗転用イメージ
    [SerializeField] private Image blackImage;

    //Phaseによって変わるテキスト
    [SerializeField] private TextMeshProUGUI upperTextBox;
    [SerializeField] private TextMeshProUGUI centerTextBox;

    //ボタンの強調アニメーション・グレーアウト状態を管理するスクリプトコンポーネント
    [SerializeField] private ButtonAnimator NameInputFieldAnimator;
    [SerializeField] private ButtonAnimator ConnectButtonAnimator;

    //各種ボタン
    [SerializeField] private Button TouchToStartButton;
    [SerializeField] private Button BackButton;
    [SerializeField] private Button ConnectButton;
    #endregion

    #region Stateインターフェース
    public interface IClientState
    {
        void EnterState(GameClientManager gameClientManager);
        void UpdateProcess(GameClientManager gameClientManager);
        void ExitState(GameClientManager gameClientManager);
    }

    //Stateの切り替え
    public void ChangeClientState(IClientState newState)
    {
        if (currentClientState != null) currentClientState.ExitState(this);
        currentClientState = newState;
        currentClientState.EnterState(this);
    }

    public class Phase0State : IClientState
    {
        public void EnterState(GameClientManager gameClientManager)
        {
            //必要なUI出す
            gameClientManager.Phase0UniqueUI.SetActive(true);
            //テキスト変える
            gameClientManager.upperTextBox.text = "";
            gameClientManager.centerTextBox.text = "";
        }

        public void UpdateProcess(GameClientManager gameClientManager)
        {
        }

        public void ExitState(GameClientManager gameClientManager)
        {
            //不要なUI消す
            gameClientManager.Phase0UniqueUI.SetActive(false);
        }
    }

    public class Phase1State : IClientState
    {
        public void EnterState(GameClientManager gameClientManager)
        {
            //必要なUI出す
            gameClientManager.Phase1UniqueUI.SetActive(true);
            //テキスト変える
            gameClientManager.upperTextBox.text = "プレイヤー名を入力してください";
            gameClientManager.centerTextBox.text = "";
            //名前欄の強調
            gameClientManager.NameInputFieldAnimator.IsAnimating = true;
            //接続ボタンをグレーアウト
            gameClientManager.ConnectButtonAnimator.IsGrayedOut = true;
            //エラーメッセージは非表示に
            gameClientManager.characterCountError.SetActive(false);
            //インプットフィールドに既に何か書き込まれているならアニメーション状態の更新
            if (gameClientManager.inputField.text.Length != 0)
            {
                gameClientManager.CheckNameCharacterCount();
            }
        }

        public void UpdateProcess(GameClientManager gameClientManager)
        {
        }

        public void ExitState(GameClientManager gameClientManager)
        {
            //不要なUI消す
            gameClientManager.Phase1UniqueUI.SetActive(false);
        }
    }

    public class Phase2State : IClientState
    {
        public void EnterState(GameClientManager gameClientManager)
        {
            //必要なUI出す
            gameClientManager.processingLogo.SetActive(true);
            //テキスト変える
            gameClientManager.upperTextBox.text = "";
            gameClientManager.centerTextBox.text = "接続中…";
        }

        public void UpdateProcess(GameClientManager gameClientManager)
        {
        }

        public void ExitState(GameClientManager gameClientManager)
        {
            //不要なUI消す
            gameClientManager.processingLogo.SetActive(false);
        }
    }

    //通常の状態
    public class NormalState : IClientState
    {
        public void EnterState(GameClientManager gameClientManager)
        {
            //テキスト変える
            gameClientManager.upperTextBox.text = "";
            gameClientManager.centerTextBox.text = "";
        }

        public void UpdateProcess(GameClientManager gameClientManager)
        {
        }

        public void ExitState(GameClientManager gameClientManager)
        {
        }
    }
    #endregion

    private void Start()
    {
        inGame = false;

        packetQueue = new Queue<Header>();
        actorDictionary = new Dictionary<ushort, ActorController>();
        entityDictionary = new Dictionary<ushort, Entity>();

        //通信用インスタンス作成
        udpGameClient = new UdpGameClient(ref packetQueue, initSessionPass);

        Task.Run(() => ProcessPacket());
        Task.Run(() => SendPlayerPosition());

        //State初期化
        ChangeClientState(new Phase0State());

        //マップは作っておく
        MapGenerator.instance.GenerateMapForClient();

        //各種ボタンに関数を設定
        TouchToStartButton.OnClickAsObservable().Subscribe(_ => 
        {
            blackImage.DOFade(1f, 0.3f).OnComplete(() =>
            {
                ChangeClientState(new Phase1State());
                blackImage.DOFade(0f, 0.3f);
            });
        });
        BackButton.OnClickAsObservable().Subscribe(_ => 
        {
            blackImage.DOFade(1f, 0.3f).OnComplete(() =>
            {
                ChangeClientState(new Phase0State());
                blackImage.DOFade(0f, 0.3f);
            });
        });
        ConnectButton.OnClickAsObservable().Subscribe(_ => 
        {
            blackImage.DOFade(1f, 0.3f).OnComplete(async () =>
            {
                ChangeClientState(new Phase2State());
                blackImage.DOFade(0f, 0.3f);

                // 既に接続中なら何もしない
                if (this.sessionID != 0)
                {
                    Debug.Log("もうセッションID受け取ってるよ");
                    return;
                }

                isRunning = true;
                // Initパケット送信 (非同期)
                await UniTask.Delay(1000);
                Task t = Task.Run(() => udpGameClient.Send(new Header(0, 0, 0, 0, (byte)Definer.PT.IPC, new InitPacketClient(sessionPass, udpGameClient.rcvPort, initSessionPass, (int)playerColor, myName).ToByte()).ToByte()), SendCts);
            });
        });
    }

    private void Update()
    {
        currentClientState.UpdateProcess(this); //stateによって異なる処理
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
            myActionPacket = new ActionPacket((byte)Definer.RID.MOV, default, sessionID, default, playerController.transform.position, playerController.transform.forward);
            myHeader = new Header(this.sessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
            udpGameClient.Send(myHeader.ToByte());

            await UniTask.Delay(170);
        }
    }

    private async void ProcessPacket()
    {
        //返信用クラスを外側のスコープで宣言しておく
        ActionPacket myActionPacket;
        Header myHeader;

        try //サブスレッドの例外をコンソールに出すためのtry-catch
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

                            centerTextBox.text = "接続完了！";
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
                                            break;
                                        case (byte)Definer.NDID.DISCONNECT:
                                            //ClientInternalSubject.OnNext(CLIENT_INTERNAL_EVENT.COMM_ERROR_FATAL); //予期せずサーバーから切断された場合エラーを出す
                                            break;
                                        case (byte)Definer.NDID.STG:
                                            //ここでプレイヤーを有効化してゲーム開始
                                            //全アクターの有効化
                                            foreach (KeyValuePair<ushort, ActorController> k in actorDictionary)
                                            {
                                                k.Value.gameObject.SetActive(true);
                                            }
                                            inGame = true;

                                            ChangeClientState(new NormalState());
                                            break;
                                        case (byte)Definer.NDID.EDG:
                                            break;
                                        case (byte)Definer.NDID.ALLOW_MAGIC:
                                            if (receivedActionPacket.targetID == sessionID) //自分に向けた許可なら
                                            {
                                                playerController.AcceptUsingMagic(); //魔法を実行させる
                                            }
                                            break;
                                        case (byte)Definer.NDID.DECLINE_MAGIC:
                                            if (receivedActionPacket.targetID == sessionID) //自分に向けた許可なら
                                            {
                                                playerController.DeclineUsingMagic(); //魔法の使用を許可しない
                                            }
                                            break;
                                        case (byte)Definer.NDID.END_MAGIC_SUCCESSFULLY:
                                            if (receivedActionPacket.targetID == sessionID) //自分に向けた許可なら
                                            {
                                                playerController.EndUsingMagicSuccessfully(); //魔法を正しく終了する
                                            }
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
                                                //playerControllerはアクセスしやすいように取得しておく
                                                playerController = actorController.gameObject.GetComponent<PlayerControllerV2>();
                                                //Playerクラスには別にSessionIDとUdpGameClientを渡し、パケット送信を自分でやらせる。
                                                playerController.SessionID = this.sessionID;
                                                playerController.UdpGameClient = this.udpGameClient;
                                                //色変更
                                                actorController.ChangePlayerColor((Definer.PLAYER_COLOR)receivedActionPacket.value);
                                            }
                                            else //他人のIDなら
                                            {
                                                //アクターををインスタンス化しながらActorControllerを取得
                                                actorController = Instantiate(ActorPrefab).GetComponent<ActorController>();
                                                //色変更
                                                actorController.ChangePlayerColor((Definer.PLAYER_COLOR)receivedActionPacket.value);
                                            }

                                            //アクターを指定地点へ移動させる
                                            actorController.Warp(receivedActionPacket.pos, Vector3.forward);
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
                                            preparedActors++;
                                            Debug.Log($"現在{numOfActors}人中{preparedActors}人分のアクターが用意できています。");
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
                                                playerController.GetPunchFront();
                                            }
                                            else
                                            {
                                                //他人が殴られたならモーション同期
                                                actorDictionary[receivedActionPacket.targetID].GuardAnimation();
                                            }
                                            break;
                                        case (byte)Definer.EDID.HIT_BACK:
                                            //殴られたのがプレイヤーなら
                                            if (receivedActionPacket.targetID == this.sessionID)
                                            {
                                                //プレイヤー側で演出
                                                //playerController.Blown(receivedActionPacket.pos); //パンチの方向に吹っ飛ぶ
                                                playerController.GetPunchBack();
                                                if (actorDictionary[sessionID].Gold != 0) playerController.PlayLostCoinAnimation(); //所持金が0でなければコインをこぼすアニメーション
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
                                                if (receivedActionPacket.value > 0)
                                                {
                                                    playerController.PlayGetCoinAnimation(); //所持金が増えたときはコイン吸い取りアニメーション
                                                    SEPlayer.instance.PlaySEGetGold();
                                                }
                                                else if (receivedActionPacket.value < 0)
                                                {
                                                    //失った金額に応じてSE再生
                                                    switch (receivedActionPacket.value)
                                                    {
                                                        case >= -50:
                                                            SEPlayer.instance.PlaySEDropGold_S();
                                                            break;
                                                        case < -50 and >= -500:
                                                            SEPlayer.instance.PlaySEDropGold_M();
                                                            break;
                                                        default:
                                                            SEPlayer.instance.PlaySEDropGold_L();
                                                            break;
                                                    }
                                                }
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
                                        case (byte)Definer.EDID.SPAWN_SCROLL:
                                            //オブジェクトを生成しつつ、エンティティのコンポーネントを取得
                                            //scrollという変数名をここでだけ使いたいのでブロック文でスコープ分け
                                            {
                                                Scroll scroll = Instantiate(ScrollPrefab, receivedActionPacket.pos, Quaternion.identity).GetComponent<Scroll>();
                                                entityDictionary.Add(receivedActionPacket.targetID, scroll); //管理用のIDと共に辞書へ
                                                scroll.EntityID = receivedActionPacket.targetID; //ID割り当て
                                                scroll.MagicID = (Definer.MID)receivedActionPacket.value; //金額設定
                                                scroll.name = $"Scroll ({receivedActionPacket.targetID} ({(Definer.MID)receivedActionPacket.value}))";
                                            }
                                            break;
                                        case (byte)Definer.EDID.SPAWN_GOLDPILE:
                                            //オブジェクトを生成しつつ、エンティティのコンポーネントを取得
                                            //goldPileという変数名をここでだけ使いたいのでブロック文でスコープ分け
                                            {
                                                GoldPile goldPile = receivedActionPacket.value > 50 ? Instantiate(GoldPilePrefab, receivedActionPacket.pos, Quaternion.identity).GetComponent<GoldPile>() : Instantiate(GoldPileMiniPrefab, receivedActionPacket.pos, Quaternion.identity).GetComponent<GoldPile>();
                                                entityDictionary.Add(receivedActionPacket.targetID, goldPile); //管理用のIDと共に辞書へ
                                                goldPile.EntityID = receivedActionPacket.targetID; //ID割り当て
                                                goldPile.Value = receivedActionPacket.value; //金額設定
                                                goldPile.name = $"GoldPile ({receivedActionPacket.targetID})";
                                                goldPile.InitEntity(); //生成時のメソッドを呼ぶ
                                                goldPile.ActivateEntity(); //有効化時のメソッドを呼ぶ
                                            }
                                            break;
                                        case (byte)Definer.EDID.SPAWN_THUNDER:
                                            //オブジェクトを生成しつつ、エンティティのコンポーネントを取得
                                            //thunderという変数名をここでだけ使いたいのでブロック文でスコープ分け
                                            {
                                                //雷は自動消滅するのでDictionaryで管理しない
                                                ThunderEntity thunder = Instantiate(ThunderPrefab, receivedActionPacket.pos, Quaternion.Euler(0, 0, 90)).GetComponent<ThunderEntity>();
                                                thunder.InitEntity(); //生成時のメソッドを呼ぶ
                                            }
                                            break;
                                        case (byte)Definer.EDID.DELETE_ACTOR:
                                            //アクターのオブジェクトを削除
                                            Destroy(actorDictionary[receivedActionPacket.targetID].gameObject);
                                            //アクター登録の削除
                                            actorDictionary.Remove(receivedActionPacket.targetID);
                                            break;
                                        case (byte)Definer.EDID.DESTROY_ENTITY:
                                            //エンティティを動的ディスパッチしてオーバーライドされたDestroyメソッド実行
                                            entityDictionary[receivedActionPacket.targetID].DestroyEntity();
                                            entityDictionary.Remove(receivedActionPacket.targetID);
                                            break;
                                        case (byte)Definer.EDID.GIVE_MAGIC:
                                            //アクター側の魔法所持数を変更する。
                                            actorDictionary[receivedActionPacket.targetID].MagicInventry++;
                                            //対象がプレイヤーならプレイヤーに魔法の巻物を渡す
                                            if (receivedActionPacket.targetID == this.sessionID)
                                            {
                                                //valueのmagicIDを見て処理
                                                playerController.SetMagicToHotbar((Definer.MID)receivedActionPacket.value);
                                            }
                                            break;
                                        case (byte)Definer.EDID.TELEPORT_ACTOR:
                                            actorDictionary[receivedActionPacket.targetID].Warp(receivedActionPacket.pos, actorDictionary[receivedActionPacket.targetID].transform.forward);
                                            break;
                                        case (byte)Definer.EDID.CHANGE_ACTOR_COLOR_TO_WHITE:
                                            //サーバー側でも色変更
                                            ActorController tmp;
                                            if (actorDictionary.TryGetValue(receivedActionPacket.targetID, out tmp))
                                            {
                                                actorDictionary[receivedActionPacket.targetID].ChangeGreenToWhiteClient();
                                            }
                                            break;
                                        case (byte)Definer.EDID.MOTION_CHEST:
                                            //自分でなければモーション再生
                                            if (receivedActionPacket.targetID != this.sessionID)
                                            {
                                                actorDictionary[receivedActionPacket.targetID].PlayChestAnimation();
                                            }
                                            break;
                                        case (byte)Definer.EDID.MOTION_SCROLL:
                                            //自分でなければモーション再生
                                            if (receivedActionPacket.targetID != this.sessionID)
                                            {
                                                actorDictionary[receivedActionPacket.targetID].PlayScrollAnimation();
                                            }
                                            break;
                                        case (byte)Definer.EDID.MOTION_STUN:
                                            //自分でなければモーション再生
                                            if (receivedActionPacket.targetID != this.sessionID)
                                            {
                                                actorDictionary[receivedActionPacket.targetID].PlayStunAnimation();
                                            }
                                            break;
                                        case (byte)Definer.EDID.MOTION_END:
                                            //自分でなければモーション再生
                                            if (receivedActionPacket.targetID != this.sessionID)
                                            {
                                                actorDictionary[receivedActionPacket.targetID].EndChestAnimation();
                                                actorDictionary[receivedActionPacket.targetID].EndScrollAnimation();
                                                actorDictionary[receivedActionPacket.targetID].EndStunAnimation();
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
        catch(System.Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void OnDestroy()
    {
        isRunning = false;

        // サーバーに接続中なら切断パケットを送信
        if (this.sessionID != 0)
        {
            Debug.Log("サーバーに切断パケットを送信");
            if (udpGameClient != null)
            {
                udpGameClient.Send(new Header(this.sessionID, 0, 0, 0, (byte)Definer.PT.AP, new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.DISCONNECT, this.sessionID).ToByte()).ToByte());
            }
        }

        this.udpGameClient?.Dispose();
        this.sendCts?.Cancel();
    }

    //インプットフィールドの編集を終えたときに呼び出す。名前の文字数チェックをしてUI状況を更新しつつ、myNameに値を格納
    public void CheckNameCharacterCount()
    {
        string name = inputField.text;
        if (name.Length > 0 && name.Length <= 8)
        {
            characterCountError.SetActive(false);
            NameInputFieldAnimator.IsAnimating = false;
            ConnectButtonAnimator.IsAnimating = true;
            ConnectButtonAnimator.IsGrayedOut = false;
        }
        else
        {
            characterCountError.SetActive(true);
            NameInputFieldAnimator.IsAnimating = true;
            ConnectButtonAnimator.IsAnimating = false;
            ConnectButtonAnimator.IsGrayedOut = true;
        }
        myName = inputField.text;
    }
}
