using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("レイを飛ばしたいカメラ")]
    [SerializeField] private Camera m_playerCamera;

    [Header("インタラクト可能な距離")]
    [SerializeField] private float m_interactableDistance = 1.5f;

    [Header("パンチの射程")]
    [SerializeField] private float m_punchReachableDistance = 1f;

    [Header("パンチのクールダウン時間（ミリ秒）")]
    [SerializeField] private int m_punchCooldownTime = 1000;

    [Header("正面から左右に何度までをキャラクターの正面と見做すか")]
    [Range(0f, 360f)]
    [SerializeField] private float m_flontRange = 120f;

    [Header("宝箱開錠の射程")]
    [SerializeField] private float m_chestReachableDistance = 1f;

    [Header("クリックによるインタラクトを有効化する。ただしタッチを受け付けなくなるので注意。")]
    [SerializeField] private bool m_clickIsAvailable;

    //パンチのクールダウン管理用
    private bool m_isPunchable = true;

    /// <summary>
    /// パラメータで指定されたカメラを基準にインタラクトを実行するよ。インタラクトの情報はタプルで返却するよ。
    /// </summary>
    /// <returns>成立したインタラクト種別、パケット送信に必要な相手のSessionIDやEntityID、実行が成立した魔法のID、背面取りが成立したパンチのベクトル</returns>
    public (INTERACT_TYPE interactType, ushort targetID, Definer.MID magicID, Vector3 punchHitVec) Interact()
    {
        //返り値宣言
        INTERACT_TYPE interactType = INTERACT_TYPE.NONE;
        ushort targetID = 0;
        Definer.MID magicID = Definer.MID.NONE;
        Vector3 punchHitVec = Vector3.zero;

        if (!m_clickIsAvailable) //タッチ版インタラクト処理
        {
            //どこかしらタッチされているなら
            if (Input.touchCount > 0)
            {
                foreach (Touch t in Input.touches)
                {
                    //タッチし始めたフレームでないなら処理しない
                    if (t.phase != TouchPhase.Began) continue;

                    //カメラの位置からタッチした位置に向けrayを飛ばす
                    RaycastHit hit;
                    Ray ray = m_playerCamera.ScreenPointToRay(t.position);

                    //rayがなにかに当たったら調べる
                    if (Physics.Raycast(ray, out hit, m_interactableDistance))
                    {
                        switch (hit.collider.gameObject.tag)
                        {
                            case "Enemy": //プレイヤーならパンチ
                                //クールダウン中なら終了
                                if (!m_isPunchable) break;

                                //パンチ射程外なら終了
                                if (hit.distance > m_punchReachableDistance)
                                {
                                    interactType = INTERACT_TYPE.ENEMY_MISS;
                                    break;
                                }

                                //ActorControllerの取得
                                ActorController actorController = hit.collider.gameObject.GetComponent<ActorController>();

                                //パンチが正面に当たったのか背面に当たったのか調べる
                                Vector3 punchVec = hit.point - this.transform.position;
                                float angle = Vector3.Angle(punchVec, actorController.transform.forward);

                                //パンチの結果
                                if (angle < m_flontRange) interactType = INTERACT_TYPE.ENEMY_FRONT;
                                else interactType = INTERACT_TYPE.ENEMY_BACK;
                                //パンチした相手のsessionID
                                targetID = actorController.SessionID;
                                //パンチのベクトル
                                punchHitVec = punchVec;

                                //パンチしたならクールダウン実行
                                UniTask u = UniTask.RunOnThreadPool(() => PunchCoolDown());

                                break;

                            case "Chest": //宝箱なら開錠を試みる
                                //宝箱射程外なら終了
                                if (hit.distance > m_chestReachableDistance) break;

                                //インタラクト結果
                                interactType = INTERACT_TYPE.CHEST;
                                //宝箱のID
                                targetID = hit.collider.gameObject.GetComponent<Chest>().EntityID;

                                break;

                            case "MagicIcon": //魔法のUIならその魔法の巻物を開く
                                //インタラクト結果
                                interactType = INTERACT_TYPE.MAGIC_ICON;
                                break;

                            case "MagicUse":
                                //インタラクト結果
                                interactType = INTERACT_TYPE.MAGIC_USE;
                                break;

                            case "MagicCancel":
                                //インタラクト結果
                                interactType = INTERACT_TYPE.MAGIC_CANCEL;
                                break;

                            default: //そうでないものはインタラクト不可能なオブジェクトなので無視
                                break;
                        }
                    }
                }
            }
        }
        else //クリック版インタラクト処理
        {
            //左クリックされているなら
            if (Input.GetMouseButtonDown(0))
            {
                //カメラの位置からクリックした位置に向けrayを飛ばす
                RaycastHit hit;
                Ray ray = m_playerCamera.ScreenPointToRay(Input.mousePosition);

                //rayがなにかに当たったら調べる
                if (Physics.Raycast(ray, out hit, m_interactableDistance))
                {
                    switch (hit.collider.gameObject.tag)
                    {
                        case "Enemy": //プレイヤーならパンチ
                            //クールダウン中なら終了
                            if (!m_isPunchable) break;

                            //パンチ射程外なら終了
                            if (hit.distance > m_punchReachableDistance)
                            {
                                interactType = INTERACT_TYPE.ENEMY_MISS;
                                break;
                            }

                            //ActorControllerの取得
                            ActorController actorController = hit.collider.gameObject.GetComponent<ActorController>();

                            //パンチが正面に当たったのか背面に当たったのか調べる
                            Vector3 punchVec = hit.point - this.transform.position;
                            float angle = Vector3.Angle(punchVec, actorController.transform.forward);

                            //パンチの結果
                            if (angle < m_flontRange) interactType = INTERACT_TYPE.ENEMY_FRONT;
                            else interactType = INTERACT_TYPE.ENEMY_BACK;
                            //パンチした相手のsessionID
                            targetID = actorController.SessionID;
                            //パンチのベクトル
                            punchHitVec = punchVec;

                            //パンチしたならクールダウン実行
                            UniTask u = UniTask.RunOnThreadPool(() => PunchCoolDown());

                            break;

                        case "Chest": //宝箱なら開錠を試みる
                                      //宝箱射程外なら終了
                            if (hit.distance > m_chestReachableDistance) break;

                            //インタラクト結果
                            interactType = INTERACT_TYPE.CHEST;
                            //宝箱のID
                            targetID = hit.collider.gameObject.GetComponent<Chest>().EntityID;

                            break;

                        case "MagicIcon": //魔法のUIならその魔法の巻物を開く
                                          //インタラクト結果
                            interactType = INTERACT_TYPE.MAGIC_ICON;
                            break;

                        case "MagicUse":
                            //インタラクト結果
                            interactType = INTERACT_TYPE.MAGIC_USE;
                            break;

                        case "MagicCancel":
                            //インタラクト結果
                            interactType = INTERACT_TYPE.MAGIC_CANCEL;
                            break;

                        default: //そうでないものはインタラクト不可能なオブジェクトなので無視
                            break;
                    }
                }
            }
        }

        //タプルで返却
        return (interactType, targetID, magicID, punchHitVec);

        //一定時間パンチができなくなるローカル関数
        async void PunchCoolDown()
        {
            m_isPunchable = false; //クールダウン開始
            await UniTask.Delay(m_punchCooldownTime); //指定された秒数待ったら
            m_isPunchable = true; //クールダウン終了
        }
    }
}
