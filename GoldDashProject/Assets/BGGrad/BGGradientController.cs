using UnityEngine;

public class BGGradientController : MonoBehaviour
{
    [SerializeField] private Material gradientMaterial; // グラデーション用マテリアル
    [SerializeField] private float cycleDuration = 5f;  // 色が変化する周期
    [SerializeField] private float timeOffset = 0.5f;  // グラデーションの時差

    public enum BG_GRAD_STATE //グラデーションの状態。サーバーから操作
    { 
        NORMAL,
        IN_USE_PLAYER_1,
        IN_USE_PLAYER_2,
        IN_USE_PLAYER_3,
        IN_USE_PLAYER_4,
    }

    [SerializeField] private Gradient normal; //未使用状態のカラー指定
    [SerializeField] private Gradient player1;　//プレイヤー1が使用している状態でのカラー指定
    [SerializeField] private Gradient player2;
    [SerializeField] private Gradient player3;
    [SerializeField] private Gradient player4;

    private BG_GRAD_STATE state;
    public BG_GRAD_STATE State { set { state = value; ChangeGradiantState(value); } get { return state; } }
    private Gradient currentGradiant;

    private void Start()
    {
        //起動時はノーマルのグラデーションにしておく
        currentGradiant = normal;
    }

    void Update()
    {
        if (gradientMaterial != null)
        {
            float t = Mathf.PingPong(Time.time / cycleDuration, 1f);

            gradientMaterial.SetColor("_ColorTopLeft", currentGradiant.Evaluate(Mathf.PingPong(timeOffset + Time.time / cycleDuration, 1.0f)));
            gradientMaterial.SetColor("_ColorTopRight", currentGradiant.Evaluate(Mathf.PingPong(Time.time / cycleDuration, 1.0f)));
            gradientMaterial.SetColor("_ColorBottomLeft", currentGradiant.Evaluate(Mathf.PingPong(Time.time / cycleDuration, 1.0f)));
            gradientMaterial.SetColor("_ColorBottomRight", currentGradiant.Evaluate(Mathf.PingPong(timeOffset + Time.time / cycleDuration, 1.0f)));
        }
    }

    void ChangeGradiantState(BG_GRAD_STATE state)
    {
        switch (state)
        {
            case BG_GRAD_STATE.NORMAL:
                currentGradiant = normal;
                break;
            case BG_GRAD_STATE.IN_USE_PLAYER_1:
                currentGradiant = player1;
                break;
            case BG_GRAD_STATE.IN_USE_PLAYER_2:
                currentGradiant = player2;
                break;
            case BG_GRAD_STATE.IN_USE_PLAYER_3:
                currentGradiant = player3;
                break;
            case BG_GRAD_STATE.IN_USE_PLAYER_4:
                currentGradiant = player4;
                break;
        }
    }
}
