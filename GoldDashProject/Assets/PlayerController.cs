using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("移動速度（マス／毎秒）")]
    [SerializeField] private float playerMoveSpeed = 1f;

    [Header("カメラ回転速度（度／毎秒）")]
    [SerializeField] private float cameraMoveSpeed = 1f;

    [Header("カメラの縦方向（X軸中心）回転の角度制限")]
    [Range(0f, 90f)]
    [SerializeField] float camRotateLimitX = 90f;

    //プレイヤーを移動させる左ジョイスティック
    private VariableJoystick leftJoystick;
    //カメラを操作する右ジョイスティック
    private DynamicJoystick rightJoystick;

    //右スティックの操作対象になるカメラ
    private Camera playerCam;
    //カメラがX軸中心に何度回転しているか
    private float rotationX;
    //カメラがY軸中心に何度回転しているか
    private float rotationY;

    private void Start()
    {
        leftJoystick = GetComponentInChildren<VariableJoystick>(); //プレイヤープレハブの子のキャンバスにある
        rightJoystick = GetComponentInChildren<DynamicJoystick>(); //同上
        playerCam = Camera.main; //プレイヤーカメラにはMainCameraのタグがついている
    }

    private void LateUpdate()
    {
        #region 左スティックでプレイヤーを移動させる
        //WASDの入力をベクトルにする
        Vector3 playerMoveVec = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

        //ジョイスティックの入力があればそれで上書きする
        if (!Mathf.Approximately(leftJoystick.Horizontal, 0) || !Mathf.Approximately(leftJoystick.Vertical, 0)) //左スティックの水平垂直どちらの入力も"ほぼ0"でないなら
        playerMoveVec = new Vector3(leftJoystick.Horizontal, 0f, leftJoystick.Vertical); //上書き

        this.transform.Translate(playerMoveVec * playerMoveSpeed * Time.deltaTime); //求めたベクトルに移動速度とdeltaTimeをかけて座標書き換え
        #endregion

        #region 右スティックでカメラを操作しつつ、プレイヤーを左右に回転させる
        //カメラ操作の入力がないなら回転しない
        if (!Mathf.Approximately(rightJoystick.Horizontal, 0) || !Mathf.Approximately(rightJoystick.Vertical, 0)) //右スティックの水平垂直どちらの入力も"ほぼ0"でないなら
        {
            //ジョイスティックの入力をオイラー角（〇軸を中心に△度回転、という書き方）にする
            //前提：カメラはZ軸の正の方向を向いている
            //水平の入力はY軸中心、垂直の入力はX軸中心になる。Z軸中心の回転はペテルギウス・ロマネコンティになってしまうため行わない。
            rotationX -= rightJoystick.Vertical * cameraMoveSpeed * Time.deltaTime; //Unityは左手座標系なので、上下の回転角度（X軸中心）にはマイナスをかけなければならない
            rotationX = Mathf.Clamp(rotationX, -camRotateLimitX, camRotateLimitX); //縦方向(X軸中心)回転には角度制限をつけないと宙返りしてしまう
            rotationY += rightJoystick.Horizontal * cameraMoveSpeed * Time.deltaTime; //Unityは左手座標系なので、左右の回転角度（Y軸中心）は加算でいい
            Vector3 cameraMoveEulers = new Vector3(rotationX, rotationY, 0f); //X軸だけマイナスをかけています

            //オイラー角をtransform.rotationに代入するため、クォータニオンに変換する
            playerCam.transform.rotation = Quaternion.Euler(cameraMoveEulers);

            //プレイヤーの正面方向を、カメラの正面方向（注視点の方向）と（XZ平面について）合わせる。カメラのX軸回転によって注視点のY座標（高さ）が変化するが、これは無視して0fを代入。
            this.transform.forward = new Vector3(playerCam.transform.forward.x, 0f, playerCam.transform.forward.z);
        }
        #endregion
    }
}
