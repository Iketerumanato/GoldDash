using System;
using System.Linq;
using System.Text;
using UnityEngine;

//�p�P�b�g�n�N���X�̊��N���X
public abstract class Packet
{
    //�o�C�g�z��ւ̕ϊ����\�b�h����������
    public abstract byte[] ToByte();

    //�o�C�g�z��A�̖����Ƀo�C�g�z��B����������
    protected byte[] AddBytes(byte[] originBytes, byte[] addBytes)
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

        ret = AddBytes(ret, BitConverter.GetBytes(sessionID));
        ret = AddBytes(ret, BitConverter.GetBytes(indexDiff));
        ret = AddBytes(ret, BitConverter.GetBytes(sendNum));
        ret = AddBytes(ret, BitConverter.GetBytes(ackNum));
        ret = AddBytes(ret, BitConverter.GetBytes(packetType));
        ret = AddBytes(ret, data);

        return ret;
    }
}

//����ʐM�p�p�P�b�g
public class InitPacketClient : Packet
{
    private ushort pass; //�}�b�`���O�p�p�X���[�h
    private ushort rcvPort; //�N���C�A���g����M�p�ɋ󂯂Ă���|�[�g�̔ԍ�
    private byte playerNameLength; //�v���C���[���̃o�C�g��
    private string playerName; //�v���C���[��

    public InitPacketClient(ushort pass, ushort rcvPort, string playerName)
    {
        this.pass = pass;
        this.rcvPort = rcvPort;
        this.playerName = playerName;
        this.playerNameLength = (byte)playerName.Length;
    }

    public InitPacketClient(byte[] bytes)
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

        ret = AddBytes(ret, BitConverter.GetBytes(pass));
        ret = AddBytes(ret, BitConverter.GetBytes(rcvPort));
        ret = AddByte(ret, playerNameLength);
        ret = AddBytes(ret, Encoding.UTF8.GetBytes(playerName));

        return ret;
    }
}

public class InitPacketServer : Packet
{
    private ushort rcvPort; //�T�[�o�̃|�[�g�ԍ�
    private ushort sessionID; //�T�[�o�[����^����ID
    private byte state; //���݂̃T�[�o�̏�Ԃ�Ԃ��v���C���[���̏d�����N�����Ƃ��Ȃ�
    private byte error; //�G���[�R�[�h�@���݂̃T�[�o�̏�Ԃ�Ԃ��v���C���[���̏d�����N�����Ƃ��Ȃ�

    public InitPacketServer(ushort rcvPort, ushort sessionID, byte state, byte error)
    {
        this.rcvPort = rcvPort;
        this.sessionID = sessionID;
        this.state = state;
        this.error = error;
    }

    public InitPacketServer(byte[] bytes)
    {
        int index = 0;

        this.rcvPort = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        this.sessionID = BitConverter.ToUInt16(bytes, index);
        index += sizeof(ushort);
        this.state = bytes[index];
        index++;
        this.error = bytes[index];
    }
    public override byte[] ToByte()
    {
        byte[] ret = new byte[0];

        ret = AddBytes(ret, BitConverter.GetBytes(rcvPort));
        ret = AddBytes(ret, BitConverter.GetBytes(sessionID));
        ret = AddByte(ret, state);
        ret = AddByte(ret, error);

        return ret;
    }
}

public class ActionPacket : Packet
{
    byte roughID; //�A�N�V�����̃J�e�S��������
    byte detailID; //�A�N�V�����̏ڍׂȎ�ނ�����
    byte targetID; //�A�N�V�����̑Ώۂ�����
    Vector3 pos; //���W�f�[�^�����A�N�V�����ŎQ�Ƃ���

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
        pos = new Vector3(x, y, z);
    }

    public override byte[] ToByte()
    {
        byte[] ret = new byte[0];

        ret = AddByte(ret, roughID);
        ret = AddByte(ret, detailID);
        ret = AddByte(ret, targetID);
        ret = AddBytes(ret, BitConverter.GetBytes(pos.x));
        ret = AddBytes(ret, BitConverter.GetBytes(pos.y));
        ret = AddBytes(ret, BitConverter.GetBytes(pos.z));

        return ret;
    }
}

public class PositionPacket : Packet
{
    byte id0;
    Vector3 pos0;
    byte id1;
    Vector3 pos1;
    byte id2;
    Vector3 pos2;
    byte id3;
    Vector3 pos3;

    public PositionPacket(byte id0, Vector3 pos0, byte id1, Vector3 pos1, byte id2, Vector3 pos2, byte id3, Vector3 pos3)
    {
        this.id0 = id0;
        this.pos0 = pos0;
        this.id1 = id1;
        this.pos1 = pos1;
        this.id2 = id2;
        this.pos2 = pos2;
        this.id3 = id3;
        this.pos3 = pos3;
    }

    public PositionPacket(byte[] bytes)
    {
        int index = 0;
        float x, y, z;

        //1�l��
        this.id0 = bytes[index];
        index++;
        x = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        y = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        z = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        pos0 = new Vector3(x, y, z);
        //2�l��
        this.id1 = bytes[index];
        index++;
        x = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        y = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        z = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        pos1 = new Vector3(x, y, z);
        //3�l��
        this.id2 = bytes[index];
        index++;
        x = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        y = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        z = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        pos2 = new Vector3(x, y, z);
        //4�l��
        this.id3 = bytes[index];
        index++;
        x = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        y = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        z = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        pos3 = new Vector3(x, y, z);
    }

    public override byte[] ToByte()
    {
        byte[] ret = new byte[0];

        ret = AddByte(ret, id0);
        ret = AddBytes(ret, BitConverter.GetBytes(pos0.x));
        ret = AddBytes(ret, BitConverter.GetBytes(pos0.y));
        ret = AddBytes(ret, BitConverter.GetBytes(pos0.z));
        ret = AddByte(ret, id1);
        ret = AddBytes(ret, BitConverter.GetBytes(pos1.x));
        ret = AddBytes(ret, BitConverter.GetBytes(pos1.y));
        ret = AddBytes(ret, BitConverter.GetBytes(pos1.z));
        ret = AddByte(ret, id2);
        ret = AddBytes(ret, BitConverter.GetBytes(pos2.x));
        ret = AddBytes(ret, BitConverter.GetBytes(pos2.y));
        ret = AddBytes(ret, BitConverter.GetBytes(pos2.z));
        ret = AddByte(ret, id3);
        ret = AddBytes(ret, BitConverter.GetBytes(pos3.x));
        ret = AddBytes(ret, BitConverter.GetBytes(pos3.y));
        ret = AddBytes(ret, BitConverter.GetBytes(pos3.z));

        return ret;
    }
}
