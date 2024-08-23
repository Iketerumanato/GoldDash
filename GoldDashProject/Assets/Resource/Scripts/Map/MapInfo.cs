using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class MapInfo : MonoBehaviour
{
    public bool IsNetwork = false;//true = Server/false = Client

    // 親オブジェクト
    [SerializeField] Transform stageParent;

    //生成するオブジェクト
    [SerializeField] GameObject floorObj;
    [SerializeField] GameObject wallObj;
    [SerializeField] GameObject doorObj;
    [SerializeField] GameObject chestObj;
    [SerializeField] GameObject respawnObj;
    [SerializeField] GameObject Player;

    //組み合わせるマップパーツの数。4つ。
    const int NUM_OF_PARTS = 4;

    //マップの一辺のマス数
    const int MAP_SIZE = 19;
    //マップパーツの一辺のマス数
    const int MAP_PART_SIZE = (MAP_SIZE - 1) / 2;

    //セル19*19個を集めた配列でマップを作る
    public CellInfo[,] map;

    private void Start()
    {
        //ランダムに選んだcsvファイルから19*19のcellInfo2次元配列を作成する
        //map = MergeMap(SelectCSVFileRandomly(NUM_OF_PARTS));

        //ランダム抽選は保留。指定した名前のファイルを４つ読み込む
        TextAsset[] textAsset_array = {
            Resources.Load("MapPart06") as TextAsset,
            Resources.Load("MapPart07") as TextAsset,
            Resources.Load("MapPart09") as TextAsset,
            Resources.Load("MapPart14") as TextAsset};

        map = MergeMap(textAsset_array);

        // Playerが生成される位置を格納する変数
        Vector3 playerSpawnPosition = Vector3.zero;

        //TODO
        //廊下を生成()
        //上の廊下
        for (int row = 0; row < MAP_SIZE; row++)
        {
            map[row, MAP_PART_SIZE] = new CellInfo();
        }
        //左の廊下
        for (int column = 0; column < MAP_SIZE; column++)
        {
            map[MAP_PART_SIZE, column] = new CellInfo();
        }

        //マップ中央に広場生成()
        map[8, 8] = new CellInfo();
        map[8, 10] = new CellInfo();
        map[10, 8] = new CellInfo();
        map[10, 10] = new CellInfo();

        //重複した壁データの削除
        DeleteDuplicatedWall(map);

        for (int i = 0; i < MAP_SIZE; i++)
        {
            for (int j = 0; j < MAP_SIZE; j++)
            {
                // 床
                if (map[i, j].cellType != CellInfo.CELL_TYPE.NONE)
                {
                    GameObject floor = Instantiate(floorObj, new Vector3(i + 0.5f, 0, j + 0.5f), Quaternion.identity);
                    floor.transform.parent = stageParent;
                }
                // 天井
                if (map[i, j].cellType != CellInfo.CELL_TYPE.NONE)
                {
                    GameObject ceiling = Instantiate(floorObj, new Vector3(i + 0.5f, 1f, j + 0.5f), Quaternion.Euler(180f, 0f, 0f));
                    ceiling.transform.parent = stageParent;
                }
                // 左の壁
                if (map[i, j].wallLeft == CellInfo.WALL_TYPE.WALL)
                {
                    GameObject leftWall = Instantiate(wallObj, new Vector3(i + 0.5f, 0.5f, j), Quaternion.Euler(0f, 90f, 0f));
                    leftWall.transform.parent = stageParent;
                }
                else if (map[i, j].wallLeft == CellInfo.WALL_TYPE.DOOR)
                {
                    GameObject leftDoor = Instantiate(doorObj, new Vector3(i + 0.5f, 0.5f, j), Quaternion.Euler(0f, 90f, 0f));
                    leftDoor.transform.parent = stageParent;
                }
                // 右の壁
                if (map[i, j].wallRight == CellInfo.WALL_TYPE.WALL)
                {
                    GameObject rightWall = Instantiate(wallObj, new Vector3(i + 0.5f, 0.5f, j + 1f), Quaternion.Euler(0f, 90f, 0f));
                    rightWall.transform.parent = stageParent;
                }
                else if (map[i, j].wallRight == CellInfo.WALL_TYPE.DOOR)
                {
                    GameObject rightDoor = Instantiate(doorObj, new Vector3(i + 0.5f, 0.5f, j + 1f), Quaternion.Euler(0f, 90f, 0f));
                    rightDoor.transform.parent = stageParent;
                }
                // 上の壁
                if (map[i, j].wallUpper == CellInfo.WALL_TYPE.WALL)
                {
                    GameObject upperWall = Instantiate(wallObj, new Vector3(i, 0.5f, j + 0.5f), Quaternion.Euler(0f, 0f, 0f));
                    upperWall.transform.parent = stageParent;
                }
                else if (map[i, j].wallUpper == CellInfo.WALL_TYPE.DOOR)
                {
                    GameObject upperDoor = Instantiate(doorObj, new Vector3(i, 0.5f, j + 0.5f), Quaternion.Euler(0f, 0f, 0f));
                    upperDoor.transform.parent = stageParent;
                }
                // 下の壁
                if (map[i, j].wallLower == CellInfo.WALL_TYPE.WALL)
                {
                    GameObject lowerWall = Instantiate(wallObj, new Vector3(i + 1f, 0.5f, j + 0.5f), Quaternion.Euler(0f, 0f, 0f));
                    lowerWall.transform.parent = stageParent;
                }
                else if (map[i, j].wallLower == CellInfo.WALL_TYPE.DOOR)
                {
                    GameObject lowerDoor = Instantiate(doorObj, new Vector3(i + 1f, 0.5f, j + 0.5f), Quaternion.Euler(0f, 0f, 0f));
                    lowerDoor.transform.parent = stageParent;
                }

                // 宝箱のスポーン位置
                if (map[i, j].spawnChest)
                {
                    GameObject chest = Instantiate(chestObj, new Vector3(i + 0.5f, 0.2f, j + 0.5f), Quaternion.identity);
                    chest.transform.parent = stageParent;
                }

                // プレイヤーのスポーン位置
                if (map[i, j].spawnPlayer)
                {
                    GameObject respawn = Instantiate(respawnObj, new Vector3(i + 0.5f, 0.4f, j + 0.5f), Quaternion.identity);
                    respawn.transform.parent = stageParent;

                    // Playerがスポーンする位置を保存
                    playerSpawnPosition = respawn.transform.position;
                }
            }
        }

        // Playerをスポーン位置に生成
        if (playerSpawnPosition != Vector3.zero && !IsNetwork)
        {
            Instantiate(Player, playerSpawnPosition, Quaternion.identity);
        }
    }

    //ランダム抽選用
    TextAsset[] SelectCSVFileRandomly(int size)
    {
        //返却用
        TextAsset[] ret = new TextAsset[size];

        //LoadAllでResourcesフォルダ下のすべてのCSVファイルを格納する
        TextAsset[] csvMap_Array = Resources.LoadAll("", typeof(TextAsset)).Cast<TextAsset>().ToArray();

        //↑のすべてのcsvファイルをリストに格納する
        List<TextAsset> csvMap_List = new List<TextAsset>();
        csvMap_List.AddRange(csvMap_Array);

        Debug.Log("リストに" + csvMap_List.Count + "個のテキストアセットを格納");

        //重複なしでTextAssetを4つ取り出して配列に格納する
        for (int i = 0; i < size; i++)
        {
            int index = UnityEngine.Random.Range(0, csvMap_List.Count);
            ret[i] = csvMap_List[index];
            csvMap_List.RemoveAt(index);
        }

        //返却
        return ret;
    }

    CellInfo[,] MergeMap(TextAsset[] textAsset_Array)
    {
        //返却用
        CellInfo[,] ret = new CellInfo[MAP_SIZE, MAP_SIZE];

        for (int index = 0; index < NUM_OF_PARTS; index++)
        {
            CellInfo[,] mapPart = ConvertTextAssetToCellInfo2DArray(textAsset_Array[index]);

            //map側の配列はポインタをかなり気持ち悪く走らせるが、mapPart側は一般的に走らせるので一般的なループを使う。そのための変数
            int partRow = 0;
            int partColumn = 0;

            switch (index)
            {
                //左上
                case 0:
                    //そのまま左上にコピー。rowは常にiと等しく、columnは常にjと等しい。
                    for (int row = 0; row < MAP_PART_SIZE; row++)
                    {
                        for (int column = 0; column < MAP_PART_SIZE; column++)
                        {
                            //Debug.Log(row + "," + row);
                            //Debug.Log(partRow + "," + partColumn);

                            ret[row, column] = mapPart[partRow, partColumn];
                            partColumn++;
                        }
                        partRow++;
                        partColumn = 0;
                    }
                    break;
                //右上
                case 1:
                    //時計回りに90度回しながら右上にコピー。ポインタは右上から真下→左の列へ走らせる
                    for (int column = MAP_SIZE - 1; column >= MAP_SIZE - MAP_PART_SIZE; column--)
                    {
                        for (int row = 0; row < MAP_PART_SIZE; row++)
                        {
                            //Debug.Log(row + "," + row);
                            //Debug.Log(partRow + "," + partColumn);

                            //mapPartのセルを回転させてからはめていく
                            ret[row, column] = RotateCell(mapPart[partRow, partColumn], index);
                            partColumn++;
                        }
                        partRow++;
                        partColumn = 0;
                    }
                    break;
                //右下
                case 2:
                    //時計回りに180度回しながら右下にコピー。ポインタは右下から左→上の行へ走らせる
                    for (int row = MAP_SIZE - 1; row >= MAP_SIZE - MAP_PART_SIZE; row--)
                    {
                        for (int column = MAP_SIZE - 1; column >= MAP_SIZE - MAP_PART_SIZE; column--)
                        {
                            //Debug.Log(row + "," + row);
                            //Debug.Log(partRow + "," + partColumn);

                            //mapPartのセルを回転させてからはめていく
                            ret[row, column] = RotateCell(mapPart[partRow, partColumn], index);
                            partColumn++;
                        }
                        partRow++;
                        partColumn = 0;
                    }
                    break;
                case 3:
                    //時計回りに270度回しながら左下にコピー。ポインタは左下から上→右の列へ走らせる
                    for (int column = 0; column < MAP_PART_SIZE; column++)
                    {
                        for (int row = MAP_SIZE - 1; row >= MAP_SIZE - MAP_PART_SIZE; row--)
                        {
                            //Debug.Log(row + "," + row);
                            //Debug.Log(partRow + "," + partColumn);

                            //mapPartのセルを回転させてからはめていく
                            ret[row, column] = RotateCell(mapPart[partRow, partColumn], index);
                            partColumn++;
                        }
                        partRow++;
                        partColumn = 0;
                    }
                    break;
                default:
                    break;
            }
        }

        CellInfo[,] ConvertTextAssetToCellInfo2DArray(TextAsset textAsset)
        {
            //返却用
            CellInfo[,] ret = new CellInfo[MAP_PART_SIZE, MAP_PART_SIZE];

            //TextAssetの情報を格納する配列＊リスト
            //行のデータはString.Split()で取り出すためstring[]型である必要があり、それをListにAdd()して列を作りたいのでリスト型を使う
            List<string[]> sheetData_List = new List<string[]>();

            //取得したTextAssetのtextをStringreaderに変換
            StringReader sheetReader = new StringReader(textAsset.text);

            //Peekはインデックスの次にある文字を返す。何もない場合-1
            //-1になる、つまり文字列の最後までインデックスが移動するまでwhileで繰り返す
            while (sheetReader.Peek() != -1)
            {
                //インデックスの現在地から改行文字が来るまでの文字列を取り出し（改行文字は無視）
                //改行文字の次の位置にインデックスを移動させる
                string str = sheetReader.ReadLine();
                //取り出した文字列をカンマで区切りながらstring[]型の配列に格納する
                sheetData_List.Add(str.Split(','));
            }

            //Debug.Log("リストに" + sheetData_List.Count + "列のデータを書き込み");

            for (int i = 0; i < MAP_PART_SIZE; i++)
            {
                for (int j = 0; j < MAP_PART_SIZE; j++)
                {
                    //Debug.Log(i + "," + j);

                    //あとは各要素をstringReaderで読んで、インデックスを進めながらcellInfoを作っていく
                    //セル内の文字を格納
                    StringReader cellReader = new StringReader(sheetData_List[i][j]);

                    CellInfo cellInfo = new CellInfo();

                    //頭文字をスキップ
                    cellReader.Read();

                    //1文字目から順に読んで変数に代入
                    cellInfo.wallLeft = (CellInfo.WALL_TYPE)(ConvertASCIIToInt(cellReader.Read()));
                    cellInfo.wallRight = (CellInfo.WALL_TYPE)(ConvertASCIIToInt(cellReader.Read()));
                    cellInfo.wallUpper = (CellInfo.WALL_TYPE)(ConvertASCIIToInt(cellReader.Read()));
                    cellInfo.wallLower = (CellInfo.WALL_TYPE)(ConvertASCIIToInt(cellReader.Read()));

                    cellInfo.cellType = (CellInfo.CELL_TYPE)(ConvertASCIIToInt(cellReader.Read()));

                    cellInfo.spawnChest = Convert.ToBoolean(ConvertASCIIToInt(cellReader.Read()));
                    cellInfo.spawnPlayer = Convert.ToBoolean(ConvertASCIIToInt(cellReader.Read()));

                    //返却用配列に書き込む
                    ret[i, j] = cellInfo;

                    //Debug.Log(i + "," + j);
                }
            }


            //返却
            return ret;
        }

        int ConvertASCIIToInt(int charCode)
        {
            int ret = 0;

            char character = (char)charCode;

            //文字コードで0から9の間にあるなら
            if (character >= '0' && character <= '9')
            {
                //0の文字コードを引くことで対応する整数に変換
                ret = character - '0';
            }
            else
            {
                ret = -1;
            }

            return ret;
        }

        //セル１個の壁データを90度間隔で回したものを新インスタンスで返却する関数。元データは書き換えない。
        //絶対にここでしか使わないのでローカル関数にした。単に行数を減らす目的なのと、indexを直接使いたかったのもある。
        CellInfo RotateCell(CellInfo cell, int index)
        {
            //返却用にまず複製
            CellInfo ret = cell;

            //壁のデータを複製
            CellInfo.WALL_TYPE[] wallCopy = new CellInfo.WALL_TYPE[4];
            //データをLeftから反時計回りに配列に格納
            wallCopy[0] = cell.wallLeft;
            wallCopy[1] = cell.wallLower;
            wallCopy[2] = cell.wallRight;
            wallCopy[3] = cell.wallUpper;
            //時計回りに90*index度ズレるようにデータを取り出す
            ret.wallLeft = wallCopy[(0 + index) % 4];
            ret.wallLower = wallCopy[(1 + index) % 4];
            ret.wallRight = wallCopy[(2 + index) % 4];
            ret.wallUpper = wallCopy[(3 + index) % 4];

            //書き換えが終わったら返却する
            return ret;
        }

        return ret;
    }

    //左上のセルから右下のセルにかけて、左側の壁と下側の壁が重複して生成されないようデータを編集する
    void DeleteDuplicatedWall(CellInfo[,] map)
    {
        //右の壁と重複する壁を削除
        for (int i = 0; i < MAP_SIZE; i++)
        {
            for (int j = 0; j < MAP_SIZE - 1; j++)
            {
                if (map[i, j].wallRight != CellInfo.WALL_TYPE.NONE)
                {
                    map[i, j + 1].wallLeft = CellInfo.WALL_TYPE.NONE;
                }
            }
        }

        //下の壁と重複する壁を削除
        for (int i = 0; i < MAP_SIZE - 1; i++)
        {
            for (int j = 0; j < MAP_SIZE; j++)
            {
                if (map[i, j].wallLower != CellInfo.WALL_TYPE.NONE)
                {
                    map[i + 1, j].wallUpper = CellInfo.WALL_TYPE.NONE;
                }
            }
        }
    }

#if UNITY_EDITOR
    //エディター限定の自動デバッグ
    private void OnValidate()
    {
        if (MAP_SIZE % 2 == 0) Debug.LogError("MAP_SIZEが偶数で草");
    }
#endif
}