﻿using jp.nyatla.tbaskmodem;
using jp.nyatla.kokolink.utils.wavefile;


var tone = TbskTone.CreateXPskSin(10, 10).Mul(0.5);//    # SSFM DPSK
var payload = "アンタヤルーニャ";
int carrier = 8000;

//# modulation
var mod = new TbskModulator(tone);
IList<double> src_pcm = mod.Modulate(payload).ToList();

//# save to wave
var wav = new PcmData(src_pcm, 16, carrier);
using (var stream = File.Open("./step4.cs.wav", FileMode.Create, FileAccess.Write))
{
    var pcm = new PcmData(src_pcm, 16, carrier);
    PcmData.Dump(pcm, stream);
}

//# demodulate to bytes
var demod = new TbskDemodulator(tone);
var ret = demod.DemodulateAsStr(wav.DataAsFloat());

foreach (var i in ret!)
{
    Console.Write(String.Format("{0}", i));
}

