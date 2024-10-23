using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : MonoBehaviour
{
    public ushort EntityID { set; get; } //サーバーから割り振られたID。

    public abstract void InitEntity(); //初期化する。変数の初期化など

    public abstract void ActivateEntity(); //活性化する。例えば罠であれば、Colliderで踏まれたことを検出してから実行するなど

    public abstract void DestroyEntity(); //破壊される。エフェクト演出など
}
