
using jp.nyatla.tbaskmodem;
using jp.nyatla.kokolink.utils.wavefile;


var tone = TbskTone.CreateXPskSin(10, 10).Mul(0.5);
//var tone = TbskTone.CreateSin(100,1);
var payload = new List<int>();
for (int i = 0; i < 16; i++)
{
    payload.AddRange(new int[] { 0, 1, 0, 1, 0, 1, 0, 1 });
}

var carrier = 8000;
var mod = new TbskModulator(tone);

var src_pcm = new List<double>(mod.ModulateAsBit(payload));


using (var stream = File.Open("../../../step1.cs.wav", FileMode.Create, FileAccess.Write))
{
    var pcm=new PcmData(src_pcm,16, carrier);
    PcmData.Dump(pcm, stream);
}


