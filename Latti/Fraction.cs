namespace Latti;

public struct Fraction
{
    public int Numerator;
    public int Denominator;

    public readonly float Value => ((float)Numerator) / Denominator;

    public Fraction(int numerator = 1, int denominator = 1)
    {
        Numerator = numerator;
        Denominator = denominator;
    }
}
