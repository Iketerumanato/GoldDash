using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;

[RequireComponent(typeof(AudioSource))]
public class TmpSoundManager : MonoBehaviour
{
    private AudioSource audioSource;

    [SerializeField] private AudioClip sePositive; //決定など
    [SerializeField] private AudioClip seNegative; //キャンセルなど

    [SerializeField] private AudioClip seActive; //起動
    [SerializeField] private AudioClip seDeactive; //停止

    public void InitObservation(UdpButtonManager udpUIManager)
    {
        udpUIManager.udpUIManagerSubject.Subscribe(e => ProcessUdpManagerEvent(e));
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void ProcessUdpManagerEvent(UdpButtonManager.UDP_BUTTON_EVENT e)
    {
        switch (e)
        {
            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_START_SERVER_MODE:
                audioSource.PlayOneShot(sePositive);
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_SERVER_ACTIVATE:
                audioSource.PlayOneShot(seActive);
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_SERVER_DEACTIVATE:
                audioSource.PlayOneShot(seDeactive);
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_START_CLIENT_MODE:
                audioSource.PlayOneShot(sePositive);
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_CLIENT_CONNECT:
                audioSource.PlayOneShot(seActive);
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_CLIENT_DISCONNECT:
                audioSource.PlayOneShot(seDeactive);
                break;

            case UdpButtonManager.UDP_BUTTON_EVENT.BUTTON_BACK_TO_SELECT:
                audioSource.PlayOneShot(seNegative);
                break;

            default:
                break;
        }
    }
}
