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

    private Queue<byte[]> packetQueue; //udpGameServerは”勝手に”このキューにパケットを入れてくれる。不正パケット処理なども済んだ状態で入る。

    [SerializeField] private ushort sessionPass; //サーバーに入るためのパスワード。udpGameServerのコンストラクタに渡す。

    private Dictionary<ushort, ActorController> actorDictionary; //sessionパスを鍵としてactorインスタンスを保管

    private HashSet<ushort> usedID; //sessionIDの重複防止に使う。使用済IDを記録して新規発行時にはcontainsで調べる

    private HashSet<string> usedName; //プレイヤーネームの重複防止に使う。

    #region ボタンが押されたらサーバーを有効化したり無効化したり
    public void InitObservation(UdpButtonManager udpUIManager)
    {
        packetQueue = new Queue<byte[]>();

        udpUIManager.udpUIManagerSubject.Subscribe(e => ProcessUdpManagerEvent(e));
    }

    private void ProcessUdpManagerEvent(UdpButtonManager.UDP_BUTTON_EVENT e)
    {
        switch (e)
        {
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_START_SERVER_MODE:
                udpGameServer = new UdpGameServer(ref packetQueue, sessionPass);
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
        actorDictionary = new Dictionary<ushort, ActorController>();
        usedID = new HashSet<ushort>();
        usedName = new HashSet<string>();

        //sessionIDについて、0はsessionIDを持っていないクライアントを表すナンバーなので、予め使用済にしておく。
        usedID.Add(0);
        //同様に、1はサーバーを表すナンバーなので、予め使用済にしておく。
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

                //パケットを取り出す
                byte[] receivedBytes = packetQueue.Dequeue();

                Debug.Log("パケットを受け取ったぜ！開封するぜ！");

                //まずHeaderを取り出す
                Header rcvHeader = new Header(receivedBytes);

                Debug.Log($"ヘッダーを確認するぜ！パケット種別は{(PacketDefiner.PACKET_TYPE)rcvHeader.packetType}だぜ！");

                switch (rcvHeader.packetType)
                {
                    case (byte)PacketDefiner.PACKET_TYPE.INIT_PACKET_CLIENT:

                        //InitPacketを受け取ったときの処理
                        Debug.Log($"Initパケットを処理するぜ！ActorDictionaryに追加するぜ！");

                        //クラスに変換する
                        InitPacketClient rcvPacket = new InitPacketClient(rcvHeader.data);

                        //送られてきたプレイヤーネームが使用済ならエラーコード1番を返す。sessionIDは登録しない。
                        if (usedName.Contains(rcvPacket.playerName))
                        {
                            InitPacketServer errorPacket = new InitPacketServer(rcvPacket.initSessionPass, udpGameServer.rcvPort, 0, 1);
                            Header errorHeader = new Header(0, 0, 0, 0, (byte)PacketDefiner.PACKET_TYPE.INIT_PACKET_SERVER, errorPacket.ToByte());

                            Debug.Log($"プレイヤーネーム:{rcvPacket.playerName} は既に使われていたぜ。出直してもらうぜ。");
                        }
                        //TODO プレイヤーが規定人数集まっていたらエラーコード2番

                        //重複しないSessionIDを作る
                        ushort sessionID;
                        do
                        {
                            System.Random random = new System.Random(); //UnityEngine.Randomはマルチスレッドで使用できないのでSystemを使う
                            sessionID = (ushort)random.Next(0, 65535); //0から65535までの整数を生成して2バイトにキャスト
                        }
                        while (usedID.Contains(sessionID)); //使用済IDと同じ値を生成してしまったならやり直し

                        //ActorControllerインスタンスを作りDictionaryに加える
                        actorDictionary.Add(sessionID, new ActorController(rcvPacket.playerName));
                        usedID.Add(sessionID); //このIDを使用済にする
                        usedName.Add(rcvPacket.playerName); //登録したプレイヤーネームを使用済にする

                        Debug.Log($"sessionID:{sessionID},プレイヤーネーム:{rcvPacket.playerName} でDictionaryに登録したぜ！");

                        //パケットを返信する
                        InitPacketServer myPacket = new InitPacketServer(rcvPacket.initSessionPass, udpGameServer.rcvPort, sessionID);
                        Header myHeader = new Header(1, 0, 0, 0, (byte)PacketDefiner.PACKET_TYPE.INIT_PACKET_CLIENT, myPacket.ToByte());

                        udpGameServer.Send(myHeader.ToByte());
                        Debug.Log($"パケット返信したぜ！");
                        break;
                    case (byte)PacketDefiner.PACKET_TYPE.ACTION_PACKET:
                        //ActionPacketを受け取ったときの処理
                        break;
                    default:
                        Debug.Log($"{(PacketDefiner.PACKET_TYPE)rcvHeader.packetType}はサーバーでは処理できないぜ。処理を終了するぜ。");
                        break;
                }
            }
        }
    }
}
