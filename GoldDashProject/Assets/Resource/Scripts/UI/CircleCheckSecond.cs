using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleCheckSecond : MonoBehaviour
{
	private float angle;
    private float angleSum;
    private Vector3 swipePosition;
    private Vector3 previousSwipePosition;
    private Vector3 vector;
    private Vector3 previousVector;
    private int sign;
    private int previousSign;

    //�X���C�v�n�_�̊p�x�̑��a���A���̒l�𒴂����ꍇ�͉~��`�����ƌ��Ȃ��B
    static readonly float AngleSumThreshold = 330.0f;

    //�O�񂩂�̃^�b�`�n�_�̍����A���̋����������Ɩ�������(sqrMagnitude�Ȃ̂ŁA2��B���[���h���W�ł͂Ȃ��A�X�N���[�����W�Ȃ̂ő傫�߂�)�B
    static readonly float SwipeDeltaSqrThreshold = 15.0f * 15.0f;

    Coroutine checkCircleSwipeGesture;
    Coroutine checkCircleSwipeGestureMouseButtonUp;

    //�X���C�v�W�F�X�`���[�̃`�F�b�N�Ԋu�B
    static readonly WaitForSeconds CheckCircleSwipeGestureWait = new(0.05f);

    public LineRenderer lineRenderer;
    private List<Vector3> circlePoints = new List<Vector3>();
    private int pointsCount = 0;

    private void Start()
    {
        lineRenderer.positionCount = 0;
    }

    private void Update()
    {
        StartCoroutine(CheckCircleSwipeGesture());
    }

    // �Q�[���`�����A�W�F�X�`���[�`�F�b�N��t�J�n���ɁA1�񂾂��R�����ĂԁB
    public void StartCheckCircleSwipeGesture()
    {
        // �ꉞ�A�d���`�F�b�N���Ă���B
        if (checkCircleSwipeGesture != null)
        {
            StopCoroutine(checkCircleSwipeGesture);
        }

        checkCircleSwipeGesture = StartCoroutine(CheckCircleSwipeGesture());

        if (checkCircleSwipeGestureMouseButtonUp != null)
        {
            StopCoroutine(checkCircleSwipeGestureMouseButtonUp);
        }

        checkCircleSwipeGestureMouseButtonUp = StartCoroutine(CheckCircleSwipeGestureMouseButtonUp());
    }

    // ��~���ɂ́A�R�����ĂԁB
    public void StopCheckCircleSwipeGesture()
    {
        if (checkCircleSwipeGesture != null)
        {
            StopCoroutine(checkCircleSwipeGesture);
            checkCircleSwipeGesture = null;
        }

        if (checkCircleSwipeGestureMouseButtonUp != null)
        {
            StopCoroutine(checkCircleSwipeGestureMouseButtonUp);
            checkCircleSwipeGestureMouseButtonUp = null;
        }
    }

    // �v�Z�Ɏg�p����l�̏������B
    void ResetCheckCircleSwipeGesture()
    {
        previousSwipePosition = Input.mousePosition;
        angleSum = 0;

        vector = Vector3.zero;
        previousVector = Vector3.zero;

        sign = 0;
        previousSign = 0;

        lineRenderer.positionCount = 0;
        circlePoints.Clear();
        pointsCount = 0;
    }

    IEnumerator CheckCircleSwipeGesture()
    {
        ResetCheckCircleSwipeGesture();

        yield return CheckCircleSwipeGestureWait;

        while (true)
        {
            if (Input.GetMouseButton(0))
            {
                swipePosition = Input.mousePosition;

                if ((swipePosition - previousSwipePosition).sqrMagnitude < SwipeDeltaSqrThreshold)
                {
                    yield return CheckCircleSwipeGestureWait;
                    continue;
                }

                angle = Vector3.Angle(previousVector, vector);

                if (vector != Vector3.zero)
                    previousVector = vector;

                vector = swipePosition - previousSwipePosition;

                previousSwipePosition = swipePosition;

                if (previousVector != Vector3.zero && previousSign == 0)
                    previousSign = Vector3.Cross(previousVector, vector).z < 0 ? 1 : -1;

                if (previousVector != Vector3.zero)
                {
                    sign = Vector3.Cross(previousVector, vector).z < 0 ? 1 : -1;

                    // �t��]�ɂȂ��Ă���̂ŁA�p�x�̑��a�����Z�b�g�B
                    if (previousSign != sign)
                    {
                        previousSign = sign;
                        angleSum = 0;
                    }
                    else
                    {
                        angleSum += angle;
                    }
                }
                else
                {
                    angleSum += angle;
                }

                // LineRenderer�Ƀ|�C���g��ǉ�
                Vector3 worldPosition = Camera.main.ScreenToWorldPoint(swipePosition);
                worldPosition.z = 0;
                circlePoints.Add(worldPosition);
                lineRenderer.positionCount = circlePoints.Count;
                lineRenderer.SetPosition(pointsCount, worldPosition);
                pointsCount++;

                if (AngleSumThreshold <= angleSum)
                {
                    Debug.Log("�~��`������");

                    // �~��`�����Ɣ��肳�ꂽ���̏������R�R�ɋL�q�B
                    // previousSign�̒l������ƁA���v��肩�����v��肩������(-1 == �����v���A1 == ���v���)�B
                    ResetCheckCircleSwipeGesture();
                }
            }

            yield return CheckCircleSwipeGestureWait;
        }
    }

    // �X���C�v�����f���ꂽ����́A���t���[�����Ȃ��Ƃ����Ȃ��̂ŁA�ʃR���[�`���ɕ�����B
    IEnumerator CheckCircleSwipeGestureMouseButtonUp()
    {
        while (true)
        {
            if (Input.GetMouseButtonUp(0))
            {
                ResetCheckCircleSwipeGesture();
            }

            yield return null;
        }
    }
}