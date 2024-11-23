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
    [SerializeField] private int Score;
    private int previousScore;
    private Sequence CountUpAnimation;
    private Vector2[] digitsInitialPositions;

    [SerializeField] DisplayScoreHistory displayScoreHistory;

    private void Awake()
    {
        SetupDigitImages();
        StoreInitialPositions();
        previousScore = Score;
        UpdateScoreDisplay();
    }

    private void Update()
    {
        // Inspectorでscoreを変更した時のみアニメーションを更新
        if (Score != previousScore)
        {
            UpdateScoreDisplay();
            previousScore = Score;
        }
    }

    private void SetupDigitImages()
    {
        foreach (var scoreimages in digitImages)
        {
            scoreimages.GetComponent<RawImage>().texture = numberTexture;
        }
    }

    private void StoreInitialPositions()
    {
        digitsInitialPositions = new Vector2[digitImages.Length];
        for (int digitsCnt = 0; digitsCnt < digitImages.Length; digitsCnt++)
        {
            digitsInitialPositions[digitsCnt] = digitImages[digitsCnt].anchoredPosition;
        }
    }

    private void UpdateScoreDisplay()
    {
        if (CountUpAnimation != null && CountUpAnimation.IsActive())
        {
            CountUpAnimation.Kill();
        }

        int scoreDifference = Score - previousScore;
        if (scoreDifference != 0 && displayScoreHistory != null) displayScoreHistory.AddScoreHistory(scoreDifference);

        int[] digits = GetAllDigits(Score);
        CountUpAnimation = DOTween.Sequence();

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
