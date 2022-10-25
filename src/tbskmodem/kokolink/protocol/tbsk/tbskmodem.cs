using System.Globalization;
using System.Collections;
using System.Diagnostics;
using jp.nyatla.kokolink.interfaces;
using jp.nyatla.kokolink.utils;
using jp.nyatla.kokolink.utils.recoverable;
using jp.nyatla.kokolink.streams;
using jp.nyatla.kokolink.filter;
using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.protocol.tbsk.preamble;
using jp.nyatla.kokolink.streams.rostreams;
using jp.nyatla.kokolink.protocol.tbsk.toneblock;
using jp.nyatla.kokolink.compatibility;
using jp.nyatla.kokolink.protocol.tbsk.traitblockcoder;

namespace jp.nyatla.kokolink.protocol.tbsk.tbaskmodem
{










    // """ TBSKの変調クラスです。
    //     プリアンブルを前置した後にビットパターンを置きます。
    // """
    public class TbskModulator
    {
        // """ ビット配列を差動ビットに変換します。
        // """
        private class DiffBitEncoder : BasicRoStream<int>, IBitStream
        {
            private int _last_bit;
            readonly private IRoStream<int> _src;
            private bool _is_eos;
            private int _pos;
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
            override public int Pos
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


        readonly private TraitTone _tone;
        readonly private Preamble _preamble;
        readonly private TraitBlockEncoder _enc;
        public TbskModulator(TraitTone tone, Preamble? preamble = null)
        {
            // """
            //     Args:
            //         tone
            //             特徴シンボルのパターンです。
            // """
            this._tone = tone;
            this._preamble = (preamble != null) ? preamble : new CoffPreamble(this._tone);
            this._enc = new TraitBlockEncoder(tone);
        }
        public ISequentialEnumerable<double> ModulateAsBit(IPyIterator<int> src)
        {
            var ave_window_shift = Math.Max((int)(this._tone.Count * 0.1), 2) / 2; //#検出用の平均フィルタは0.1*len(tone)//2だけずれてる。ここを直したらTraitBlockDecoderも直せ
            return Functions.ToEnumerable<double>(new IterChain<double>(
                this._preamble.GetPreamble(),
                this._enc.SetInput(new DiffBitEncoder(0, new BitStream(src, 1))),
                new Repeater<double>(0, ave_window_shift)    //#demodulatorが平均値で補正してる関係で遅延分を足してる。
            ));
        }
        public IEnumerable<double> ModulateAsBit(IEnumerable<int> src)
        {
            //既にIPyIteratorを持っていたらそのまま使う。
            return this.ModulateAsBit(Functions.ToPyIter<int>(src));
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






    public class TbskDemodulator
    {
        // """ nBit intイテレータから1バイト単位のhex stringを返すフィルタです。
        // """
        private class Bits2HexStrFilter : BasicRoStream<string>, IFilter<Bits2HexStrFilter, IRoStream<int>, string>
        {
            readonly private BitsWidthFilter _src;
            public Bits2HexStrFilter(int input_bits = 1)
            {
                this._src = new BitsWidthFilter(input_bits: input_bits, output_bits: 8);
            }

            override public string Next()
            {
                while (true)
                {
                    int d;
                    try
                    {
                        d = this._src.Next();
                    }
                    catch (RecoverableStopIteration)
                    {
                        throw;
                    }
                    Debug.Assert(0 < d && d < 256);
                    return Convert.ToString(d, 16);

                }
            }
            public Bits2HexStrFilter SetInput(IRoStream<int> src)
            {
                this._src.SetInput(src);
                return this;
            }
            override public int Pos
            {
                get => this._src.Pos;
            }
        }




        private class AsyncDemodulateX<T> : AsyncMethod<ISequentialEnumerable<T>?>
        {


            public AsyncDemodulateX(TbskDemodulator parent, IPyIterator<double> src, Func<TraitBlockDecoder,ISequentialEnumerable<T>> resultbuilder) : base()
            {
                this._tone_ticks = parent._tone.Count;
                this._result = null;
                this._stream = (src is IRoStream<double> stream) ? stream : new RoStream<double>(src);
                this._peak_offset = null;
                this._parent = parent;
                this._wsrex = null;
                this._co_step = 0;
                this._closed = false;
                this._resultbuilder = resultbuilder;

            }
            override public ISequentialEnumerable<T>? Result
            {
                get
                {
                    Debug.Assert(this._co_step >= 4);
                    return this._result;

                }
            }


            override public void Close()
            {
                if (!this._closed)
                {
                    try
                    {
                        if (this._wsrex != null)
                        {
                            this._wsrex.Close();
                        }
                    }
                    finally
                    {
                        this._wsrex = null;
                        this._parent._asmethod_lock = false;
                        this._closed = true;
                    }

                }

            }
            readonly private Func<TraitBlockDecoder, ISequentialEnumerable<T>> _resultbuilder;
            private bool _closed;
            readonly private int _tone_ticks;
            private AsyncMethodRecoverException<AsyncMethod<int?>, int?>? _wsrex;
            private int? _peak_offset;
            readonly private IRoStream<double> _stream;
            readonly private TbskDemodulator _parent;
            private int _co_step;
            private ISequentialEnumerable<T>? _result;
            override public bool Run()
            {
                //# print("run",self._co_step)
                Debug.Assert(!this._closed);

                if (this._co_step == 0)
                {
                    try
                    {
                        this._peak_offset = this._parent._pa_detector.WaitForSymbol(this._stream); //#現在地から同期ポイントまでの相対位置
                        Debug.Assert(this._wsrex == null);
                        this._co_step = 2;
                    }
                    catch (AsyncMethodRecoverException<AsyncMethod<int?>, int?> rexp)
                    {
                        this._wsrex = rexp;
                        this._co_step = 1;
                        return false;
                    }
                }
                if (this._co_step == 1)
                {
                    try
                    {
                        this._peak_offset = this._wsrex!.Recover();
                        this._wsrex = null;
                        this._co_step = 2;
                    }
                    catch (AsyncMethodRecoverException<AsyncMethod<int?>, int?> rexp)
                    {
                        this._wsrex = rexp;
                        return false;
                    }
                }
                if (this._co_step == 2)
                {
                    if (this._peak_offset == null)
                    {
                        this._result = null;
                        this.Close();
                        this._co_step = 4;
                        return true;
                    }
                    //# print(self._peak_offset)
                    this._co_step = 3;
                }
                if (this._co_step == 3)
                {
                    try
                    {
                        Debug.Assert(this._peak_offset != null);
                        //# print(">>",self._peak_offset+self._stream.pos)
                        this._stream.Seek(this._tone_ticks + (int)this._peak_offset);// #同期シンボル末尾に移動
                        //# print(">>",stream.pos)
                        var tbd = new TraitBlockDecoder(this._tone_ticks);
                        this._result = this._resultbuilder(tbd.SetInput(this._stream));
                        this.Close();
                        this._co_step = 4;
                        return true;

                    }
                    catch (RecoverableStopIteration)
                    {
                        return false;

                    }
                    catch (PyStopIteration)
                    {
                        this._result = null;
                        this.Close();
                        this._co_step = 4;
                        return true;

                    }
                }
                throw new Exception();

            }
        }

        readonly private TraitTone _tone;
        readonly private Preamble _pa_detector;
        private bool _asmethod_lock;

        public TbskDemodulator(TraitTone tone, Preamble? preamble = null)
        {
            this._tone = tone;
            this._pa_detector = preamble != null ? preamble : new CoffPreamble(tone, threshold: 1.0);
            this._asmethod_lock = false;
        }




        //""" TBSK信号からビットを復元します。
        //    関数は信号を検知する迄制御を返しません。信号を検知せずにストリームが終了した場合はNoneを返します。
        //"""
        public ISequentialEnumerable<int>? DemodulateAsBit(IEnumerable<double> src)
        {
            Debug.Assert(!this._asmethod_lock);
            static ISequentialEnumerable<int> builder(TraitBlockDecoder src) => Functions.ToEnumerable<int>(src);
            AsyncMethod<ISequentialEnumerable<int>?> asmethod = new AsyncDemodulateX<int>(this, new PyIterator<double>(src), builder);
            if (asmethod.Run())
            {
                return asmethod.Result;
            }
            else
            {
                this._asmethod_lock = true;// #解放はAsyncDemodulateXのcloseで
                throw new AsyncMethodRecoverException<AsyncMethod<ISequentialEnumerable<int>?>, ISequentialEnumerable<int>?>(asmethod);
            }
        }
        //    """ TBSK信号からnビットのint値配列を復元します。
        //        関数は信号を検知する迄制御を返しません。信号を検知せずにストリームが終了した場合はNoneを返します。
        //    """
        public ISequentialEnumerable<int>? DemodulateAsInt(IEnumerable<double> src, int bitwidth = 8)
        {
            Debug.Assert(!this._asmethod_lock);
            ISequentialEnumerable<int> builder(TraitBlockDecoder src) => Functions.ToEnumerable<int>(new BitsWidthFilter(1, bitwidth).SetInput(src));
            AsyncMethod<ISequentialEnumerable<int>?> asmethod = new AsyncDemodulateX<int>(this, new PyIterator<double>(src), builder);
            if (asmethod.Run())
            {
                return asmethod.Result;
            }
            else
            {
                this._asmethod_lock = true;// #解放はAsyncDemodulateXのcloseで
                throw new AsyncMethodRecoverException<AsyncMethod<ISequentialEnumerable<int>?>, ISequentialEnumerable<int>?>(asmethod);
            }
        }
        //    """ TBSK信号からバイト単位でbytesを返します。
        //        途中でストリームが終端した場合、既に読みだしたビットは破棄されます。
        //        関数は信号を検知する迄制御を返しません。信号を検知せずにストリームが終了した場合はNoneを返します。   
        //    """
        public ISequentialEnumerable<byte>? DemodulateAsBytes(IEnumerable<double> src)
        {
            Debug.Assert(!this._asmethod_lock);
            static ISequentialEnumerable<byte> builder(TraitBlockDecoder src) => Functions.ToEnumerable<byte>(new Bits2BytesFilter(input_bits: 1).SetInput(src));
            AsyncMethod<ISequentialEnumerable<byte>?> asmethod = new AsyncDemodulateX<byte>(this, new PyIterator<double>(src), builder);
            if (asmethod.Run())
            {
                return asmethod.Result;
            }
            else
            {
                this._asmethod_lock = true;// #解放はAsyncDemodulateXのcloseで
                throw new AsyncMethodRecoverException<AsyncMethod<ISequentialEnumerable<byte>?>, ISequentialEnumerable<byte>?>(asmethod);
            }
        }

        //    """ TBSK信号からsize文字単位でstrを返します。
        //        途中でストリームが終端した場合、既に読みだしたビットは破棄されます。
        //        関数は信号を検知する迄制御を返しません。信号を検知せずにストリームが終了した場合はNoneを返します。
        //    """
        public ISequentialEnumerable<char>? DemodulateAsStr(IEnumerable<double> src)
        {
            Debug.Assert(!this._asmethod_lock);
            static ISequentialEnumerable<char> builder(TraitBlockDecoder src) => Functions.ToEnumerable<char>(new Bits2StrFilter(input_bits: 1).SetInput(src));
            AsyncMethod<ISequentialEnumerable<char>?> asmethod = new AsyncDemodulateX<char>(this, new PyIterator<double>(src), builder);
            if (asmethod.Run())
            {
                return asmethod.Result;
            }
            else
            {
                this._asmethod_lock = true;// #解放はAsyncDemodulateXのcloseで
                throw new AsyncMethodRecoverException<AsyncMethod<ISequentialEnumerable<char>?>, ISequentialEnumerable<char>?>(asmethod);
            }
        }

        public ISequentialEnumerable<string>? DemodulateAsHexStr(IEnumerable<double> src)
        {
            Debug.Assert(!this._asmethod_lock);
            static ISequentialEnumerable<string> builder(TraitBlockDecoder src) => Functions.ToEnumerable<string>(new Bits2HexStrFilter(input_bits: 1).SetInput(src));
            AsyncMethod<ISequentialEnumerable<string>?> asmethod = new AsyncDemodulateX<string>(this, new PyIterator<double>(src), builder);
            if (asmethod.Run())
            {
                return asmethod.Result;
            }
            else
            {
                this._asmethod_lock = true;// #解放はAsyncDemodulateXのcloseで
                throw new AsyncMethodRecoverException<AsyncMethod<ISequentialEnumerable<string>?>, ISequentialEnumerable<string>?>(asmethod);
            }
        }

    }


}
