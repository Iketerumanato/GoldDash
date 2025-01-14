using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SEPlayer : MonoBehaviour
{
    //シングルトン化
    public static SEPlayer instance;

    //再生用コンポーネント
    public AudioSource audioSource;

    public AudioSource titleBGMPlayer;
    public AudioSource mainBGMPlayer;

    [Header("ボタンが押されたときのSE")]
    [SerializeField] private AudioClip seButton;

    [Header("タイトル画面でタッチしたときのSE")]
    [SerializeField] private AudioClip seTouchToStart;

    [Header("パンチが外れたときのSE")]
    [SerializeField] private AudioClip sePunchMiss;

    [Header("パンチが正面に当たったときのSE")]
    [SerializeField] private AudioClip sePunchHitFront;

    [Header("パンチが背中に当たったときのSE")]
    [SerializeField] private AudioClip sePunchHitBack;

    [Header("金貨がこぼれた時のSE:小")]
    [SerializeField] private AudioClip seDropGold_S;

    [Header("金貨がこぼれた時のSE:中")]
    [SerializeField] private AudioClip seDropGold_M;

    [Header("金貨がこぼれた時のSE:大")]
    [SerializeField] private AudioClip seDropGold_L;

    [Header("金貨を拾った時のSE")]
    [SerializeField] private AudioClip seGetGold;

    [Header("宝箱に触れたときのSE")]
    [SerializeField] private AudioClip seAccessChest;

    [Header("宝箱が開いた時のSE")]
    [SerializeField] private AudioClip seOpenChest;

    [Header("宝物の鍵5段階")]
    [SerializeField] private AudioClip seUnlock1;
    [SerializeField] private AudioClip seUnlock2;
    [SerializeField] private AudioClip seUnlock3;
    [SerializeField] private AudioClip seUnlock4;
    [SerializeField] private AudioClip seUnlock5;

    [Header("雷に撃たれたときのSE")]
    [SerializeField] private AudioClip seParalysed;

    //以下サーバー専用
    [Header("雷が落ちた時のSE")]
    [SerializeField] private AudioClip seThunder;

    [Header("ダッシュが発動したときのSE")]
    [SerializeField] private AudioClip seDash;
    
    [Header("誰かがワープしたときのSE")]
    [SerializeField] private AudioClip seWarp;

    [Header("誰かがログインしたときのSE4種")]
    [SerializeField] private AudioClip seLogInRed;
    [SerializeField] private AudioClip seLogInBlue;
    [SerializeField] private AudioClip seLogInGreen;
    [SerializeField] private AudioClip seLogInYellow;

    [Header("地図が生成されたときのSE")]
    [SerializeField] private AudioClip seGenerateCell;

    private void Start()
    {
        //シングルトンな静的変数の初期化
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        //コンポーネント取得
        audioSource = GetComponent<AudioSource>();
    }

    public void PlaySEButton()
    {
        audioSource.PlayOneShot(seButton);
    }

    public void PlaySETouchToStart()
    {
        audioSource.PlayOneShot(seTouchToStart);
    }

    public void PlaySEPunchMiss()
    {
        audioSource.PlayOneShot(sePunchMiss);
    }

    public void PlaySEPunchHitFront()
    {
        audioSource.PlayOneShot(sePunchHitFront);
    }

    public void PlaySEPunchHitBack()
    {
        audioSource.PlayOneShot(sePunchHitBack);
    }

    public void PlaySEDropGold_S()
    {
        audioSource.PlayOneShot(seDropGold_S);
    }

    public void PlaySEDropGold_M()
    {
        audioSource.PlayOneShot(seDropGold_M);
    }

    public void PlaySEDropGold_L()
    {
        audioSource.PlayOneShot(seDropGold_L);
    }

    public void PlaySEGetGold()
    {
        audioSource.PlayOneShot(seGetGold);
    }

    public void PlaySEAccessChest()
    {
        audioSource.PlayOneShot(seAccessChest);
    }

    public void PlaySEOpenChest()
    {
        audioSource.PlayOneShot(seOpenChest);
    }

    public void PlaySEUnlock1()
    {
        audioSource.PlayOneShot(seUnlock1);
    }

    public void PlaySEUnlock2()
    {
        audioSource.PlayOneShot(seUnlock2);
    }

    public void PlaySEUnlock3()
    {
        audioSource.PlayOneShot(seUnlock3);
    }

    public void PlaySEUnlock4()
    {
        audioSource.PlayOneShot(seUnlock4);
    }

    public void PlaySEUnlock5()
    {
        audioSource.PlayOneShot(seUnlock5);
    }

    public void PlaySEParayzed()
    {
        audioSource.PlayOneShot(seParalysed);
    }

    //以下サーバー用
    public void PlaySEThunder()
    {
        audioSource.PlayOneShot(seThunder);
    }

    public void PlaySEDash()
    {
        audioSource.PlayOneShot(seDash);
    }

    public void PlaySEWarp()
    {
        audioSource.PlayOneShot(seWarp);
    }

    public void PlaySELoginRed()
    {
        audioSource.PlayOneShot(seLogInRed);
    }

    public void PlaySELoginBlue()
    {
        audioSource.PlayOneShot(seLogInBlue);
    }

    public void PlaySELoginGreen()
    {
        audioSource.PlayOneShot(seLogInGreen);
    }

    public void PlaySELoginYellow()
    {
        audioSource.PlayOneShot(seLogInYellow);
    }

    public void PlaySEGenerateCell()
    {
        audioSource.PlayOneShot(seGenerateCell);
    }
}
