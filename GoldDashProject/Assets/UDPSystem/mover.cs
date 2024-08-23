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
            pos.x += Time.deltaTime * speed; // speedは移動速度
            transform.position = pos;

            if (pos.x > 6) // 終点（自由に変更可能）
            {
                isStop = true;
            }
        }
        else
        {
            pos.x -= Time.deltaTime * speed;
            transform.position = pos;

            if (pos.x < 0) // 始点（自由に変更可能）
            {
                isStop = false;
            }
        }
    }
}
