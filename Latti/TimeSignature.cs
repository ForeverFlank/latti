namespace Latti;

public class TimeSignature
{
    public float BeatsPerBar;
    public float BeatUnit;

    public TimeSignature(float beatsPerBar, float beatUnit)
    {
        BeatsPerBar = beatsPerBar;
        BeatUnit = beatUnit;
    }

    public double GetTime(int bar, double beat, double bpm)
    {
        double beatInQuarters = 4.0 / BeatUnit;
        double totalBeats = bar * BeatsPerBar + beat;
        double totalQuarters = totalBeats * beatInQuarters;
        double secondsPerQuarter = 60.0 / bpm;

        return totalQuarters * secondsPerQuarter;
    }
}