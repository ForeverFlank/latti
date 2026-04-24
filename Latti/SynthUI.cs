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

        ImGui.Separator();

        ImGui.InputFloat("BPM", ref _synth.Tempo.Bpm);

        ImGui.Separator();

        ImGui.Text("Zoom");
        ImGui.SliderFloat("Horizontal", ref _zoomX, 0.1f, 10f, "%.3f", ImGuiSliderFlags.Logarithmic);
        ImGui.SliderFloat("Vertical", ref _zoomY, 0.01f, 1f, "%.3f", ImGuiSliderFlags.Logarithmic);
        ImGui.SliderFloat("Pan X", ref _viewX, 0f, 8f);
        ImGui.SliderFloat("Pan Y", ref _viewY, 0f, 8f);

        if (ImGui.Button("+ Add note"))
        {
            _synth.Notes.Add(new Note
            {
                Velocity = 0.75f
            });
        }

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
            DrawNoteCurve(note, dl);
        }
        ImGui.PopClipRect();

        foreach (Note note in _synth.Notes)
        {
            bool isSelected = _selectedNote?.Note == note;
            if (isSelected)
            {
                DrawPitchPoints(note, dl);
            }
            else
            {
                DrawStartPoint(note, dl);
            }
        }

        if (!_dragging && _pendingSelectedNote is not null)
        {
            _selectedNote = _pendingSelectedNote;
            _pendingSelectedNote = null;
            ImGui.OpenPopup("NotePopup");
        }

        if (ImGui.BeginPopup("NotePopup") && _selectedNote is not null)
        {
            ImGui.Text($"Editing note pitch point {_selectedNote.PitchIndex}");
            ImGui.Separator();

            if (ImGui.InputText("Intervals", ref _selectedNote.IntervalsText, 64))
            {
                _selectedNote.ApplyIntervals();
            }

            ImGui.EndPopup();
        }

        HandleDragging();

        if (_pendingDelete != null)
        {
            _synth.Notes.Remove(_pendingDelete);

            if (_selectedNote?.Note == _pendingDelete) _selectedNote = null;
            if (_dragTarget?.Note == _pendingDelete) _dragTarget = null;

            _pendingDelete = null;
        }
    }

    Vector2 BeatToScreen(Note note, double beat)
    {
        float freq = note.GetFrequency(_synth.RootFrequency, beat);
        float time = (float)_synth.Tempo.BeatsToSeconds(beat);
        return TimeToScreen(time, MathF.Log2(freq));
    }

    void DrawNoteCurve(Note note, ImDrawListPtr dl)
    {
        const int segments = 64;

        Vector2[] points = Enumerable.Range(0, segments).Select(i =>
        {
            double t = i / (segments - 1.0);
            double beat = note.StartBeat + t * note.DurationBeats;
            return BeatToScreen(note, beat);
        }).ToArray();

        dl.AddPolyline(ref points[0], points.Length, ImGui.GetColorU32(new Vector4(1f)), ImDrawFlags.None, 1.0f);
    }

    void DrawStartPoint(Note note, ImDrawListPtr dl)
    {
        Vector2 pos = BeatToScreen(note, note.StartBeat);
        if (!IsOnScreen(pos)) return;

        DrawDiamond(pos, 10f, ImGui.GetColorU32(new Vector4(1f)), dl);
        HandlePointInteraction(note, 0, pos, 10f, canDelete: true);
    }

    void DrawPitchPoints(Note note, ImDrawListPtr dl)
    {
        for (int i = 0; i < note.PitchBeats.Count; i++)
        {
            Vector2 pos = BeatToScreen(note, note.StartBeat + note.PitchBeats[i]);
            if (!IsOnScreen(pos)) continue;

            bool isFirst = i == 0;
            uint color = ImGui.GetColorU32(isFirst ? new Vector4(1f) : new Vector4(0.6f, 0.9f, 1f, 1f));
            DrawDiamond(pos, 8f, color, dl);
            HandlePointInteraction(note, i, pos, 8f, canDelete: isFirst);
        }
    }

    static void DrawDiamond(Vector2 center, float size, uint color, ImDrawListPtr dl)
    {
        dl.AddQuad(
            new Vector2(center.X, center.Y - size),
            new Vector2(center.X + size, center.Y),
            new Vector2(center.X, center.Y + size),
            new Vector2(center.X - size, center.Y),
            color
        );
    }

    void HandlePointInteraction(Note note, int pitchIndex, Vector2 center, float size, bool canDelete)
    {
        ImGui.SetCursorScreenPos(center - new Vector2(size, size));
        ImGui.InvisibleButton($"ep_{pitchIndex}_{note.GetHashCode()}", new Vector2(size * 2));

        Vector2 mouse = ImGui.GetIO().MousePos;

        if (ImGui.IsItemActivated())
        {
            _dragging = true;
            _dragTarget = new(note, pitchIndex);
            _dragStartMouse = mouse;
            _dragMoved = false;
        }

        if (_dragging && Vector2.Distance(mouse, _dragStartMouse) > 3f) _dragMoved = true;

        if (ImGui.IsItemDeactivated())
        {
            if (!_dragMoved) _pendingSelectedNote = new(note, pitchIndex);
            _dragging = false;
            _dragTarget = null;
        }

        if (canDelete && ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            _pendingDelete = note;
        }
    }

    void HandleDragging()
    {
        if (!_dragging || _dragTarget == null) return;

        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            _dragging = false;
            _dragTarget = null;
            return;
        }

        Vector2 mouse = ImGui.GetIO().MousePos;
        double beat = SnapBeat(ScreenToBeat(mouse.X));

        Note note = _dragTarget.Note;
        int idx = _dragTarget.PitchIndex;

        if (idx == 0)
        {
            double delta = beat - note.StartBeat;
            for (int i = 0; i < note.PitchBeats.Count; i++)
            {
                note.PitchBeats[i] += delta;
            }
        }
        else if (idx == note.PitchBeats.Count - 1)
        {
            double minEnd = note.StartBeat + note.PitchBeats[^2] + 0.01;
            note.PitchBeats[^1] = Math.Max(beat - note.StartBeat, minEnd - note.StartBeat);
        }
        else
        {
            double localBeat = beat - note.StartBeat;
            double prev = note.PitchBeats[idx - 1];
            double next = note.PitchBeats[idx + 1];
            note.PitchBeats[idx] = Math.Clamp(localBeat, prev + 0.01, next - 0.01);
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
            string[] parts = input.Split('*');
            List<Rational> result = [];

            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                string[] fracParts = trimmed.Split('/');
                if (fracParts.Length != 2) continue;

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
        const double snap = 1 / 4.0;
        return Math.Round(beat / snap) * snap;
    }

    Vector2 TimeToScreen(float t, float y) => new(
        _pMin.X + (t - _viewX) * _zoomX * _canvasSize.X,
        _pMax.Y - (y - _viewY) * _zoomY * _canvasSize.Y
    );

    double ScreenToBeat(float x)
    {
        float time = (x - _pMin.X) / (_zoomX * _canvasSize.X) + _viewX;
        return _synth.Tempo.SecondsToBeats(time);
    }

    bool IsOnScreen(Vector2 p) =>
        p.X >= _pMin.X && p.X <= _pMax.X &&
        p.Y >= _pMin.Y && p.Y <= _pMax.Y;

    class SelectedNote
    {
        public Note Note;
        public int PitchIndex;
        public string IntervalsText;

        public SelectedNote(Note note, int pitchIndex)
        {
            Note = note;
            PitchIndex = pitchIndex;
            IntervalsText = SerializeIntervals(note.PitchIntervals[pitchIndex]);
        }

        public void ApplyIntervals()
        {
            Note.PitchIntervals[PitchIndex] = DeserializeIntervals(IntervalsText);
        }
    }
}