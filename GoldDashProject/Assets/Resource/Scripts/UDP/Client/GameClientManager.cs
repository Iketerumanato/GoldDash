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

    private int numOfActors; //アクターの人数
    private int preparedActors; //生成し終わったアクターの数

    [SerializeField] private GameObject ActorObject; //アクターのプレハブ
    [SerializeField] private GameObject PlayerObject; //プレイヤーのプレハブ

    #region ボタンが押されたら有効化したり無効化したり
    public void InitObservation(UdpButtonManager udpUIManager)
    {
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
        packetQueue = new Queue<Header>();

        Task.Run(() => ProcessPacket());
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
                        Debug.Log($"Initパケットを処理するぜ！SessionIDを受け取るぜ！");

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

                        Debug.Log($"{receivedActionPacket.roughID}を処理するぜ！");

                        switch (receivedActionPacket.roughID)
                        {
                            case (byte)Definer.RID.NOT:

                                switch (receivedActionPacket.detailID)
                                {
                                    case (byte)Definer.NDID.NONE:
                                        break;

                                    case (byte)Definer.NDID.HELLO:
                                        break;
                                    case (byte)Definer.NDID.PSG:
                                        //生成すべきアクターの数を受け取る
                                        numOfActors = receivedActionPacket.targetID;
                                        break;
                                    case (byte)Definer.NDID.STG:
                                        //ここでプレイヤーを有効化してゲーム開始
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
                                        actorDictionary.Add(receivedHeader.sessionID, actorController);

                                        //準備が完了したアクターの数を加算
                                        preparedActors++;
                                        if (preparedActors == numOfActors) //準備完了通知をサーバに送る
                                        {

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
                        //全アクターの位置を更新
                        break;

                    default:
                        Debug.Log($"{(Definer.PT)receivedHeader.packetType}はクライアントでは処理できないぜ。処理を終了するぜ。");
                        break;
                }
            }
        }
    }
}
