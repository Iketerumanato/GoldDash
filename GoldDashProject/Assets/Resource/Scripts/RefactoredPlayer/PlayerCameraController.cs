using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    //制御対象のカメラをインスペクタからアタッチ
    [Header("回転させたいカメラ")]
    [SerializeField] private Camera m_playerCamera;

    //パラメータ
    [Header("カメラ回転速度（度／毎秒）")]
    [SerializeField] private float m_cameraMoveSpeed = 1f;

    [Header("カメラの縦方向（X軸中心）回転の角度制限")]
    [Range(0f, 90f)]
    [SerializeField] private float m_camRotateLimitX = 90f;

    //現在のカメラのオイラー角をxのみ保存
    private float m_rotationX;

    private void Start()
    {
        if (m_playerCamera == null) Debug.LogError("プレイヤーカメラがアタッチされていないよ～！"); //アタッチ漏れ検出
        m_rotationX = this.transform.rotation.eulerAngles.x; //オイラー角の初期化
    }

    /// <summary>
    /// 垂直方向のInputを受け取ってカメラを縦に回転させるよ。横方向の回転はプレイヤーの旋回に紐づいて行われるからここでは実行しないよ。
    /// </summary>
    /// <param name="inputVertical"> Input.GetAxis("Vertical")の値が渡されることを期待しているよ。</param>
    public void RotateCamara(float inputVertical)
    {
        //カメラ操作の入力がないなら回転しない
        if (!Mathf.Approximately(inputVertical, 0)) //水平方向の入力が"ほぼ0"でないなら
        {
            //ジョイスティックの入力をオイラー角（〇軸を中心に△度回転、という書き方）にする
            //前提：カメラはZ軸の負の方向を向いている
            //垂直の入力を使って、カメラのみ、X軸中心回転を行う。
            m_rotationX -= inputVertical * m_cameraMoveSpeed * Time.deltaTime; //Unityは左手座標系なので、上下の回転角度（X軸中心）にはマイナスをかけなければならない
            m_rotationX = Mathf.Clamp(m_rotationX, -m_camRotateLimitX, m_camRotateLimitX); //縦方向(X軸中心)回転には角度制限をつけないと宙返りしてしまう
            m_playerCamera.transform.eulerAngles = new Vector3(m_rotationX, m_playerCamera.transform.eulerAngles.y, 0f); //m_playerCamera.transform.eulerAngles.yは親の回転に委ねているので弄らない
        }
    }
}
