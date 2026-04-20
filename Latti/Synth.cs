using ImGuiNET;

namespace Latti;

public struct Fraction
{
    public int Numerator;
    public int Denominator;

    public readonly float Value => ((float)Numerator) / Denominator;

    public Fraction(int numerator = 1, int denominator = 1)
    {
        Numerator = numerator;
        Denominator = denominator;
    }
}

public class Note
{
    public Fraction Frac1;
    public Fraction Frac2;
    public float Velocity;
    public int StartBar;
    public double StartBeat;
    public int EndBar;
    public double EndBeat;

    public float GetFrequency(float rootFrequency)
    {
        return rootFrequency * Frac1.Value * Frac2.Value;
    }

    public double GetStartTime(float upper, float lower, double bpm)
    {
        return GetTime(StartBar, StartBeat, upper, lower, bpm);
    }

    public double GetEndTime(float upper, float lower, double bpm)
    {
        return GetTime(EndBar, EndBeat, upper, lower, bpm);
    }

    static double GetTime(int bar, double beat, float upper, float lower, double bpm)
    {
        double beatInQuarters = 4.0 / lower;
        double totalBeats = bar * upper + beat;
        double totalQuarters = totalBeats * beatInQuarters;
        double secondsPerQuarter = 60.0 / bpm;

        return totalQuarters * secondsPerQuarter;
    }
}

public class Synth
{
    readonly SawOscillator[] SawOsc;
    readonly List<Note> Notes = [];

    float TimeSignatureUpper = 4;
    float TimeSignatureLower = 4;
    float Bpm = 120;
    float RootFrequency = 256;

    const int Polyphony = 8;

    public Synth()
    {
        SawOsc = Enumerable.Range(0, Polyphony).Select(_ => new SawOscillator()).ToArray();
    }

    public void DrawGUI()
    {
        ImGui.Begin("Synth");

        ImGui.InputFloat("Root frequency", ref RootFrequency);

        if (ImGui.Button("+ Add note"))
        {
            Notes.Add(new()
            {
                Frac1 = new(1, 1),
                Frac2 = new(1, 1),
                Velocity = 0.75f
            });
        }

        for (int i = 0; i < Notes.Count; i++)
        {
            Note note = Notes[i];
            if (ImGui.CollapsingHeader($"Note #{i} | bar {note.StartBar} beat {note.StartBeat:F2}"))
            {
                ImGui.InputInt($"Start bar##{i}", ref note.StartBar);
                ImGui.InputDouble($"Start beat##{i}", ref note.StartBeat);

                ImGui.InputInt($"End bar##{i}", ref note.EndBar);
                ImGui.InputDouble($"End beat##{i}", ref note.EndBeat);

                ImGui.Separator();

                ImGui.InputInt($"n1##{i}", ref note.Frac1.Numerator);
                ImGui.InputInt($"d1##{i}", ref note.Frac1.Denominator);

                ImGui.InputInt($"n2##{i}", ref note.Frac2.Numerator);
                ImGui.InputInt($"d2##{i}", ref note.Frac2.Denominator);

                ImGui.Separator();

                if (ImGui.Button($"- Remove##{i}"))
                {
                    Notes.RemoveAt(i);
                }
            }
        }

        ImGui.End();
    }

    public float GenerateAudio(double t, double dt)
    {
        Note[] notes = Notes
            .Where(note =>
                note.GetStartTime(TimeSignatureUpper, TimeSignatureLower, Bpm) <= t &&
                t < note.GetEndTime(TimeSignatureUpper, TimeSignatureLower, Bpm))
            .ToArray();

        float total = 0f;
        for (int i = 0; i < Math.Min(Polyphony, notes.Length); i++)
        {
            Note note = notes[i];
            total += SawOsc[i].GenerateAudio(note.GetFrequency(RootFrequency), note.Velocity, t, dt);
        }
        return total;
    }
}