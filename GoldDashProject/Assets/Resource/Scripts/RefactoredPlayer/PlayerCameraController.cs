using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    //制御対象のカメラをインスペクタからアタッチ
    [Header("回転させたいカメラ")]
    [SerializeField] private Camera m_playerCamera;

    [Header("カメラに振動を与えるコンポーネント")]
    [SerializeField] private ShakeEffect m_shakeEffect;

    //パラメータ
    [Header("カメラ回転速度（度／毎秒）")]
    [SerializeField] private float m_cameraMoveSpeed = 1f;

    [Header("カメラの縦方向（X軸中心）回転の上向き角度制限")]
    [Range(0f, 90f)]
    [SerializeField] private float m_camRotateLimitX_Upper = 90f;

    [Header("カメラの縦方向（X軸中心）回転の下向き角度制限")]
    [Range(0f, 90f)]
    [SerializeField] private float m_camRotateLimitX_Lower = 90f;

    [Header("右スティックを動かしたときの視点移動速度の加速具合")]
    [Range(0f, 0.99f)]
    [SerializeField] private float m_camRotateAcceleration = 0f;

    private float AccelerateInput(float input)
    {
        float absInput = input < 0 ? -input : input;

        float ret = (m_camRotateAcceleration * absInput * absInput) + ((1 - m_camRotateAcceleration) * absInput);
        //Debug.Log($"input: {absInput} return: {ret}");

        return input < 0 ? -ret : ret;
    }

    //現在のカメラのオイラー角をxのみ保存
    private float m_rotationX;

    private void Start()
    {
        if (m_playerCamera == null) Debug.LogError("プレイヤーカメラがアタッチされていないよ～！"); //アタッチ漏れ検出
        if (m_shakeEffect == null) Debug.LogError("シェイクエフェクトがアタッチされていないよ～！"); //アタッチ漏れ検出
        m_rotationX = this.transform.rotation.eulerAngles.x; //オイラー角の初期化
    }

    /// <summary>
    /// 垂直方向のInputを受け取ってカメラを縦に回転させるよ。横方向の回転はプレイヤーの旋回に紐づいて行われるからここでは実行しないよ。
    /// </summary>
    /// <param name="D_InputVertcal">DynamicJoystickの垂直方向入力</param>
    public void RotateCamara(float D_InputVertcal)
    {
        //カメラ操作の入力がないなら回転しない
        if (!Mathf.Approximately(D_InputVertcal, 0)) //水平方向の入力が"ほぼ0"でないなら
        {
            //ジョイスティックの入力をオイラー角（〇軸を中心に△度回転、という書き方）にする
            //前提：カメラはZ軸の負の方向を向いている
            //垂直の入力を使って、カメラのみ、X軸中心回転を行う。
            m_rotationX -= AccelerateInput(D_InputVertcal) * m_cameraMoveSpeed * Time.deltaTime; //Unityは左手座標系なので、上下の回転角度（X軸中心）にはマイナスをかけなければならない
            m_rotationX = Mathf.Clamp(m_rotationX, -m_camRotateLimitX_Upper, m_camRotateLimitX_Lower); //縦方向(X軸中心)回転には角度制限をつけないと宙返りしてしまう
            m_playerCamera.transform.eulerAngles = new Vector3(m_rotationX, m_playerCamera.transform.eulerAngles.y, 0f); //m_playerCamera.transform.eulerAngles.yは親の回転に委ねているので弄らない
        }
    }

    /// <summary>
    /// インタラクトの結果を受け取って、必要なら時間差をつけて画面振動を実行するよ。
    /// </summary>
    /// <param name="interactType">インタラクトの種別</param>
    async public void InvokeShakeEffectFromInteract(INTERACT_TYPE interactType)
    {
        switch (interactType)
        {
            case INTERACT_TYPE.ENEMY_MISS:
                //画面揺れ小
                await UniTask.Delay(400);
                m_shakeEffect.ShakeCameraEffect(ShakeEffect.ShakeType.Small);
                break;
            case INTERACT_TYPE.ENEMY_FRONT:
                //画面揺れ小
                await UniTask.Delay(400);
                m_shakeEffect.ShakeCameraEffect(ShakeEffect.ShakeType.Small);
                break;
            case INTERACT_TYPE.ENEMY_BACK:
                //画面揺れ中
                await UniTask.Delay(400);
                m_shakeEffect.ShakeCameraEffect(ShakeEffect.ShakeType.Medium);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// プレイヤーのStateが更新されたときに呼ぶことを想定しているよ。State更新時に画面振動が必要なときは振動させるよ。
    /// </summary>
    /// <param name="state">現在のプレイヤーState</param>
    public void InvokeShakeEffectFromState(PLAYER_STATE state)
    {
        switch (state)
        {
            case PLAYER_STATE.DASH:
                //画面揺れ小
                m_shakeEffect.ShakeCameraEffect(ShakeEffect.ShakeType.Small);
                break;
            case PLAYER_STATE.KNOCKED:
                //画面揺れ大
                m_shakeEffect.ShakeCameraEffect(ShakeEffect.ShakeType.Large);
                break;
            case PLAYER_STATE.STUNNED:
                //画面揺れ大
                m_shakeEffect.ShakeCameraEffect(ShakeEffect.ShakeType.Large);
                break;
            default:
                break;
        }
    }

    public void InvokeShakeEffect(ShakeEffect.ShakeType shakeType)
    {
        switch (shakeType)
        {
            case ShakeEffect.ShakeType.Small:
                //画面揺れ小
                m_shakeEffect.ShakeCameraEffect(ShakeEffect.ShakeType.Small);
                break;
            case ShakeEffect.ShakeType.Medium:
                //画面揺れ中
                m_shakeEffect.ShakeCameraEffect(ShakeEffect.ShakeType.Medium);
                break;
            case ShakeEffect.ShakeType.Large:
                //画面揺れ大
                m_shakeEffect.ShakeCameraEffect(ShakeEffect.ShakeType.Large);
                break;
        }
    }
}
