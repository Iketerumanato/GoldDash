using UnityEngine;

public class Tresure : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {           
            CanvasFade._canvusfadeIns.FadeInImage();
            //Debug.Log("Player���ڐG���܂���");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            CanvasFade._canvusfadeIns.FadeOutImage();
            //Debug.Log("Player���󔠂͈̔͂��o�܂���");
        }
    }
}