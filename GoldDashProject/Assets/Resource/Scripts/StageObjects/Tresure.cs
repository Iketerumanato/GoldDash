using UnityEngine;

public class Tresure : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {           
            CanvasFade._canvusfadeIns.FadeInImage();
            CameraControll._cameracontrollIns.OffCamera();
            //Debug.Log("Player���ڐG���܂���");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            CanvasFade._canvusfadeIns.FadeOutImage();
            CameraControll._cameracontrollIns.ActiveCamera();
            //Debug.Log("Player���󔠂͈̔͂��o�܂���");
        }
    }
}