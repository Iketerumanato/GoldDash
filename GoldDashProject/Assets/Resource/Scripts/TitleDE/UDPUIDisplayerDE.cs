using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using TMPro;

public class UDPUIDisplayerDE : MonoBehaviour
{
    [Header("サーバー用UI")]
    [SerializeField] private GameObject serverUICanvas;

    [SerializeField] private GameObject serverProcessingIcon;
    [SerializeField] private TextMeshProUGUI serverUpperText;
    [SerializeField] private TextMeshProUGUI serverLowerText;

    [SerializeField] private GameObject s2UniqueCanvas;
    [SerializeField] private GameObject s3UniqueCanvas;
    [SerializeField] private GameObject playerInfoCanvas;

    [Header("クライアント用UI")]
    [SerializeField] private GameObject clientUICanvas;

    [SerializeField] private GameObject clientProcessingIcon;
    [SerializeField] private GameObject clientArrowIcon;
    [SerializeField] private GameObject clientBackButton;
    [SerializeField] private TextMeshProUGUI clientUpperText;
    [SerializeField] private TextMeshProUGUI clientCenterText;

    [SerializeField] private GameObject c0UniqueCanvas;
    [SerializeField] private GameObject c1UniqueCanvas;

    [Header("モード選択用UI")]
    [SerializeField] private GameObject modeSelectUICanvas;

    public void InitOvservation(UdpPhaseChanger udpPhaseChanger)
    {
        udpPhaseChanger.udpPhaseSubject.Subscribe(p => ProcessEvent(p));
    }

    private void ProcessEvent(UdpPhaseChanger.UDP_PHASE p)
    {
        switch (p)
        {
            case UdpPhaseChanger.UDP_PHASE.MODE_SELECT:
                serverUICanvas.SetActive(false);
                clientUICanvas.SetActive(false);
                modeSelectUICanvas.SetActive(true);
                break;

            case UdpPhaseChanger.UDP_PHASE.S0:
                serverUICanvas.SetActive(true);
                serverProcessingIcon.SetActive(true);
                playerInfoCanvas.SetActive(true);
                serverUpperText.text = "プレイヤーの接続を待っています…";
                serverLowerText.text = "プレイヤーの接続を待っています…";
                break;
            case UdpPhaseChanger.UDP_PHASE.S1:
                serverUpperText.text = "プレイヤーが集まりました！";
                serverLowerText.text = "プレイヤーが集まりました！";
                break;
            case UdpPhaseChanger.UDP_PHASE.S2:
                playerInfoCanvas.SetActive(false);
                s2UniqueCanvas.SetActive(true);
                serverUpperText.text = "プレイヤーの配色を選んでください";
                serverLowerText.text = "プレイヤーの配色を選んでください";
                break;
            case UdpPhaseChanger.UDP_PHASE.S3:
                s2UniqueCanvas.SetActive(false);
                playerInfoCanvas.SetActive(true);
                s3UniqueCanvas.SetActive(true);
                break;

            case UdpPhaseChanger.UDP_PHASE.C0:
                modeSelectUICanvas.SetActive(false);
                clientUICanvas.SetActive(true);
                c0UniqueCanvas.SetActive(true);
                clientUpperText.text = "";
                clientCenterText.text = "";
                break;
            case UdpPhaseChanger.UDP_PHASE.C1:
                c0UniqueCanvas.SetActive(false);
                clientProcessingIcon.SetActive(false);
                c1UniqueCanvas.SetActive(true);
                clientBackButton.SetActive(true);
                clientUpperText.text = "プレイヤー名を入力してください";
                clientCenterText.text = "";
                break;
            case UdpPhaseChanger.UDP_PHASE.C2:
                c1UniqueCanvas.SetActive(false);
                clientBackButton.SetActive(false);
                clientProcessingIcon.SetActive(true);
                clientUpperText.text = "";
                clientCenterText.text = "接続中…";
                break;
            case UdpPhaseChanger.UDP_PHASE.C3:
                clientBackButton.SetActive(true);
                clientUpperText.text = "他のプレイヤーを待っています…";
                clientCenterText.text = "接続完了！";
                break;
            case UdpPhaseChanger.UDP_PHASE.C4:
                clientBackButton.SetActive(false);
                clientArrowIcon.SetActive(true);
                clientUpperText.text = "テーブルの画面をタッチで操作してください";
                clientCenterText.text = "";
                break;
            case UdpPhaseChanger.UDP_PHASE.C5:
                clientArrowIcon.SetActive(false);
                clientUpperText.text = "";
                clientCenterText.text = "ゲームを開始します！";
                break;
            case UdpPhaseChanger.UDP_PHASE.C6:
                clientProcessingIcon.SetActive(false);
                clientUICanvas.SetActive(false);
                break;
        }
    }
}
