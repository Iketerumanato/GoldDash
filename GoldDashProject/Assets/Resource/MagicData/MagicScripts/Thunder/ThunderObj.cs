using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class ThunderObj : MonoBehaviour
{
    [SerializeField] float fadeDuration = 2f; // �t���b�V���̌�������
    [SerializeField] Light FlashLightPrefab;
    [SerializeField] GameObject FlashImagePrefab;
    Camera mainCamera;
    CancellationTokenSource cancellationToken;

    private void Start()
    {
        // ���C���J�����̎擾
        mainCamera = Camera.main;
        cancellationToken = new CancellationTokenSource();

        // ���C�g���J�����̎��E�ɂ��邩���`�F�b�N
        if (IsInCameraView(transform.position, mainCamera) && mainCamera != null) FlashImagePrefab.SetActive(true);

        FadeOutLightAsync(FlashLightPrefab, fadeDuration, cancellationToken.Token).Forget();
    }

    private async UniTask FadeOutLightAsync(Light light, float duration, CancellationToken token)
    {
        // �t�F�[�h�A�E�g����
        float startIntensity = light.intensity;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            light.intensity = Mathf.Lerp(startIntensity, 0, elapsedTime / duration);
            await UniTask.Yield(token);
        }
    }

    private bool IsInCameraView(Vector3 targetposition, Camera playercamera)
    {
        // ���[���h���W����X�N���[�����W�ɕϊ�
        Vector3 screenPoint = playercamera.WorldToViewportPoint(targetposition);

        // �X�N���[�����W��0����1�͈͓̔��ɂ��邩���`�F�b�N
        return (screenPoint.x >= 0 && screenPoint.x <= 1 &&
                screenPoint.y >= 0 && screenPoint.y <= 1 &&
                screenPoint.z > 0); // z > 0 �̓J�����̑O�ɂ��邱�Ƃ��m�F
    }

    //�j�󎞂�UniTask���L�����Z��
    private void OnDestroy()
    {
        cancellationToken.Cancel();
    }
}