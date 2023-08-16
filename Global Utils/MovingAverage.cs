public class MovingAverage
{
    public float[] values;
    public int index;
    public int count;
    public float sum;

    public MovingAverage(int count)
    {
        this.count = count;
        values = new float[count];
    }

    public void Add(float value)
    {
        sum -= values[index];
        sum += value;
        values[index] = value;
        index = (index + 1) % count;
    }

    public float GetAverage()
    {
        return sum / count;
    }

    public string ToString(string format)
    {
        return GetAverage().ToString(format);
    }

    public static implicit operator float(MovingAverage average)
    {
        return average.GetAverage();
    }

}