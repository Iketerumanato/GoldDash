using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestUnlocker : MonoBehaviour
{
    [Header("keyのゲームオブジェクト")]
    [SerializeField] private GameObject m_keyObject;
    private Animator m_animator; //keyのanimator

    private int m_maxDrawCount = 5; //この回数以上円を描いたなら宝箱が開錠される。デフォルトで5
    public int MaxDrawCount //5の倍数であることを期待するので、セッターをカスタム
    {
        set //最低でも5以上の、5の倍数が代入される
        {
            if (value < 5) m_maxDrawCount = 5; //5未満なら5を代入
            else if (value % 5 != 0)
            {
                int lowerMultiple = (value / 5) * 5; //5より小さいまたは等しい倍数
                int upperMultiple = lowerMultiple + 5; //valueより大きい5の倍数
                //lowerMultipleとupperMultipleのうち、valueに近い方を代入
                m_maxDrawCount = Mathf.Abs(value - lowerMultiple) <= Mathf.Abs(value - upperMultiple) ? lowerMultiple : upperMultiple;
            }
            else m_maxDrawCount = value; //5の倍数ならそのまま代入

            m_spanOfAnimations = m_maxDrawCount / 5; //アニメーションを再生するのに必要な回転数は開錠に必要な回転数を5で割った値なので、ここで同時に変更
        }
        get { return m_maxDrawCount; }
    }

    //鍵が崩れていく5段階のモーション
    //private readonly string strUnlockTrigger1 = "UnlockTrigger1"; //最初
    //private readonly string strUnlockTrigger2 = "UnlockTrigger2";
    //private readonly string strUnlockTrigger3 = "UnlockTrigger3";
    //private readonly string strUnlockTrigger4 = "UnlockTrigger4";
    //private readonly string strUnlockTrigger5 = "UnlockTrigger5"; //最後
    //アニメーションの進行状況をリセットする
    private readonly string strResetTrigger = "ResetTrigger";

    //円を描く判定に使うプライベート変数
    private readonly List<Vector2> m_drawPoints = new List<Vector2>();
    private float m_totalAngle = 0f; // 累計角度
    private float m_previousAngle = 0f; // 前回の角度
    private bool m_isCounterClockwise = true; // 反時計回りかどうかを記録
    private Vector2 m_center = Vector2.zero; // プレイヤーが描こうとしている円の推定される中心点

    private int m_circleCount = 0; // 描ききった円の数
    private int m_spanOfAnimations = 1; //描ききった円がこの数増えるごとに次のアニメーションを再生 
    private int m_currentStageOfAnimations; //現在何番目のアニメーションまで再生したか

    //円を描く判定に使う定数
    private const float MIN_DISTANCE_THRESHOLD = 5f; //あまりにも小さいグルグル行為は無視するため
    private const int CENTER_RECALCULATION_INTERVAL = 10;//点をCENTER_RECALCULATION_INTERVALの倍数個記録する度に、円の推定半径を再計算する
    private const float MAX_CIRCLE_ANGLE = 360f; //累計角度（totalAngle）がこの値を超えたら円を描いたとみなす

    private void Start()
    {
        m_animator = m_keyObject.gameObject.GetComponent<Animator>(); //keyのanimator取得
    }

    /// <summary>
    /// 画面から指を離す(クリックから離す)ごとに呼び出す
    /// </summary>
    public void StartDrawCircle()
    {
        m_drawPoints.Clear(); //記録したスクリーン座標データの削除
        m_totalAngle = 0f; //累計角度の初期化
        m_previousAngle = 0f; //前フレームの角度保存用変数を初期化
        m_isCounterClockwise = true; //デフォルトの向きに直す(反時計回り)
    }

    /// <summary>
    /// 画面をクリックし続けているとき呼び出す
    /// </summary>
    /// <param name="inputPosition">クリックしている座標</param>
    /// <returns>円を既定の回数以上描ききったらtrue,でなければfalse</returns>
    public bool DrawingCircle(Vector2 inputPosition)
    {
        // ポイント間隔をフィルタリング
        if (m_drawPoints.Count > 0)
        {
            float distance = Vector2.Distance(m_drawPoints[m_drawPoints.Count - 1], inputPosition);
            //最後に記録した点と今回入力された点の位置がMIN_DISTANCE_THRESHOLDより近いと記録されない
            if (distance < MIN_DISTANCE_THRESHOLD) return false;
        }

        // 入力ポイントを記録
        m_drawPoints.Add(inputPosition);

        if (m_drawPoints.Count > 1)
        {
            // 一定間隔で点群の重心を再計算する。プレイヤーが円を描くことを期待するならば、点データが増えるほど円の中心点に近づく
            if (m_drawPoints.Count % CENTER_RECALCULATION_INTERVAL == 0)
            {
                m_center = GetCenter(m_drawPoints);
            }

            // 点群の重心を原点として、現在描いている点の角度（ラジアン）を求め、度に変換
            // -180~180度で得られる角度を0~360度の範囲に変換
            Vector2 currentVector = m_drawPoints[m_drawPoints.Count - 1] - m_center;
            float currentAngle = Mathf.Repeat(Mathf.Atan2(currentVector.y, currentVector.x) * Mathf.Rad2Deg, 360f);

            // 2点以上が記録されている時、角度の差分を計算
            if (m_drawPoints.Count > 2)
            {
                //前フレームで求めたcurrentAngleと、今フレームのcurrentAngleを角度差を計算（-179～180度）
                float deltaAngle = Mathf.DeltaAngle(m_previousAngle, currentAngle);

                // 回転方向が切り替わったかチェック
                // 反時計回りを期待しているとき角度差が負であれば逆回転している、時計回りのとき正であれば逆回転しているとみなす
                if ((m_isCounterClockwise && deltaAngle < 0f) || (!m_isCounterClockwise && deltaAngle > 0f))
                {
                    m_totalAngle = 0f; // 累計角度をリセット
                    m_isCounterClockwise = !m_isCounterClockwise; // 回転方向を反転
                }

                m_totalAngle += deltaAngle; //累計角度に加算。累計角度は負の値にもなり得る

                // 円を描いたか確認。負の値の可能性があるので絶対値を求めてから比較
                if (Mathf.Abs(m_totalAngle) >= MAX_CIRCLE_ANGLE)
                {
                    m_circleCount++; //描ききった円の数を加算する

                    m_totalAngle %= MAX_CIRCLE_ANGLE; // 累計角度を0~360度の範囲に戻す

                    // アニメーション再生
                    if (m_circleCount <= MaxDrawCount)
                    {
                        int stageOfAnimation = m_circleCount / m_spanOfAnimations; //spanOfAnimationsはMaxDrawCount(5の倍数)を5で割った値

                        //前フレームのアニメーション段階よりも今フレームのアニメーション段階の方が大きいなら
                        if (stageOfAnimation > 0 && stageOfAnimation > m_currentStageOfAnimations)
                        {
                            m_currentStageOfAnimations = stageOfAnimation; //現在のアニメーション段階を記録
                            string triggerName = "UnlockTrigger" + stageOfAnimation.ToString(); //triggerのパラメータ名を作成する
                            m_animator.SetTrigger(triggerName); //作成したパラメータ名を使ってアニメーションを再生
                        }
                        
                    }
                    if (m_circleCount == MaxDrawCount)
                    {
                        return true; //円を既定の回数以上描いていたならtrueを返却
                    }
                }
            }

            m_previousAngle = currentAngle; // 今フレームに入力した点の、重心からの角度を保存
        }

        return false; //円を既定の回数以上描いていないのでfalseを返却

        //計算用ローカル関数
        //点群の重心の取得
        Vector2 GetCenter(List<Vector2> points)
        {
            if (points.Count < 2)
                return Vector2.zero;

            Vector2 sum = Vector2.zero;
            foreach (var point in points)
            {
                sum += point;
            }
            return sum / points.Count;
        }
    }

    /// <summary>
    /// 宝箱の処理がすべて終了したときや、殴られたときなど、円を描く行為を辞めるときに呼び出す
    /// </summary>
    public void ResetCircleDraw()
    {
        m_drawPoints.Clear(); //記録したスクリーン座標データの削除
        m_circleCount = 0; //描いた円の数を0に
        m_currentStageOfAnimations = 0; //アニメーション段階を0に
        //m_animator モーションリセット
    }
}
