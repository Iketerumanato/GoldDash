using UnityEngine;
using TMPro;

public class SelectPlayerColor : MonoBehaviour
{
    [SerializeField] TMP_Dropdown PlayerColorDropdown;
    [SerializeField] Texture[] PlayerColorTexs;
    [SerializeField] Renderer ActorBodyRenderer;
    const string ActorBaseMap = "_BaseMap";

    enum PLAYER_TEXTURE_COLOR
    { 
        RED,
        BLUE,
        YELLOW,
        GREEN
    }

    // Update is called once per frame
    void Update()
    {
        //Actorモデルの体の部分のBaseMapを動的に変更
        switch (PlayerColorDropdown.value)
        {
            //赤
            case (int)PLAYER_TEXTURE_COLOR.RED:
                ActorBodyRenderer.material.SetTexture(ActorBaseMap, PlayerColorTexs[(int)PLAYER_TEXTURE_COLOR.RED]);
                break;
            //青
            case (int)PLAYER_TEXTURE_COLOR.BLUE:
                ActorBodyRenderer.material.SetTexture(ActorBaseMap, PlayerColorTexs[(int)PLAYER_TEXTURE_COLOR.BLUE]);
                break;
            //黄色
            case (int)PLAYER_TEXTURE_COLOR.YELLOW:
                ActorBodyRenderer.material.SetTexture(ActorBaseMap, PlayerColorTexs[(int)PLAYER_TEXTURE_COLOR.YELLOW]);
                break;
            //緑
            case (int)PLAYER_TEXTURE_COLOR.GREEN:
                ActorBodyRenderer.material.SetTexture(ActorBaseMap, PlayerColorTexs[(int)PLAYER_TEXTURE_COLOR.GREEN]);
                break;
        }
    }
}
