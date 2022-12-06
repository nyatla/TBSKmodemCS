// from typing import TypeVar
// from ..types import Iterator
// from .RingBuffer import RingBuffer
// from .recoverable import RecoverableIterator, RecoverableStopIteration
using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.utils.recoverable;
// T=TypeVar("T")

namespace jp.nyatla.kokolink.utils
{
    // """ 末尾からticksまでの平均値を連続して返却します。
    //     このイテレータはRecoverableStopInterationを利用できます。
    // """
    class AverageInterator : SumIterator
    {
        private int _length;
        public AverageInterator(IPyIterator<double> src, int ticks) : base(src, ticks)
        {
            this._length = ticks;
        }
        override public double Next()
        {
            double r;
            try
            {
                r = base.Next();
            }
            catch (RecoverableStopIteration e)
            {
                throw e;
            }
            return r / this._length;
        }
    }


}




