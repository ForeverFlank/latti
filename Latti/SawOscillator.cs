namespace Latti;

public class SawOscillator
{
    double _phase;

    public SawOscillator()
    {
        _phase = 0f;
    }

    public float GenerateAudio(float frequency, float gain, double t, double dt)
    {
        _phase += frequency * dt;
        _phase %= 1;
        double value = 2f * (_phase - Math.Floor(0.5f + _phase));

        return (float)value * gain;
    }
}