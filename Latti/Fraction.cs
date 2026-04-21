namespace Latti;

public struct Rational
{
    public int Numerator;
    public int Denominator;

    public readonly float Value => ((float)Numerator) / Denominator;

    public Rational(int numerator = 1, int denominator = 1)
    {
        Numerator = numerator;
        Denominator = denominator;
    }
}
