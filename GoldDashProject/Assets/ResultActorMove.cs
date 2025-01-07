using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

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
    const string isResultGame = "IsResultGame";

    [SerializeField] ResultImage _resultImage;

    const string WinerActorTag = "1stActor";

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

        if (gameObject.CompareTag(WinerActorTag)) Invoke(MoveCenterMethod, 0.2f);
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
        // 現在のオブジェクトとターゲットの方向ベクトルを計算
        Vector3 directionToTarget = transform.position - centerPoint.position;
        // LookAtでその方向を向かせる
        transform.rotation = Quaternion.LookRotation(directionToTarget);
        ResultActorAnimator.enabled = true;
        this.transform.DOMove(centerPoint.position, 1f).SetEase(Ease.Linear).OnComplete(() =>
        {
            //transform.position = centerPoint.position;
            transform.eulerAngles = Vector3.zero;
            ResultActorAnimator.SetBool(isResultGame, true);
            _resultImage.ResultTextCanvasAlphaToMax(5f, 0.5f);
        });
    }
}