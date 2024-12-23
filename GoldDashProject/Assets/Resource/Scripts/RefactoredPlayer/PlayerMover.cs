using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    [Header("通常時のプレイヤーの移動速度（マス／毎秒）")]
    [SerializeField] private float m_playerMoveSpeed = 1f;

    [Header("通常時のプレイヤーの回転速度（度／毎秒）")]
    [SerializeField] private float m_playerRorateSpeed = 1f;

    [Header("特定のStateで移動速度に倍率をかけたい場合、ここから設定しておく")]
    [SerializeField] private List<MoveSpeedMagnification> m_moveSpeedMagnificationsList;

    //KeyValuePairはインスペクタで表示できない（Serializableでない）ので自前のペアを作って使う
    [Serializable]
    private class MoveSpeedMagnification
    {
        public PLAYER_STATE playerState;
        public float magnification;
    }

    //現在のカメラのオイラー角をyのみ保存
    private float m_rotationY;

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

    /// <summary>
    /// プレイヤーをtransform.Translate()で移動させるよ。
    /// </summary>
    /// <param name="currentState">移動速度を算出するためにstateを教えてね。</param>
    /// <param name="V_InputHorizontal">VariableJoystickの水平方向入力</param>
    /// <param name="V_InputVertical">VariableJoystickの垂直方向入力</param>
    /// <param name="D_InputHorizontal">DynamicJoystickの水平方向入力</param>
    public float MovePlayer(PLAYER_STATE currentState, float V_InputHorizontal, float V_InputVertical, float D_InputHorizontal)
    {
        //プレイヤーの旋回
        if (!Mathf.Approximately(D_InputHorizontal, 0)) //ダイナミックジョイスティックの水平方向入力がほぼ0でないなら
        {
            //ジョイスティックの入力をオイラー角（〇軸を中心に△度回転、という書き方）にする
            //前提：カメラはZ軸の負の方向を向いている
            //水平入力を使って、プレイヤーのみ、Y軸中心回転を行う。
            m_rotationY += D_InputHorizontal * m_playerRorateSpeed * Time.deltaTime; //Unityは左手座標系なので、左右の回転角度（Y軸中心）は加算でいい
            this.transform.eulerAngles = new Vector3(0f, m_rotationY, 0f);
        }
        //プレイヤーの移動量
        if (!Mathf.Approximately(V_InputHorizontal, 0) && !Mathf.Approximately(V_InputVertical, 0))
        {
            Vector3 oldPos = transform.position;

            Vector3 playerMoveVec = new Vector3(V_InputHorizontal, 0f, V_InputVertical); //移動方向のベクトルを計算

            //注意！プレイヤーオブジェクトの腕やカメラは、オブジェクトのforwardとは逆を向いているので移動方向にマイナスをかける。Mayaの座標系がすべての元凶
            this.transform.Translate(-playerMoveVec * m_playerMoveSpeed * Time.deltaTime); //求めたベクトルに移動速度とdeltaTimeをかけて座標書き換え

            return (transform.position - oldPos).magnitude; //移動量を計算して返却
        }

            return 0f; //移動していないなら0fを返却
    }
    
}