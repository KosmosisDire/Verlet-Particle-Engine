

using ProtoGUI;

public class Space : Control
{
    public Space(int height, Panel panel) : base("", panel)
    {
        drawLabel = false;
        LineHeight = height;
    }

    protected override void Update(){}

    public override void Draw(float y)
    {
        base.Draw(y);
    }

}