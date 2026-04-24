using System.Numerics;
using ImGuiNET;

namespace Latti;

class NoteEditorUI
{
    readonly Synth _synth;
    readonly NoteEditorViewport _viewport = new();
    readonly NoteEditorInteraction _interaction = new();

    public NoteEditorUI(Synth synth)
    {
        _synth = synth;
    }

    public void Draw()
    {
        ImGui.Begin("Note roll");

        DrawViewportControls();
        DrawAddNoteButton();

        ImDrawListPtr dl = ImGui.GetWindowDrawList();

        Vector2 canvasSize = new(ImGui.GetWindowWidth() - 16f, 400f);
        Vector2 pMin = ImGui.GetCursorScreenPos();
        _viewport.Update(pMin, canvasSize);

        dl.AddRect(_viewport.PMin, _viewport.PMax, ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.3f, 1f)));

        ImGui.PushClipRect(_viewport.PMin, _viewport.PMax, true);

        DrawBeatLines(dl);
        DrawPitchLines(dl);
        foreach (Note note in _synth.Notes) DrawNoteCurve(note, dl);

        ImGui.PopClipRect();

        foreach (Note note in _synth.Notes)
        {
            DrawPitchPoints(note, dl);
        }

        _interaction.FlushPendingSelection();
        DrawSelectionPopup();
        _interaction.HandleDragging(_viewport, _synth.Tempo);
        _interaction.FlushPendingDelete(_synth.Notes);

        ImGui.End();
    }

    void DrawViewportControls()
    {
        ImGui.Text("Zoom");
        ImGui.SliderFloat("Horizontal", ref _viewport.Zoom.X, 0.1f, 10f, "%.3f", ImGuiSliderFlags.Logarithmic);
        ImGui.SliderFloat("Vertical", ref _viewport.Zoom.Y, 0.01f, 1f, "%.3f", ImGuiSliderFlags.Logarithmic);
        ImGui.SliderFloat("Pan X", ref _viewport.View.X, 0f, 8f);
        ImGui.SliderFloat("Pan Y", ref _viewport.View.Y, 0f, 8f);
    }

    void DrawAddNoteButton()
    {
        if (ImGui.Button("+ Add note"))
        {
            _synth.Notes.Add(new Note { Velocity = 0.75f });
        }
    }

    void DrawBeatLines(ImDrawListPtr dl)
    {
        uint colorBar = ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1f));
        uint colorBeat = ImGui.GetColorU32(new Vector4(0.25f, 0.25f, 0.25f, 1f));

        double startBeat = _synth.Tempo.SecondsToBeats(_viewport.View.X);
        double endBeat = _synth.Tempo.SecondsToBeats(_viewport.View.X + _viewport.CanvasSize.X / _viewport.Zoom.X);

        int firstBeat = (int)Math.Floor(startBeat);
        int lastBeat = (int)Math.Ceiling(endBeat);

        for (int b = firstBeat; b <= lastBeat; b++)
        {
            float x = (float)(_viewport.PMin.X + (_synth.Tempo.BeatsToSeconds(b) - _viewport.View.X) * _viewport.Zoom.X * _viewport.CanvasSize.X);
            bool isBar = b % 4 == 0;
            uint color = isBar ? colorBar : colorBeat;
            dl.AddLine(new Vector2(x, _viewport.PMin.Y), new Vector2(x, _viewport.PMax.Y), color, isBar ? 1.5f : 1f);

            if (!isBar) continue;
            int bar = b / 4;
            dl.AddText(new Vector2(x + 3f, _viewport.PMin.Y + 2f), colorBar, $"{bar}");
        }
    }

    void DrawPitchLines(ImDrawListPtr dl)
    {
        uint color = ImGui.GetColorU32(new Vector4(0.4f, 0.4f, 0.4f, 1f));

        float log2ViewBottom = _viewport.View.Y;
        float log2ViewTop = _viewport.View.Y + _viewport.CanvasSize.Y / _viewport.Zoom.Y;

        float log2Root = MathF.Log2(_synth.RootFrequency);

        int firstOctave = (int)Math.Floor(log2ViewBottom - log2Root);
        int lastOctave = (int)Math.Ceiling(log2ViewTop - log2Root);

        for (int n = firstOctave; n <= lastOctave; n++)
        {
            float log2Freq = log2Root + n;
            float y = _viewport.PMax.Y - (log2Freq - _viewport.View.Y) * _viewport.Zoom.Y * _viewport.CanvasSize.Y;

            if (y < _viewport.PMin.Y || y > _viewport.PMax.Y) continue;

            dl.AddLine(new Vector2(_viewport.PMin.X, y), new Vector2(_viewport.PMax.X, y), color, 1f);

            string label = n switch
            {
                0 => "1/1",
                > 0 => $"{1 << n}/1",
                < 0 => $"1/{1 << -n}"
            };

            dl.AddText(new Vector2(_viewport.PMin.X + 3f, y + 4f), color, label);
        }
    }

    void DrawNoteCurve(Note note, ImDrawListPtr dl)
    {
        const int segments = 64;

        Vector2[] points = Enumerable.Range(0, segments).Select(i =>
        {
            double t = i / (segments - 1.0);
            double beat = note.StartBeat + t * note.DurationBeats;
            return _viewport.NoteToScreen(note, beat, _synth.RootFrequency, _synth.Tempo);
        }).ToArray();

        dl.AddPolyline(ref points[0], points.Length, ImGui.GetColorU32(new Vector4(1f)), ImDrawFlags.None, 1.0f);
    }

    void DrawPitchPoints(Note note, ImDrawListPtr dl)
    {
        for (int i = 0; i < note.PitchBeats.Count; i++)
        {
            Vector2 pos = _viewport.NoteToScreen(note, note.PitchBeats[i], _synth.RootFrequency, _synth.Tempo);
            if (!_viewport.IsOnScreen(pos)) continue;

            bool isFirst = i == 0;

            if (isFirst)
            {
                DrawDiamond(dl, pos, 10f, ImGui.GetColorU32(new Vector4(1f)));
                _interaction.HandlePointInteraction(note, 0, pos, 10f, true);
            }
            else
            {
                DrawDiamond(dl, pos, 6f, ImGui.GetColorU32(new Vector4(0.7f)));
                _interaction.HandlePointInteraction(note, i, pos, 8f, false);
            }
        }
    }

    void DrawSelectionPopup()
    {
        SelectedNote? sel = _interaction.SelectedNote;
        if (!ImGui.BeginPopup("NotePopup") || sel is null) return;

        ImGui.SeparatorText("Intervals");

        for (int i = 0; i < sel.IntervalsText.Length; i++)
        {
            if (ImGui.InputText($"##{i}", ref sel.IntervalsText[i], 64)) sel.ApplyIntervals();
        }

        ImGui.EndPopup();
    }

    static void DrawDiamond(ImDrawListPtr dl, Vector2 center, float size, uint color)
    {
        dl.AddQuad(
            new Vector2(center.X, center.Y - size),
            new Vector2(center.X + size, center.Y),
            new Vector2(center.X, center.Y + size),
            new Vector2(center.X - size, center.Y),
            color
        );
    }
}