using System;
using System.Text;
using UnityEngine;

public class CommData
{
    //����ʐM�p
    private const string KEY_WORD = "ARTORIAS_THE_ABYSSWALKER";
    public UInt16 num; //�N���C�A���g����T�[�o�[�ւ͎�M�|�[�g�ԍ����A�T�[�o�[����N���C�A���g�ւ̓v���C���[�ԍ���

    //�v���C���[�̍��W�����Ɏg��
    
    public struct POS_DATA //�Q�ƌ^�ɂ���K�v���Ȃ������Ȃ̂ŃN���X�ł͂Ȃ��\���̂�
    {
        public byte playerNum;
        public Vector3 positionVec;
        public Vector3 forwardVec;
    }

    public POS_DATA[] posDataArray = new POS_DATA[4];

    //private int id;
    //public Vector3 vec;
    //private int messageSize;
    //private string message;

    //�U����A�C�e������̃g���K�[�ƂȂ�ϐ�
    //byte actionID; //���s�A�N�V�����̑�܂��ȕ���
    //byte detailID; //���s�A�N�V�����̏ڍׂȕ���
    //byte targetID; //�A�N�V�����̑Ώ�
    //Vector3 actionVec; //���W�������A�N�V�����ł���ꍇ�g��

    //�R���X�g���N�^�@�o�C�g����ϊ��@index�̎����ɂ��āA���ݗ͋Z������̂Ń��\�b�h������������
    public CommData(byte[] bytes)
    {
        int index = Encoding.UTF8.GetBytes(KEY_WORD).Length;

        //�L�[���[�h�����͔�΂���ID��ǂ�
        this.num = BitConverter.ToUInt16(bytes, index);
        index += sizeof(UInt16);

        for (int i = 0; i < 4; i++)
        {
            this.posDataArray[i].playerNum = bytes[index];
            index++;
            this.posDataArray[i].positionVec.x = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
            this.posDataArray[i].positionVec.y = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
            this.posDataArray[i].positionVec.z = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);

            this.posDataArray[i].forwardVec.x = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
            this.posDataArray[i].forwardVec.y = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
            this.posDataArray[i].forwardVec.z = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
        }
    }

    //�R���X�g���N�^�@��������쐬
    public CommData(UInt16 num, POS_DATA[] data)
    {
        this.num = num;

        this.posDataArray = data;
    }

    //���̃C���X�^���X�̃����o�[���o�C�g�z��ɕϊ�����
    public byte[] ToByte()
    {
        //�T�C�Y0�̔z��ɑ΂��ĐV���Ȕz��𑫂��čs�����ƂŁA�ϒ��̃R���N�V�����̂悤�Ɏg��
        byte[] ret = new byte[0];

        ret = AddByte(ret, Encoding.UTF8.GetBytes(KEY_WORD)); //�L�[���[�h������
        ret = AddByte(ret, BitConverter.GetBytes(this.num)); //ID������

        foreach (POS_DATA data in posDataArray) //���W�f�[�^��������
        {
            ret = AddByte(ret, BitConverter.GetBytes(data.playerNum));
            ret = AddByte(ret, BitConverter.GetBytes(data.positionVec.x));
            ret = AddByte(ret, BitConverter.GetBytes(data.positionVec.y));
            ret = AddByte(ret, BitConverter.GetBytes(data.positionVec.z));
            ret = AddByte(ret, BitConverter.GetBytes(data.forwardVec.x));
            ret = AddByte(ret, BitConverter.GetBytes(data.forwardVec.y));
            ret = AddByte(ret, BitConverter.GetBytes(data.forwardVec.z));
        }

        return ret;

        //�o�C�g�z��A�̖����Ƀo�C�g�z��B����������
        byte[] AddByte(byte[] originBytes, byte[] addBytes)
        { 
            byte[] ret = new byte[originBytes.Length + addBytes.Length];

            for (int i = 0; i < ret.Length; i++)
            {
                ret [i] = i < originBytes.Length ? originBytes[i] : addBytes[i - originBytes.Length];
            }

            return ret;
        }
    }

    //�L�[���[�h�������Ă��邩�m�F����ÓI���\�b�h
    public static bool CheckKeyWord(byte[] bytes)
    {
        UnityEngine.Debug.Log($"�����Ă����L�[���[�h��{Encoding.UTF8.GetString(bytes, 0, Encoding.UTF8.GetBytes(KEY_WORD).Length)}");
        UnityEngine.Debug.Log($"�������L�[���[�h��{KEY_WORD}");
        return Encoding.UTF8.GetString(bytes, 0, Encoding.UTF8.GetBytes(KEY_WORD).Length).Equals(KEY_WORD);
    }

    //�����I�@�L�[���[�h�̎��ɏ����ꂽ�|�[�g�ԍ��𒲂ׂ�
    public static UInt16 GetPort(byte[] bytes)
    {
        return BitConverter.ToUInt16(bytes, Encoding.UTF8.GetBytes(KEY_WORD).Length);
    }
}
