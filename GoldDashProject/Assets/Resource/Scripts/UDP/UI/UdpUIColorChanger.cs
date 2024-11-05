using UnityEngine;
using R3;
using TMPro;
using UnityEngine.UI;

public class UdpUIColorChanger : MonoBehaviour
{
    [SerializeField] private Gradient idle;
    [SerializeField] private Color idleColor;

    [SerializeField] private Gradient select;

    [SerializeField] private Gradient server;

    [SerializeField] private Gradient client;

    [SerializeField] private Gradient error;

    private Gradient currentGradiant;

    //色を変えたいUI
    [SerializeField] private TMP_Text stateMsg;
    [SerializeField] private TMP_Text stateMsgMini;

    [SerializeField] private RawImage line;

    [SerializeField] private RawImage originSign;
    [SerializeField] private RawImage serverSign;
    [SerializeField] private RawImage clientSign;
    [SerializeField] private RawImage originSignMini;
    [SerializeField] private RawImage serverSignMini;
    [SerializeField] private RawImage clientSignMini;

    //操作対象
    private TMP_Text currentTextComponent;
    private TMP_TextInfo currentTextInfo;
    RawImage currentProcessImage; //その時色変更の対称としている画像

    private float timeOffsetSize;

    public void InitObservation(UdpButtonManager udpUIManager, GameServerManager gameServerManager, GameClientManager gameClientManager)
    {
        udpUIManager.udpUIManagerSubject.Subscribe(e => ProcessUdpManagerEvent(e));
        gameServerManager.ServerInternalSubject.Subscribe(e => ProcessServerInternalEvent(e));
        gameClientManager.ClientInternalSubject.Subscribe(e => ProcessClientInternalEvent(e));
    }

    private void ProcessServerInternalEvent(GameServerManager.SERVER_INTERNAL_EVENT e)
    {
        switch (e)
        {
            case GameServerManager.SERVER_INTERNAL_EVENT.EDIT_GUI_FOR_GAME:
                currentTextComponent = stateMsgMini;
                currentProcessImage = serverSignMini;
                break;
            default:
                break;
        }
    }

    private void ProcessClientInternalEvent(GameClientManager.CLIENT_INTERNAL_EVENT e)
    {
        switch (e)
        {
            case GameClientManager.CLIENT_INTERNAL_EVENT.EDIT_GUI_FOR_GAME:
                currentTextComponent = stateMsgMini;
                currentProcessImage = clientSignMini;
                break;
            case GameClientManager.CLIENT_INTERNAL_EVENT.COMM_ESTABLISHED:
                currentGradiant = client;
                timeOffsetSize = 0.1f;
                break;
            case GameClientManager.CLIENT_INTERNAL_EVENT.COMM_ERROR_FATAL:
                currentGradiant = error;
                break;
            default:
                break;
        }
    }

    private void ProcessUdpManagerEvent(UdpButtonManager.UDP_BUTTON_EVENT e)
    {
        switch (e)
        {
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_START_SERVER_MODE:
                currentGradiant = idle;
                originSign.color = idleColor;
                timeOffsetSize = 0f;
                serverSign.gameObject.SetActive(true);
                currentProcessImage = serverSign;
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_SERVER_ACTIVATE:
                currentGradiant = server;
                timeOffsetSize = 0.1f;
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_START_CLIENT_MODE:
                currentGradiant = idle;
                timeOffsetSize = 0f;
                originSign.color = idleColor;
                clientSign.gameObject.SetActive(true);
                currentProcessImage = clientSign;
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_BACK_TO_SELECT:
                currentGradiant = select;
                timeOffsetSize = 0f;
                serverSign.gameObject.SetActive(false);
                clientSign.gameObject.SetActive(false);
                currentProcessImage = originSign;
                break;

            default:
                currentGradiant = idle;
                timeOffsetSize = 0f;
                break;
        }
    }

    private void Start()
    {
        timeOffsetSize = 0f;
        currentGradiant = select;

        currentTextComponent = stateMsg;
        currentProcessImage = originSign;

        //ロゴの表示非表示
        originSign.gameObject.SetActive(true);
        serverSign.gameObject.SetActive(false);
        clientSign.gameObject.SetActive(false);
    }

    private void Update()
    {
        ColorAnimation();
    }

    //genaralmessageをグラデーションさせるネットから拾ったものを改造した
    //変数の宣言が抜け落ちていたり、リッチテキストを考慮した処理ができていなかったりという問題を修正
    //コメントを追加
    //メッシュ再生成などの処理は無駄がありそうだが、現状分からないし優先するべきことでもないのでそのままに
    //https://coposuke.hateblo.jp/entry/2020/06/07/020330#%EF%BC%93%E3%83%AA%E3%83%83%E3%83%81%E3%83%86%E3%82%AD%E3%82%B9%E3%83%88s--u--mark
    private void ColorAnimation()
    {
        //lineの色変更
        Color currentColor = currentGradiant.Evaluate(Mathf.PingPong(Time.time / 2, 1.0f));

        line.color = currentColor;
        currentProcessImage.color = currentColor;

        //genaralMessageの色変更
        // ① メッシュを再生成する（リセット）
        this.currentTextComponent.ForceMeshUpdate(true);
        this.currentTextInfo = currentTextComponent.textInfo;

        // ②頂点データ配列の編集
        int count = Mathf.Min(this.currentTextInfo.characterCount, this.currentTextInfo.characterInfo.Length); //後者の値の方が小さいことってあり得る？？？削っていいかも
        //不可視の文字を無視するインデックス
        int visibleCharactorIndex = 0;

        for (int i = 0; i < count; i++)
        {
            TMP_CharacterInfo charInfo = this.currentTextInfo.characterInfo[i]; //操作する文字のTMP_CharacterInfo

            if (!charInfo.isVisible) //見えない文字（リッチテキスト用の文字列など）はパス
                continue;

            int materialIndex = charInfo.materialReferenceIndex; //この文字のメッシュを指すmeshInfoのindex
            int vertexIndex = charInfo.vertexIndex; //この文字のmeshInfo内での最初の頂点Index

            //Gradientカラーを適用する
            //1文字ごとに時差をつけたい
            float timeOffset = -timeOffsetSize * visibleCharactorIndex; //0.1秒の時差
            float time1 = Mathf.PingPong((timeOffset + Time.time) / 2, 1.0f); //gradientの中で参照する時間を2つ用意することで、文字の中でグラデーションを付けられる
            float time2 = Mathf.PingPong((timeOffset + Time.time - timeOffsetSize) / 2, 1.0f); //文字ごとの時差と同じく両端で0.1秒の時差。作りたい表現によって異なるが、今回は滑らかなカラーウェーブ表現のため時差を同じにする
            currentTextInfo.meshInfo[materialIndex].colors32[vertexIndex + 0] = currentGradiant.Evaluate(time1); //左下の頂点のカラーをGradientを元に変更。
            currentTextInfo.meshInfo[materialIndex].colors32[vertexIndex + 1] = currentGradiant.Evaluate(time1); //左上。TMPの頂点インデックスは決まった順番に割り振られているのでこういう書き方ができる。
            currentTextInfo.meshInfo[materialIndex].colors32[vertexIndex + 2] = currentGradiant.Evaluate(time2); //右上
            currentTextInfo.meshInfo[materialIndex].colors32[vertexIndex + 3] = currentGradiant.Evaluate(time2); //右下

            //不可視でない文字を処理したなら専用のインデックスを増やす（iではダメ）
            visibleCharactorIndex++;
        }

        // ③ メッシュを更新
        for (int i = 0; i < this.currentTextInfo.materialCount; i++)
        {
            if (this.currentTextInfo.meshInfo[i].mesh == null) { continue; }

            this.currentTextInfo.meshInfo[i].mesh.colors32 = this.currentTextInfo.meshInfo[i].colors32;
            currentTextComponent.UpdateGeometry(this.currentTextInfo.meshInfo[i].mesh, i);
        }
    }
}
