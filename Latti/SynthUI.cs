using ImGuiNET;

namespace Latti;

public class SynthUI
{
    readonly Synth _synth;
    readonly NoteEditorUI _noteEditor;

    public SynthUI(Synth synth)
    {
        _synth = synth;
        _noteEditor = new NoteEditorUI(synth);
    }

    public void DrawUI()
    {
        DrawJIGrid();
        DrawSynthControl();
        _noteEditor.Draw();
    }

    void DrawJIGrid()
    {
        string[][] noteNames = [
            ["Gb++ ", "Bb+  ", "D    ", "F#-  ", "A#-- "],
            ["Cb++ ", "Eb+  ", "G    ", "B-   ", "D#-- "],
            ["Fb++ ", "Ab+  ", "C    ", "E-   ", "G#-- "],
            ["Bbb++", "Db+  ", "F    ", "A-   ", "C#-- "],
            ["Ebb++", "Gb+  ", "Bb   ", "D-   ", "F#-- "],
        ];

        ImGui.Begin("JI Grid");
        ImGui.BeginTable("##table", 5);

        for (int y = -2; y <= 2; y++)
        {
            ImGui.TableNextRow();
            for (int x = -2; x <= 2; x++)
            {
                ImGui.TableNextColumn();
                ImGui.Text(noteNames[y + 2][x + 2]);
            }
        }

        ImGui.EndTable();
        ImGui.End();
    }

    void DrawSynthControl()
    {
        ImGui.Begin("Synth Control");

        ImGui.InputFloat("Root frequency", ref _synth.RootFrequency);
        ImGui.Separator();

        ImGui.InputFloat("BPM", ref _synth.Tempo.Bpm);
        ImGui.Separator();

        ImGui.End();
    }
}