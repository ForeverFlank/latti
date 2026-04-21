namespace Latti;

public class Program
{
    public static void Main()
    {
        using DAW synth = new(1280, 720);
        synth.Run();
    }
}