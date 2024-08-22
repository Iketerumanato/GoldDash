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

    //セルを取り囲む壁の有無、そのタイプ
    public enum WALL_TYPE
    {
        NONE = 0,
        WALL = 1,
        DOOR,
    }

    //セルが部屋であるか、廊下であるかなど
    public enum CELL_TYPE
    {
        NONE = 0,
        ROOM = 1,
        PATH,
    }

    //変数↓
    //壁の有無
    public WALL_TYPE wallLeft;
    public WALL_TYPE wallRight;
    public WALL_TYPE wallUpper;
    public WALL_TYPE wallLower;

    //セルのタイプ
    public CELL_TYPE cellType;

    //宝箱がスポーンし得るマスか
    public bool spawnChest;

    //プレイヤーがスポーンし得るマスか
    public bool spawnPlayer;

}
