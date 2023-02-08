using jp.nyatla.kokolink.compatibility;
using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.utils.wavefile.riffio;
using jp.nyatla.kokolink.utils.recoverable;
using System.Diagnostics;
using static jp.nyatla.kokolink.utils.wavefile.PcmData;

namespace jp.nyatla.kokolink.utils.wavefile
{

    class SReader : IBinaryReader
    {
        Stream _src;
        public SReader(Stream src)
        {
            this._src = src;
        }
        public byte[] ReadBytes(int size)
        {
            var ret = new byte[size];
            for (var i = 0; i < size; i++)
            {
                var w = this._src.ReadByte();
                if (w < 0)
                {
                    throw new EndOfStreamException();
                }
                ret[i] = (byte)w;
            }
            return ret;
        }
    }
    class SWriter : IBinaryWriter
    {
        Stream _src;
        public SWriter(Stream src)
        {
            this._src = src;
        }
        public int WriteBytes(List<byte> buf)
        {
            return this.WriteBytes(buf.ToArray());
        }
        public int WriteBytes(byte[] buf)
        {
            this._src.Write(buf);
            return buf.Length;
        }
    }



    public class PcmData
    {
        private readonly WaveFile _wavfile;
        public static PcmData Load(IBinaryReader fp)
        {
            return new PcmData(fp);
        }
        public static PcmData Load(Stream src)
        {


            var io = new SReader(src);
            return new PcmData(io);
        }


        public static void Dump(PcmData src, IBinaryWriter dest)
        {
            src._wavfile.Dump(dest);
        }
        public static void Dump(PcmData src, Stream dest)
        {
            var io = new SWriter(dest);
            PcmData.Dump(src, io);
        }


        public PcmData(IBinaryReader fp)
        {
            this._wavfile = new WaveFile(fp);
            if (this._wavfile.Fmt == null)
            {
                throw new Exception();
            }
            if (this._wavfile.Data == null)
            {
                throw new Exception();
            }
        }

        public PcmData(byte[] src, int sample_bits, int frame_rate, List<Chunk>? chunks)
        {
            this._wavfile = new WaveFile(frame_rate, sample_bits / 8, 1, src, chunks);
            Debug.Assert(this._wavfile.Fmt != null);
            Debug.Assert(this._wavfile.Data != null);
        }
        public PcmData(byte[] src, int sample_bits, int frame_rate) : this(src, sample_bits, frame_rate, null) { }
        public PcmData(IPyIterator<double> src, int sample_bits, int frame_rate) : this(src, sample_bits, frame_rate, null) { }
        public PcmData(IPyIterator<double> src, int sample_bits, int frame_rate, List<Chunk>? chunks) : this(Float2bytes(src, sample_bits), sample_bits, frame_rate, chunks) { }
        public PcmData(IList<double> src, int sample_bits, int frame_rate) : this(new PyIterator<double>(src), sample_bits, frame_rate, null) { }


        // """サンプリングビット数
        // """

        public int SampleBits
        {
            get
            {
                Debug.Assert(this._wavfile.Fmt != null);
                return this._wavfile.Fmt.Samplewidth;

            }
        }
        // @property
        // def frame_rate(self)->int:
        //     """サンプリングのフレームレート
        //     """
        //     return self._frame_rate
        public int Framerate
        {
            get
            {
                Debug.Assert(this._wavfile.Fmt != null);
                return this._wavfile.Fmt.Framerate;

            }
        }
        public List<byte> WavData
        {
            get
            {
                Debug.Assert(this._wavfile.Data != null);
                return this._wavfile.Data.Data;

            }
        }




        public int Byteslen
        {
            get
            {
                Debug.Assert(this._wavfile.Data != null);
                return this._wavfile.Data.Size;
            }
        }


        public class DoubleBytesCovertor
        {
            /**
             * [RecovableStopIteratonSAFE]
             */
            public class Byte16ToDouble : IPyIterator<double>
            {
                private IPyIterator<byte> _src;
                private int _nod;
                private byte[] _buf;
                readonly static double R = (Math.Pow(2, 16) - 1) / 2;//(2 * *16 - 1)//2 #Daisukeパッチ
                public Byte16ToDouble(IPyIterator<byte> src)
                {
                    this._src = src;
                    this._nod = 0;
                    this._buf = new byte[2];
                }
                public double Next()
                {
                    //2個ためる。
                    try
                    {
                        while (this._nod < 2)
                        {
                            this._buf[this._nod] = this._src.Next();
                            this._nod++;
                        }
                    }
                    catch (RecoverableStopIteration)
                    {
                        throw;
                    }
                    catch (PyStopIteration)
                    {
                        //端数ビットのトラップ
                        if (this._nod != 0)
                        {
                            throw new InvalidDataException("Fractional bytes detected.");
                        }
                        throw;
                    }
                    this._nod = 0;
                    //変換
                    byte[] buf = this._buf;
                    var b = (UInt16)(buf[0] | ((UInt16)buf[1] << 8));
                    double ret;
                    if ((0x8000 & b) == 0)
                    {
                        ret = b / R;
                    }
                    else
                    {
                        ret = (((Int32)b - 0x0000ffff) - 1) / R;
                        ret = ret > 1 ? 1 : (ret < -1) ? -1 : 0;
                    }
                    return ret;
                }

            }
            /**
             * [RecovableStopIteratonSAFE]
             */
            public class Byte8ToDouble : IPyIterator<double>
            {
                private IPyIterator<byte> _src;
                public Byte8ToDouble(IPyIterator<byte> src)
                {
                    this._src = src;
                }
                public double Next()
                {
                    return (double)this._src.Next() / 255 - 0.5;
                }
            }
            public class DoubleToByte8 : IPyIterator<byte>
            {
                private IPyIterator<double> _src;
                public DoubleToByte8(IPyIterator<double> src)
                {
                    this._src = src;
                }
                public byte Next()
                {
                    return (byte)(this._src.Next() * 127 + 128);
                }
            }

            public class DoubleToByte16 : IPyIterator<byte>
            {
                readonly static double R = (Math.Pow(2, 16) - 1) / 2;//(2 * *16 - 1)//2 #Daisukeパッチ

                private IPyIterator<double> _src;
                private int _c;
                private byte _ret2;
                public DoubleToByte16(IPyIterator<double> src)
                {
                    this._src = src;
                    this._c = 0;
                    this._ret2 = 0;
                }
                public byte Next()
                {
                    if (this._c > 0)
                    {
                        this._c = 0;
                        return this._ret2;
                    }
                    var d = this._src.Next();
                    var f = d * R;
                    if (f >= 0)
                    {
                        UInt16 v = (UInt16)(Math.Min((double)Int16.MaxValue, f));
                        this._ret2 = (byte)((v >> 8) & 0xff);
                        this._c = 1;
                        return (byte)(v & 0xff);
                    }
                    else
                    {
                        UInt16 v = (UInt16)(0xffff + (UInt16)(Math.Max(f, (double)Int16.MinValue)) + 1);
                        this._ret2 = ((byte)((v >> 8) & 0xff));
                        this._c = 1;
                        return (byte)(v & 0xff);
                    }
                }

            }

            static public IPyIterator<double> ToDouble(IPyIterator<byte> src, int sample_rate)
            {
                switch (sample_rate)
                {
                    case 8: return new Byte8ToDouble(src);
                    case 16: return new Byte16ToDouble(src);
                    default: throw new NotImplementedException("Invalid bits");
                }
            }
            static public IPyIterator<byte> ToBytes(IPyIterator<double> src, int sample_rate)
            {
                switch (sample_rate)
                {
                    case 8: return new DoubleToByte8(src);
                    case 16: return new DoubleToByte16(src);
                    default: throw new NotImplementedException("Invalid bits");
                }
            }
        }


        public List<double> DataAsFloat()
        {
            Debug.Assert(this._wavfile.Data!= null);
            var s = new PyIterator<byte>(this._wavfile.Data.Data);
            return Functions.ToList<double>(DoubleBytesCovertor.ToDouble(s,this.Framerate));
        }



        static private byte[] Float2bytes(IPyIterator<Double> fdata, int bits)
        {
            return Functions.ToArray<byte>(DoubleBytesCovertor.ToBytes(fdata, bits));
        }


    }
}