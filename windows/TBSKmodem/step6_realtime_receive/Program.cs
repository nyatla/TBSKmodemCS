using jp.nyatla.kokolink.protocol.tbsk.tbaskmodem;
using jp.nyatla.kokolink.protocol.tbsk.toneblock;
using jp.nyatla.kokolink.utils.wavefile;
using jp.nyatla.kokolink.io.audioif;
using NAudio.SoundFont;
using System.IO;
using jp.nyatla.kokolink.compatibility;




//#save to sample
var tone = new XPskSinTone(10, 10).Mul(0.5);    //# SSFM DPSK
var payload = "アンタヤルーニャ";// # ?byte
int carrier = 16000;

//# modulation
var mod = new TbskModulator(tone);
var padding = new double[carrier / 2];
Array.Fill<double>(padding, 0);
var wav=Functions.Flatten<double>(padding, mod.Modulate(payload), padding);

//#save to wave
using (var stream = File.Open("step6.wav", FileMode.Create, FileAccess.Write))
{
    var pcm = new PcmData(wav, 16, (uint)carrier);
    PcmData.Dump(pcm, stream);
}




Console.WriteLine(String.Format("{0}bps",carrier / tone.Count));
Console.WriteLine("Play step6.wave in your player.");
Console.WriteLine("Start capturing");


var demod = new TbskDemodulator(tone);
using (var stream=new NAudioInputInterator(carrier, bits_par_sample: 16,device_no: -1)) {
    Console.WriteLine("Ctrl^C to stop.");
    stream.Start();
    while (true)
    {
        while (true)
        {
            //print(">", end = "", flush = True)
            var s = demod.DemodulateAsStr(stream);
            if (s == null)
            {
                break;
            }
            foreach (var i in s)
            {
                Console.Write(i);
            }
            Console.WriteLine("\nEnd of signal.");

        }

    }

}

