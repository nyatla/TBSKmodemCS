﻿using System.Text;
using jp.nyatla.kokolink.protocol.tbsk.tbaskmodem;
using jp.nyatla.kokolink.protocol.tbsk.toneblock;
using jp.nyatla.kokolink.utils.wavefile;


var tone = new XPskSinTone(10, 10).Mul(0.5);//    # SSFM DPSK
var payload = Encoding.ASCII.GetBytes("0123456789");// # 10byte
uint carrier = 8000;

//# modulation
var mod = new TbskModulator(tone);
IList<double> src_pcm = mod.Modulate(payload).ToList();

//# save to wave
var wav = new PcmData(src_pcm, 16, carrier);
using (var stream = File.Open("./step3.cs.wav", FileMode.Create, FileAccess.Write))
{
    var pcm = new PcmData(src_pcm, 16, (uint)carrier);
    PcmData.Dump(pcm, stream);
}

//# demodulate to bytes
var demod = new TbskDemodulator(tone);
var ret = demod.DemodulateAsBytes(wav.DataAsFloat());

foreach(var i in ret!)
{
    Console.Write(String.Format("{0}({1}) ", i,Encoding.ASCII.GetChars(new byte[] { i })[0]));
}
Console.WriteLine();