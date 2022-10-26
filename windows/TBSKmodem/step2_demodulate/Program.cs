using jp.nyatla.kokolink.protocol.tbsk.tbaskmodem;
using jp.nyatla.kokolink.protocol.tbsk.toneblock;
using jp.nyatla.kokolink.utils.wavefile;


var tone = new XPskSinTone(10, 10);
var demod = new TbskDemodulator(tone);


PcmData pcm;
using (var stream = new FileStream("../../../../step1_modulate/step1.cs.wav", FileMode.Open, FileAccess.Read))
{
    pcm=PcmData.Load(stream);
    //var pcm = new PcmData(src_pcm, 16, (uint)carrier);
}

var ret=demod.DemodulateAsBit(pcm.DataAsFloat());
if (ret == null)
{
    Console.WriteLine("None");
}
else
{
    //シーケンシャルEnumurableは終端がないのでCountによる計測はできません。
    var a=ret.ToArray();
    Console.WriteLine(a.Length);
    foreach(var i in a)
    {
        Console.Write(i);
        Console.Write(",");
    }
    Console.WriteLine();
}


   