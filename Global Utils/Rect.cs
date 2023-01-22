

public struct Rect
{
    public float2 size;
    public int2 sizeInt;
    public float scale;

    public int width;
    public int height;

    public float2 center;
    public float2 topLeft;
    public float2 bottomRight;
    public float2 bottomLeft;
    public float2 topRight;


    public Rect(float2 center, float2 size)
    {
        this.center = center;
        this.topLeft = new float2(center.X - size.X / 2, center.Y - size.Y / 2);
        this.bottomRight = new float2(center.X + size.X / 2, center.Y + size.Y / 2);
        this.bottomLeft = new float2(center.X - size.X / 2, center.Y + size.Y / 2);
        this.topRight = new float2(center.X + size.X / 2, center.Y - size.Y / 2);
        
        this.size = size;
        this.width = (int)size.X;
        this.height = (int)size.Y;
        this.sizeInt = new int2(width, height);

        scale = 1;
    }
}