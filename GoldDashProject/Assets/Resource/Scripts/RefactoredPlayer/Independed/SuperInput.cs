using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class SuperInput
{
    //HorizontalとVerticalについて、Inputクラスをラップ
    public static float Horizontal { get { return Input.GetAxis("Horizontal"); } }
    public static float Vertical { get { return Input.GetAxis("Vertical"); } }

    public static SuperTouch[] SuperTouches //クリックとタッチを統合して取り出せるようにした夢のプロパティ
    {
        get
        {
            bool clicked = Input.GetMouseButton(0) || Input.GetMouseButtonUp(0); //マウスクリックの有無を調べる

            //返す配列のサイズ決定　クリック入力があるならサイズを1確保し、そこにタッチの数を足す。
            int num = (clicked ? 1 : 0) + Input.touchCount;
            //配列を作る
            SuperTouch[] array = new SuperTouch[num];
            for (int i = 0; i < num; i++)
            {
                if (i == 0 && clicked) //クリック入力の情報は配列の先頭に格納することにする
                {
                    array[i] = new SuperTouch(true);
                }
                else if (clicked) //クリック入力を格納済なら、iから1引いた数をfingerIDとしてInput.GetTouch(fingerID)を呼んでいく
                {
                    array[i] = new SuperTouch(false, i - 1);
                }
                else //クリック入力を格納済でないならiをfingerIDとしてInput.GetTouch(fingerID)を呼んでいく
                {
                    array[i] = new SuperTouch(false, i);
                }
            }

            return array;
        }
    }

    public struct SuperTouch
    {
        public SuperTouch(bool isClick, int fingerID = 0) //コンストラクタ。クリックの情報なのか否か、タッチなら指のIDを指定する
        {
            position = isClick ? Input.mousePosition : Input.GetTouch(fingerID).position;
            phase = ConvertPhase(isClick, fingerID);
        }

        public Vector2 position;
        SuperTouchPhase phase;

        public enum SuperTouchPhase
        {
            //パッと開かずグッと握って
            DAN = 0, //ダン！入力開始
            GYUN, //ギューン！入力継続
            DOKAN, //ドカーン！入力終了
            //正義の！鉄！拳！
        }

        //コンストラクタで使用する静的メソッド。クリックとタッチのフェーズをSuperTouchPhaseに変換する
        private static SuperTouchPhase ConvertPhase(bool isClick, int fingerID)
        {
            if (isClick) //クリックの場合
            {
                if (Input.GetMouseButtonUp(0)) return SuperTouchPhase.DAN;
                if (Input.GetMouseButtonDown(0)) return SuperTouchPhase.DOKAN;
                else return SuperTouchPhase.GYUN;
            }
            else //タッチの場合
            {
                switch (Input.GetTouch(fingerID).phase)
                {
                    case TouchPhase.Began:
                        return SuperTouchPhase.DAN;
                    case TouchPhase.Moved:
                        return SuperTouchPhase.GYUN;
                    case TouchPhase.Stationary:
                        return SuperTouchPhase.GYUN;
                    case TouchPhase.Ended:
                        return SuperTouchPhase.DOKAN;
                    default: //次はTouchPhase.Canceledが来るはずだけど構文エラー回避のためここでdefaultを使う
                        return SuperTouchPhase.DOKAN;
                }
            }
        }
    }
}
