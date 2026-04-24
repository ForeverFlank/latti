using System.Numerics;

namespace Latti;

class NoteEditorViewport
{
    public Vector2 Zoom = new(1.0f, 0.25f);
    public Vector2 View = new(0.0f, 6.0f);

    public Vector2 PMin { get; private set; }
    public Vector2 PMax { get; private set; }
    public Vector2 CanvasSize { get; private set; }

    public void Update(Vector2 pMin, Vector2 canvasSize)
    {
        PMin = pMin;
        CanvasSize = canvasSize;
        PMax = pMin + canvasSize;
    }

    public Vector2 TimeToScreen(float t, float y) => new(
        PMin.X + (t - View.X) * Zoom.X * CanvasSize.X,
        PMax.Y - (y - View.Y) * Zoom.Y * CanvasSize.Y
    );

    public Vector2 NoteToScreen(Note note, double beat, float rootFrequency, Tempo tempo)
    {
        float freq = note.GetFrequency(rootFrequency, beat);
        float time = (float)tempo.BeatsToSeconds(beat);
        return TimeToScreen(time, MathF.Log2(freq));
    }

    public double ScreenToBeat(float x, Tempo tempo)
    {
        float time = (x - PMin.X) / (Zoom.X * CanvasSize.X) + View.X;
        return tempo.SecondsToBeats(time);
    }

    public bool IsOnScreen(Vector2 p) =>
        p.X >= PMin.X && p.X <= PMax.X &&
        p.Y >= PMin.Y && p.Y <= PMax.Y;

    public static double SnapBeat(double beat)
    {
        const double snap = 1 / 4.0;
        return Math.Round(beat / snap) * snap;
    }
}