using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class MapInfo : MonoBehaviour
{
    public bool IsNetwork = false;//true = Server/false = Client

    // �e�I�u�W�F�N�g
    [SerializeField] Transform stageParent;

    //��������I�u�W�F�N�g
    [SerializeField] GameObject floorObj;
    [SerializeField] GameObject wallObj;
    [SerializeField] GameObject doorObj;
    [SerializeField] GameObject chestObj;
    [SerializeField] GameObject respawnObj;
    [SerializeField] GameObject Player;

    //�g�ݍ��킹��}�b�v�p�[�c�̐��B4�B
    const int NUM_OF_PARTS = 4;

    //�}�b�v�̈�ӂ̃}�X��
    const int MAP_SIZE = 19;
    //�}�b�v�p�[�c�̈�ӂ̃}�X��
    const int MAP_PART_SIZE = (MAP_SIZE - 1) / 2;

    //�Z��19*19���W�߂��z��Ń}�b�v�����
    public CellInfo[,] map;

    private void Start()
    {
        //�����_���ɑI��csv�t�@�C������19*19��cellInfo2�����z����쐬����
        //map = MergeMap(SelectCSVFileRandomly(NUM_OF_PARTS));

        //�����_�����I�͕ۗ��B�w�肵�����O�̃t�@�C�����S�ǂݍ���
        TextAsset[] textAsset_array = {
            Resources.Load("MapPart06") as TextAsset,
            Resources.Load("MapPart07") as TextAsset,
            Resources.Load("MapPart09") as TextAsset,
            Resources.Load("MapPart14") as TextAsset};

        map = MergeMap(textAsset_array);

        // Player�����������ʒu���i�[����ϐ�
        Vector3 playerSpawnPosition = Vector3.zero;

        //TODO
        //�L���𐶐�()
        //��̘L��
        for (int row = 0; row < MAP_SIZE; row++)
        {
            map[row, MAP_PART_SIZE] = new CellInfo();
        }
        //���̘L��
        for (int column = 0; column < MAP_SIZE; column++)
        {
            map[MAP_PART_SIZE, column] = new CellInfo();
        }

        //�}�b�v�����ɍL�ꐶ��()
        map[8, 8] = new CellInfo();
        map[8, 10] = new CellInfo();
        map[10, 8] = new CellInfo();
        map[10, 10] = new CellInfo();

        //�d�������ǃf�[�^�̍폜
        DeleteDuplicatedWall(map);

        for (int i = 0; i < MAP_SIZE; i++)
        {
            for (int j = 0; j < MAP_SIZE; j++)
            {
                // ��
                if (map[i, j].cellType != CellInfo.CELL_TYPE.NONE)
                {
                    GameObject floor = Instantiate(floorObj, new Vector3(i + 0.5f, 0, j + 0.5f), Quaternion.identity);
                    floor.transform.parent = stageParent;
                }
                // �V��
                if (map[i, j].cellType != CellInfo.CELL_TYPE.NONE)
                {
                    GameObject ceiling = Instantiate(floorObj, new Vector3(i + 0.5f, 1f, j + 0.5f), Quaternion.Euler(180f, 0f, 0f));
                    ceiling.transform.parent = stageParent;
                }
                // ���̕�
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
                // �E�̕�
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
                // ��̕�
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
                // ���̕�
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

                // �󔠂̃X�|�[���ʒu
                if (map[i, j].spawnChest)
                {
                    GameObject chest = Instantiate(chestObj, new Vector3(i + 0.5f, 0.2f, j + 0.5f), Quaternion.identity);
                    chest.transform.parent = stageParent;
                }

                // �v���C���[�̃X�|�[���ʒu
                if (map[i, j].spawnPlayer)
                {
                    GameObject respawn = Instantiate(respawnObj, new Vector3(i + 0.5f, 0.4f, j + 0.5f), Quaternion.identity);
                    respawn.transform.parent = stageParent;

                    // Player���X�|�[������ʒu��ۑ�
                    playerSpawnPosition = respawn.transform.position;
                }
            }
        }

        // Player���X�|�[���ʒu�ɐ���
        if (playerSpawnPosition != Vector3.zero && !IsNetwork)
        {
            Instantiate(Player, playerSpawnPosition, Quaternion.identity);
        }
    }

    //�����_�����I�p
    TextAsset[] SelectCSVFileRandomly(int size)
    {
        //�ԋp�p
        TextAsset[] ret = new TextAsset[size];

        //LoadAll��Resources�t�H���_���̂��ׂĂ�CSV�t�@�C�����i�[����
        TextAsset[] csvMap_Array = Resources.LoadAll("", typeof(TextAsset)).Cast<TextAsset>().ToArray();

        //���̂��ׂĂ�csv�t�@�C�������X�g�Ɋi�[����
        List<TextAsset> csvMap_List = new List<TextAsset>();
        csvMap_List.AddRange(csvMap_Array);

        Debug.Log("���X�g��" + csvMap_List.Count + "�̃e�L�X�g�A�Z�b�g���i�[");

        //�d���Ȃ���TextAsset��4���o���Ĕz��Ɋi�[����
        for (int i = 0; i < size; i++)
        {
            int index = UnityEngine.Random.Range(0, csvMap_List.Count);
            ret[i] = csvMap_List[index];
            csvMap_List.RemoveAt(index);
        }

        //�ԋp
        return ret;
    }

    CellInfo[,] MergeMap(TextAsset[] textAsset_Array)
    {
        //�ԋp�p
        CellInfo[,] ret = new CellInfo[MAP_SIZE, MAP_SIZE];

        for (int index = 0; index < NUM_OF_PARTS; index++)
        {
            CellInfo[,] mapPart = ConvertTextAssetToCellInfo2DArray(textAsset_Array[index]);

            //map���̔z��̓|�C���^�����Ȃ�C�����������点�邪�AmapPart���͈�ʓI�ɑ��点��̂ň�ʓI�ȃ��[�v���g���B���̂��߂̕ϐ�
            int partRow = 0;
            int partColumn = 0;

            switch (index)
            {
                //����
                case 0:
                    //���̂܂܍���ɃR�s�[�Brow�͏��i�Ɠ������Acolumn�͏��j�Ɠ������B
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
                //�E��
                case 1:
                    //���v����90�x�񂵂Ȃ���E��ɃR�s�[�B�|�C���^�͉E�ォ��^�������̗�֑��点��
                    for (int column = MAP_SIZE - 1; column >= MAP_SIZE - MAP_PART_SIZE; column--)
                    {
                        for (int row = 0; row < MAP_PART_SIZE; row++)
                        {
                            //Debug.Log(row + "," + row);
                            //Debug.Log(partRow + "," + partColumn);

                            //mapPart�̃Z������]�����Ă���͂߂Ă���
                            ret[row, column] = RotateCell(mapPart[partRow, partColumn], index);
                            partColumn++;
                        }
                        partRow++;
                        partColumn = 0;
                    }
                    break;
                //�E��
                case 2:
                    //���v����180�x�񂵂Ȃ���E���ɃR�s�[�B�|�C���^�͉E�����獶����̍s�֑��点��
                    for (int row = MAP_SIZE - 1; row >= MAP_SIZE - MAP_PART_SIZE; row--)
                    {
                        for (int column = MAP_SIZE - 1; column >= MAP_SIZE - MAP_PART_SIZE; column--)
                        {
                            //Debug.Log(row + "," + row);
                            //Debug.Log(partRow + "," + partColumn);

                            //mapPart�̃Z������]�����Ă���͂߂Ă���
                            ret[row, column] = RotateCell(mapPart[partRow, partColumn], index);
                            partColumn++;
                        }
                        partRow++;
                        partColumn = 0;
                    }
                    break;
                case 3:
                    //���v����270�x�񂵂Ȃ��獶���ɃR�s�[�B�|�C���^�͍�������と�E�̗�֑��点��
                    for (int column = 0; column < MAP_PART_SIZE; column++)
                    {
                        for (int row = MAP_SIZE - 1; row >= MAP_SIZE - MAP_PART_SIZE; row--)
                        {
                            //Debug.Log(row + "," + row);
                            //Debug.Log(partRow + "," + partColumn);

                            //mapPart�̃Z������]�����Ă���͂߂Ă���
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
            //�ԋp�p
            CellInfo[,] ret = new CellInfo[MAP_PART_SIZE, MAP_PART_SIZE];

            //TextAsset�̏����i�[����z�񁖃��X�g
            //�s�̃f�[�^��String.Split()�Ŏ��o������string[]�^�ł���K�v������A�����List��Add()���ė����肽���̂Ń��X�g�^���g��
            List<string[]> sheetData_List = new List<string[]>();

            //�擾����TextAsset��text��Stringreader�ɕϊ�
            StringReader sheetReader = new StringReader(textAsset.text);

            //Peek�̓C���f�b�N�X�̎��ɂ��镶����Ԃ��B�����Ȃ��ꍇ-1
            //-1�ɂȂ�A�܂蕶����̍Ō�܂ŃC���f�b�N�X���ړ�����܂�while�ŌJ��Ԃ�
            while (sheetReader.Peek() != -1)
            {
                //�C���f�b�N�X�̌��ݒn������s����������܂ł̕���������o���i���s�����͖����j
                //���s�����̎��̈ʒu�ɃC���f�b�N�X���ړ�������
                string str = sheetReader.ReadLine();
                //���o������������J���}�ŋ�؂�Ȃ���string[]�^�̔z��Ɋi�[����
                sheetData_List.Add(str.Split(','));
            }

            //Debug.Log("���X�g��" + sheetData_List.Count + "��̃f�[�^����������");

            for (int i = 0; i < MAP_PART_SIZE; i++)
            {
                for (int j = 0; j < MAP_PART_SIZE; j++)
                {
                    //Debug.Log(i + "," + j);

                    //���Ƃ͊e�v�f��stringReader�œǂ�ŁA�C���f�b�N�X��i�߂Ȃ���cellInfo������Ă���
                    //�Z�����̕������i�[
                    StringReader cellReader = new StringReader(sheetData_List[i][j]);

                    CellInfo cellInfo = new CellInfo();

                    //���������X�L�b�v
                    cellReader.Read();

                    //1�����ڂ��珇�ɓǂ�ŕϐ��ɑ��
                    cellInfo.wallLeft = (CellInfo.WALL_TYPE)(ConvertASCIIToInt(cellReader.Read()));
                    cellInfo.wallRight = (CellInfo.WALL_TYPE)(ConvertASCIIToInt(cellReader.Read()));
                    cellInfo.wallUpper = (CellInfo.WALL_TYPE)(ConvertASCIIToInt(cellReader.Read()));
                    cellInfo.wallLower = (CellInfo.WALL_TYPE)(ConvertASCIIToInt(cellReader.Read()));

                    cellInfo.cellType = (CellInfo.CELL_TYPE)(ConvertASCIIToInt(cellReader.Read()));

                    cellInfo.spawnChest = Convert.ToBoolean(ConvertASCIIToInt(cellReader.Read()));
                    cellInfo.spawnPlayer = Convert.ToBoolean(ConvertASCIIToInt(cellReader.Read()));

                    //�ԋp�p�z��ɏ�������
                    ret[i, j] = cellInfo;

                    //Debug.Log(i + "," + j);
                }
            }


            //�ԋp
            return ret;
        }

        int ConvertASCIIToInt(int charCode)
        {
            int ret = 0;

            char character = (char)charCode;

            //�����R�[�h��0����9�̊Ԃɂ���Ȃ�
            if (character >= '0' && character <= '9')
            {
                //0�̕����R�[�h���������ƂőΉ����鐮���ɕϊ�
                ret = character - '0';
            }
            else
            {
                ret = -1;
            }

            return ret;
        }

        //�Z���P�̕ǃf�[�^��90�x�Ԋu�ŉ񂵂����̂�V�C���X�^���X�ŕԋp����֐��B���f�[�^�͏��������Ȃ��B
        //��΂ɂ����ł����g��Ȃ��̂Ń��[�J���֐��ɂ����B�P�ɍs�������炷�ړI�Ȃ̂ƁAindex�𒼐ڎg�����������̂�����B
        CellInfo RotateCell(CellInfo cell, int index)
        {
            //�ԋp�p�ɂ܂�����
            CellInfo ret = cell;

            //�ǂ̃f�[�^�𕡐�
            CellInfo.WALL_TYPE[] wallCopy = new CellInfo.WALL_TYPE[4];
            //�f�[�^��Left���甽���v���ɔz��Ɋi�[
            wallCopy[0] = cell.wallLeft;
            wallCopy[1] = cell.wallLower;
            wallCopy[2] = cell.wallRight;
            wallCopy[3] = cell.wallUpper;
            //���v����90*index�x�Y����悤�Ƀf�[�^�����o��
            ret.wallLeft = wallCopy[(0 + index) % 4];
            ret.wallLower = wallCopy[(1 + index) % 4];
            ret.wallRight = wallCopy[(2 + index) % 4];
            ret.wallUpper = wallCopy[(3 + index) % 4];

            //�����������I�������ԋp����
            return ret;
        }

        return ret;
    }

    //����̃Z������E���̃Z���ɂ����āA�����̕ǂƉ����̕ǂ��d�����Đ�������Ȃ��悤�f�[�^��ҏW����
    void DeleteDuplicatedWall(CellInfo[,] map)
    {
        //�E�̕ǂƏd������ǂ��폜
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

        //���̕ǂƏd������ǂ��폜
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
    //�G�f�B�^�[����̎����f�o�b�O
    private void OnValidate()
    {
        if (MAP_SIZE % 2 == 0) Debug.LogError("MAP_SIZE�������ő�");
    }
#endif
}