using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Audio.OpenAL;
using ImGuiNET;

namespace Latti;

public class SynthWindow : GameWindow
{
    readonly ImGuiController ImGuiController;
    ALContext _context;
    int _source;
    int[] _buffers;
    double _time;
    bool playing = false;

    readonly Synth Synth;
    float gain = 0.5f;

    const int BufferSize = 1024;
    const int BufferCount = 4;
    const int SampleRate = 48000;

    public SynthWindow(int width, int height) : base(GameWindowSettings.Default, new NativeWindowSettings()
    {
        ClientSize = (width, height),
        Title = "Latti"
    })
    {
        ImGuiController = new ImGuiController(width, height);
        VSync = VSyncMode.Adaptive;
        UpdateFrequency = 60.0;

        Synth = new();

        UpdateFrame += e => ImGuiController.Update(this, (float)e.Time);
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        _context = ALC.CreateContext(ALC.OpenDevice(null), null as int[]);
        ALC.MakeContextCurrent(_context);

        _source = AL.GenSource();
        _buffers = AL.GenBuffers(BufferCount);

        ImGui.CreateContext();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (!playing) return;

        AL.GetSource(_source, ALGetSourcei.BuffersProcessed, out int processed);

        while (processed-- > 0)
        {
            int buf = AL.SourceUnqueueBuffer(_source);

            short[] samples = GetSynthAudio();
            AL.BufferData(buf, ALFormat.Mono16, samples, SampleRate);

            AL.SourceQueueBuffer(_source, buf);
        }

        AL.GetSource(_source, ALGetSourcei.SourceState, out int state);
        if (state == (int)ALSourceState.Stopped)
        {
            AL.SourcePlay(_source);
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        Synth.DrawGUI();
        DrawControlGUI();

        ImGuiController.Render();
        ImGuiController.CheckGLError("End of frame");

        SwapBuffers();
    }

    void DrawControlGUI()
    {
        ImGui.Begin("Control");

        ImGui.SliderFloat("Gain", ref gain, 0f, 1f);

        if (!playing)
        {
            if (ImGui.Button("Play"))
            {
                PlayTone();
                _time = 0;
                playing = true;
            }
        }
        else
        {
            if (ImGui.Button("Stop"))
            {
                AL.SourceStop(_source);

                AL.GetSource(_source, ALGetSourcei.BuffersQueued, out int queued);
                while (queued-- > 0)
                {
                    AL.SourceUnqueueBuffer(_source);
                }

                playing = false;
            }
        }

        ImGui.End();
    }

    void PlayTone()
    {
        AL.SourceStop(_source);

        AL.GetSource(_source, ALGetSourcei.BuffersQueued, out int queued);
        while (queued-- > 0)
        {
            AL.SourceUnqueueBuffer(_source);
        }

        foreach (int buf in _buffers)
        {
            short[] samples = GetSynthAudio();
            AL.BufferData(buf, ALFormat.Mono16, samples, SampleRate);
        }

        AL.SourceQueueBuffers(_source, BufferCount, _buffers);
        AL.SourcePlay(_source);
    }

    short[] GetSynthAudio()
    {
        // _time = 0;
        short[] data = new short[BufferSize];

        double dt = 1.0 / SampleRate;

        for (int i = 0; i < BufferSize; i++)
        {
            float value = Synth.GenerateAudio(_time, dt);

            data[i] = (short)(Math.Clamp(value * gain, -1f, 1f) * short.MaxValue);

            _time += dt;
        }

        return data;
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);

        ImGuiController.WindowResized(e.Width, e.Height);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        ImGuiController.PressChar((char)e.Unicode);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        ImGuiController.MouseScroll(e.Offset);
    }

    protected override void OnUnload()
    {
        base.OnUnload();

        AL.DeleteSource(_source);
        AL.DeleteBuffers(_buffers);

        ALC.DestroyContext(_context);
    }
}