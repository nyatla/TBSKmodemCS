using System.Collections;
using System.Diagnostics;
using jp.nyatla.kokolink.interfaces;
using jp.nyatla.kokolink.streams;
using jp.nyatla.kokolink.filter;
using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.protocol.tbsk.preamble;
using jp.nyatla.kokolink.streams.rostreams;
using jp.nyatla.kokolink.protocol.tbsk.toneblock;
using jp.nyatla.kokolink.compatibility;
using jp.nyatla.kokolink.protocol.tbsk.tbaskmodem;

namespace jp.nyatla.tbaskmodem
{

    // """ TBSKの変調クラスです。
    //     プリアンブルを前置した後にビットパターンを置きます。
    // """
    public class TbskModulator : TbskModulator_impl
    {
        // """ ビット配列を差動ビットに変換します。
        // """
        private class DiffBitEncoder : BasicRoStream<int>, IBitStream
        {
            private int _last_bit;
            readonly private IRoStream<int> _src;
            private bool _is_eos;
            private Int64 _pos;
            public DiffBitEncoder(int firstbit, IRoStream<int> src)
            {

                this._last_bit = firstbit;
                this._src = src;
                this._is_eos = false;
                this._pos = 0;
            }
            override public int Next()
            {
                if (this._is_eos)
                {
                    throw new PyStopIteration();
                }
                if (this._pos == 0)
                {
                    this._pos = this._pos + 1;
                    return this._last_bit; //#1st基準シンボル
                }
                int n;
                try
                {
                    n = this._src.Next();
                }
                catch (PyStopIteration e)
                {
                    this._is_eos = true;
                    throw new PyStopIteration(e);
                }
                if (n == 1)
                {
                    //pass
                }
                else
                {
                    this._last_bit = (this._last_bit + 1) % 2;
                }
                return this._last_bit;
            }
            // @property
            override public Int64 Pos
            {
                get => this._pos;
            }
        }
        private class EnumerableWrapper<T> : IEnumerable<T>
        {
            class EnumeratorWrapper : IEnumerator<T>
            {
                private T? _current;
                readonly private IPyIterator<T> _src;
                public EnumeratorWrapper(IPyIterator<T> src)
                {
                    this._src = src;
                }
                T IEnumerator<T>.Current => this._current!;

                object IEnumerator.Current => this._current!;

                void IDisposable.Dispose()
                {
                    //throw new NotImplementedException();
                }

                bool IEnumerator.MoveNext()
                {
                    try
                    {
                        this._current = this._src.Next();
                        return true;
                    }
                    catch (PyStopIteration)
                    {
                        return false;
                    }
                }

                void IEnumerator.Reset()
                {
                    throw new NotImplementedException();
                }
            }
            readonly private IEnumerator<T> _enumerator;

            public EnumerableWrapper(IPyIterator<T> src)
            {
                this._enumerator = new EnumeratorWrapper(src);
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator() => this._enumerator;

            IEnumerator IEnumerable.GetEnumerator() => this._enumerator;
        }


        public TbskModulator(TraitTone tone, Preamble? preamble = null):base(tone,preamble)
        {
        }

        public IEnumerable<double> ModulateAsBit(IEnumerable<int> src)
        {
            //既にIPyIteratorを持っていたらそのまま使う。
            return base.ModulateAsBit(Functions.ToPyIter<int>(src));
        }



        public ISequentialEnumerable<double> ModulateAsHexStr(string src)
        {
            // """ hex stringを変調します。
            //     hex stringは(0x)?[0-9a-fA-F]{2}形式の文字列です。
            //     hex stringはbytesに変換されて送信されます。
            // """
            Debug.Assert(src.Length % 2 == 0);
            if (src.Substring(0, 2) == "0x")
            {
                src = src.Substring(2, src.Length - 2);
            }
            var d = new List<byte>();
            for (var i = 0; i < src.Length / 2; i++)
            {
                d.Add(Convert.ToByte(src[i * 2] + src[i * 2 + 1]));
            }
            return this.Modulate(d);
        }



        public ISequentialEnumerable<double> Modulate(IEnumerable<int> src, int bitwidth = 8)
        {
            //既にIPyIteratorを持っていたらそのまま使う。
            return this.ModulateAsBit(
                new BitsWidthFilter(bitwidth).SetInput(new RoStream<int>(Functions.ToPyIter<int>(src)))
                );
        }
        public ISequentialEnumerable<double> Modulate(IEnumerable<byte> src)
        {
            //既にIPyIteratorを持っていたらそのまま使う。
            return this.ModulateAsBit(
                new BitsWidthFilter(8).SetInput(new ByteStream(Functions.ToPyIter<byte>(src)))
                );
        }
        public ISequentialEnumerable<double> Modulate(string src, string encoding = "utf-8")
        {
            return this.ModulateAsBit(
                new BitsWidthFilter(8).SetInput(new ByteStream(src, encoding: encoding))
                );
        }
    }
}
