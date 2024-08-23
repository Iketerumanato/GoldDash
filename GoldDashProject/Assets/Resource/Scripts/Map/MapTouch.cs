using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapTouch : MonoBehaviour
{
    // ray���͂��͈�
    public float distance = 100f;
    void Update()
    {
        // ���N���b�N���擾
        if (Input.GetMouseButtonDown(0))
        {
            // �N���b�N�����X�N���[�����W��ray�ɕϊ�
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            // Ray�̓��������I�u�W�F�N�g�̏����i�[����
            RaycastHit hit = new RaycastHit();
            // �I�u�W�F�N�g��ray������������
            if (Physics.Raycast(ray, out hit, distance))
            {
                // ray�����������I�u�W�F�N�g�̖��O���擾
                string objectName = hit.collider.gameObject.name;
                Debug.Log(objectName);
            }
        }
    }
}
