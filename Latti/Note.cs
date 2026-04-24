namespace Latti;

public class Note
{
    public List<double> PitchBeats = [0.0, 1.0];
    public List<List<Rational>> PitchIntervals = [[new(1, 1)], [new(1, 1)]];

    public double StartBeat => PitchBeats[0];
    public double EndBeat => PitchBeats[^1];
    public double DurationBeats => EndBeat - StartBeat;

    private float _velocity;
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

    float IntervalsToFrequency(float rootFrequency, List<Rational> intervals) =>
        rootFrequency * intervals.Aggregate(1f, (acc, r) => acc * r.Value);

    public float GetFrequency(float rootFrequency, double beat)
    {
        if (PitchBeats.Count == 0) return rootFrequency;

        if (beat <= PitchBeats[0]) return IntervalsToFrequency(rootFrequency, PitchIntervals[0]);
        if (beat >= PitchBeats[^1]) return IntervalsToFrequency(rootFrequency, PitchIntervals[^1]);

        for (int i = 0; i < PitchBeats.Count - 1; i++)
        {
            double b0 = PitchBeats[i];
            double b1 = PitchBeats[i + 1];

            if (beat <= b1)
            {
                double t = (b1 > b0) ? (beat - b0) / (b1 - b0) : 0.0;

                float f0 = IntervalsToFrequency(rootFrequency, PitchIntervals[i]);
                float f1 = IntervalsToFrequency(rootFrequency, PitchIntervals[i + 1]);

                return f0 * MathF.Pow(f1 / f0, (float)t);
            }
        }

        return IntervalsToFrequency(rootFrequency, PitchIntervals[^1]);
    }
}