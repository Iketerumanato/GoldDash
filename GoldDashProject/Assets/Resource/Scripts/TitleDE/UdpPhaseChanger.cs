using R3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UdpPhaseChanger : MonoBehaviour
{
    //フェーズを変え得るボタン
    [SerializeField] private Button ServerActivateButton;
    [SerializeField] private Button ServerStartGameButton;

    [SerializeField] private Button ClientTouchToStartButton;
    [SerializeField] private Button ClientActivateButton;
    [SerializeField] private Button ClientConnectButton;
    [SerializeField] private Button ClientBackButton;

    //シングルトンにする
    public static UdpPhaseChanger instance;

    //フェーズを通知する
    public Subject<UDP_PHASE> udpPhaseSubject;

    //現在のフェーズをプロパティ化し、値が変更された際の通知をセッターで自動化
    private UDP_PHASE m_currentUdpPhase;
    public UDP_PHASE CurrentUdpPhase
    {
        set
        {
            //値が変更されたときだけ代入して通知を飛ばす
            if (m_currentUdpPhase != value)
            { 
                m_currentUdpPhase = value;
                udpPhaseSubject.OnNext(m_currentUdpPhase);
            }
        }
        get { return m_currentUdpPhase; }
    }

    //インスタンス生成など
    public void InitOvservation()
    {
        //シングルトンインスタンス生成
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        udpPhaseSubject = new Subject<UDP_PHASE>();
    }

    //フェーズを定義するenum
    public enum UDP_PHASE : int
    {
        MODE_SELECT = 0, //モード選択画面
        S0, S1, S2, S3, //サーバー用
        C0, C1, C2, C3, C4, C5, C6, //クライアント用
    }
}
