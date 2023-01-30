using jp.nyatla.kokolink.io.audioif;



foreach(var i in NAudioInputInterator.GetDevices())
{
    Console.WriteLine(i);
}
int framerate = 8000;
using (var na = new NAudioInputInterator(framerate: framerate, bits_par_sample:16))
{
    na.Start();
    try
    {
        for (; ; )
        {
            int scale = framerate / 30;
            double s = 0;
            for (var i = 0; i < scale; i++)
            {
                s = s + Math.Abs(na.Next());
            }
            s /= scale;
            var v = (int)(Math.Min(Math.Max(Math.Log10(s) + 2.2, 0), 2) * 25);
            Console.Write("\r" + new String('#', v) + new string(' ', (50 - v)));
        }

    }
    finally
    {
        Console.WriteLine();
        na.Stop();
    }

}
