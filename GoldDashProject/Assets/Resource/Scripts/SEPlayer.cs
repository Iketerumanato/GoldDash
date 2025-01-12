using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SEPlayer : MonoBehaviour
{
    //シングルトン化
    public static SEPlayer instance;

    //再生用コンポーネント
    public AudioSource audioSource;

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

    [Header("雷が落ちた時のSE")]
    [SerializeField] private AudioClip seThunder;

    [Header("ダッシュが発動したときのSE")]
    [SerializeField] private AudioClip seDash;
    
    [Header("誰かがワープしたときのSE")]
    [SerializeField] private AudioClip seWarp;

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
}
