namespace Latti;

public class Synth
{
    public float RootFrequency = 256f;

    public readonly List<Note> Notes = [];

    readonly SawOscillator[] _oscillators;

    const int Polyphony = 8;

    public Synth()
    {
        _oscillators = Enumerable.Range(0, Polyphony)
                                 .Select(_ => new SawOscillator())
                                 .ToArray();
    }

    public float GenerateAudio(double t, double dt)
    {
        Note[] active = Notes
            .Where(n => n.StartTime <= t && t < n.EndTime)
            .Take(Polyphony)
            .ToArray();

        float total = 0f;
        for (int i = 0; i < active.Length; i++)
        {
            Note note = active[i];
            total += _oscillators[i].GenerateAudio(
                note.GetFrequency(RootFrequency, t),
                note.Velocity,
                t, dt);
        }

        return total;
    }
}