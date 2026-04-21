using System.Numerics;
using ImGuiNET;

namespace Latti;

public class SynthUI
{
    readonly Synth _synth;

    float _zoomX = 1.0f;
    float _zoomY = .1f;
    float _viewX = 0.0f;
    float _viewY = 0.0f;

    SelectedNote? _pendingSelectedNote;
    SelectedNote? _selectedNote;
    SelectedNote? _dragTarget;
    Note? _pendingDelete;
    bool _dragging;
    Vector2 _dragStartMouse;
    bool _dragMoved;

    Vector2 _pMin, _pMax, _canvasSize;

    public SynthUI(Synth synth)
    {
        _synth = synth;
    }

    string GetNoteName(int y, int x)
    {
        y += 2;
        x += 2;
        return new string[][]{
            ["Gb++ ", "Bb+  ", "D    ", "F#-  ", "A#-- "],
            ["Cb++ ", "Eb+  ", "G    ", "B-   ", "D#-- "],
            ["Fb++ ", "Ab+  ", "C    ", "E-   ", "G#-- "],
            ["Bbb++", "Db+  ", "F    ", "A-   ", "C#-- "],
            ["Ebb++", "Gb+  ", "Bb   ", "D-   ", "F#-- "],
        }[y][x];
    }

    public void DrawUI()
    {
        ImGui.Begin("JI Grid");

        ImGui.BeginTable("##table", 5);

        for (int y = -2; y <= 2; y++)
        {
            ImGui.TableNextRow();
            for (int x = -2; x <= 2; x++)
            {
                ImGui.TableNextColumn();
                ImGui.Text(GetNoteName(y, x));
            }
        }

        ImGui.EndTable();

        ImGui.End();

        ImGui.Begin("Synth");

        ImGui.InputFloat("Root frequency", ref _synth.RootFrequency);

        ImGui.Text("Zoom");
        ImGui.SliderFloat("Horizontal", ref _zoomX, 0.1f, 10f, "%.3f", ImGuiSliderFlags.Logarithmic);
        ImGui.SliderFloat("Vertical", ref _zoomY, 0.01f, 1f, "%.3f", ImGuiSliderFlags.Logarithmic);
        ImGui.SliderFloat("Pan X", ref _viewX, 0f, 8f);
        ImGui.SliderFloat("Pan Y", ref _viewY, 0f, 8f);

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
            ImGui.Separator();

            if (ImGui.InputText("Start intervals", ref _selectedNote.StartIntervals, 64))
            {
                _selectedNote.ApplyStartIntervals();
            }

            if (ImGui.InputText("End intervals", ref _selectedNote.EndIntervals, 64))
            {
                _selectedNote.ApplyEndIntervals();
            }

            if (ImGui.Button("- Remove"))
            {
                _pendingDelete = _selectedNote.Note;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        HandleDragging();

        if (_pendingDelete != null)
        {
            _synth.Notes.Remove(_pendingDelete);

            if (_selectedNote?.Note == _pendingDelete)
                _selectedNote = null;

            if (_dragTarget?.Note == _pendingDelete)
                _dragTarget = null;

            _pendingDelete = null;
        }
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

        bool hovered = ImGui.InvisibleButton(
            $"ep_{which}_{note.GetHashCode()}",
            new Vector2(size * 2)
        );

        Vector2 mouse = ImGui.GetIO().MousePos;

        // START DRAG
        if (ImGui.IsItemActivated())
        {
            _dragging = true;
            _dragTarget = new(note, which);
            _dragStartMouse = mouse;
            _dragMoved = false;
        }

        // DETECT MOVEMENT
        if (_dragging && Vector2.Distance(mouse, _dragStartMouse) > 3f)
        {
            _dragMoved = true;
        }

        // CLICK VS DRAG RESOLUTION ON RELEASE
        if (ImGui.IsItemDeactivated())
        {
            if (!_dragMoved)
            {
                _pendingSelectedNote = new(note, which);
            }

            _dragging = false;
            _dragTarget = null;
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

    static string SerializeIntervals(List<Rational> intervals)
    {
        if (intervals.Count == 0) return "";

        return string.Join(" * ", intervals.Select(r => $"{r.Numerator}/{r.Denominator}"));
    }

    static List<Rational> DeserializeIntervals(string input)
    {
        List<Rational> fallback = [new(1, 1)];

        if (string.IsNullOrWhiteSpace(input)) return fallback;

        try
        {
            var parts = input.Split('*');
            var result = new List<Rational>();

            foreach (string part in parts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                var fracParts = trimmed.Split('/');
                if (fracParts.Length != 2)
                    continue;

                if (int.TryParse(fracParts[0].Trim(), out int n) &&
                    int.TryParse(fracParts[1].Trim(), out int d) &&
                    d != 0)
                {
                    result.Add(new Rational(n, d));
                }
            }

            return result.Count > 0 ? result : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    static double SnapBeat(double beat)
    {
        const double snap = 1 / 4f;
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
        public string StartIntervals;
        public string EndIntervals;

        public SelectedNote(Note note, SelectType type)
        {
            Note = note;
            Type = type;
            StartIntervals = SerializeIntervals(note.StartIntervals);
            EndIntervals = SerializeIntervals(note.EndIntervals);
        }

        public void ApplyStartIntervals()
        {
            Note.StartIntervals.Clear();
            Note.StartIntervals.AddRange(DeserializeIntervals(StartIntervals));
        }

        public void ApplyEndIntervals()
        {
            Note.EndIntervals.Clear();
            Note.EndIntervals.AddRange(DeserializeIntervals(EndIntervals));
        }
    }

    enum SelectType { Start, End }
}