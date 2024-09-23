using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

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


    private bool inGame = false; //ゲームが始まっているか

    #region ボタンが押されたらサーバーを有効化したり無効化したり
    public void InitObservation(UdpButtonManager udpUIManager)
    {
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

        //sessionIDについて、0はsessionIDを持っていないクライアントを表すナンバーなので、予め使用済にしておく。
        usedID.Add(0);

        //パケットの処理をUpdateでやると1フレームの計算量が保障できなくなる（カクつきの原因になり得る）のでマルチスレッドで
        //スレッドが何個いるのかは試してみないと分からない
        Task.Run(() => ProcessPacket());
    }

    private async void ProcessPacket()
    {
        while (true)
        {
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
                    case (byte)Definer.PT.IPC:

                        //InitPacketを受け取ったときの処理
                        Debug.Log($"Initパケットを処理するぜ！ActorDictionaryに追加するぜ！");

                        //クラスに変換する
                        InitPacketClient receivedInitPacket = new InitPacketClient(receivedHeader.data);

                        //送られてきたプレイヤーネームが使用済ならエラーコード1番を返す。sessionIDは登録しない。
                        if (usedName.Contains(receivedInitPacket.playerName))
                        {
                            InitPacketServer errorPacket = new InitPacketServer(receivedInitPacket.initSessionPass, rcvPort, receivedHeader.sessionID, 1);
                            Header errorHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.IPS, errorPacket.ToByte());

                            Debug.Log($"プレイヤーネーム:{receivedInitPacket.playerName} は既に使われていたぜ。出直してもらうぜ。");
                            break;
                        }
                        //TODO プレイヤーが規定人数集まっていたらエラーコード2番

                        //ActorControllerインスタンスを作りDictionaryに加える
                        actorDictionary.Add(receivedHeader.sessionID, new ActorController(receivedInitPacket.playerName));
                        usedID.Add(receivedHeader.sessionID); //このIDを使用済にする
                        usedName.Add(receivedInitPacket.playerName); //登録したプレイヤーネームを使用済にする

                        Debug.Log($"sessionID:{receivedHeader.sessionID},プレイヤーネーム:{receivedInitPacket.playerName} でactorDictionaryに登録したぜ！");

                        Debug.Log($"actorDictionaryには現在、{actorDictionary.Count}人のプレイヤーが登録されているぜ！");

                        //パケットを返信する
                        InitPacketServer myIPacket = new InitPacketServer(receivedInitPacket.initSessionPass, rcvPort, receivedHeader.sessionID);
                        Header myIHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.IPS, myIPacket.ToByte());

                        udpGameServer.Send(myIHeader.ToByte());
                        Debug.Log($"パケット返信したぜ！");

                        if (actorDictionary.Count == 1)
                        {
                            Debug.Log($"十分なプレイヤーが集まったぜ。闇のゲームの始まりだぜ。");

                            //Dictionaryへの登録を締め切る処理

                            //ゲーム開始処理
                            inGame = true;

                            float f = 0.5f;
                            foreach (KeyValuePair<ushort, ActorController> k in actorDictionary)
                            {
                                Debug.Log($"パケット送ってゲームはじめます");



                                ActionPacket myAPacket = new ActionPacket((byte)Definer.RID.NOT, (byte)Definer.NDID.STG, k.Key, new Vector3(9.5f, 0.2f, 9 + f));
                                Header myAHeader = new Header(serverSessionID, 0, 0, 0, (byte)Definer.PT.AP, myAPacket.ToByte());
                                udpGameServer.Send(myAHeader.ToByte());
                                f += 0.5f;

                                Debug.Log($"送りました");
                            }
                            
                        }
                        
                        break;
                    case (byte)Definer.PT.AP:
                        //ActionPacketを受け取ったときの処理
                        break;

                    default:
                        Debug.Log($"{(Definer.PT)receivedHeader.packetType}はサーバーでは処理できないぜ。処理を終了するぜ。");
                        break;
                }
            }
        }
    }
}
