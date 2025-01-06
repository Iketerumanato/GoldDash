using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using R3;

public class MapGenerator : MonoBehaviour
{
    //定数
    private const int NUM_OF_PARTS = 4; //組み合わせるマップパーツの数。4つ。
    private const int MAP_SIZE = 13; //マップの一辺のマス数。偶数にしないでください。自動デバッグしようとしたらunreachable code警告が消えない:(
    private const int MAP_PART_SIZE = (MAP_SIZE - 1) / 2; //マップパーツの一辺のマス数

    //ヒエラルキーが見やすいよう、マップのオブジェクトはこの親オブジェクトの子にする
    [SerializeField] Transform Parenttransform;

    //生成に使用するパーツ類
    [SerializeField] GameObject floorObj;
    [SerializeField] GameObject ceilingObj;
    [SerializeField] GameObject wallObj;
    [SerializeField] GameObject doorObj;
    [SerializeField] GameObject chestObj;
    [SerializeField] GameObject respawnObj;
    [SerializeField] GameObject Player;

    //MapGeneratorはシングルトンにする
    public static MapGenerator instance;

    //セル19*19個を集めた配列でマップを作る
    //どこからでも情報を読み取れるように静的にする
    public static CellInfo[,] map;

    //正方形の中心を通り、もとの正方形を4つの正方形に区分するような2本の直線を引き、それらを2次元の座標軸とみなしたときの4つの象限に存在しているリスポーン地点のリスト。宣言順が気持ち悪いが、マップ生成処理等では第2,第1,第4,第3象限の順に処理しているので統一した。
    private List<Vector3> respawnPointsInQuadrant2;
    private List<Vector3> respawnPointsInQuadrant1;
    private List<Vector3> respawnPointsInQuadrant4;
    private List<Vector3> respawnPointsInQuadrant3;
    private List<Vector3> allRespawnPoints; //全象限のリスポーン地点

    //宝箱について同じもの
    private List<Vector3> chestPointsOrigin; //全象限の宝箱出現地点
    private List<Vector3> chestPointsDeck; //↑の全ての要素をランダムな順で並べ替えたリスト。文字通り山札

    public void InitObservation(GameServerManager gameServerManager, GameClientManager gameClientManager)
    {
        gameServerManager.ServerInternalSubject.Subscribe(e => ProcessServerInternalEvent(e));
        gameClientManager.ClientInternalSubject.Subscribe(e => ProcessClientInternalEvent(e));
    }

    private void ProcessServerInternalEvent(GameServerManager.SERVER_INTERNAL_EVENT e)
    {
        switch (e)
        {
            case GameServerManager.SERVER_INTERNAL_EVENT.GENERATE_MAP:
                GenerateMap();
                break;
            default:
                break;
        }
    }

    private void ProcessClientInternalEvent(GameClientManager.CLIENT_INTERNAL_EVENT e)
    {
        switch (e)
        {
            case GameClientManager.CLIENT_INTERNAL_EVENT.GENERATE_MAP:
                GenerateMap();
                break;
            default:
                break;
        }
    }

    private void Start()
    {
        //シングルトンな静的変数の初期化
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        //コレクションのインスタンス生成
        respawnPointsInQuadrant2 = new List<Vector3>();
        respawnPointsInQuadrant1 = new List<Vector3>();
        respawnPointsInQuadrant4 = new List<Vector3>();
        respawnPointsInQuadrant3 = new List<Vector3>();
        allRespawnPoints = new List<Vector3>();
        chestPointsOrigin = new List<Vector3>();
        chestPointsDeck = new List<Vector3>();
    }

    private void GenerateMap()
    {
        //ランダムに選んだcsvファイルから19*19のcellInfo2次元配列を作成する

        //ランダム抽選は保留。指定した名前のファイルを４つ読み込む
        TextAsset[] textAsset_array = {
            Resources.Load("MapPart6x6_1") as TextAsset,
            Resources.Load("MapPart6x6_2") as TextAsset,
            Resources.Load("MapPart6x6_3") as TextAsset,
            Resources.Load("MapPart6x6_4") as TextAsset};

        map = MergeMap(textAsset_array);

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
                    floor.transform.parent = Parenttransform;
                }
                // 天井
                if (map[i, j].cellType != CellInfo.CELL_TYPE.NONE)
                {
                    GameObject ceiling = Instantiate(ceilingObj, new Vector3(i + 0.5f, 1f, j + 0.5f), Quaternion.identity);
                    ceiling.transform.parent = Parenttransform;
                }
                // 左の壁
                if (map[i, j].wallLeft == CellInfo.WALL_TYPE.WALL)
                {
                    GameObject leftWall = Instantiate(wallObj, new Vector3(i + 0.5f, 0f, j), Quaternion.identity);
                    leftWall.transform.parent = Parenttransform;
                }
                else if (map[i, j].wallLeft == CellInfo.WALL_TYPE.DOOR)
                {
                    GameObject leftDoor = Instantiate(doorObj, new Vector3(i + 0.5f, 0f, j), Quaternion.identity);
                    leftDoor.transform.parent = Parenttransform;
                }
                // 右の壁
                if (map[i, j].wallRight == CellInfo.WALL_TYPE.WALL)
                {
                    GameObject rightWall = Instantiate(wallObj, new Vector3(i + 0.5f, 0f, j + 1f), Quaternion.identity);
                    rightWall.transform.parent = Parenttransform;
                }
                else if (map[i, j].wallRight == CellInfo.WALL_TYPE.DOOR)
                {
                    GameObject rightDoor = Instantiate(doorObj, new Vector3(i + 0.5f, 0f, j + 1f), Quaternion.identity);
                    rightDoor.transform.parent = Parenttransform;
                }
                // 上の壁
                if (map[i, j].wallUpper == CellInfo.WALL_TYPE.WALL)
                {
                    GameObject upperWall = Instantiate(wallObj, new Vector3(i, 0f, j + 0.5f), Quaternion.Euler(0f, 90f, 0f));
                    upperWall.transform.parent = Parenttransform;
                }
                else if (map[i, j].wallUpper == CellInfo.WALL_TYPE.DOOR)
                {
                    GameObject upperDoor = Instantiate(doorObj, new Vector3(i, 0f, j + 0.5f), Quaternion.Euler(0f, 90f, 0f));
                    upperDoor.transform.parent = Parenttransform;
                }
                // 下の壁
                if (map[i, j].wallLower == CellInfo.WALL_TYPE.WALL)
                {
                    GameObject lowerWall = Instantiate(wallObj, new Vector3(i + 1f, 0f, j + 0.5f), Quaternion.Euler(0f, 90f, 0f));
                    lowerWall.transform.parent = Parenttransform;
                }
                else if (map[i, j].wallLower == CellInfo.WALL_TYPE.DOOR)
                {
                    GameObject lowerDoor = Instantiate(doorObj, new Vector3(i + 1f, 0f, j + 0.5f), Quaternion.Euler(0f, 90f, 0f));
                    lowerDoor.transform.parent = Parenttransform;
                }

                // 宝箱のスポーン位置
                if (map[i, j].spawnChest)
                {
                    //第2または第1象限なら
                    if (i < MAP_PART_SIZE)
                    {
                        if (j < MAP_PART_SIZE) //第2象限なら
                        {
                            chestPointsOrigin.Add(new Vector3(i + 0.5f, 0f, j + 0.5f));
                        }
                        else //第1象限なら
                        {
                            chestPointsOrigin.Add(new Vector3(i + 0.5f, 0f, j + 0.5f));
                        }
                    }
                    else //第3または第4象限なら
                    {
                        if (j < MAP_PART_SIZE) //第3象限なら
                        {
                            chestPointsOrigin.Add(new Vector3(i + 0.5f, 0f, j + 0.5f));
                        }
                        else //第4象限なら
                        {
                            chestPointsOrigin.Add(new Vector3(i + 0.5f, 0f, j + 0.5f));
                        }
                    }
                }

                // プレイヤーのスポーン位置をリストに追加
                if (map[i, j].spawnPlayer)
                {
                    //第2または第1象限なら
                    if (i < MAP_PART_SIZE)
                    {
                        if (j < MAP_PART_SIZE) //第2象限なら
                        {
                            respawnPointsInQuadrant2.Add(new Vector3(i + 0.5f, 0.4f, j + 0.5f));
                            allRespawnPoints.Add(new Vector3(i + 0.5f, 0.4f, j + 0.5f));
                        }
                        else //第1象限なら
                        {
                            respawnPointsInQuadrant1.Add(new Vector3(i + 0.5f, 0.4f, j + 0.5f));
                            allRespawnPoints.Add(new Vector3(i + 0.5f, 0.4f, j + 0.5f));
                        }
                    }
                    else //第3または第4象限なら
                    {
                        if (j < MAP_PART_SIZE) //第3象限なら
                        {
                            respawnPointsInQuadrant3.Add(new Vector3(i + 0.5f, 0.4f, j + 0.5f));
                            allRespawnPoints.Add(new Vector3(i + 0.5f, 0.4f, j + 0.5f));
                        }
                        else //第4象限なら
                        {
                            respawnPointsInQuadrant4.Add(new Vector3(i + 0.5f, 0.4f, j + 0.5f));
                            allRespawnPoints.Add(new Vector3(i + 0.5f, 0.4f, j + 0.5f));
                        }
                    }
                }
            }
        }
    }

    ////ランダム抽選用
    //TextAsset[] SelectCSVFileRandomly(int size)
    //{
    //    //返却用
    //    TextAsset[] ret = new TextAsset[size];

    //    //LoadAllでResourcesフォルダ下のすべてのCSVファイルを格納する
    //    TextAsset[] csvMap_Array = Resources.LoadAll("", typeof(TextAsset)).Cast<TextAsset>().ToArray();

    //    //↑のすべてのcsvファイルをリストに格納する
    //    List<TextAsset> csvMap_List = new List<TextAsset>();
    //    csvMap_List.AddRange(csvMap_Array);

    //    Debug.Log("リストに" + csvMap_List.Count + "個のテキストアセットを格納");

    //    //重複なしでTextAssetを4つ取り出して配列に格納する
    //    for (int i = 0; i < size; i++)
    //    {
    //        int index = UnityEngine.Random.Range(0, csvMap_List.Count);
    //        ret[i] = csvMap_List[index];
    //        csvMap_List.RemoveAt(index);
    //    }

    //    //返却
    //    return ret;
    //}

    //4つのテキストアセットをマージしてマップデータを作成
    private CellInfo[,] MergeMap(TextAsset[] textAsset_Array)
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
    private void DeleteDuplicatedWall(CellInfo[,] map)
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

    //ランダムなリスポーン地点を4個選んで返す。4つのリスポーン地点は、X軸とZ軸によって仕切られた4つの象限からひとつずつ選ばれる
    public Vector3[] Get4RespawnPointsRandomly()
    {
        Vector3[] ret = new Vector3[4];

        System.Random random = new System.Random(); //4回乱数を生成するが、1ミリ秒以内にrandomインスタンスを生成すると同じ値が出てしまうので一つのインスタンスを使いまわす
        //参考： https://qiita.com/waokitsune/items/068be8e71cea59e0a703
        //参考２： https://qiita.com/neko_the_shadow/items/72f0285324100a596979

        ret[0] = respawnPointsInQuadrant2[random.Next(0, respawnPointsInQuadrant2.Count)]; //これはサブスレッドで実行される可能性があるのでSystemの乱数を使用
        ret[1] = respawnPointsInQuadrant1[random.Next(0, respawnPointsInQuadrant1.Count)];
        ret[2] = respawnPointsInQuadrant4[random.Next(0, respawnPointsInQuadrant4.Count)];
        ret[3] = respawnPointsInQuadrant3[random.Next(0, respawnPointsInQuadrant3.Count)];

        return ret;
    }

    public Vector3 GetUniqueChestPointRandomly()
    {
        //抽選用デッキが空なら出現位置候補データを参照する
        if (chestPointsDeck.Count == 0)
        {
            //宝箱の出現地点候補を一つずつコピーする
            for (int i = 0; i < chestPointsOrigin.Count; i++)
            {
                chestPointsDeck.Add(chestPointsOrigin[i]);
            }
            //このままだとマップ生成時の順番に従っているので、フィッシャー・イェーツ法でシャッフルする
            System.Random rand = new System.Random(); //これはサブスレッドで実行される可能性があるのでSystemの乱数を使用
            for (int i = chestPointsDeck.Count - 1; i > 0; i--)
            {
                int swapIndex = rand.Next(0, i + 1);
                Vector3 tmp = chestPointsDeck[i];
                chestPointsDeck[i] = chestPointsDeck[swapIndex];
                chestPointsDeck[swapIndex] = tmp;
            }
        }

        Vector3 ret = chestPointsDeck[0]; //リストの0番目要素を返却用変数に格納する
        chestPointsDeck.RemoveAt(0); //0番目要素をリストから消す。1番目以降のすべての要素はインデックスが繰り下がる
        return ret; //返却
    }

    public void AddChestPointToDeck(Vector3 chestPos)
    {
        chestPointsDeck.Add(chestPos);
    }

    public Vector3 GetRespawnPointRandomly()
    {
        return allRespawnPoints[new System.Random().Next(0, allRespawnPoints.Count)]; //これはサブスレッドで実行される可能性があるのでSystemの乱数を使用
    }
}