using UnityEngine;
using System.Collections.Generic;

public class DrawCircle : MonoBehaviour
{
    readonly private List<Vector2> drawpoints = new();
    private Vector2 center = Vector2.zero;
    private float angleSum = 0;
    private int circleCount = 0;
    private float previousSign = 0;

    [SerializeField] RectTransform drawPanel;

    MagicList magicList;

    [SerializeField] MagicManagement _magicmanagement;

    private void Start()
    {
        magicList = FindObjectOfType<MagicList>();
    }

    void Update()
    {
        #region �~��`��
        if (Input.GetMouseButtonDown(0))
        {
            drawpoints.Clear();
            angleSum = 0;
            previousSign = 0;
        }

        if (Input.GetMouseButton(0))
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(drawPanel, Input.mousePosition, null, out Vector2 localPoint);
            drawpoints.Add(localPoint);

            // �|�C���g��2�ȏ゠��ꍇ�̂ݏ������s��
            if (drawpoints.Count > 1)
            {
                if (drawpoints.Count > 1) center = GetCenter(drawpoints);

                float angle = CalculateAngle(drawpoints, center);

                //���S�_�ƃN���b�N�����Ƃ��̓_�Ƃ̂Ȃ��p�����߂Ċp�x�̕ω��������`�F�b�N
                float sign = Mathf.Sign(Vector2.SignedAngle(drawpoints[drawpoints.Count - 2] - center, drawpoints[drawpoints.Count - 1] - center));
                if (previousSign == 0)
                {
                    previousSign = sign;
                }
                else if (sign != previousSign)
                {
                    Debug.Log("�p�x�̕������t�]���܂����B�J�E���g�����Z�b�g���܂�");
                    circleCount = 0;
                    angleSum = 0;
                    previousSign = sign;
                }

                angleSum += angle;

                if (angleSum >= 360f)
                {
                    circleCount++;
                    angleSum -= 360f;
                    Debug.Log("�~��������܂����I ���݂̃J�E���g: " + circleCount);
                    if (circleCount == 5)
                    {
                        Debug.Log("�󔠃I�[�v��");
                        CanvasFade._canvusfadeIns.FadeOutImage();
                        CameraControll._cameracontrollIns.ActiveCamera();
                        magicList.GrantRandomMagics(_magicmanagement);
                        circleCount = 0;
                    }
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            Debug.Log("�}�E�X�𗣂��܂����B�~�̃J�E���g�����Z�b�g���܂��B");
            circleCount = 0;
        }
        #endregion
    }

    #region ���S�_�̎擾
    Vector2 GetCenter(List<Vector2> points)
    {
        if (points.Count < 2)
            return Vector2.zero;

        Vector2 sum = Vector2.zero;
        foreach (var point in points)
        {
            sum += point;
        }
        return sum / points.Count;
    }
    #endregion

    #region �`���ꂽ�~�̊p�x�v�Z
    float CalculateAngle(List<Vector2> points, Vector2 center)
    {
        // �|�C���g��2�����̏ꍇ�A�p�x���v�Z���Ȃ�
        if (points.Count < 2)
            return 0;

        Vector2 prevVector = points[points.Count - 2] - center;
        Vector2 currentVector = points[points.Count - 1] - center;

        float angle = Vector2.SignedAngle(prevVector, currentVector);

        return Mathf.Abs(angle);
    }
    #endregion
}