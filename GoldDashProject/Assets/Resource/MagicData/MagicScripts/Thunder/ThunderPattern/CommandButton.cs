using UnityEngine;
using UnityEngine.EventSystems;

public class CommandButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] InputThunderCommand _inputThunderCommandIns;

    // ボタンが押されたとき
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_inputThunderCommandIns != null) _ = _inputThunderCommandIns.OnPointerDownAsync();
    }

    // ボタンが離されたとき
    public void OnPointerUp(PointerEventData eventData)
    {
        if (_inputThunderCommandIns != null) _inputThunderCommandIns.OnPointerUp();
    }
}