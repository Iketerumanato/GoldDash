using System;
using System.Collections.Generic;
using UnityEngine;

public class GameClientManager : MonoBehaviour
{
    //クライアント側で募集するリモートエンドポイントの数
    private const int NUM_OF_REQUIRED_REMOTE_END_POINTS = 1;

    private Queue<byte[]> packetQueue;

    private UdpCommunicator udpComm;

    private UInt16 id;
    private bool idInit = false;

    GameObject[] players = new GameObject[4];

    //試験用！！　サーバーの座標を受けて反映させるオブジェクト
    [SerializeField] GameObject obj;

    private void Start()
    {
        //パケットをしまっておくキュー
        packetQueue = new Queue<byte[]>();

        //UdpCommunicatorを生成
        udpComm = new UdpCommunicator(ref packetQueue, NUM_OF_REQUIRED_REMOTE_END_POINTS);

        //サーバーを探すため、挨拶代わりの適当なパケットを送信する
        CommData data = new CommData(udpComm.GetReceivePort(), new CommData.POS_DATA[4]);
        udpComm.Send(data.ToByte());
    }

    private void FixedUpdate()
    {
        CommData data = new CommData(packetQueue.Dequeue());

        //もしサーバーからパケットが来ていたら開封して座標をもらう
        if (packetQueue.Count != 0)
        {
            if (!idInit)
            {
                this.id = data.num;
                players[id] = obj;
            }


            for (int i = 0; i < 4; i++)
            {
                if (i == id) continue;

                players[i].transform.position = data.posDataArray[i].positionVec;
                players[i].transform.forward = data.posDataArray[id].forwardVec;
            }
        }

        if (idInit)
        {
            CommData sendData = new CommData(this.id, new CommData.POS_DATA[4]);
            sendData.posDataArray[id].positionVec = obj.transform.position;
            sendData.posDataArray[id].positionVec = obj.transform.forward;

            udpComm.Send(sendData.ToByte());
        }
    }

    private void OnDestroy()
    {
        //通信切断時の処理
    }
}