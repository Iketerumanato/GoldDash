using UnityEngine;

public class SubscribeOrderManager : MonoBehaviour
{
    [SerializeField] private UdpUIManager udpUIManager;
    [SerializeField] private UdpUIDisplayer udpUIDisplayer;

    //subjectの初期化および購読はNull参照頻発地点。ここにエントリーポイントを作って、そこで購買関係の構築順序を制御する
    private void Awake()
    {
        udpUIManager.InitObservation();

        udpUIDisplayer.InitObservation(udpUIManager);
    }
}
