using System.Numerics;
using ImGuiNET;

namespace Latti;

public class Synth
{
    readonly SawOscillator[] SawOsc;
    readonly List<Note> Notes = [];

    float RootFrequency = 256;

    float ZoomX = 1.0f;
    float ZoomY = 1.0f;

    float ViewX = 0.0f;
    float ViewY = 0.0f;

    const int Polyphony = 8;

    public Synth()
    {
        SawOsc = Enumerable.Range(0, Polyphony).Select(_ => new SawOscillator()).ToArray();
    }

    (Note note, string interval)? _selectedNote;

    public void DrawGUI()
    {
        ImGui.Begin("Synth");

        ImGui.InputFloat("Root frequency", ref RootFrequency);

        ImGui.Text("Zoom");
        ImGui.SliderFloat("Horizontal", ref ZoomX, .1f, 10f, "%.2f", ImGuiSliderFlags.Logarithmic);
        ImGui.SliderFloat("Vertical", ref ZoomY, .1f, 10f, "%.2f", ImGuiSliderFlags.Logarithmic);

        ImGui.SliderFloat("Pan X", ref ViewX, 0f, 1f);
        ImGui.SliderFloat("Pan Y", ref ViewY, 0f, 1f);

        if (ImGui.Button("+ Add note"))
        {
            Notes.Add(new()
            {
                StartTime = 0,
                EndTime = 1,
                StartIntervals = [new(1, 1)],
                EndIntervals = [new(1, 1)],
                Velocity = 0.75f
            });
        }

        DrawNoteEditor();

        ImGui.End();
    }

    public void DrawNoteEditor()
    {
        ImDrawListPtr dl = ImGui.GetWindowDrawList();

        Vector2 canvasPos = ImGui.GetCursorScreenPos();
        float canvasWidth = ImGui.GetWindowWidth() - 16f;
        float canvasHeight = 300f;
        Vector2 canvasSize = new(canvasWidth, canvasHeight);

        ImGui.InvisibleButton("curve_canvas", canvasSize);

        Vector2 pMin = canvasPos;
        Vector2 pMax = canvasPos + canvasSize;

        uint borderColor = ImGui.GetColorU32(new Vector4(0.3f));
        dl.AddRect(pMin, pMax, borderColor);

        ImGui.PushClipRect(pMin, pMax, true);
        foreach (Note note in Notes)
        {
            DrawNote(note, dl, pMin, pMax, canvasSize);
        }
        ImGui.PopClipRect();

        if (ImGui.BeginPopup("NotePopup"))
        {
            if (_selectedNote.HasValue)
            {
                var (note, which) = _selectedNote.Value;

                ImGui.Text($"Editing {which} endpoint");

                ImGui.InputDouble("Start Time", ref note.StartTime);
                ImGui.InputDouble("End Time", ref note.EndTime);
                ImGui.InputFloat("Velocity", ref note.Velocity);
            }

            ImGui.EndPopup();
        }


    }

    public void DrawNote(Note note, ImDrawListPtr dl, Vector2 pMin, Vector2 pMax, Vector2 canvasSize)
    {
        int segments = 64;
        Vector2[] points = new Vector2[segments];

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);

            float freq = note.GetFrequency(RootFrequency, t);
            float y = MathF.Log2(freq);
            y = Math.Clamp(y, 0f, 1f);

            float tZoomed = (t - ViewX) * ZoomX;
            float yZoomed = (y - ViewY) * ZoomY;

            float xScreen = pMin.X + tZoomed * canvasSize.X;
            float yScreen = pMax.Y - yZoomed * canvasSize.Y;

            points[i] = new Vector2(xScreen, yScreen);
        }

        uint curveColor = ImGui.GetColorU32(new Vector4(1f));
        dl.AddPolyline(ref points[0], points.Length, curveColor, ImDrawFlags.None, 1.0f);

        if (IsOnScreen(points[0], pMin, pMax))
            DrawEndpoint(note, "start", points[0], dl);

        if (IsOnScreen(points[^1], pMin, pMax))
            DrawEndpoint(note, "end", points[^1], dl);
    }

    bool IsOnScreen(Vector2 p, Vector2 min, Vector2 max)
    {
        return p.X >= min.X && p.X <= max.X &&
               p.Y >= min.Y && p.Y <= max.Y;
    }

    void DrawEndpoint(Note note, string id, Vector2 center, ImDrawListPtr dl)
    {
        float size = 10f;

        Vector2 p1 = new(center.X, center.Y - size);
        Vector2 p2 = new(center.X + size, center.Y);
        Vector2 p3 = new(center.X, center.Y + size);
        Vector2 p4 = new(center.X - size, center.Y);

        uint color = ImGui.GetColorU32(new Vector4(1f));

        dl.AddQuad(p1, p2, p3, p4, color);

        ImGui.SetCursorScreenPos(center - new Vector2(size, size));

        ImGui.InvisibleButton($"endpoint_{id}_{note.GetHashCode()}", new Vector2(size * 2, size * 2));

        if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            _selectedNote = (note, id);
            ImGui.OpenPopup("NotePopup");
        }
    }


    public float GenerateAudio(double t, double dt)
    {
        Note[] notes = Notes.Where(note => note.StartTime <= t && t < note.EndTime).ToArray();

        float total = 0f;
        for (int i = 0; i < Math.Min(Polyphony, notes.Length); i++)
        {
            Note note = notes[i];
            total += SawOsc[i].GenerateAudio(note.GetFrequency(RootFrequency, t), note.Velocity, t, dt);
        }

        return total;
    }
}