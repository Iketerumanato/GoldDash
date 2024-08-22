using UnityEngine;

public class Tresure : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {           
            CanvasFade._canvusfadeIns.FadeInImage();
            //Debug.Log("Player‚ªÚG‚µ‚Ü‚µ‚½");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            CanvasFade._canvusfadeIns.FadeOutImage();
            //Debug.Log("Player‚ª•ó” ‚Ì”ÍˆÍ‚ğo‚Ü‚µ‚½");
        }
    }
}