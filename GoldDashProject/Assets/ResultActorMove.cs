using UnityEngine;
using DG.Tweening;

public class ResultActorMove : MonoBehaviour
{
    [SerializeField] Transform centerPoint;
    float ActorAngle = -50f;

    [SerializeField] Rigidbody actorRig;
    const string StopMethod = "StopActorMove";
    const string MoveCenterMethod = "MovetoCenterPoint";
    float SegmentAnimationTime = 5.5f;
    private bool isMoving = true;
    private bool isFinishAnimation = false;
    [SerializeField] float forcePower = 10f;

    [SerializeField] Animator ResultActorAnimator;
    void Start()
    {
        // 一度だけInvokeを設定
        Invoke(StopMethod, SegmentAnimationTime);
    }

    void Update()
    {
        if (isMoving && !isFinishAnimation)
        {
            transform.RotateAround(centerPoint.position, Vector3.up, ActorAngle * Time.deltaTime);
        }
    }

    void StopActorMove()
    {
        isMoving = false; // フラグを変更して動きを停止
        isFinishAnimation = true;
        actorRig.velocity = Vector3.zero;
        actorRig.angularVelocity = Vector3.zero;
        ResultActorAnimator.enabled = false;
        Debug.Log("動くな");
        Vector3 forceVec = gameObject.transform.position - centerPoint.position;

        if (gameObject.CompareTag("Player")) Invoke(MoveCenterMethod, 0.2f);
        else
        {
            actorRig.AddForce(forceVec * forcePower, ForceMode.Impulse);
            Debug.Log("それ以外は吹っ飛べ");
        }
    }

    void MovetoCenterPoint()
    {
        Debug.Log("一位が中心へ");
        isMoving = true;
        transform.LookAt(centerPoint, Vector3.right);
        ResultActorAnimator.enabled = true;
        this.transform.DOMove(centerPoint.position, 1f).SetEase(Ease.Linear);
    }
}