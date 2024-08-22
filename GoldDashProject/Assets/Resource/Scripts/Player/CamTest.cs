using UnityEngine;

public class CamTest : MonoBehaviour
{
    private GameObject ParentObj;

    private Vector3 Position;

    private RaycastHit Hit;

    private float Distance;

    private int Mask;

    void Start()
    {
        ParentObj = transform.root.gameObject;

        Position = transform.localPosition;

        Distance = Vector3.Distance(ParentObj.transform.position, transform.position);

        Mask = ~(1 << LayerMask.NameToLayer("Player"));
    }

    void Update()
    {
        if (Physics.CheckSphere(ParentObj.transform.position, 0.3f, Mask))
        {
            transform.position = Vector3.Lerp(transform.position, ParentObj.transform.position, 1);
        }
        else if (Physics.SphereCast(ParentObj.transform.position, 0.3f, (transform.position - ParentObj.transform.position).normalized, out Hit, Distance, Mask))
        {
            transform.position = ParentObj.transform.position + (transform.position - ParentObj.transform.position).normalized * Hit.distance;
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, Position, 1);
        }
    }
}
