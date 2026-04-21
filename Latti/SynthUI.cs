using System.Numerics;
using ImGuiNET;

namespace Latti;

public class SynthUI
{
    readonly Synth _synth;

    float _zoomX = 1.0f;
    float _zoomY = 1.0f;
    float _viewX = 0.0f;
    float _viewY = 0.0f;

    Note? _pendingPopupNote;
    string _pendingPopupWhich = "";
    Note? _selectedNote;
    string _selectedWhich = "";

    public SynthUI(Synth synth)
    {
        _synth = synth;
    }

    public void DrawUI()
    {
        ImGui.Begin("Synth");

        ImGui.InputFloat("Root frequency", ref _synth.RootFrequency);

        ImGui.Text("Zoom");
        ImGui.SliderFloat("Horizontal", ref _zoomX, .1f, 10f, "%.2f", ImGuiSliderFlags.Logarithmic);
        ImGui.SliderFloat("Vertical", ref _zoomY, .1f, 10f, "%.2f", ImGuiSliderFlags.Logarithmic);

        ImGui.SliderFloat("Pan X", ref _viewX, 0f, 1f);
        ImGui.SliderFloat("Pan Y", ref _viewY, 0f, 1f);

        if (ImGui.Button("+ Add note"))
        {
            _synth.Notes.Add(new Note
            {
                StartTime = 0,
                EndTime = 1,
                StartIntervals = [new Rational(1, 1)],
                EndIntervals = [new Rational(1, 1)],
                Velocity = 0.75f
            });
        }

        DrawNoteEditor();

        ImGui.End();
    }

    void DrawNoteEditor()
    {
        ImDrawListPtr dl = ImGui.GetWindowDrawList();

        Vector2 canvasPos = ImGui.GetCursorScreenPos();
        float canvasWidth = ImGui.GetWindowWidth() - 16f;
        float canvasHeight = 300f;
        Vector2 canvasSize = new(canvasWidth, canvasHeight);

        // ImGui.InvisibleButton("curve_canvas", canvasSize);
        // if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        // {
        //     _selectedNote = null;
        //     _selectedWhich = "";
        // }

        Vector2 pMin = canvasPos;
        Vector2 pMax = canvasPos + canvasSize;

        uint borderColor = ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.3f, 1f));
        dl.AddRect(pMin, pMax, borderColor);

        ImGui.PushClipRect(pMin, pMax, true);
        foreach (Note note in _synth.Notes)
        {
            DrawNote(note, dl, pMin, pMax, canvasSize);
        }
        ImGui.PopClipRect();

        if (_pendingPopupNote is not null)
        {
            _selectedNote = _pendingPopupNote;
            _selectedWhich = _pendingPopupWhich;
            _pendingPopupNote = null;
            _pendingPopupWhich = "";
            ImGui.OpenPopup("NotePopup");
        }

        if (ImGui.BeginPopup("NotePopup"))
        {
            if (_selectedNote is not null)
            {
                Note note = _selectedNote;

                ImGui.Text($"Editing {_selectedWhich} endpoint");

                ImGui.InputDouble("Start Time", ref note.StartTime);
                ImGui.InputDouble("End Time", ref note.EndTime);
                ImGui.InputFloat("Velocity", ref note.Velocity);
            }

            ImGui.EndPopup();
        }
    }

    void DrawNote(Note note, ImDrawListPtr dl, Vector2 pMin, Vector2 pMax, Vector2 canvasSize)
    {
        const int Segments = 64;
        Vector2[] points = new Vector2[Segments];

        for (int i = 0; i < Segments; i++)
        {
            double t = note.StartTime + (note.EndTime - note.StartTime) * (i / (double)(Segments - 1));

            float freq = note.GetFrequency(_synth.RootFrequency, t);
            float y = Math.Clamp(MathF.Log2(freq), 0f, 1f);

            float xScreen = pMin.X + ((float)t - _viewX) * _zoomX * canvasSize.X;
            float yScreen = pMax.Y - (y - _viewY) * _zoomY * canvasSize.Y;

            points[i] = new Vector2(xScreen, yScreen);
        }

        uint curveColor = ImGui.GetColorU32(new Vector4(1f));
        dl.AddPolyline(ref points[0], points.Length, curveColor, ImDrawFlags.None, 1.0f);

        if (IsOnScreen(points[0], pMin, pMax)) DrawEndpoint(note, "start", points[0], dl);
        if (IsOnScreen(points[^1], pMin, pMax)) DrawEndpoint(note, "end", points[^1], dl);
    }

    static bool IsOnScreen(Vector2 p, Vector2 min, Vector2 max) =>
        p.X >= min.X && p.X <= max.X &&
        p.Y >= min.Y && p.Y <= max.Y;

    void DrawEndpoint(Note note, string which, Vector2 center, ImDrawListPtr dl)
    {
        const float Size = 10f;

        dl.AddQuad(
            new Vector2(center.X, center.Y - Size),
            new Vector2(center.X + Size, center.Y),
            new Vector2(center.X, center.Y + Size),
            new Vector2(center.X - Size, center.Y),
            ImGui.GetColorU32(new Vector4(1f))
        );

        ImGui.SetCursorScreenPos(center - new Vector2(Size, Size));
        bool clicked = ImGui.InvisibleButton($"ep_{which}_{note.GetHashCode()}", new Vector2(Size * 2, Size * 2));

        if (clicked)
        {
            _pendingPopupNote = note;
            _pendingPopupWhich = which;
        }
    }
}