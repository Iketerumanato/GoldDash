using UnityEngine;

public class SubscribeOrderManager : MonoBehaviour
{
    [SerializeField] private UdpButtonManager udpButtonManager;
    [SerializeField] private UdpUIDisplayer udpUIDisplayer;
    [SerializeField] private UdpTextManager udpTextManager;

    //subjectの初期化および購読はNull参照頻発地点。ここにエントリーポイントを作って、そこで購買関係の構築順序を制御する
    private void Awake()
    {
        udpButtonManager.InitObservation();

        udpUIDisplayer.InitObservation(udpButtonManager);
        udpTextManager.InitObservation(udpButtonManager);
    }
}
