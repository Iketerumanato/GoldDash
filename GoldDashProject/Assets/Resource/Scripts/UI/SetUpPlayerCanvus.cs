using UnityEngine;

public class SetUpPlayerCanvus : MonoBehaviour
{
    public void OnStartServer()
    {
        gameObject.SetActive(false); // サーバー側でクライアントのキャンバスを非表示
    }
}