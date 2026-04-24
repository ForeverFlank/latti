using System.Numerics;

namespace Latti;

class NoteEditorViewport
{
    public float ZoomX = 1.0f;
    public float ZoomY = 0.1f;
    public float ViewX = 0.0f;
    public float ViewY = 0.0f;

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
        PMin.X + (t - ViewX) * ZoomX * CanvasSize.X,
        PMax.Y - (y - ViewY) * ZoomY * CanvasSize.Y
    );

    public Vector2 BeatToScreen(Note note, double beat, float rootFrequency, Tempo tempo)
    {
        float freq = note.GetFrequency(rootFrequency, beat);
        float time = (float)tempo.BeatsToSeconds(beat);
        return TimeToScreen(time, MathF.Log2(freq));
    }

    public double ScreenToBeat(float x, Tempo tempo)
    {
        float time = (x - PMin.X) / (ZoomX * CanvasSize.X) + ViewX;
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