using UnityEngine;

public class Tresure : MonoBehaviour
{
    private UIFade uiFade;
    private CameraControll cameraControll;

    private void Start()
    {
        uiFade = FindObjectOfType<UIFade>();
        cameraControll = FindObjectOfType<CameraControll>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            cameraControll.OffCameraControll();
            uiFade.FadeInImage();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            cameraControll.ActiveCameraControll();
            uiFade.FadeOutImage();
        }
    }
}