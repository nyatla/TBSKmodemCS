using System.Diagnostics;
using System.Text;
using System;
using jp.nyatla.kokolink.interfaces;
using jp.nyatla.kokolink.streams.rostreams;
using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.utils;
using jp.nyatla.kokolink.utils.recoverable;
using System.Runtime.InteropServices;

namespace jp.nyatla.kokolink.filter
{


    // """ nBit intイテレータから1バイト単位のbytesを返すフィルタです。
    // """
    class Bits2StrFilter : BasicRoStream<char>, IFilter<Bits2StrFilter, IRoStream<int>, char>
    {
        private int _pos;
        private int _input_bits;
        readonly private string _encoding;
        private IList<byte> _savedata;
        private IPyIterator<int>? _iter;

        public Bits2StrFilter(int input_bits = 1, string encoding = "utf-8"):base()
        {
            this._input_bits = input_bits;
            this._encoding = encoding;
            this._savedata = new List<byte>();
        }
        public Bits2StrFilter SetInput(IRoStream<int> src)
        {
            this._pos = 0;
            this._iter = src == null ? null : new BitsWidthConvertIterator(src!, this._input_bits, 8);
            return this;
        }
        override public char Next()
        {
            if (this._iter == null)
            {
                throw new PyStopIteration();
            }
            while (true)
            {
                int d;
                try
                {
                    d = this._iter.Next();
                }
                catch (RecoverableStopIteration e)
                {
                    throw e;
                }
                this._savedata.Add((byte)d);
                try
                {
                    var r = System.Text.Encoding.GetEncoding(this._encoding,new EncoderExceptionFallback(),new DecoderExceptionFallback()).GetChars(this._savedata.ToArray());
                    this._savedata.Clear();
                    return r[0];
                }
                catch (DecoderFallbackException)
                {
                    continue;
                }
            }
        }
        override public int Pos
        {
            get => this._pos;
        }
    }

}