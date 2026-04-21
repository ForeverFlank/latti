namespace Latti;

public class Note
{
    public List<Fraction> StartIntervals = [];
    public List<Fraction> EndIntervals = [];
    public float Velocity;

    public double StartTime;
    public double EndTime;

    public float GetFrequency(float rootFrequency, double time)
    {
        float t = (float)((time - StartTime) / (EndTime - StartTime));

        float startInterval = StartIntervals.Aggregate(1f, (acc, frac) => acc * frac.Value);
        float endInterval = EndIntervals.Aggregate(1f, (acc, frac) => acc * frac.Value);
        float currInterval = MathF.Pow(2, MathF.Log(startInterval) * (1f - t) + MathF.Log(endInterval) * t);
        return rootFrequency * currInterval;
    }
}
