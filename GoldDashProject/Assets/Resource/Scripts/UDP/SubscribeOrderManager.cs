using UnityEngine;

public class SubscribeOrderManager : MonoBehaviour
{
    [SerializeField] private UdpButtonManager udpButtonManager;
    [SerializeField] private UdpUIDisplayer udpUIDisplayer;
    [SerializeField] private UdpTextWriter udpTextWriter;
    [SerializeField] private UdpUIColorChanger udpUIColorChanger;
    [SerializeField] private GameServerManager gameServerManager;
    [SerializeField] private GameClientManager gameClientManager;

    [SerializeField] private MapGenerator mapGenerator;

    //仮のサウンドマネージャー
    [SerializeField] private TmpSoundManager soundManager;

    //subjectの初期化および購読はNull参照頻発地点。ここにエントリーポイントを作って、そこで購買関係の構築順序を制御する
    private void Awake()
    {
        //ここがエントリーポイントなのでFPSも指定しておく
        Application.targetFrameRate = 45;

        //ボタン関連
        udpButtonManager.InitObservation(); //InitObservationに引数がないということは、何にも依存していないいうことなので、最初に初期化できる

        //マネージャー
        gameServerManager.InitObservation(udpButtonManager);
        //gameClientManager.InitObservation(udpButtonManager);

        //UI関連

        udpUIDisplayer.InitObservation(udpButtonManager, gameServerManager, gameClientManager);
        udpTextWriter.InitObservation(udpButtonManager, gameServerManager, gameClientManager);
        udpUIColorChanger.InitObservation(udpButtonManager, gameServerManager, gameClientManager);

        soundManager.InitObservation(udpButtonManager);

        //サーバー関連
        mapGenerator.InitObservation(gameServerManager, gameClientManager);
    }
}
