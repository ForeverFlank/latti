namespace Latti;

class SelectedNote
{
    public Note Note;
    public int PitchIndex;
    public string IntervalsText;

    public SelectedNote(Note note, int pitchIndex)
    {
        Note = note;
        PitchIndex = pitchIndex;
        IntervalsText = IntervalSerializer.Serialize(note.PitchIntervals[pitchIndex]);
    }

    public void ApplyIntervals()
    {
        Note.PitchIntervals[PitchIndex] = IntervalSerializer.Deserialize(IntervalsText);
    }
}

static class IntervalSerializer
{
    public static string Serialize(List<Rational> intervals)
    {
        if (intervals.Count == 0) return "";
        return string.Join(" * ", intervals.Select(r => $"{r.Numerator}/{r.Denominator}"));
    }

    public static List<Rational> Deserialize(string input)
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
}