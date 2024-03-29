﻿using jp.nyatla.kokolink.io.audioif;



foreach(var i in NAudioInputIterator.GetDevices())
{
    Console.WriteLine(i);
}
int framerate = 8000;
using (var na = new NAudioInputIterator(framerate: framerate, bits_par_sample:16))
{
    na.Start();
    try
    {
        for (; ; )
        {
            Thread.Sleep(30);
            var rms = na.GetRms();
            var v = (int)Math.Max(0, ((rms==0?0:Math.Log(rms)) + 5) * 5);
            Console.Write("\r" + new String('#', v) + new string(' ', (50 - v)));
        }

    }
    finally
    {
        Console.WriteLine();
        na.Stop();
    }

}
