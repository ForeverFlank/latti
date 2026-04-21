namespace Latti;

public class TempoMap
{
    public TimeSignature TimeSignature = new(4f, 4f);
    public float Bpm = 120f;

    public double BeatsPerSecond => Bpm / 60.0;
    public double SecondsPerBeat => 60.0 / Bpm;
    public double BeatsPerBar => TimeSignature.BeatsPerBar;

    public double BeatsToSeconds(double beats) => beats * SecondsPerBeat;
    public double SecondsToBeats(double seconds) => seconds * BeatsPerSecond;

    public double BarsToBeats(double bars) => bars * BeatsPerBar;
    public double BeatsToBar(double beats) => beats / BeatsPerBar;

    public TempoMap(TimeSignature timeSignature, float bpm)
    {
        TimeSignature = timeSignature;
        Bpm = bpm;
    }
}