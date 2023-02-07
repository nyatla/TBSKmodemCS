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
    public class TbskModulator_impl
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


        readonly private TraitTone _tone;
        readonly private Preamble _preamble;
        readonly private TraitBlockEncoder _enc;
        public TbskModulator_impl(TraitTone tone, Preamble? preamble = null)
        {
            // """
            //     Args:
            //         tone
            //             特徴シンボルのパターンです。
            // """
            this._preamble = (preamble != null) ? preamble : new CoffPreamble(tone);
            this._enc = new TraitBlockEncoder(tone);
            this._tone = tone;
        }
        public ISequentialEnumerable<double> ModulateAsBit(IPyIterator<int> src)
        {
            var ave_window_shift = Math.Max((int)(this._tone.Count * 0.1), 2) / 2; //#検出用の平均フィルタは0.1*len(tone)//2だけずれてる。ここを直したらTraitBlockDecoderも直せ
            return SequentialEnumerable<double>.CreateInstance(new IterChain<double>(
                this._preamble.GetPreamble(),
                this._enc.SetInput(new DiffBitEncoder(0, new BitStream(src, 1))),
                new Repeater<double>(0, ave_window_shift)    //#demodulatorが平均値で補正してる関係で遅延分を足してる。
            ));
        }
    }






    public class TbskDemodulator_impl
    {

        public class AsyncDemodulateX<T> : AsyncMethod<ISequentialEnumerable<T>?>
        {


            public AsyncDemodulateX(TbskDemodulator_impl parent, IPyIterator<double> src, Func<TraitBlockDecoder,ISequentialEnumerable<T>> resultbuilder) : base()
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
            private CoffPreamble.WaitForSymbolAS? _wsrex;
            private int? _peak_offset;
            readonly private IRoStream<double> _stream;
            readonly private TbskDemodulator_impl _parent;
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
                    catch (RecoverableException<CoffPreamble.WaitForSymbolAS, int?> rexp)
                    {
                        this._wsrex = rexp.Detach();
                        this._co_step = 1;
                        return false;
                    }
                }
                if (this._co_step == 1)
                {
                    if (!this._wsrex!.Run())
                    {
                        return false;
                    }
                    else
                    {
                        this._peak_offset = this._wsrex!.Result;
                        this._wsrex = null;
                        this._co_step = 2;
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
        protected bool _asmethod_lock;

        public TbskDemodulator_impl(TraitTone tone, Preamble? preamble = null)
        {
            this._tone = tone;
            this._pa_detector = preamble != null ? preamble : new CoffPreamble(tone, threshold: 1.0);
            this._asmethod_lock = false;
        }
        public class DemodulateAsBitAS : AsyncDemodulateX<int>
        {
            public DemodulateAsBitAS(TbskDemodulator_impl parent, IPyIterator<double> src) :
                base(parent, src, (TraitBlockDecoder src) => SequentialEnumerable<int>.CreateInstance(src))
            { }
        }

        //""" TBSK信号からビットを復元します。
        //    関数は信号を検知する迄制御を返しません。信号を検知せずにストリームが終了した場合はNoneを返します。
        //"""
        public ISequentialEnumerable<int>? DemodulateAsBit(IPyIterator<double> src)
        {
            Debug.Assert(!this._asmethod_lock);
            DemodulateAsBitAS asmethod = new DemodulateAsBitAS(this, src);
            if (asmethod.Run())
            {
                return asmethod.Result;
            }
            else
            {
                this._asmethod_lock = true;// #解放はAsyncDemodulateXのcloseで
                throw new RecoverableException<DemodulateAsBitAS, ISequentialEnumerable<int>?>(asmethod);
            }
        }



    }


}
