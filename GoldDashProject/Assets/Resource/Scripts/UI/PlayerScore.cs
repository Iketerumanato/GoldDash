using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class PlayerScore : MonoBehaviour
{
    [SerializeField] private RectTransform[] digitImages;
    [SerializeField] private Texture numberTexture;
    [SerializeField] private float animationTime = 0.5f;
    [SerializeField] private float digitHeight = 32f;
    [SerializeField] private float delayBetweenDigits = 0.1f;
    [SerializeField] ActorController actorScore;
    private int previousScore;
    private Sequence CountUpAnimation;
    private Vector2[] digitsInitialPositions;

    private void Awake()
    {
        SetupDigitImages();
        StoreInitialPositions();
        previousScore = actorScore.Gold;
        UpdateScoreDisplay();
    }

    private void Update()
    {
        // Inspectorでscoreを変更した時のみアニメーションを更新
        if (actorScore.Gold != previousScore)
        {
            UpdateScoreDisplay();
            previousScore = actorScore.Gold;//現在のスコアの値を渡す
        }
    }

    //一文字分の画像(テクスチャ)を取得
    private void SetupDigitImages()
    {
        foreach (var scoreimages in digitImages)
        {
            scoreimages.GetComponent<RawImage>().texture = numberTexture;
        }
    }

    //文字の字間を取得
    private void StoreInitialPositions()
    {
        digitsInitialPositions = new Vector2[digitImages.Length];
        for (int digitsCnt = 0; digitsCnt < digitImages.Length; digitsCnt++)
        {
            digitsInitialPositions[digitsCnt] = digitImages[digitsCnt].anchoredPosition;
        }
    }

    //スコア更新時のアニメーション
    private void UpdateScoreDisplay()
    {
        //画像を動かしたら処理を一旦Kill
        if (CountUpAnimation != null && CountUpAnimation.IsActive())
        {
            CountUpAnimation.Kill();
        }

        int scoreDifference = actorScore.Gold - previousScore;

        int[] digits = GetAllDigits(actorScore.Gold);//全ての桁の文字の大きさを取得
        CountUpAnimation = DOTween.Sequence();

        //桁数ごとに上下動かしていく
        for (int digitsCnt = 0; digitsCnt < digitImages.Length; digitsCnt++)
        {
            RectTransform rectTransform = digitImages[digitImages.Length - 1 - digitsCnt];
            int targetDigit = digits[digitsCnt];

            // 現在の位置を基準に、ターゲットの位置を計算
            Vector2 targetPosition = digitsInitialPositions[digitsCnt] + new Vector2(0, targetDigit * digitHeight);

            // 各桁のアニメーションを設定
            float delay = digitsCnt * delayBetweenDigits;
            CountUpAnimation.Insert(delay, rectTransform.DOAnchorPosY(targetPosition.y, animationTime).SetEase(Ease.OutQuad));
        }

        CountUpAnimation.Play();
    }

    //すべての桁の情報を取得
    private int[] GetAllDigits(int number)
    {
        int[] result = new int[digitImages.Length];
        string numberStr = number.ToString().PadLeft(digitImages.Length, '0');

        for (int digitsCnt = 0; digitsCnt < digitImages.Length; digitsCnt++)
        {
            result[digitsCnt] = int.Parse(numberStr[numberStr.Length - 1 - digitsCnt].ToString());
        }

        return result;
    }
}