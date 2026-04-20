using ImGuiNET;

namespace Latti;

public class Synth
{
    float frequency;
    float gain;
    int waveform;

    float _phase;

    public Synth()
    {
        _phase = 0f;
    }

    public void DrawGUI()
    {
        ImGui.Begin("Synth");

        ImGui.SliderFloat("Frequency (Hz)", ref frequency, 50f, 2000f);
        ImGui.SliderFloat("Gain", ref gain, 0f, 1f);

        string[] waves = ["Sine", "Square", "Saw", "Triangle"];
        ImGui.Combo("Waveform", ref waveform, waves, waves.Length);

        ImGui.Text($"{waveform}");

        ImGui.End();
    }

    public float GenerateAudio(float t, float dt)
    {
        _phase += 2f * MathF.PI * frequency * dt;
        _phase %= MathF.Tau;
        float value = 0f;

        switch (waveform)
        {
            case 0: value = MathF.Sin(_phase); break;
            case 1: value = MathF.Sign(MathF.Sin(_phase)); break;
            case 2: value = 2f * (t * frequency - MathF.Floor(0.5f + t * frequency)); break;
            case 3: value = MathF.Abs(2f * (t * frequency - MathF.Floor(t * frequency + 0.5f))) * 2 - 1; break;
        }

        return value;
    }
}