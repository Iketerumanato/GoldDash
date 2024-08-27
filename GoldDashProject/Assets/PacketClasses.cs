using System;
using System.Linq;
using System.Text;

//�p�P�b�g�n�N���X�̊��N���X
public abstract class Packet
{
    //�o�C�g�z��ւ̕ϊ����\�b�h����������
    public abstract byte[] ToByte();

    //�o�C�g�z��A�̖����Ƀo�C�g�z��B����������
    protected byte[] AddByte(byte[] originBytes, byte[] addBytes)
    {
        byte[] ret = new byte[originBytes.Length + addBytes.Length];

        for (int i = 0; i < ret.Length; i++)
        {
            ret[i] = i < originBytes.Length ? originBytes[i] : addBytes[i - originBytes.Length];
        }

        return ret;
    }
    //�o�C�g�z��̖����ɔC�ӂ̃o�C�g�����������
    protected byte[] AddByte(byte[] originBytes, byte addByte)
    {
        byte[] ret = new byte[originBytes.Length + 1];

        for (int i = 0; i < ret.Length; i++)
        {
            ret[i] = i < originBytes.Length ? originBytes[i] : addByte;
        }

        return ret;
    }
}

//UDPClient���瑗�M����p�P�b�g�̐擪�ɕt�^����J�X�^��UDP�w�b�_�B���M�ԍ����������ĒʐM���������RUDP�ɐi���B���߂łƂ��I
public class Header : Packet
{
    private ushort sessionID; //�T�[�o�[����^����ID�B�Z�L�����e�B���v��Ȃ�n�b�V�����g���ׂ����B
    private ushort indexDiff; //���̃p�P�b�g�ȍ~�ɑ����p�P�b�g(RUDP�p�̌Â��p�P�b�g)�̈ʒu�ƁA���̃p�P�b�g�̐擪�C���f�b�N�X�̍�������
    private uint sendNum; //���̃p�P�b�g�̑��M�ԍ�
    private uint ackNum; //�Ō�ɑ��肩��󂯎�����p�P�b�g�̑��M�ԍ�
    private ushort packetType; //���̃p�P�b�g�̃^�C�v
    private byte[] data; //�f�[�^�{��

    //�R���X�g���N�^�P�@�e�ϐ��̒l�𒼐ڎw�肷��
    public Header(ushort sessionID, ushort indexDiff, uint sendNum, uint ackNum, byte packetType, byte[] data)
    {
        this.sessionID = sessionID;
        this.indexDiff = indexDiff; //�Â��p�P�b�g�Ƃ̈ʒu�֌W�͑��M���ɕ�����̂ŁA�������璼�ڂƂ�΂悢
        this.sendNum = sendNum;
        this.ackNum = ackNum;
        this.packetType = packetType;
        this.data = data;
    }

    //�R���X�g���N�^�Q�@�o�C�g�z���ǂ�ŕϐ���������
    public Header(byte[] bytes)
    {
        int index = 0;

        this.sessionID = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        this.indexDiff = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        this.sendNum = BitConverter.ToUInt32(bytes, index);
        index += sizeof(uint);
        this.ackNum = BitConverter.ToUInt32(bytes, index);
        index += sizeof(uint);
        this.sessionID = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        data = bytes.Skip(index).ToArray();
    }

    //�ϐ�����o�C�g�z����o�͂���
    public override byte[] ToByte()
    {
        byte[] ret = new byte[0];

        ret = AddByte(ret, BitConverter.GetBytes(sessionID));
        ret = AddByte(ret, BitConverter.GetBytes(indexDiff));
        ret = AddByte(ret, BitConverter.GetBytes(sendNum));
        ret = AddByte(ret, BitConverter.GetBytes(ackNum));
        ret = AddByte(ret, BitConverter.GetBytes(packetType));
        ret = AddByte(ret, data);

        return ret;
    }
}

//����ʐM�p�p�P�b�g
public class InitPacket : Packet
{
    private ushort pass; //�}�b�`���O�p�p�X���[�h
    private ushort rcvPort; //�N���C�A���g����M�p�ɋ󂯂Ă���|�[�g�̔ԍ�
    private byte playerNameLength; //�v���C���[���̃o�C�g��
    private string playerName; //�v���C���[��

    public InitPacket(ushort pass, ushort rcvPort, string playerName)
    {
        this.pass = pass;
        this.rcvPort = rcvPort;
        this.playerName = playerName;
        this.playerNameLength = (byte)playerName.Length;
    }

    public InitPacket(byte[] bytes)
    {
        int index = 0;

        this.pass = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        this.rcvPort = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        this.playerNameLength = bytes[index];
        index++;
        this.playerName = Encoding.UTF8.GetString(bytes, index, playerNameLength);
    }

    public override byte[] ToByte()
    {
        byte[] ret = new byte[0];

        ret = AddByte(ret, BitConverter.GetBytes(pass));
        ret = AddByte(ret, BitConverter.GetBytes(rcvPort));
        ret = AddByte(ret, BitConverter.GetBytes(playerNameLength));
        ret = AddByte(ret, Encoding.UTF8.GetBytes(playerName));

        return ret;
    }
}

public class ActionPacket : Packet
{
    byte roughID; //�A�N�V�����̃J�e�S��������
    byte detailID; //�A�N�V�����̏ڍׂȎ�ނ�����
    byte targetID; //�A�N�V�����̑Ώۂ�����
    UnityEngine.Vector3 pos; //���W�f�[�^�����A�N�V�����ŎQ�Ƃ���

    public ActionPacket(byte roughID, byte detailID, byte targetID, UnityEngine.Vector3 pos)
    {
        this.roughID = roughID;
        this.detailID = detailID;
        this.targetID = targetID;
        this.pos = pos;
    }

    public ActionPacket(byte[] bytes)
    {
        int index = 0;
        float x, y, z;

        this.roughID = bytes[index];
        index++;
        this.detailID = bytes[index];
        index++;
        this.targetID = bytes[index];
        index++;
        x = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        y = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        z = BitConverter.ToSingle(bytes, index);
        pos = new UnityEngine.Vector3(x, y, z);
    }

    public override byte[] ToByte()
    {
        byte[] ret = new byte[0];

        ret = AddByte(ret, roughID);
        ret = AddByte(ret, detailID);
        ret = AddByte(ret, targetID);
        ret = AddByte(ret, BitConverter.GetBytes(pos.x));
        ret = AddByte(ret, BitConverter.GetBytes(pos.y));
        ret = AddByte(ret, BitConverter.GetBytes(pos.z));

        return ret;
    }
}
