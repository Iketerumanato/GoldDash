using UnityEngine;

public class SubscribeOrderManager : MonoBehaviour
{
    [SerializeField] private UdpButtonManager udpButtonManager;
    [SerializeField] private UdpUIDisplayer udpUIDisplayer;
    [SerializeField] private UdpTextWriter udpTextWriter;
    [SerializeField] private UdpUIColorChanger udpUIColorChanger;
    [SerializeField] private GameServerManager gameServerManager;

    //仮のサウンドマネージャー
    [SerializeField] private TmpSoundManager soundManager;

    //subjectの初期化および購読はNull参照頻発地点。ここにエントリーポイントを作って、そこで購買関係の構築順序を制御する
    private void Awake()
    {
        Application.targetFrameRate = 60;

        udpButtonManager.InitObservation();

        udpUIDisplayer.InitObservation(udpButtonManager);
        udpTextWriter.InitObservation(udpButtonManager);
        udpUIColorChanger.InitObservation(udpButtonManager);

        soundManager.InitObservation(udpButtonManager);

        gameServerManager.InitObservation(udpButtonManager);
    }
}
