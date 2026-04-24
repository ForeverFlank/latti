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

        Vector2 canvasSize = new(ImGui.GetWindowWidth() - 16f, 300f);
        Vector2 pMin = ImGui.GetCursorScreenPos();
        _viewport.Update(pMin, canvasSize);

        dl.AddRect(_viewport.PMin, _viewport.PMax, ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.3f, 1f)));

        ImGui.PushClipRect(_viewport.PMin, _viewport.PMax, true);
        DrawBeatLines(dl);
        foreach (Note note in _synth.Notes) DrawNoteCurve(note, dl);
        ImGui.PopClipRect();

        foreach (Note note in _synth.Notes)
        {
            bool isSelected = _interaction.SelectedNote?.Note == note;
            DrawPitchPoints(note, dl, isSelected);
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
        ImGui.SliderFloat("Horizontal", ref _viewport.ZoomX, 0.1f, 10f, "%.3f", ImGuiSliderFlags.Logarithmic);
        ImGui.SliderFloat("Vertical", ref _viewport.ZoomY, 0.01f, 1f, "%.3f", ImGuiSliderFlags.Logarithmic);
        ImGui.SliderFloat("Pan X", ref _viewport.ViewX, 0f, 8f);
        ImGui.SliderFloat("Pan Y", ref _viewport.ViewY, 0f, 8f);
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
        double startBeat = _synth.Tempo.SecondsToBeats(_viewport.ViewX);
        double endBeat = _synth.Tempo.SecondsToBeats(_viewport.ViewX + _viewport.CanvasSize.X / _viewport.ZoomX);

        int firstBeat = (int)Math.Floor(startBeat);
        int lastBeat = (int)Math.Ceiling(endBeat);

        for (int b = firstBeat; b <= lastBeat; b++)
        {
            float x = (float)(_viewport.PMin.X + (_synth.Tempo.BeatsToSeconds(b) - _viewport.ViewX) * _viewport.ZoomX * _viewport.CanvasSize.X);
            bool isBar = b % 4 == 0;
            uint color = isBar
                ? ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1f))
                : ImGui.GetColorU32(new Vector4(0.25f, 0.25f, 0.25f, 1f));
            dl.AddLine(new Vector2(x, _viewport.PMin.Y), new Vector2(x, _viewport.PMax.Y), color, isBar ? 1.5f : 1f);
        }
    }

    void DrawNoteCurve(Note note, ImDrawListPtr dl)
    {
        const int segments = 64;

        Vector2[] points = Enumerable.Range(0, segments).Select(i =>
        {
            double t = i / (segments - 1.0);
            double beat = note.StartBeat + t * note.DurationBeats;
            return _viewport.BeatToScreen(note, beat, _synth.RootFrequency, _synth.Tempo);
        }).ToArray();

        dl.AddPolyline(ref points[0], points.Length, ImGui.GetColorU32(new Vector4(1f)), ImDrawFlags.None, 1.0f);
    }

    void DrawPitchPoints(Note note, ImDrawListPtr dl, bool isSelected)
    {
        for (int i = 0; i < note.PitchBeats.Count; i++)
        {
            Vector2 pos = _viewport.BeatToScreen(note, note.PitchBeats[i], _synth.RootFrequency, _synth.Tempo);
            if (!_viewport.IsOnScreen(pos)) continue;

            bool isFirst = i == 0;

            if (isSelected)
            {
                uint color = ImGui.GetColorU32(isFirst ? new Vector4(1f) : new Vector4(0.6f, 0.9f, 1f, 1f));
                DrawDiamond(dl, pos, 8f, color);
                _interaction.HandlePointInteraction(note, i, pos, 8f, canDelete: isFirst);
            }
            else if (isFirst)
            {
                DrawDiamond(dl, pos, 10f, ImGui.GetColorU32(new Vector4(1f)));
                _interaction.HandlePointInteraction(note, 0, pos, 10f, canDelete: true);
            }
            else
            {
                DrawDiamond(dl, pos, 5f, ImGui.GetColorU32(new Vector4(0.5f, 0.7f, 0.8f, 0.7f)));
            }
        }
    }

    void DrawSelectionPopup()
    {
        SelectedNote? sel = _interaction.SelectedNote;
        if (!ImGui.BeginPopup("NotePopup") || sel is null) return;

        ImGui.Text($"Editing note pitch point {sel.PitchIndex}");
        ImGui.Separator();

        if (ImGui.InputText("Intervals", ref sel.IntervalsText, 64)) sel.ApplyIntervals();

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