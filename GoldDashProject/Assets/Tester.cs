using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tester : MonoBehaviour
{
    [SerializeField] GameObject player;

    // Start is called before the first frame update
    void Start()
    {
        Instantiate(player, new Vector3(2f, 0.5f, 2f), Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
