using System;
using System.Collections.Generic;
using UnityEngine;

public class GameClientManager : MonoBehaviour
{
    //�N���C�A���g���ŕ�W���郊���[�g�G���h�|�C���g�̐�
    private const int NUM_OF_REQUIRED_REMOTE_END_POINTS = 1;

    private Queue<byte[]> packetQueue;

    private UdpCommunicator udpComm;

    private UInt16 id;
    private bool idInit = false;

    GameObject[] players = new GameObject[4];

    //�����p�I�I�@�T�[�o�[�̍��W���󂯂Ĕ��f������I�u�W�F�N�g
    [SerializeField] GameObject obj;

    private void Start()
    {
        //�p�P�b�g�����܂��Ă����L���[
        packetQueue = new Queue<byte[]>();

        //UdpCommunicator�𐶐�
        udpComm = new UdpCommunicator(ref packetQueue, NUM_OF_REQUIRED_REMOTE_END_POINTS);

        //�T�[�o�[��T�����߁A���A����̓K���ȃp�P�b�g�𑗐M����
        CommData data = new CommData(udpComm.GetReceivePort(), new CommData.POS_DATA[4]);
        udpComm.Send(data.ToByte());
    }

    private void FixedUpdate()
    {
        CommData data = new CommData(packetQueue.Dequeue());

        //�����T�[�o�[����p�P�b�g�����Ă�����J�����č��W�����炤
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
        //�ʐM�ؒf���̏���
    }
}