using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SEPlayer : MonoBehaviour
{
    //シングルトン化
    public static SEPlayer instance;

    //再生用コンポーネント
    private AudioSource audioSource;

    [Header("パンチが外れたときのSE")]
    [SerializeField] private AudioClip sePunchMiss;

    [Header("パンチが正面に当たったときのSE")]
    [SerializeField] private AudioClip sePunchHitFront;

    [Header("パンチが背中に当たったときのSE")]
    [SerializeField] private AudioClip sePunchHitBack;

    [Header("金貨がこぼれた時のSE")]
    [SerializeField] private AudioClip seDropGold;

    [Header("金貨を拾った時のSE")]
    [SerializeField] private AudioClip seGetGold;

    [Header("宝箱に触れたときのSE")]
    [SerializeField] private AudioClip seAccessChest;

    [Header("宝箱が開いた時のSE")]
    [SerializeField] private AudioClip seOpenChest;

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

    public void PlaySEDropGold()
    {
        audioSource.PlayOneShot(seDropGold);
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
}
