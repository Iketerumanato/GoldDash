using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System.Collections.Concurrent;
using TMPro;
using System;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class GameServerManager : MonoBehaviour
{
    #region 変数宣言リージョン
    private bool isRunning; //サーバーが稼働中か
    private bool inGame; //メインゲームは始まっているか

    //udpGameServerインスタンスがDisposeメソッド以外で破棄されることは想定していない。そのときはおしまいだろう。
    private UdpGameServer udpGameServer; //UdpCommunicatorを継承したUdpGameServerのインスタンス
    private ushort rcvPort; //udpGameServerの受信用ポート番号
    private ushort serverSessionID; //クライアントにサーバーを判別させるためのID
    private ConcurrentQueue<Header> packetQueue; //udpGameServerは”勝手に”このキューにパケットを入れてくれる。不正パケット処理なども済んだ状態で入る。

    private Dictionary<ushort, ActorController> actorDictionary; //sessionパスを鍵としてactorインスタンスを管理
    private Dictionary<ushort, Entity> entityDictionary; //entityIDを鍵としてentityインスタンスを管理

    private HashSet<ushort> usedEntityID; //EntityIDの重複防止に使う。

    [SerializeField] private ushort sessionPass; //サーバーに入るためのパスワード。udpGameServerのコンストラクタに渡す。
    [SerializeField] private int numOfPlayers; //何人のプレイヤーを募集するか
    private int preparedPlayers; //準備が完了したプレイヤーの数
    private bool allActorsPrepared = false;

    [SerializeField] private int maxNumOfChests; //現在の（ゲーム開始時もそう）宝箱の同時出現数の上限。この値より少なければ生成される。
    //動的に減らしたり増やしたりしても問題ないが、宝箱の出現候補地点の数より多くならないように注意が必要。宝箱が同じ位置に重なって生成されてしまう。
    private int currentNumOfChests; //現在生成されている宝箱の数

    [SerializeField] private GameObject RedActorPrefab; //アクターのアイコンプレハブ
    [SerializeField] private GameObject BlueActorPrefab;
    [SerializeField] private GameObject GreenActorPrefab;
    [SerializeField] private GameObject YellowActorPrefab;
    [SerializeField] private GameObject WhiteActorPrefab;
    [SerializeField] private GameObject GoldPilePrefab; //金貨の山のプレハブ
    [SerializeField] private GameObject GoldPileMiniPrefab; //小金貨の山のプレハブ
    [SerializeField] private GameObject ChestPrefab; //宝箱のプレハブ
    [SerializeField] private GameObject Chest2Prefab; //宝箱ティア2のプレハブ
    [SerializeField] private GameObject Chest3Prefab; //宝箱ティア3のプレハブ
    [SerializeField] private GameObject ScrollPrefab; //巻物のプレハブ
    [SerializeField] private GameObject ThunderPrefab; //雷のプレハブ
    [SerializeField] private GameObject GameFinalResultSetPrefab;
    [SerializeField] Camera GameFinalResultCamera;

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

    //UI関連
    [SerializeField] private GameObject PlayerInfoUI;
    [SerializeField] private GameObject Phase1UniqueUI;
    [SerializeField] private GameObject Phase2UniqueUI;

    //Gマーク
    [SerializeField] private GameObject processingLogo;

    //暗転用イメージ
    [SerializeField] private Image blackImage;

    //プレイヤーネームと矢印
    [SerializeField] private GameObject redArrow;
    private RectTransform redArrowIconPos;//矢印の親のオブジェクト(矢印の背景)←これと一緒にループして動かす
    private Vector2 originRedArrowPos;//ステート切り替え時に元の位置に戻るように初期値をとっておく
    Tween redArrowTween;//アニメーション停止用トゥイーン

    [SerializeField] private GameObject blueArrow;
    private RectTransform blueArrowIconPos;
    private Vector2 originBlueArrowPos;
    Tween blueArrowTween;

    [SerializeField] private GameObject greenArrow;
    private RectTransform greenArrowIconPos;
    private Vector2 originGreenArrowPos;
    Tween greenArrowTween;

    [SerializeField] private GameObject yellowArrow;
    private RectTransform yellowArrowIconPos;
    private Vector2 originYellowArrowPos;
    Tween yellowArrowTween;

    [SerializeField] private TextMeshProUGUI redNameText;
    [SerializeField] private TextMeshProUGUI blueNameText;
    [SerializeField] private TextMeshProUGUI greenNameText;
    [SerializeField] private TextMeshProUGUI yellowNameText;

    //Phaseによって変わるテキスト
    [SerializeField] private TextMeshProUGUI lowerTextBox;
    [SerializeField] private TextMeshProUGUI upperTextBox;

    //制限時間
    [SerializeField] private TextMeshProUGUI timeTextLeft;
    [SerializeField] private TextMeshProUGUI timeTextRight;

    private float timeLimitSeconds = 10f;

    //色変えボタン
    [SerializeField] private ColorSelectButtonColorChanger colorSelectButtonColorChanger1;
    [SerializeField] private ColorSelectButtonColorChanger colorSelectButtonColorChanger2;
    [SerializeField] private ButtonAnimator colorSelectButtonAnimator1;
    [SerializeField] private ButtonAnimator colorSelectButtonAnimator2;
    [SerializeField] private Button colorSelectButton1;
    [SerializeField] private Button colorSelectButton2;

    //プレイヤーの配色パターン
    public enum COLOR_TYPE
    {
        DEFAULT,
        CHANGE_GREEN_TO_WHITE,
    }
    //現在の色パターン
    public COLOR_TYPE currentColorType;

    [SerializeField] private Button gameStartButton;
    [SerializeField] private ButtonAnimator gameStartButtonAnimator;

    //現在のstate
    private ISetverState currentSetverState;
    //魔法の実行待機ならtrue
    private bool isAwaitingMagic;
    //待機中の魔法の種類
    private Definer.MID awaitingMagicID;
    //魔法を使用をしようとしているプレイヤーのsessionID
    private ushort magicUserID;

    //プロセッシングロゴアニメーション用
    Sequence CenterLogoAnimation;
    [SerializeField] RectTransform CenterLogoImageTransform;
    #endregion

    #region Stateインターフェース
    public interface ISetverState
    {
        void EnterState(GameServerManager gameServerManager, Definer.MID magicID, ushort magicUserID);
        void UpdateProcess(GameServerManager gameServerManager);
        void ExitState(GameServerManager gameServerManager);
    }

    //魔法IDを渡しつつStateの切り替え
    public void ChangeServerState(ISetverState newState, Definer.MID magicID = Definer.MID.NONE, ushort magicUserID = 0)
    {
        if (currentSetverState != null) currentSetverState.ExitState(this);
        currentSetverState = newState;
        currentSetverState.EnterState(this, magicID, magicUserID);
    }

    //通常の状態
    public class Phase0State : ISetverState
    {
        public void EnterState(GameServerManager gameServerManager, Definer.MID magicID, ushort magicUserID)
        {
            //フェードイン
            gameServerManager.blackImage.DOFade(0f, 3f);

            //必要なUI出す
            gameServerManager.PlayerInfoUI.SetActive(true);
            //テキスト変える
            gameServerManager.upperTextBox.text = "プレイヤーの接続を待っています…";
            gameServerManager.lowerTextBox.text = "プレイヤーの接続を待っています…";

            //プレイヤー情報の初期化
            gameServerManager.redArrow.SetActive(false);
            gameServerManager.blueArrow.SetActive(false);
            gameServerManager.greenArrow.SetActive(false);
            gameServerManager.yellowArrow.SetActive(false);

            gameServerManager.redNameText.text = "未参加";
            gameServerManager.blueNameText.text = "未参加";
            gameServerManager.greenNameText.text = "未参加";
            gameServerManager.yellowNameText.text = "未参加";

            //初期設定が終わったら稼働
            gameServerManager.isRunning = true;

            //BGM
            SEPlayer.instance.titleBGMPlayer.DOFade(1f, 0.3f);
            SEPlayer.instance.titleBGMPlayer.Play();

            //回転ロゴの角度リセット
            gameServerManager.CenterLogoImageTransform.rotation = Quaternion.identity;

            //ロゴアニメーション
            gameServerManager.CenterLogoAnimation = DOTween.Sequence();
            gameServerManager.CenterLogoAnimation.Append(gameServerManager.CenterLogoImageTransform.DOLocalRotate(new Vector3(0f, 0f, 360f), 1.3f, RotateMode.FastBeyond360).SetEase(Ease.InOutBack))//InOutBackを付けつつ一回目の回転
                .SetDelay(0.3f)//少し待機
                .Append(gameServerManager.CenterLogoImageTransform.DOLocalRotate(new Vector3(0f, 0f, 360f), 1.5f, RotateMode.FastBeyond360).SetEase(Ease.OutBack))//InOutBackでの回転速度に追いつくためOutBackで２回目の回転
                .SetLoops(-1);//無限ループ
        }

        public void UpdateProcess(GameServerManager gameServerManager)
        {
        }

        public void ExitState(GameServerManager gameServerManager)
        {
            //不要なUI消す
            gameServerManager.PlayerInfoUI.SetActive(false);

            //アニメーション止める
            gameServerManager.CenterLogoAnimation.Kill();
            //回転ロゴの角度リセット
            gameServerManager.CenterLogoImageTransform.rotation = Quaternion.identity;
        }
    }

    //通常の状態
    public class Phase1State : ISetverState
    {
        public void EnterState(GameServerManager gameServerManager, Definer.MID magicID, ushort magicUserID)
        {
            //必要なUI出す
            gameServerManager.Phase1UniqueUI.SetActive(true);
            //テキスト変える
            gameServerManager.upperTextBox.text = "プレイヤーの配色を選んでください";
            gameServerManager.lowerTextBox.text = "プレイヤーの配色を選んでください";
            //スタートボタンをグレーアウト
            gameServerManager.gameStartButtonAnimator.IsGrayedOut = true;
            //色変更ボタンを強調、どちらも非選択状態にする
            gameServerManager.colorSelectButtonAnimator1.IsAnimating = true;
            gameServerManager.colorSelectButtonAnimator2.IsAnimating = true;
            gameServerManager.colorSelectButtonColorChanger1.IsSelected = false;
            gameServerManager.colorSelectButtonColorChanger2.IsSelected = false;

            //このステートに戻ってきたと同時に矢印のアニメーションをしてアイコンを初期位置に戻す
            gameServerManager.redArrowTween.Kill();
            gameServerManager.redArrowIconPos.position = gameServerManager.originRedArrowPos;

            gameServerManager.blueArrowTween.Kill();
            gameServerManager.blueArrowIconPos.position = gameServerManager.originBlueArrowPos;

            gameServerManager.greenArrowTween.Kill();
            gameServerManager.greenArrowIconPos.position = gameServerManager.originGreenArrowPos;

            gameServerManager.yellowArrowTween.Kill();
            gameServerManager.yellowArrowIconPos.position = gameServerManager.originYellowArrowPos;
        }

        public void UpdateProcess(GameServerManager gameServerManager)
        {
        }

        public void ExitState(GameServerManager gameServerManager)
        {
            //不要なUI消す
            gameServerManager.Phase1UniqueUI.SetActive(false);
            gameServerManager.processingLogo.SetActive(false);

            //BGM消す
            SEPlayer.instance.titleBGMPlayer.DOFade(0f, 0.3f);
        }
    }

    //通常の状態
    public class Phase2State : ISetverState
    {
        public async void EnterState(GameServerManager gameServerManager, Definer.MID magicID, ushort magicUserID)
        {
            //必要なUI出す
            gameServerManager.PlayerInfoUI.SetActive(true);
            gameServerManager.Phase2UniqueUI.SetActive(true);
            //テキスト変える
            gameServerManager.upperTextBox.text = "";
            gameServerManager.lowerTextBox.text = "";

            await UniTask.WaitUntil(() => gameServerManager.allActorsPrepared == true);

            ActionPacket myActionPacket;
            Header myHeader;

            //色がタイプ２なら色変更パケット
            ushort greenID = 0;
            if (gameServerManager.currentColorType == COLOR_TYPE.CHANGE_GREEN_TO_WHITE)
            {
                //全アクターの中から緑のアクターを見つける
                foreach (KeyValuePair<ushort, ActorController> k in gameServerManager.actorDictionary)
                {
                    if (k.Value.Color == Definer.PLAYER_COLOR.GREEN)
                    {
                        greenID = k.Key;
                        break;
                    }
                }

                myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.CHANGE_ACTOR_COLOR_TO_WHITE, greenID);
                myHeader = new Header(gameServerManager.serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                gameServerManager.udpGameServer.Send(myHeader.ToByte());
            }
            //サーバー側でも色変更
            ActorController tmp;
            if (gameServerManager.actorDictionary.TryGetValue(greenID, out tmp))
            {
                gameServerManager.actorDictionary[greenID].ChangeGreenToWhiteServer();
                gameServerManager.greenArrow.GetComponent<RawImage>().color = Color.white;
            }

            //サーバー側のマップ生成 //非同期に行うので待つ
            MapGenerator.instance.GenerateMapForServer();

            await UniTask.Delay(6200);

            for (int i = 0; i < gameServerManager.maxNumOfChests; i++) //宝箱が上限数に達するまで宝箱を生成する
            {
                //まずサーバー側のシーンで
                ushort entityID = gameServerManager.GetUniqueEntityID(); //エンティティID生成
                Vector3 chestPos = MapGenerator.instance.GetUniqueChestPointRandomly(); //座標決め
                Chest chest = Instantiate(gameServerManager.ChestPrefab, chestPos, Quaternion.identity).GetComponent<Chest>();
                chest.EntityID = entityID; //ID書き込み
                chest.Tier = 1; //レア度はまだ適当に1
                chest.gameObject.name = $"Chest ({entityID})";
                gameServerManager.entityDictionary.Add(entityID, chest); //辞書に登録

                //ティア（１）と座標を指定して、宝箱を生成する命令
                myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.SPAWN_CHEST, entityID, 1, chestPos);
                myHeader = new Header(gameServerManager.serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                gameServerManager.udpGameServer.Send(myHeader.ToByte());

                //宝箱の数を記録
                gameServerManager.currentNumOfChests++;
            }

            await UniTask.Delay(1000);

            //全アクターの有効化
            foreach (KeyValuePair<ushort, ActorController> k in gameServerManager.actorDictionary)
            {
                k.Value.gameObject.transform.position = new Vector3(5.5f, 0.4f, 5.5f);
                k.Value.gameObject.SetActive(true);
            }


            SEPlayer.instance.mainBGMPlayer.Play();

            await UniTask.Delay(3000);

            //ゲーム開始
            gameServerManager.inGame = true;

            //ゲーム開始命令を送る
            myActionPacket = new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.STG);
            myHeader = new Header(gameServerManager.serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
            gameServerManager.udpGameServer.Send(myHeader.ToByte());
        }

        public void UpdateProcess(GameServerManager gameServerManager)
        {
        }

        public void ExitState(GameServerManager gameServerManager)
        {
        }
    }

    //通常の状態
    public class Phase3State : ISetverState
    {
        public async void EnterState(GameServerManager gameServerManager, Definer.MID magicID, ushort magicUserID)
        {
            await UniTask.Delay(34000);

            SEPlayer.instance.resultBGMPlayer.DOFade(0f, 3f).OnComplete(() =>
            {
                gameServerManager.blackImage.DOFade(1f, 0.3f).OnComplete(() =>
                {
                    Debug.Log("終了");
                    DOTween.KillAll();
                    DOTween.Clear(true);
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                });
            });
        }

        public void UpdateProcess(GameServerManager gameServerManager)
        {
        }

        public void ExitState(GameServerManager gameServerManager)
        {
        }
    }


    //通常の状態
    public class NormalState : ISetverState
    {
        public void EnterState(GameServerManager gameServerManager, Definer.MID magicID, ushort magicUserID)
        {
        }

        public void UpdateProcess(GameServerManager gameServerManager)
        {
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

                                    //SE再生
                                    SEPlayer.instance.PlaySEThunder();

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

                                    //SE再生
                                    SEPlayer.instance.PlaySEWarp();

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

    private void Start()
    {
        //コレクションのインスタンス作成
        packetQueue = new ConcurrentQueue<Header>();
        actorDictionary = new Dictionary<ushort, ActorController>();
        entityDictionary = new Dictionary<ushort, Entity>();

        usedEntityID = new HashSet<ushort>();

        inGame = false;

        //udp通信の準備
        udpGameServer = new UdpGameServer(ref packetQueue, sessionPass);
        rcvPort = udpGameServer.GetReceivePort(); //受信用ポート番号とサーバーのセッションIDがここで決まるので取得
        serverSessionID = udpGameServer.GetServerSessionID();

        //パケットの処理をUpdateでやると1フレームの計算量が保障できなくなる（カクつきの原因になり得る）のでマルチスレッドで
        //スレッドが何個いるのかは試してみないと分からない
        Task.Run(() => ProcessPacket());
        Task.Run(() => SendAllActorsPosition());

        //MagicLottely取得
        magicLottely = GetComponent<MagicLottely>();

        //State初期化
        ChangeServerState(new Phase0State());

        //ボタンの処理記述
        colorSelectButton1.OnClickAsObservable().Subscribe(_ =>
        {
            currentColorType = COLOR_TYPE.DEFAULT;
            colorSelectButtonColorChanger1.IsSelected = true;
            colorSelectButtonColorChanger2.IsSelected = false;
            colorSelectButtonAnimator1.IsAnimating = false;
            colorSelectButtonAnimator2.IsAnimating = false;
            gameStartButtonAnimator.IsGrayedOut = false;
            gameStartButtonAnimator.IsAnimating = true;
            SEPlayer.instance.PlaySEButton();
        });
        colorSelectButton2.OnClickAsObservable().Subscribe(_ =>
        {
            currentColorType = COLOR_TYPE.CHANGE_GREEN_TO_WHITE;
            colorSelectButtonColorChanger1.IsSelected = false;
            colorSelectButtonColorChanger2.IsSelected = true;
            colorSelectButtonAnimator1.IsAnimating = false;
            colorSelectButtonAnimator2.IsAnimating = false;
            gameStartButtonAnimator.IsGrayedOut = false;
            gameStartButtonAnimator.IsAnimating = true;
            SEPlayer.instance.PlaySEButton();
        });
        gameStartButton.OnClickAsObservable().Subscribe(_ =>
        {
            SEPlayer.instance.PlaySEButton();
            blackImage.DOFade(1f, 0.3f).OnComplete(() =>
            {
                ChangeServerState(new Phase2State());
                blackImage.DOFade(0f, 0.3f);
            });
        });

        //親(背景の画像)のRectTransformを取得
        redArrowIconPos = redArrow.transform.parent.GetComponent<RectTransform>();
        //初期位置の記録
        originRedArrowPos = redArrowIconPos.position;

        blueArrowIconPos = blueArrow.transform.parent.GetComponent<RectTransform>();
        originBlueArrowPos = blueArrowIconPos.position;

        greenArrowIconPos = greenArrow.transform.parent.GetComponent<RectTransform>();
        originGreenArrowPos = greenArrowIconPos.position;

        yellowArrowIconPos = yellowArrow.transform.parent.GetComponent<RectTransform>();
        originYellowArrowPos = yellowArrowIconPos.position;
    }

    private bool displayedRemain3min = false;
    private bool displayedRemain1min = false;
    private bool displayedRemain30sec = false;

    private bool chest2First = true;
    private bool chest3First = true;

    private bool IsPhase3 = false;

    private void Update()
    {
        currentSetverState.UpdateProcess(this);

        if (!inGame) return;

        //秒数を減らす
        timeLimitSeconds -= Time.deltaTime;
        //0秒未満なら0で固定する
        if (timeLimitSeconds < 0f)
        {
            timeLimitSeconds = 0f;

            //フェーズ3に移行
            if (!IsPhase3)
            {
                //パケットを送って試合終了
                ActionPacket myActionPacket = new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.EDG);
                Header myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                udpGameServer.Send(myHeader.ToByte());

                //ここでフェードさせつつオブジェクトを起こし、結果発表用のカメラをMainCameraに変化させて疑似画面遷移開始
                //フェードで暗転
                blackImage.DOFade(1f, 1f).OnComplete(() =>
                {
                    //フェードが明ける
                    blackImage.DOFade(0f, 0.5f);
                    GameFinalResultSetPrefab.SetActive(true);
                    GameFinalResultCamera.tag = "MainCamera";
                    PlayerInfoUI.SetActive(false);
                    Phase2UniqueUI.SetActive(false);
                    ChangeServerState(new Phase3State());
                });
                IsPhase3 = true;
            }
        }
        //分：秒表記に変換
        TimeSpan span = new TimeSpan(0, 0, (int)timeLimitSeconds);
        string timeText = span.ToString(@"m\:ss");
        timeTextLeft.text = $"試合終了まで\r\n<size=120>{timeText}</size>";
        timeTextRight.text = $"試合終了まで\r\n<size=120>{timeText}</size>";

        if (!displayedRemain3min && timeLimitSeconds < 180f)
        {
            ActionPacket myActionPacket = new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.DISPLAY_LARGE_MSG, value: 2, msg: "のこり３分！");
            Header myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
            udpGameServer.Send(myHeader.ToByte());
            displayedRemain3min = true;
        }
        else if (!displayedRemain1min && timeLimitSeconds < 60f)
        {
            ActionPacket myActionPacket = new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.DISPLAY_LARGE_MSG, value: 2, msg: "のこり１分！");
            Header myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
            udpGameServer.Send(myHeader.ToByte());
            displayedRemain1min = true;
        }
        else if (!displayedRemain30sec && timeLimitSeconds < 30f)
        {
            ActionPacket myActionPacket = new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.DISPLAY_LARGE_MSG, value: 2, msg: "のこり３０秒！");
            Header myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
            udpGameServer.Send(myHeader.ToByte());
            displayedRemain30sec = true;
        }
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

                            //ActorControllerインスタンスを作りDictionaryに加える
                            //Actorをインスタンス化しながらActorControllerを取得
                            GameObject actorPrefab;
                            switch ((Definer.PLAYER_COLOR)receivedInitPacket.playerColor)
                            {
                                case Definer.PLAYER_COLOR.RED:
                                    actorPrefab = RedActorPrefab;
                                    redNameText.text = receivedInitPacket.playerName;
                                    redArrow.SetActive(true);
                                    SEPlayer.instance.PlaySELoginRed();

                                    redArrowTween = redArrowIconPos.DOLocalMoveY(redArrowIconPos.localPosition.y + 30f, 1f).SetLoops(-1, LoopType.Yoyo);
                                    break;
                                case Definer.PLAYER_COLOR.GREEN:
                                    actorPrefab = GreenActorPrefab;
                                    greenNameText.text = receivedInitPacket.playerName;
                                    greenArrow.SetActive(true);
                                    SEPlayer.instance.PlaySELoginGreen();

                                    greenArrowTween = greenArrowIconPos.DOLocalMoveY(greenArrowIconPos.localPosition.y + 30f, 1f).SetLoops(-1, LoopType.Yoyo);
                                    break;
                                case Definer.PLAYER_COLOR.BLUE:
                                    actorPrefab = BlueActorPrefab;
                                    blueNameText.text = receivedInitPacket.playerName;
                                    blueArrow.SetActive(true);
                                    SEPlayer.instance.PlaySELoginBlue();

                                    blueArrowTween = blueArrowIconPos.DOLocalMoveX(blueArrowIconPos.localPosition.x + 30f, 1f).SetLoops(-1, LoopType.Yoyo);
                                    break;
                                case Definer.PLAYER_COLOR.YELLOW:
                                    actorPrefab = YellowActorPrefab;
                                    yellowNameText.text = receivedInitPacket.playerName;
                                    yellowArrow.SetActive(true);
                                    SEPlayer.instance.PlaySELoginYellow();

                                    yellowArrowTween = yellowArrowIconPos.DOLocalMoveX(yellowArrowIconPos.localPosition.x + 30f, 1f).SetLoops(-1, LoopType.Yoyo);
                                    break;
                                default:
                                    actorPrefab = WhiteActorPrefab;
                                    break;
                            }
                            ActorController actorController = Instantiate(actorPrefab).GetComponent<ActorController>();

                            //アクターの名前を書き込み
                            actorController.PlayerName = receivedInitPacket.playerName;
                            //アクターの色を書き込み
                            actorController.Color = (Definer.PLAYER_COLOR)receivedInitPacket.playerColor;

                            //アクターのゲームオブジェクトに名前をつける
                            actorController.name = $"Actor: {receivedInitPacket.playerName} ({receivedHeader.sessionID})"; //ActorControllerはMonoBehaviourを継承しているので"name"はオブジェクトの名称を決める
                            actorController.gameObject.SetActive(false); //初期設定が済んだら無効化して処理を止める。ゲーム開始時に有効化して座標などをセットする

                            //アクター辞書に登録
                            actorDictionary.Add(receivedHeader.sessionID, actorController);

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
                                upperTextBox.text = "プレイヤーが集まりました！";
                                lowerTextBox.text = "プレイヤーが集まりました！";

                                await UniTask.Delay(1500);

                                Debug.Log($"十分なプレイヤーが集まったぜ。闇のゲームの始まりだぜ。");

                                //ゲーム開始処理
                                //全クライアントにアクターの生成命令を送る

                                //4つのリスポーン地点を取得する
                                Vector3[] respawnPoints = MapGenerator.instance.Get4RespawnPointsRandomly(); //テストプレイでは4人未満でデバッグするかもしれないが、そのときは先頭の要素だけ使う
                                int index = 0;

                                foreach (KeyValuePair<ushort, ActorController> k in actorDictionary)
                                {
                                    //リスポーン地点を参照しながら各プレイヤーの名前とIDを載せてアクター生成命令を飛ばす
                                    myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.SPAWN_ACTOR, k.Key, (int)k.Value.Color, respawnPoints[index], default, k.Value.PlayerName);
                                    myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                    udpGameServer.Send(myHeader.ToByte());
                                    index++;
                                }

                                Debug.Log($"アクターを生成命令を出したぜ。");

                                blackImage.DOFade(1f, 0.3f).OnComplete(() =>
                                {
                                    ChangeServerState(new Phase1State());
                                    blackImage.DOFade(0f, 0.3f);
                                });
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
                                                Debug.Log("やったー！全プレイヤーの準備ができたよ！");

                                                allActorsPrepared = true;
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
                                                //UI編集
                                                switch (actorDictionary[receivedActionPacket.targetID].Color)
                                                {
                                                    case Definer.PLAYER_COLOR.RED:
                                                        redArrow.SetActive(false);
                                                        redNameText.text = "切断されました";
                                                        break;
                                                    case Definer.PLAYER_COLOR.GREEN:
                                                        greenArrow.SetActive(false);
                                                        greenNameText.text = "切断されました";
                                                        break;
                                                    case Definer.PLAYER_COLOR.BLUE:
                                                        blueArrow.SetActive(false);
                                                        blueNameText.text = "切断されました";
                                                        break;
                                                    case Definer.PLAYER_COLOR.YELLOW:
                                                        yellowArrow.SetActive(false);
                                                        yellowNameText.text = "切断されました";
                                                        break;
                                                }
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
                                            EnableShiningEffect();

                                            //重複しないentityIDを作り、オブジェクトを生成しつつ、エンティティのコンポーネントを取得
                                            //goldPileという変数名をここでだけ使いたいのでブロック文でスコープ分け
                                            {
                                                entityID = GetUniqueEntityID();
                                                Vector3 goldPos = new Vector3(actorDictionary[receivedActionPacket.targetID].transform.position.x, 0.1f, actorDictionary[receivedActionPacket.targetID].transform.position.z);
                                                //金額によってモデル変更
                                                GoldPile goldPile = lostGold > 50 ? Instantiate(GoldPilePrefab, goldPos, Quaternion.identity).GetComponent<GoldPile>() : Instantiate(GoldPileMiniPrefab, goldPos, Quaternion.identity).GetComponent<GoldPile>();
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
                                                EnableShiningEffect();
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

                                                //重複しないentityIDを作り、オブジェクトを生成しつつ、エンティティのコンポーネントを取得
                                                //goldPileという変数名をここでだけ使いたいのでブロック文でスコープ分け
                                                {
                                                    entityID = GetUniqueEntityID();
                                                    Vector3 goldPos = new Vector3(entityDictionary[receivedActionPacket.targetID].transform.position.x, 0.1f, entityDictionary[receivedActionPacket.targetID].transform.position.z);
                                                    System.Random random = new System.Random();

                                                    int chestGold;
                                                    switch (((Chest)entityDictionary[receivedActionPacket.targetID]).Tier)
                                                    {
                                                        case 1:
                                                            chestGold = random.Next(80, 201); //ランダムなゴールド量の金貨の山を生成 適当に80~200ゴールド
                                                            break;
                                                        case 2:
                                                            chestGold = random.Next(300, 501); //ランダムなゴールド量の金貨の山を生成 適当に80~200ゴールド
                                                            break;
                                                        case 3:
                                                            chestGold = random.Next(2000, 4001); //ランダムなゴールド量の金貨の山を生成 適当に80~200ゴールド
                                                            break;
                                                        default:
                                                            chestGold = 1;
                                                            break;
                                                    }
                                                    //金額によってモデル変更
                                                    GoldPile goldPile = chestGold > 50 ? Instantiate(GoldPilePrefab, goldPos, Quaternion.identity).GetComponent<GoldPile>() : Instantiate(GoldPileMiniPrefab, goldPos, Quaternion.identity).GetComponent<GoldPile>();
                                                    goldPile.EntityID = entityID; //値を書き込み
                                                    goldPile.Value = chestGold;
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
                                                    //宝箱のティアを制限時間に応じて異なる確立で抽選する
                                                    int tier = 1;
                                                    if (262 < timeLimitSeconds) //4:22まではティア１
                                                    {
                                                        tier = 1;
                                                    }
                                                    else if (196 < timeLimitSeconds) //3:16までは50%ティア1、50%ティア2
                                                    {
                                                        System.Random random = new System.Random(); //UnityEngine.Randomはマルチスレッドで使用できないのでSystemを使う
                                                        int rand = random.Next(0, 99); //0~100
                                                        if (rand < 50) tier = 2;

                                                        if (chest2First)
                                                        {
                                                            tier = 2;
                                                            chest3First = false;
                                                        }
                                                    }
                                                    else if (130 < timeLimitSeconds)
                                                    {
                                                        System.Random random = new System.Random(); //2:10までは50%ティア2、15%ティア3、35%ティア1
                                                        int rand = random.Next(0, 99); //0~100
                                                        if (rand < 50) tier = 2;
                                                        else if (rand < 65) tier = 3;

                                                        if (chest3First)
                                                        {
                                                            tier = 3;
                                                            chest3First = false;
                                                        }
                                                    }
                                                    else if (64 < timeLimitSeconds)　//1:04までは70%ティア2、30%ティア3
                                                    {
                                                        tier = 2;
                                                        System.Random random = new System.Random();
                                                        int rand = random.Next(0, 99); //0~100
                                                        if (rand < 30) tier = 3;
                                                    }
                                                    else //最後の1分は50%ティア2、50%ティア3
                                                    {
                                                        tier = 2;
                                                        System.Random random = new System.Random();
                                                        int rand = random.Next(0, 99); //0~100
                                                        if (rand < 50) tier = 3;
                                                    }

                                                    //まずサーバー側のシーンで
                                                    entityID = GetUniqueEntityID(); //エンティティID生成
                                                    Vector3 chestPos = MapGenerator.instance.GetUniqueChestPointRandomly(); //座標決め
                                                    switch (tier)
                                                    {
                                                        case 1:
                                                            chest = Instantiate(ChestPrefab, chestPos, Quaternion.identity).GetComponent<Chest>();
                                                            chest.Tier = 1; //レア度1
                                                            break;
                                                        case 2:
                                                            chest = Instantiate(Chest2Prefab, chestPos, Quaternion.identity).GetComponent<Chest>();
                                                            chest.Tier = 2; //レア度2
                                                            break;
                                                        case 3:
                                                            chest = Instantiate(Chest3Prefab, chestPos, Quaternion.identity).GetComponent<Chest>();
                                                            chest.Tier = 3; //レア度3
                                                            break;
                                                    }
                                                    chest.EntityID = entityID; //ID書き込み
                                                    chest.gameObject.name = $"Chest (Tier{tier}) ({entityID})";
                                                    entityDictionary.Add(entityID, chest); //辞書に登録

                                                    //ティア（１）と座標を指定して、宝箱を生成する命令
                                                    myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.SPAWN_CHEST, entityID, tier, chestPos);
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

                                                dropGold = random.Next((ownedGold / 100) * 1, (ownedGold / 100) * 3); //所持金の1%~3%までの間で抽選
                                                if (dropGold == 0) dropGold++; //落とすゴールドが0になってしまう場合1ゴールドにする 
                                                dropGold = Mathf.Clamp(dropGold, dropGold, ownedGold); //所持金を超えないようにclampする

                                                //プレイヤーから送られてきた座標をもとに金貨の山を生成
                                                //対象プレイヤーの所持金を減らす
                                                //まずサーバー側で
                                                actorDictionary[receivedHeader.sessionID].Gold -= dropGold;
                                                //パケット送信    
                                                myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.EDIT_GOLD, receivedHeader.sessionID, -dropGold);
                                                myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                                udpGameServer.Send(myHeader.ToByte());
                                                EnableShiningEffect();

                                                //まずサーバー側で金貨の山を生成
                                                entityID = GetUniqueEntityID();
                                                Vector3 goldPos = receivedActionPacket.pos;
                                                //金額によってモデル変更
                                                GoldPile goldPile = dropGold > 50 ? Instantiate(GoldPilePrefab, goldPos, Quaternion.identity).GetComponent<GoldPile>() : Instantiate(GoldPileMiniPrefab, goldPos, Quaternion.identity).GetComponent<GoldPile>();
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
                                        case (byte)Definer.REID.TOUCH_CHEST:
                                            myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.MOTION_CHEST, receivedHeader.sessionID);
                                            myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                            udpGameServer.Send(myHeader.ToByte());
                                            break;
                                        case (byte)Definer.REID.OPEN_SCROLL:
                                            myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.MOTION_SCROLL, receivedHeader.sessionID);
                                            myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                            udpGameServer.Send(myHeader.ToByte());
                                            break;
                                        case (byte)Definer.REID.STUNNED:
                                            myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.MOTION_STUN, receivedHeader.sessionID);
                                            myHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myActionPacket.ToByte());
                                            udpGameServer.Send(myHeader.ToByte());
                                            break;
                                        case (byte)Definer.REID.BOOL_MOTION_FLAG_FALSE:
                                            myActionPacket = new ActionPacket((byte)Definer.RID.EXE, (byte)Definer.EDID.MOTION_END, receivedHeader.sessionID);
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

    private bool EnableShiningEffect()
    {
        int topGold = 0;
        ushort topPlayerID = 0;
        foreach (KeyValuePair<ushort, ActorController> k in actorDictionary)
        {
            if (k.Value.Gold > topGold)
            {
                topPlayerID = k.Key;
                topGold = k.Value.Gold;
            }
        }
        if (topPlayerID != 0)
        {
            foreach (KeyValuePair<ushort, ActorController> k in actorDictionary)
            {
                if (k.Key == topPlayerID) k.Value.IsShining = true;
                else k.Value.IsShining = false;
            }
            return true;
        }
        else return false;
    }

    //ゲームの結果発表のために使う
    public List<(string name, int gold, Definer.PLAYER_COLOR color)> GetGameResult()
    {
        List<(string name, int gold, Definer.PLAYER_COLOR color)> ret = new List<(string name, int gold, Definer.PLAYER_COLOR color)>();

        foreach (KeyValuePair<ushort, ActorController> k in actorDictionary)
        {
            ret.Add((k.Value.PlayerName, k.Value.Gold, k.Value.Color));
        }

        return ret;
    }

    private void OnDestroy()
    {
        //稼働中なら切断パケット
        if (isRunning)
        {
            udpGameServer.Send(new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.DISCONNECT).ToByte()).ToByte());
        }
        this.udpGameServer?.Dispose();
    }
}
