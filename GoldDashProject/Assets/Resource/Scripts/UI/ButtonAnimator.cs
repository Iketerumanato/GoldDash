using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class ButtonAnimator : MonoBehaviour
{
    [SerializeField] private float m_offset; //この矢印がどのくらいの距離移動するか

    [SerializeField] private RectTransform m_rtUpperLeft; //アニメーションさせたい矢印のRectTransform
    [SerializeField] private RectTransform m_rtUpperRight;
    [SerializeField] private RectTransform m_rtLowerLeft;
    [SerializeField] private RectTransform m_rtLowerRight;

    private bool m_isAnimating = false; //アニメーションするか否か
    private bool m_isGrayedOut= false; //グレーアウトするか否か

    //強調表示のパラメータ
    [SerializeField] private float m_durationForExpand = 1.0f; // 外へ移動する際のスピード
    [SerializeField] private float m_durationForBack = 0.5f; // 元の位置に戻るアニメーション時間
    [SerializeField] private float m_animationDelayTime = 0.3f; //元の位置に戻ったとき、また広がり始めるまでの待機時間

    //グレーアウト
    [SerializeField] private Image m_editImage; // グレーアウトさせたいボタンのImageコンポーネント
    [SerializeField] private TextMeshProUGUI m_editText; // グレーアウトさせたいボタンのTMPコンポーネント
    [SerializeField] private Color m_targetImageColor; // グレーアウト時この色に変更する
    [SerializeField] private Color m_targetTextColor; // グレーアウト時この色に変更する
    private Color m_originImageColor; // 元のImageの色
    private Color m_originTextColor; // 元の色

    //制御用プロパティ
    public bool IsAnimating
    {
        set
        {
            if (m_isAnimating != value) //値の変更があった時だけセットする
            {
                m_isAnimating = value; //値の代入
                if(value) StartAnimation(); //valueに応じてアニメーションを始めたり止めたりする
                else StopAnimation();
            }
        }

        get { return m_isAnimating; }
    }

    public bool IsGrayedOut
    {
        set
        {
            if (m_isGrayedOut != value) //値の変更があった時だけセットする
            {
                m_isGrayedOut = value; //値の代入
                if (value) StartGrayOut(); //valueに応じてグレーアウトしたり解除したりする
                else StopGrayOut();
            }
        }

        get { return m_isGrayedOut; }
    }

    //矢印の初期位置
    private Vector2 m_originAnchoredPosUpperLeft;
    private Vector2 m_originAnchoredPosUpperRight;
    private Vector2 m_originAnchoredPosLowerLeft;
    private Vector2 m_originAnchoredPosLowerRight;
    
    //矢印の目標位置
    private Vector2 m_targetAnchoredPosUpperLeft;
    private Vector2 m_targetAnchoredPosUpperRight;
    private Vector2 m_targetAnchoredPosLowerLeft;
    private Vector2 m_targetAnchoredPosLowerRight;

    //アニメーションのシーケンス
    private Sequence m_animationSequenceUpperLeft;
    private Sequence m_animationSequenceUpperRight;
    private Sequence m_animationSequenceLowerLeft;
    private Sequence m_animationSequenceLowerRight;

    private void Awake()
    {
        //矢印の初期位置を保存
        m_originAnchoredPosUpperLeft = m_rtUpperLeft.anchoredPosition;
        m_originAnchoredPosUpperRight = m_rtUpperRight.anchoredPosition;
        m_originAnchoredPosLowerLeft = m_rtLowerLeft.anchoredPosition;
        m_originAnchoredPosLowerRight = m_rtLowerRight.anchoredPosition;

        //矢印の目的位置を計算して保存
        m_targetAnchoredPosUpperLeft = m_rtUpperLeft.anchoredPosition + (new Vector2(-1, 1) * m_offset);
        m_targetAnchoredPosUpperRight = m_rtUpperRight.anchoredPosition + (new Vector2(1, 1) * m_offset);
        m_targetAnchoredPosLowerLeft = m_rtLowerLeft.anchoredPosition + (new Vector2(-1, -1) * m_offset);
        m_targetAnchoredPosLowerRight = m_rtLowerRight.anchoredPosition + (new Vector2(1, -1) * m_offset);

        //グレーアウトする画像や文字の元の色を保存
        if (m_editImage == null && m_editText == null) return;

        m_originImageColor = m_editImage.color;
        m_originTextColor = m_editText.color;
    }

    private void StartAnimation()
    {
        //左上アニメーション開始
        m_animationSequenceUpperLeft = DOTween.Sequence()
                                              .Append(m_rtUpperLeft.DOAnchorPos(m_targetAnchoredPosUpperLeft, m_durationForExpand)).SetEase(Ease.InOutQuad)
                                              .Append(m_rtUpperLeft.DOAnchorPos(m_originAnchoredPosUpperLeft, m_durationForBack)).SetEase(Ease.InQuad)
                                              .AppendInterval(m_animationDelayTime) // 待機時間
                                              .SetLoops(-1); // 無限ループ

        //右上アニメーション開始
        m_animationSequenceUpperRight = DOTween.Sequence()
                                              .Append(m_rtUpperRight.DOAnchorPos(m_targetAnchoredPosUpperRight, m_durationForExpand)).SetEase(Ease.InOutQuad)
                                              .Append(m_rtUpperRight.DOAnchorPos(m_originAnchoredPosUpperRight, m_durationForBack)).SetEase(Ease.InQuad)
                                              .AppendInterval(m_animationDelayTime) // 待機時間
                                              .SetLoops(-1); // 無限ループ

        //左下アニメーション開始
        m_animationSequenceLowerLeft = DOTween.Sequence()
                                              .Append(m_rtLowerLeft.DOAnchorPos(m_targetAnchoredPosLowerLeft, m_durationForExpand)).SetEase(Ease.InOutQuad)
                                              .Append(m_rtLowerLeft.DOAnchorPos(m_originAnchoredPosLowerLeft, m_durationForBack)).SetEase(Ease.InQuad)
                                              .AppendInterval(m_animationDelayTime) // 待機時間
                                              .SetLoops(-1); // 無限ループ

        //右下アニメーション開始
        m_animationSequenceLowerRight = DOTween.Sequence()
                                              .Append(m_rtLowerRight.DOAnchorPos(m_targetAnchoredPosLowerRight, m_durationForExpand)).SetEase(Ease.InOutQuad)
                                              .Append(m_rtLowerRight.DOAnchorPos(m_originAnchoredPosLowerRight, m_durationForBack)).SetEase(Ease.InQuad)
                                              .AppendInterval(m_animationDelayTime) // 待機時間
                                              .SetLoops(-1); // 無限ループ
    }

    private void StopAnimation()
    {
        m_animationSequenceUpperLeft.Kill();
        m_animationSequenceUpperRight.Kill();
        m_animationSequenceLowerLeft.Kill();
        m_animationSequenceLowerRight.Kill();

        m_rtUpperLeft.anchoredPosition = m_originAnchoredPosUpperLeft;
        m_rtUpperRight.anchoredPosition = m_originAnchoredPosUpperRight;
        m_rtLowerLeft.anchoredPosition = m_originAnchoredPosLowerLeft;
        m_rtLowerRight.anchoredPosition = m_originAnchoredPosLowerRight;
    }

    private void StartGrayOut()
    {
        if (m_editImage == null && m_editText == null) return;

        m_editImage.color = m_targetImageColor;
        m_editText.color = m_targetTextColor;
    }

    private void StopGrayOut()
    {
        if (m_editImage == null && m_editText == null) return;

        m_editImage.color = m_originImageColor;
        m_editText.color = m_originTextColor;
    }
}