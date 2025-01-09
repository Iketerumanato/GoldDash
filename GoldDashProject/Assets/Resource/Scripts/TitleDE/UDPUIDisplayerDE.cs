using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UDPUIDisplayerDE : MonoBehaviour
{
    [Header("暗転用黒イメージ")]
    [SerializeField] private GameObject blackImage;

    [Header("サーバー用UI")]
    [SerializeField] private GameObject redArrow;
    [SerializeField] private GameObject redArrowBack;
    [SerializeField] private GameObject redName;

    [SerializeField] private GameObject blueArrow;
    [SerializeField] private GameObject blueArrowBack;
    [SerializeField] private GameObject blueName;

    [SerializeField] private GameObject greenArrow;
    [SerializeField] private GameObject greenArrowBack;
    [SerializeField] private GameObject greenName;

    [SerializeField] private GameObject yellowArrow;
    [SerializeField] private GameObject yellowArrowBack;
    [SerializeField] private GameObject yellowName;

    [SerializeField] private GameObject colorType1;
    [SerializeField] private GameObject colorType2;

    [SerializeField] private GameObject timeLimitLeft;
    [SerializeField] private GameObject timeLimitRight;

    [SerializeField] private GameObject gameStartButton;

    [SerializeField] private GameObject textBox;

    [Header("サーバー用UI")]
    [SerializeField] private GameObject titleLogo;
    [SerializeField] private GameObject spinLogo;
    [SerializeField] private GameObject touchToStart;

    [SerializeField] private GameObject backArrow;
    [SerializeField] private GameObject backArrowBack;

    [SerializeField] private GameObject nameBox;
    [SerializeField] private GameObject nameEnter;

    [SerializeField] private GameObject largeTextBox;
    [SerializeField] private GameObject upperTextBox;
    [SerializeField] private GameObject lowerTextBox;

    [SerializeField] private GameObject backButton;
}
