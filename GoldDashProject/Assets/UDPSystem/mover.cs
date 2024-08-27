using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class mover : MonoBehaviour
{
    public float speed;
    private Vector3 pos;
    private bool isStop = false;

    void Start()
    {
        pos = transform.position;
    }

    void Update()
    {
        if (!isStop)
        {
            pos.x += Time.deltaTime * speed; // speed�͈ړ����x
            transform.position = pos;

            if (pos.x > 6) // �I�_�i���R�ɕύX�\�j
            {
                isStop = true;
            }
        }
        else
        {
            pos.x -= Time.deltaTime * speed;
            transform.position = pos;

            if (pos.x < 0) // �n�_�i���R�ɕύX�\�j
            {
                isStop = false;
            }
        }
    }
}
