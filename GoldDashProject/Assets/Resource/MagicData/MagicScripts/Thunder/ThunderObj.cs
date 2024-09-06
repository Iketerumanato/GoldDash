using System.Threading.Tasks;
using UnityEngine;

public class ThunderObj : MonoBehaviour
{
    [SerializeField] float fadeDuration = 2f; // �t���b�V���̌�������
    [SerializeField] Light FlashLightPrefab;
    [SerializeField] GameObject FlashImagePrefab;
    private Camera mainCamera;

    private void Start()
    {
        // ���C���J�����̎擾
        mainCamera = Camera.main;

        // ���C�g���J�����̎��E�ɂ��邩���`�F�b�N
        if (IsInCameraView(transform.position, mainCamera) && mainCamera != null)
        {
            Debug.Log("���C�g�̓J�����̎��E�ɂ���܂�");
            GameObject flashInstance = Instantiate(FlashImagePrefab);
            flashInstance.transform.SetParent(this.transform, false);
        }
        else Debug.Log("���C�g�̓J�����̎��E�ɂ���܂���");

        FadeOutLightAsync(FlashLightPrefab, fadeDuration);
    }

    public async void FadeOutLightAsync(Light light, float duration)
    {
        // �t�F�[�h�A�E�g����
        float startIntensity = light.intensity;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            light.intensity = Mathf.Lerp(startIntensity, 0, elapsedTime / duration);
            await Task.Yield();
        }
    }

    private bool IsInCameraView(Vector3 position, Camera camera)
    {
        // ���[���h���W����X�N���[�����W�ɕϊ�
        Vector3 screenPoint = camera.WorldToViewportPoint(position);

        // �X�N���[�����W��0����1�͈͓̔��ɂ��邩���`�F�b�N
        return (screenPoint.x >= 0 && screenPoint.x <= 1 &&
                screenPoint.y >= 0 && screenPoint.y <= 1 &&
                screenPoint.z > 0); // z > 0 �̓J�����̑O�ɂ��邱�Ƃ��m�F
    }
}