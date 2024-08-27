using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainServerManager : MonoBehaviour
{
    //�T�[�o�[���ŕ�W���郊���[�g�G���h�|�C���g�̐�
    private const int NUM_OF_REQUIRED_REMOTE_END_POINTS = 4;

    private Queue<byte[]> packetQueue;

    private UdpCommunicator udpComm;


    //�����p�I�I�@�T�[�o�[���ō��W��������I�u�W�F�N�g
    [SerializeField] GameObject obj;

    private void Start()
    {
        //�p�P�b�g�����܂��Ă����L���[
        packetQueue = new Queue<byte[]>();

        //UdpCommunicator�𐶐�
        udpComm = new UdpCommunicator(ref packetQueue, NUM_OF_REQUIRED_REMOTE_END_POINTS);

    }
    
    private void FixedUpdate()
    {
        CommData.POS_DATA[] posDatas = new CommData.POS_DATA[4];

        CommData rcvData = new CommData(packetQueue.Dequeue());

        bool flg1 = false;
        bool flg2 = false;
        bool flg3 = false;
        bool flg4 = false;

        do
        {
            switch (rcvData.num)
            {
                case 0:
                    posDatas[0].positionVec = rcvData.posDataArray[0].positionVec;
                    posDatas[0].forwardVec = rcvData.posDataArray[0].forwardVec;
                    flg1 = true;
                    break;
                case 1:
                    posDatas[1].positionVec = rcvData.posDataArray[1].positionVec;
                    posDatas[1].forwardVec = rcvData.posDataArray[1].forwardVec;
                    flg2 = true;
                    break;
                case 2:
                    posDatas[2].positionVec = rcvData.posDataArray[2].positionVec;
                    posDatas[2].forwardVec = rcvData.posDataArray[2].forwardVec;
                    flg3 = true;
                    break;
                case 3:
                    posDatas[3].positionVec = rcvData.posDataArray[3].positionVec;
                    posDatas[3].forwardVec = rcvData.posDataArray[3].forwardVec;
                    flg4 = true;
                    break;
            
            }

            rcvData = new CommData(packetQueue.Dequeue());

        } while(flg1 && flg2 && flg3 && flg4);

        //�T�[�o�[�̃����[�g�G���h�|�C���g�n�b�V���Z�b�g�ɒN�������炻���ɍ��W�𑗐M����
        if (udpComm.HasRemoteEndPoint())
        {
            udpComm.Send(new CommData(0, posDatas).ToByte());
        }
    }

    private void OnDestroy()
    {
        //�ʐM�ؒf���̏���
    }
}
