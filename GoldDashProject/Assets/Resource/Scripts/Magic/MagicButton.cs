using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MagicButton : MonoBehaviour
{
    [SerializeField] MagicManagement magicManagement;
    [SerializeField] int magicIndex;
    [SerializeField] Button magicbutton;

    void Start()
    {
        magicbutton.onClick.AddListener(() => magicManagement.ActivateMagic(magicIndex));
    }
}