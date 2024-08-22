using UnityEngine;

public class SetHostCamera : MonoBehaviour
{
    [SerializeField] Camera hostCamera;

    public void OnStartServer()
    {
        hostCamera.gameObject.SetActive(true);
    }

    public void OnStartClient()
    {
        hostCamera.gameObject.SetActive(false);
    }
}