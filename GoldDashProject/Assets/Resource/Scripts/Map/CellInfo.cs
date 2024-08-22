using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CellInfo
{
    public CellInfo()
    { 
        wallLeft = WALL_TYPE.NONE;
        wallRight = WALL_TYPE.NONE;
        wallUpper = WALL_TYPE.NONE;
        wallLower = WALL_TYPE.NONE;

        cellType = CELL_TYPE.ROOM;

        spawnChest = false;
        spawnPlayer = false;
    }

    //�Z�������͂ޕǂ̗L���A���̃^�C�v
    public enum WALL_TYPE
    {
        NONE = 0,
        WALL = 1,
        DOOR,
    }

    //�Z���������ł��邩�A�L���ł��邩�Ȃ�
    public enum CELL_TYPE
    {
        NONE = 0,
        ROOM = 1,
        PATH,
    }

    //�ϐ���
    //�ǂ̗L��
    public WALL_TYPE wallLeft;
    public WALL_TYPE wallRight;
    public WALL_TYPE wallUpper;
    public WALL_TYPE wallLower;

    //�Z���̃^�C�v
    public CELL_TYPE cellType;

    //�󔠂��X�|�[��������}�X��
    public bool spawnChest;

    //�v���C���[���X�|�[��������}�X��
    public bool spawnPlayer;

}
