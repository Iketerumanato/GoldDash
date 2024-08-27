using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flash : MonoBehaviour
{
    [SerializeField] float fuseTime = 3f;
    [SerializeField] GameObject FlashPrefab;
    // Start is called before the first frame update
    void Start()
    {
        Invoke("Explode", fuseTime);
    }

    void Explode()
    {
        Destroy(gameObject);
        Destroy(Instantiate(FlashPrefab, transform.position, Quaternion.identity), 5);
    }
}
