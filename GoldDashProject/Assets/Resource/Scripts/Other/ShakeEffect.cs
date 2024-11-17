using UnityEngine;
using DG.Tweening;

public class ShakeEffect : MonoBehaviour
{
    public enum ShakeType
    {
        Small,
        Medium,
        Large
    }

    [Header("CameraShakeSetting")]
    [SerializeField] Transform shakeCameraTransform;

    [Header("Small Camera Shake Settings")]
    [SerializeField] Vector3 smallPositionStrength;
    [SerializeField] Vector3 smallRotationStrength;
    [SerializeField] float smallShakeDuration = 0.2f;

    [Header("Medium Camera Shake Settings")]
    [SerializeField] Vector3 mediumPositionStrength;
    [SerializeField] Vector3 mediumRotationStrength;
    [SerializeField] float mediumShakeDuration = 0.4f;

    [Header("Large Camera Shake Settings")]
    [SerializeField] Vector3 largePositionStrength;
    [SerializeField] Vector3 largeRotationStrength;
    [SerializeField] float largeShakeDuration = 0.6f;

    public void ShakeCameraEffect(ShakeType shakeType)
    {
        shakeCameraTransform.DOComplete();

        switch (shakeType)
        {
            case ShakeType.Small:
                ApplyShake(smallShakeDuration, smallPositionStrength, smallRotationStrength);
                break;

            case ShakeType.Medium:
                ApplyShake(mediumShakeDuration, mediumPositionStrength, mediumRotationStrength);
                break;

            case ShakeType.Large:
                ApplyShake(largeShakeDuration, largePositionStrength, largeRotationStrength);
                break;
        }
    }

    private void ApplyShake(float duration, Vector3 positionStrength, Vector3 rotationStrength)
    {
        shakeCameraTransform.DOShakePosition(duration, positionStrength);
        shakeCameraTransform.DOShakeRotation(duration, rotationStrength);
    }
}