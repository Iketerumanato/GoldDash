
public class Chest : Entity
{
    private UIFade uiFade;

    private void Start()
    {
        uiFade = FindObjectOfType<UIFade>();
    }

    public int Tier { set; get; } //レア度

    public override void InitEntity()
    {

    }

    public override void ActivateEntity()
    {
        uiFade.FadeInImage();
    }

    public override void DestroyEntity()
    {
        Destroy(this.gameObject);
    }
}