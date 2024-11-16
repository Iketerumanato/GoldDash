using UnityEngine;
using DG.Tweening;

public class ShakeEffect : MonoBehaviour
{
    [Header("CameraShakeSetting")]
    [SerializeField] Transform shakeCameraTransform;
    [SerializeField] Vector3 cameraPositionStrength;
    [SerializeField] Vector3 cameraRotationStrength;
    [SerializeField] float shakeCameraDuration = 0.3f;

    public void ShakeCameraEffect()
    {
        shakeCameraTransform.DOComplete();
        shakeCameraTransform.DOShakePosition(shakeCameraDuration, cameraPositionStrength);
        shakeCameraTransform.DOShakeRotation(shakeCameraDuration, cameraRotationStrength);
    }
}
