using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using R3;


public class TestUIManager : MonoBehaviour
{
    //最初から表示されているボタン
    [SerializeField] private Button buttonServerMode;
    [SerializeField] private Button buttonClientMode;
    [SerializeField] private Button buttonQuitApp;

    //各モード用ボタン
    [SerializeField] private Button buttonConnect;
    [SerializeField] private Button buttonDisconnect;

    [SerializeField] private Button buttonActivate;
    [SerializeField] private Button buttonDeactivate;

    [SerializeField] private Button buttonQuitMode;


    private void Start()
    {
        //buttonServerMode.OnClickAsObservable().Subscribe();
    }
}
