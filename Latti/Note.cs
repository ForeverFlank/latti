namespace Latti;

public class Note
{
    private double _startBeat;
    private double _endBeat;
    private float _velocity;

    public double StartBeat
    {
        get => _startBeat;
        set => _startBeat = Math.Max(value, 0);
    }
    public double EndBeat
    {
        get => _endBeat;
        set => _endBeat = Math.Max(_startBeat, value);
    }
    public double DurationBeats => EndBeat - StartBeat;

    public List<Rational> StartIntervals = [];
    public List<Rational> EndIntervals = [];
    public float Velocity
    {
        get => _velocity;
        set => _velocity = Math.Clamp(value, 0f, 1f);
    }

    public double StartSeconds(TempoMap tempo) => tempo.BeatsToSeconds(StartBeat);
    public double EndSeconds(TempoMap tempo) => tempo.BeatsToSeconds(EndBeat);
    public double DurationSeconds(TempoMap tempo) => tempo.BeatsToSeconds(DurationBeats);

    public double StartBar(TempoMap tempo) => tempo.BeatsToBar(StartBeat);
    public double EndBar(TempoMap tempo) => tempo.BeatsToBar(EndBeat);

    public bool IsActiveAtBeat(double beat) => beat >= StartBeat && beat < EndBeat;
    public bool IsActiveAtSecond(double t, TempoMap tempo) => IsActiveAtBeat(tempo.SecondsToBeats(t));

    public float GetFrequency(float rootFrequency, double beat)
    {
        float tNorm = (float)((beat - StartBeat) / DurationBeats);
        tNorm = Math.Clamp(tNorm, 0f, 1f);

        float startFreq = rootFrequency * StartIntervals.Aggregate(1f, (acc, r) => acc * r.Value);
        float endFreq = rootFrequency * EndIntervals.Aggregate(1f, (acc, r) => acc * r.Value);

        return startFreq * MathF.Pow(endFreq / startFreq, tNorm);
    }

    public float GetFrequencyAtSecond(float rootFrequency, double t, TempoMap tempo)
    {
        return GetFrequency(rootFrequency, tempo.SecondsToBeats(t));
    }
}