using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    [Header("通常時の移動速度（マス／毎秒）")]
    [SerializeField] private float m_playerMoveSpeed = 1f;

    [Header("特定のStateで移動速度に倍率をかけたい場合、ここから設定しておく")]
    [SerializeField] private List<MoveSpeedMagnification> m_moveSpeedMagnificationsList;

    //KeyValuePairはインスペクタで表示できない（Serializableでない）ので自前のペアを作って使う
    [Serializable]
    private class MoveSpeedMagnification
    {
        public PlayerState playerState;
        public float magnification;
    }

    //プレイ中の倍率計算はDictionaryで高速に行いたい
    private Dictionary<int, float> m_moveSpeedMagnificationsDictionary; //ここ構造体を使うとキャストの手間がかかるのでkeyはintにする

    private void Start()
    {
        //インスペクタで設定した値をDictionaryに登録する
        m_moveSpeedMagnificationsDictionary = new Dictionary<int, float>();
        foreach (MoveSpeedMagnification m in m_moveSpeedMagnificationsList)
        {
            m_moveSpeedMagnificationsDictionary.Add((int)m.playerState, m.magnification); //インスペクタで構造体だったものをintにキャストして辞書に登録
        }
    }

    public void MovePlayer(PlayerState currentState, float V_InputHorizontal, float V_InputVertical, float D_InputHorizontal)
    { 
        //TODO 移動旋回
    }
}
