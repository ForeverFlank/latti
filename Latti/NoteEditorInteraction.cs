using System.Numerics;
using ImGuiNET;

namespace Latti;

class NoteEditorInteraction
{
    SelectedNote? _pendingSelectedNote;
    SelectedNote? _dragTarget;
    Note? _pendingDelete;
    bool _dragging;
    Vector2 _dragStartMouse;
    bool _dragMoved;

    public SelectedNote? SelectedNote { get; private set; }

    public void HandlePointInteraction(Note note, int pitchIndex, Vector2 center, float size, bool canDelete)
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

    public void HandleDragging(NoteEditorViewport viewport, Tempo tempo)
    {
        if (!_dragging || _dragTarget == null) return;

        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            _dragging = false;
            _dragTarget = null;
            return;
        }

        Vector2 mouse = ImGui.GetIO().MousePos;
        double beat = NoteEditorViewport.SnapBeat(viewport.ScreenToBeat(mouse.X, tempo));

        Note note = _dragTarget.Note;
        int idx = _dragTarget.PitchIndex;

        if (idx == 0)
        {
            double delta = beat - note.StartBeat;
            for (int i = 0; i < note.PitchBeats.Count; i++) note.PitchBeats[i] += delta;
        }
        else
        {
            double localBeat = beat - note.StartBeat;
            double prev = note.PitchBeats[idx - 1];
            double clamped = Math.Max(localBeat, prev);
            double delta = clamped - note.PitchBeats[idx];

            for (int i = idx; i < note.PitchBeats.Count; i++) note.PitchBeats[i] += delta;
        }
    }

    public void FlushPendingSelection()
    {
        if (_dragging || _pendingSelectedNote is null) return;

        SelectedNote = _pendingSelectedNote;
        _pendingSelectedNote = null;
        ImGui.OpenPopup("NotePopup");
    }

    public void FlushPendingDelete(List<Note> notes)
    {
        if (_pendingDelete == null) return;

        notes.Remove(_pendingDelete);

        if (SelectedNote?.Note == _pendingDelete) SelectedNote = null;
        if (_dragTarget?.Note == _pendingDelete) _dragTarget = null;

        _pendingDelete = null;
    }

    public bool IsDragging => _dragging;
}