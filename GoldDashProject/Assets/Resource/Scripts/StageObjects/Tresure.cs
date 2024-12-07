using UnityEngine;

public class Tresure : MonoBehaviour
{
    private UIFade uiFade;
    private PlayerController _playerController;
    const string playerTagName = "Player";

    private void Start()
    {
        uiFade = FindObjectOfType<UIFade>();
        _playerController = FindObjectOfType<PlayerController>();
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.gameObject.CompareTag(playerTagName))
    //    {
    //        _playerController.isControllCam = false;
    //        uiFade.FadeInImage();
    //    }
    //}

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag(playerTagName))
        {
            _playerController.isControllCam = true;
            uiFade.FadeOutImage();
        }
    }
}