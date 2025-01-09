using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GokouRotate : MonoBehaviour
{
    void Update()
    {
        this.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
