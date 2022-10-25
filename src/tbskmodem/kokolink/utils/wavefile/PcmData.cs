using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using jp.nyatla.kokolink.utils.wavefile.riffio;
using jp.nyatla.kokolink.compatibility;
namespace jp.nyatla.kokolink.utils.wavefile{



    // """ wavファイルのラッパーです。1chモノラルpcmのみ対応します。
    // """
    public class PcmData{
        // """-1<n<1をWaveファイルペイロードへ変換
        // """
        // if bits==8:
        //     # a=np.array([i*127+128 for i in fdata]).astype("uint8").tobytes()
        //     # b=b"".join([struct.pack("B",int(i*127+128)) for i in fdata])
        //     # print(a==b)
        //     # assert(a==b)
        //     # return np.array([i*127+128 for i in fdata]).astype("uint8").tobytes()
        //     return b"".join([struct.pack("B",int(i*127+128)) for i in fdata])
        // elif bits==16:
        //     r=(2**16-1)//2 #Daisukeパッチ
        //     # a=b"".join([struct.pack("<h",int(i*r)) for i in fdata])
        //     # b=np.array([i*r for i in fdata]).astype("int16").tobytes()
        //     # print(a==b)
        //     # assert(a==b)
        //     # return np.array([i*r for i in fdata]).astype("int16").tobytes()
        //     return b"".join([struct.pack("<h",int(i*r)) for i in fdata])
        // raise ValueError()
        // """-1<n<1をWaveファイルペイロードへ変換
        // """
        static private byte[] Float2bytes(IEnumerable<double> fdata, int bits)
        {
            if (bits == 8)
            {
                var ret = new List<byte>();
                foreach (var i in fdata)
                {
                    ret.Add((byte)(i * 127 + 128)); //uint8
                }
                return ret.ToArray();
            }
            else if (bits == 16)
            {
                int r = (int)((Math.Pow(2,16) - 1) / 2); //#Daisukeパッチ
                var ret = new List<byte>();
                foreach (var i in fdata)
                {
                    var f = i * r;
                    if (f >= 0)
                    {
                        UInt16 v = (UInt16)(Math.Min(Int16.MaxValue, f));
                        ret.Add((byte)(v & 0xff));
                        ret.Add((byte)((v >> 8) & 0xff));
                    }
                    else
                    {
                        UInt16 v =(UInt16)(0xffff + (int)(Math.Max(f, Int16.MinValue)) + 1);
                        ret.Add((byte)(v & 0xff));
                        ret.Add((byte)((v >> 8) & 0xff));
                    }
                }
                return ret.ToArray();
            }
            throw new ArgumentException("Invalid bits");
        }


        public static (PcmData,IEnumerable<Chunk?>?) Load(Stream fp,IEnumerable<string>? chunks)
        {
            var wav=new WaveFile(fp);
            var fmtc=(FmtChunk?)(wav.Chunk("fmt "));
            var datac=(RawChunk?)(wav.Chunk("data"));
            Debug.Assert(fmtc!=null && datac!=null && fmtc.Nchannels == 1);

            var bits=fmtc.Samplewidth;
            var fs=fmtc.Framerate;
            if(chunks!=null){
                var cnk=new List<Chunk?>();
                foreach(var i in chunks){
                    cnk.Add(wav.Chunk(i));
                }
                return (new PcmData(datac.Data,bits,fs),cnk);
            }else{
                return (new PcmData(datac.Data,bits,fs),null);
            }
        }
        public static PcmData Load(Stream fp){
            return PcmData.Load(fp,null).Item1;
        }


        public static void Dump(PcmData src,Stream fp,IEnumerable<Chunk>? chunks=null){
            // # setting parameters
            var wf=new WaveFile((int)src.Frame_rate,(int)(src.Sample_bits/8),1,src.Data,chunks);
            fp.Write(wf.ToChunkBytes());
        }
        readonly private int _sample_bits;
        readonly private uint _frame_rate;
        readonly private byte[] _frames;

        public PcmData(IEnumerable<byte> frames, int sample_bits, uint frame_rate) 
        {
            this._sample_bits = sample_bits;
            this._frame_rate = frame_rate;
            this._frames = frames.ToArray();
            Debug.Assert((this._frames.Length) % (this._sample_bits / 8) == 0);//   #srcの境界チェック
        }
        public PcmData(IEnumerable<double> frames, int sample_bits, uint frame_rate) :
            this(Float2bytes(frames, sample_bits), sample_bits, frame_rate)
        {
        }


        // """サンプリングビット数
        // """
        public int Sample_bits{
            get=>this._sample_bits;
        }
        // @property
        // def frame_rate(self)->int:
        //     """サンプリングのフレームレート
        //     """
        //     return self._frame_rate
        public uint Frame_rate{
            get=>this._frame_rate;
        }
        // @property
        // def timelen(self):
        //     """データの記録時間
        //     """
        //     return len(self._frames)/(self._sample_bits//8*self._frame_rate)
        public float Timelen{
            get{
                return this._frames.Length/(this._sample_bits/8*this._frame_rate);
            }
        }




        // @property
        // def byteslen(self)->int:
        //     """Waveファイルのデータサイズ
        //     Waveファイルのdataセクションに格納されるサイズです。
        //     """
        //     return len(self._frames)
        public int Byteslen{
            get{
                return this._frames.Length;
            }
        }
        // @property
        // def data(self)->bytes:
        //     """ 振幅データ
        //     """
        //     return self._frames
        public byte[] Data{
            get=>this._frames;
        }
        

        // def dataAsFloat(self)->Sequence[float]:

        //     data=self._frames
        //     bits=self._sample_bits
        //     if bits==8:
        //         # a=[struct.unpack_from("B",data,i)[0]/256-0.5 for i in range(len(data))]
        //         # b=list(np.frombuffer(data, dtype="uint8")/256-0.5)
        //         # print(a==b)
        //         # return list(np.frombuffer(data, dtype="uint8")/256-0.5)
        //         return [struct.unpack_from("B",data,i)[0]/255-0.5 for i in range(len(data))]
        //     elif bits==16:
        //         assert(len(data)%2==0)
        //         r=(2**16-1)//2 #Daisukeパッチ
        //         # a=[struct.unpack_from("<h",data,i*2)[0]/r for i in range(len(data)//2)]
        //         # b=list(np.frombuffer(data, dtype="int16")/r)
        //         # print(a==b)
        //         # return list(np.frombuffer(data, dtype="int16")/r)
        //         return [struct.unpack_from("<h",data,i*2)[0]/r for i in range(len(data)//2)]
        //     raise ValueError()
        public IList<double> DataAsFloat()
        {
            var data=this._frames;
            var bits=this._sample_bits;
            var ret = new List<double>();
            if(bits==8){
                foreach(var i in data){
                    ret.Add(i/255-0.5);
                }
                return ret;
            }else if(bits==16){ 
                Debug.Assert(data.Length%2==0);
                double r = (Math.Pow(2, 16) - 1) / 2;//(2 * *16 - 1)//2 #Daisukeパッチ
                int c=0;
                UInt16 b=0;
                foreach(var i in data){
                    b=(UInt16)(b>>8|(i<< 8));
                    c=(c+1)%2;
                    if(c==0){
                        if ((0x8000 & b) == 0){
                            ret.Add(b / r);
                        }
                        else
                        {
                            ret.Add((((Int32)b - 0x0000ffff) - 1)/r);
                        }
                        b=0;
                    }
                }
                return ret;
            }
            throw new ArgumentException();
        }
    }




}
