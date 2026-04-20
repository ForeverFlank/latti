namespace Latti;

public class Program
{
    public static void Main()
    {
        using SynthWindow synth = new(1280, 720);
        synth.Run();
    }
}