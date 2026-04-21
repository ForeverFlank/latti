namespace Latti;

public class TimeSignature
{
    public float Upper = 4;
    public float Lower = 4;

    public double GetTime(int bar, double beat, double bpm)
    {
        double beatInQuarters = 4.0 / Lower;
        double totalBeats = bar * Upper + beat;
        double totalQuarters = totalBeats * beatInQuarters;
        double secondsPerQuarter = 60.0 / bpm;

        return totalQuarters * secondsPerQuarter;
    }
}