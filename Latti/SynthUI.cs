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

    SelectedNote? _pendingSelectedNote;
    SelectedNote? _selectedNote;
    SelectedNote? _dragTarget;
    bool _dragging;

    Vector2 _pMin, _pMax, _canvasSize;

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
            _synth.Notes.Add(new Note
            {
                StartBeat = 0,
                EndBeat = 1,
                StartIntervals = [new Rational(1, 1)],
                EndIntervals = [new Rational(1, 1)],
                Velocity = 0.75f
            });

        DrawNoteEditor();

        ImGui.End();
    }

    void DrawNoteEditor()
    {
        ImDrawListPtr dl = ImGui.GetWindowDrawList();

        _canvasSize = new(ImGui.GetWindowWidth() - 16f, 300f);
        _pMin = ImGui.GetCursorScreenPos();
        _pMax = _pMin + _canvasSize;

        dl.AddRect(_pMin, _pMax, ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.3f, 1f)));

        ImGui.PushClipRect(_pMin, _pMax, true);
        foreach (Note note in _synth.Notes)
        {
            DrawNote(note, dl);
        }
        ImGui.PopClipRect();

        if (!_dragging && _pendingSelectedNote is not null)
        {
            _selectedNote = _pendingSelectedNote;
            _pendingSelectedNote = null;
            ImGui.OpenPopup("NotePopup");
        }

        if (ImGui.BeginPopup("NotePopup") && _selectedNote is not null)
        {
            ImGui.Text($"Editing {_selectedNote.Type} endpoint");
            // ...
            ImGui.EndPopup();
        }

        HandleDragging();
    }

    void DrawNote(Note note, ImDrawListPtr dl)
    {
        const int segments = 64;

        Vector2[] points = Enumerable.Range(0, segments).Select(i =>
            {
                double t = i / (segments - 1.0);

                double time = note.StartSeconds(_synth.Tempo) + t * note.DurationSeconds(_synth.Tempo);
                float freq = note.GetFrequency(_synth.RootFrequency, time);
                float y = MathF.Log2(freq);

                return TimeToScreen((float)time, y);
            }).ToArray();

        dl.AddPolyline(ref points[0], points.Length, ImGui.GetColorU32(new Vector4(1f)), ImDrawFlags.None, 1.0f);

        if (IsOnScreen(points[0])) DrawEndpoint(note, SelectType.Start, points[0], dl);
        if (IsOnScreen(points[^1])) DrawEndpoint(note, SelectType.End, points[^1], dl);
    }

    void DrawEndpoint(Note note, SelectType which, Vector2 center, ImDrawListPtr dl)
    {
        const float size = 10f;

        dl.AddQuad(
            new Vector2(center.X, center.Y - size),
            new Vector2(center.X + size, center.Y),
            new Vector2(center.X, center.Y + size),
            new Vector2(center.X - size, center.Y),
            ImGui.GetColorU32(new Vector4(1f))
        );

        ImGui.SetCursorScreenPos(center - new Vector2(size, size));
        ImGui.InvisibleButton($"ep_{which}_{note.GetHashCode()}", new Vector2(size * 2));

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            _pendingSelectedNote = new(note, which);

            _dragging = true;
            _dragTarget = new(note, which);
        }
    }

    void HandleDragging()
    {
        if (_dragging && _dragTarget != null)
        {
            if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                _dragging = false;
                _dragTarget = null;
                return;
            }

            Vector2 mouse = ImGui.GetIO().MousePos;

            double time = ScreenToTime(mouse.X);

            double beat = time * _synth.Tempo.Bpm / 60.0;
            double snappedBeat = SnapBeat(beat);

            Note note = _dragTarget.Note;

            if (_dragTarget.Type == SelectType.Start)
            {
                note.StartBeat = Math.Min(snappedBeat, note.EndBeat - 0.01);
            }
            else
            {
                note.EndBeat = Math.Max(snappedBeat, note.StartBeat + 0.01);
            }
        }
    }

    static double SnapBeat(double beat)
    {
        const double snap = 1 / 16f;
        return Math.Round(beat / snap) * snap;
    }


    Vector2 TimeToScreen(float t, float y) => new(
        _pMin.X + (t - _viewX) * _zoomX * _canvasSize.X,
        _pMax.Y - (y - _viewY) * _zoomY * _canvasSize.Y
    );

    float ScreenToTime(float x) =>
        (x - _pMin.X) / (_zoomX * _canvasSize.X) + _viewX;

    bool IsOnScreen(Vector2 p) =>
        p.X >= _pMin.X && p.X <= _pMax.X &&
        p.Y >= _pMin.Y && p.Y <= _pMax.Y;

    class SelectedNote
    {
        public Note Note;
        public SelectType Type;

        public SelectedNote(Note note, SelectType type)
        {
            Note = note;
            Type = type;
        }
    }

    enum SelectType { Start, End }
}