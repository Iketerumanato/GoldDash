using UnityEngine;

public class MapTouch : MonoBehaviour
{
    public float distance = 100f;
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit = new RaycastHit();

            if (Physics.Raycast(ray, out hit, distance))
            {
                string objectName = hit.collider.gameObject.name;
                Debug.Log(objectName);
            }
        }
    }
}
