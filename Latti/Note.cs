namespace Latti;

public class Note
{
    public double StartTime;
    public double EndTime;
    public List<Rational> StartIntervals = [];
    public List<Rational> EndIntervals = [];
    public float Velocity = 0.75f;

    public float GetFrequency(float rootFrequency, double t)
    {
        float tNorm = (float)((t - StartTime) / (EndTime - StartTime));
        tNorm = Math.Clamp(tNorm, 0f, 1f);

        float startFreq = rootFrequency * StartIntervals.Aggregate(1f, (acc, r) => acc * r.Value);
        float endFreq = rootFrequency * EndIntervals.Aggregate(1f, (acc, r) => acc * r.Value);

        return startFreq * MathF.Pow(endFreq / startFreq, tNorm);
    }
}